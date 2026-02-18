using UnityEngine;
using Mirror;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Client-side script for shop UI interactions.
/// Handles purchase requests and displays currency.
/// </summary>
public class OverworldShopClient : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private Button healthRefillButton;
    [SerializeField] private TextMeshProUGUI healthRefillCostText;

    private NetworkIdentity localPlayerIdentity;

    void Start()
    {
        // Find local player when it spawns
        InvokeRepeating(nameof(FindLocalPlayer), 0.5f, 1f);

        if (healthRefillButton != null)
        {
            healthRefillButton.onClick.AddListener(BuyHealthRefill);
        }
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

    void Update()
    {
        UpdateCurrencyDisplay();
        UpdateButtonStates();
    }

    void UpdateCurrencyDisplay()
    {
        if (coinsText == null) return;

        int coins = GetLocalPlayerCoins();
        coinsText.text = $"Coins: {coins}";
    }

    void UpdateButtonStates()
    {
        if (healthRefillButton == null || ShopManager.Instance == null) return;

        int coins = GetLocalPlayerCoins();
        int cost = ShopManager.Instance.healthRefillCost;

        healthRefillButton.interactable = coins >= cost;

        if (healthRefillCostText != null)
        {
            healthRefillCostText.text = $"Cost: {cost}";
        }
    }

    int GetLocalPlayerCoins()
    {
        if (NetworkClient.localPlayer == null)
            return 0;

        PlayerCurrency currency = NetworkClient.localPlayer.GetComponent<PlayerCurrency>();
        if (currency != null)
        {
            return currency.Coins;
        }

        return 0;
    }

    public void BuyHealthRefill()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.CmdPurchaseHealthRefill();
        }
    }
}
