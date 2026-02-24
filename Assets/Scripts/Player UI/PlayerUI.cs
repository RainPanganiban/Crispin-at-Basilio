using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System;
using TMPro;

public class PlayerUI : NetworkBehaviour
{
    [Header("HUD Elements")]
    [SerializeField] private Canvas playerUICanvas;
    [SerializeField] private GameObject crosshair;
    [SerializeField] private Slider healthBar;
    [SerializeField] private Slider staminaBar;
    [SerializeField] private TMP_Text coinCountText;

    private PlayerStatsManager statsManager;
    private PlayerCurrencyManager currencyManager;
    private bool isHooked = false;

    private void Awake()
    {
        if (playerUICanvas != null) playerUICanvas.enabled = false;
        if (crosshair != null) crosshair.SetActive(false);
    }

    public override void OnStartLocalPlayer()
    {
        if (playerUICanvas != null) playerUICanvas.enabled = true;
        if (crosshair != null) crosshair.SetActive(true);

        statsManager = GetComponent<PlayerStatsManager>();
        if (statsManager == null)
        {
            Debug.LogError("[PlayerUI] PlayerStatsManager missing!");
            return;
        }

        currencyManager = GetComponent<PlayerCurrencyManager>();
        if (currencyManager == null)
        {
            Debug.LogWarning("[PlayerUI] PlayerCurrencyManager missing — coin display disabled.");
        }

        statsManager.OnStatsReady += HookSliders;

        // In case stats are already ready
        HookSliders();
    }

    private void HookSliders()
    {
        if (isHooked || !isLocalPlayer) return;

        // Health
        if (healthBar != null)
        {
            healthBar.value = statsManager.health.GetPercent();
            statsManager.health.OnValueChanged += (current, max) =>
            {
                healthBar.value = current / max;
            };
        }

        // Stamina
        if (staminaBar != null)
        {
            staminaBar.value = statsManager.stamina.GetPercent();
            statsManager.stamina.OnValueChanged += (current, max) =>
            {
                staminaBar.value = current / max;
            };
        }

        // Coins
        if (coinCountText != null && currencyManager != null)
        {
            UpdateCoinDisplay(currencyManager.GetCoins());
            currencyManager.OnCoinsUpdated += UpdateCoinDisplay;
        }

        isHooked = true;
    }

    private void UpdateCoinDisplay(int amount)
    {
        if (coinCountText != null)
            coinCountText.text = $"Coins: {amount}";
    }

    public override void OnStopLocalPlayer()
    {
        if (playerUICanvas != null) playerUICanvas.enabled = false;
        if (crosshair != null) crosshair.SetActive(false);

        if (statsManager != null)
            statsManager.OnStatsReady -= HookSliders;

        if (currencyManager != null)
            currencyManager.OnCoinsUpdated -= UpdateCoinDisplay;
    }
}

