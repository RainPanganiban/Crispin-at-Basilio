using Mirror;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(Collider))]
public class ShopManager : NetworkBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("Shop Items")]
    public ShopItemData[] shopItems;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    // =========================================================
    // Trigger — Open/Close shop when player enters zone
    // =========================================================

    void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        NetworkIdentity identity = other.GetComponent<NetworkIdentity>();
        if (identity == null || identity.connectionToClient == null) return;

        TargetOpenShop(identity.connectionToClient);
    }

    void OnTriggerExit(Collider other)
    {
        if (!isServer) return;

        NetworkIdentity identity = other.GetComponent<NetworkIdentity>();
        if (identity == null || identity.connectionToClient == null) return;

        TargetCloseShop(identity.connectionToClient);
    }

    [TargetRpc]
    void TargetOpenShop(NetworkConnection target)
    {
        OverworldShopUI ui = FindFirstObjectByType<OverworldShopUI>();
        if (ui != null) ui.Open();
    }

    [TargetRpc]
    void TargetCloseShop(NetworkConnection target)
    {
        OverworldShopUI ui = FindFirstObjectByType<OverworldShopUI>();
        if (ui != null) ui.Close();
    }

    // =========================================================
    // Generic Purchase Command
    // =========================================================

    [Command(requiresAuthority = false)]
    public void CmdPurchaseItem(string itemId, NetworkConnectionToClient sender = null)
    {
        if (sender == null || sender.identity == null) return;

        ShopItemData item = GetItemById(itemId);
        if (item == null)
        {
            Debug.LogWarning($"[ShopManager] Unknown item id: {itemId}");
            return;
        }

        HandlePurchase(item, sender);
    }

    // =========================================================
    // Server Purchase Logic
    // =========================================================

    [Server]
    void HandlePurchase(ShopItemData item, NetworkConnectionToClient conn)
    {
        // Check upgrade level cap
        if (item.type == ShopItemType.Upgrade && item.maxLevel > 0)
        {
            int currentLevel = GetUpgradeLevel(conn, item.id);
            if (currentLevel >= item.maxLevel) return;
        }

        // Try to spend coins — check PlayerCurrencyManager first (gameplay coins)
        PlayerCurrencyManager currencyManager = conn.identity.GetComponent<PlayerCurrencyManager>();
        PlayerCurrency currency = conn.identity.GetComponent<PlayerCurrency>();

        bool spent = false;
        if (currencyManager != null)
        {
            spent = currencyManager.TrySpendCoins(item.cost);
        }
        else if (currency != null)
        {
            spent = currency.TrySpendCoins(item.cost);
        }

        if (!spent) return;

        // Keep both components and session data in sync
        int remainingCoins = currencyManager != null ? currencyManager.GetCoins() : (currency != null ? currency.Coins : 0);
        if (currency != null) currency.SetCoins(remainingCoins);
        if (currencyManager != null) currencyManager.ServerSetCoins(remainingCoins);
        SyncCoinsToSession(conn, remainingCoins);

        // Apply the item effect
        switch (item.type)
        {
            case ShopItemType.Upgrade:
                ApplyUpgrade(conn, item);
                break;
            case ShopItemType.Consumable:
                ApplyConsumable(conn, item);
                break;
        }
    }

    [Server]
    void ApplyUpgrade(NetworkConnectionToClient conn, ShopItemData item)
    {
        // Record in session data
        CustomNetworkManager manager = CustomNetworkManager.Instance;
        if (manager != null && manager.TryGetSessionData(conn, out CustomNetworkManager.PlayerSessionData data))
        {
            data.purchasedUpgrades.Add(item.id);
        }

        // Apply to player stats
        PlayerStatsManager stats = conn.identity.GetComponent<PlayerStatsManager>();
        if (stats != null)
        {
            stats.ServerApplyUpgrade(item.statToUpgrade, item.upgradeAmount);
        }
    }

    [Server]
    void ApplyConsumable(NetworkConnectionToClient conn, ShopItemData item)
    {
        if (item.isHealthRefill)
        {
            PlayerStatsManager stats = conn.identity.GetComponent<PlayerStatsManager>();
            if (stats != null)
            {
                stats.RestoreHealth(stats.health.maxValue);
            }
        }

        if (item.isRevive)
        {
            // Revive: find any dead player and revive them
            // For now, revive all dead players (could be refined to pick a target)
            foreach (NetworkConnectionToClient otherConn in NetworkServer.connections.Values)
            {
                if (otherConn == conn || otherConn.identity == null) continue;

                PlayerStatsManager otherStats = otherConn.identity.GetComponent<PlayerStatsManager>();
                if (otherStats != null && otherStats.IsDead)
                {
                    otherStats.ServerRevive();
                    break; // Revive one player per purchase
                }
            }
        }
    }

    // =========================================================
    // Helpers
    // =========================================================

    public ShopItemData GetItemById(string id)
    {
        if (shopItems == null) return null;
        return shopItems.FirstOrDefault(i => i.id == id);
    }

    public int GetUpgradeLevel(NetworkConnectionToClient conn, string itemId)
    {
        CustomNetworkManager manager = CustomNetworkManager.Instance;
        if (manager == null) return 0;

        if (!manager.TryGetSessionData(conn, out CustomNetworkManager.PlayerSessionData data))
            return 0;

        return data.purchasedUpgrades.Count(u => u == itemId);
    }

    [Server]
    void SyncCoinsToSession(NetworkConnectionToClient conn, int coins)
    {
        CustomNetworkManager manager = CustomNetworkManager.Instance;
        if (manager != null && manager.TryGetSessionData(conn, out CustomNetworkManager.PlayerSessionData data))
        {
            data.coins = coins;
        }
    }
}
