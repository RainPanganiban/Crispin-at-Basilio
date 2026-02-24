using UnityEngine;
using Mirror;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Client-side script for shop UI interactions.
/// Dynamically creates item buttons from ShopManager's item list.
/// </summary>
public class OverworldShopClient : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private Transform itemListParent;   // Parent container for shop item buttons
    [SerializeField] private GameObject shopItemPrefab;   // Prefab with ShopItemButton component

    private NetworkIdentity localPlayerIdentity;
    private List<ShopItemButton> spawnedButtons = new List<ShopItemButton>();
    private bool itemsPopulated = false;

    void Start()
    {
        InvokeRepeating(nameof(FindLocalPlayer), 0.5f, 1f);
    }

    void FindLocalPlayer()
    {
        if (localPlayerIdentity == null && NetworkClient.localPlayer != null)
        {
            localPlayerIdentity = NetworkClient.localPlayer.GetComponent<NetworkIdentity>();
            if (localPlayerIdentity != null)
            {
                CancelInvoke(nameof(FindLocalPlayer));
            }
        }
    }

    void OnEnable()
    {
        // Rebuild items when shop opens
        itemsPopulated = false;
    }

    void Update()
    {
        UpdateCurrencyDisplay();

        if (!itemsPopulated && ShopManager.Instance != null && ShopManager.Instance.shopItems != null)
        {
            PopulateItems();
            itemsPopulated = true;
        }

        UpdateButtonStates();
    }

    void UpdateCurrencyDisplay()
    {
        if (coinsText == null) return;
        coinsText.text = $"Coins: {GetLocalPlayerCoins()}";
    }

    void PopulateItems()
    {
        // Clear old buttons
        foreach (ShopItemButton btn in spawnedButtons)
        {
            if (btn != null) Destroy(btn.gameObject);
        }
        spawnedButtons.Clear();

        if (shopItemPrefab == null || itemListParent == null) return;

        int coins = GetLocalPlayerCoins();

        foreach (ShopItemData item in ShopManager.Instance.shopItems)
        {
            if (item == null) continue;

            GameObject go = Instantiate(shopItemPrefab, itemListParent);
            ShopItemButton btn = go.GetComponent<ShopItemButton>();

            if (btn != null)
            {
                int level = GetLocalUpgradeLevel(item.id);
                btn.Setup(item, level, coins, OnPurchaseClicked);
                spawnedButtons.Add(btn);
            }
        }
    }

    void UpdateButtonStates()
    {
        if (ShopManager.Instance == null) return;

        int coins = GetLocalPlayerCoins();

        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            if (spawnedButtons[i] == null) continue;

            ShopItemData item = ShopManager.Instance.shopItems[i];
            int level = GetLocalUpgradeLevel(item.id);
            spawnedButtons[i].UpdateState(level, coins);
        }
    }

    void OnPurchaseClicked(string itemId)
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.CmdPurchaseItem(itemId);
        }
    }

    int GetLocalPlayerCoins()
    {
        if (NetworkClient.localPlayer == null) return 0;

        // Check PlayerCurrencyManager first (gameplay coins)
        PlayerCurrencyManager currencyManager = NetworkClient.localPlayer.GetComponent<PlayerCurrencyManager>();
        if (currencyManager != null)
            return currencyManager.GetCoins();

        // Fallback to PlayerCurrency (overworld)
        PlayerCurrency currency = NetworkClient.localPlayer.GetComponent<PlayerCurrency>();
        return currency != null ? currency.Coins : 0;
    }

    int GetLocalUpgradeLevel(string itemId)
    {
        // Client can't directly read session data, so for now just check via coins affordability.
        // The server enforces the actual level cap. This is a best-effort estimate.
        // A more accurate approach would expose upgrade counts via SyncVar, but this keeps it simple.
        return 0;
    }
}
