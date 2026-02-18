using Mirror;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ShopManager : NetworkBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("Shop Settings")]
    public int healthRefillCost = 10;

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

    void OnTriggerEnter(Collider other)
    {
        if (!isServer)
            return;

        NetworkIdentity identity = other.GetComponent<NetworkIdentity>();
        if (identity == null || identity.connectionToClient == null)
            return;

        TargetOpenShop(identity.connectionToClient);
    }

    void OnTriggerExit(Collider other)
    {
        if (!isServer)
            return;

        NetworkIdentity identity = other.GetComponent<NetworkIdentity>();
        if (identity == null || identity.connectionToClient == null)
            return;

        TargetCloseShop(identity.connectionToClient);
    }

    [TargetRpc]
    void TargetOpenShop(NetworkConnection target)
    {
        OverworldShopUI ui = FindFirstObjectByType<OverworldShopUI>();
        if (ui != null)
        {
            ui.Open();
        }
    }

    [TargetRpc]
    void TargetCloseShop(NetworkConnection target)
    {
        OverworldShopUI ui = FindFirstObjectByType<OverworldShopUI>();
        if (ui != null)
        {
            ui.Close();
        }
    }

    // Called by client-side UI to request a purchase.
    [Command(requiresAuthority = false)]
    public void CmdPurchaseHealthRefill(NetworkConnectionToClient sender = null)
    {
        if (!isServer)
            return;

        HandleHealthRefillPurchase(sender);
    }

    [Server]
    void HandleHealthRefillPurchase(NetworkConnectionToClient conn)
    {
        if (conn == null || conn.identity == null)
            return;

        // Try to use PlayerCurrency component first (preferred method)
        PlayerCurrency currency = conn.identity.GetComponent<PlayerCurrency>();
        if (currency != null)
        {
            if (!currency.TrySpendCoins(healthRefillCost))
                return;

            // Update session data to match
            CustomNetworkManager manager = CustomNetworkManager.Instance;
            if (manager != null && manager.TryGetSessionData(conn, out CustomNetworkManager.PlayerSessionData data))
            {
                data.coins = currency.Coins;
            }
        }
        else
        {
            // Fallback to session data only
            CustomNetworkManager manager = CustomNetworkManager.Instance;
            if (manager == null)
                return;

            if (!manager.TryGetSessionData(conn, out CustomNetworkManager.PlayerSessionData data))
                return;

            if (data.coins < healthRefillCost)
                return;

            data.coins -= healthRefillCost;
        }

        PlayerStatsManager stats = conn.identity.GetComponent<PlayerStatsManager>();
        if (stats != null)
        {
            stats.RestoreHealth(stats.health.maxValue);
        }
    }
}

