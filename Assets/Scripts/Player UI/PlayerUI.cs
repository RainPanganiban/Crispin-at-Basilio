using UnityEngine;
using Mirror;
using UnityEngine.UI;
using System;

public class PlayerUI : NetworkBehaviour
{
    [Header("HUD Elements")]
    [SerializeField] private Canvas playerUICanvas;
    [SerializeField] private GameObject crosshair;
    [SerializeField] private Slider healthBar;
    [SerializeField] private Slider staminaBar;

    private PlayerStatsManager statsManager;

    private Action<float, float> healthUpdate;
    private Action<float, float> staminaUpdate;

    private bool isHooked = false;

    private void Awake()
    {
        // Always disable UI at start (prefab-safe)
        if (playerUICanvas != null)
            playerUICanvas.enabled = false;

        if (crosshair != null)
            crosshair.SetActive(false);
    }

    public override void OnStartLocalPlayer()
    {
        Debug.Log("[PlayerUI] Local player detected");

        // Enable UI ONLY for local player
        if (playerUICanvas != null)
            playerUICanvas.enabled = true;

        if (crosshair != null)
            crosshair.SetActive(true);

        statsManager = GetComponent<PlayerStatsManager>();

        if (statsManager == null)
        {
            Debug.LogError("[PlayerUI] PlayerStatsManager missing!");
            return;
        }

        // Delay hookup until stats are initialized
        statsManager.OnStatsReady += TryHookSliders;

        // In case stats are already ready (client case)
        TryHookSliders();
    }

    private void TryHookSliders()
    {
        if (isHooked || !isLocalPlayer) return;

        Debug.Log("[PlayerUI] Hooking UI sliders");

        // Health
        if (healthBar != null)
        {
            healthBar.value = statsManager.health.GetPercent();
            healthUpdate = (current, max) =>
            {
                healthBar.value = current / max;
            };
            statsManager.health.OnValueChanged += healthUpdate;
        }

        // Stamina
        if (staminaBar != null)
        {
            staminaBar.value = statsManager.stamina.GetPercent();
            staminaUpdate = (current, max) =>
            {
                staminaBar.value = current / max;
            };
            statsManager.stamina.OnValueChanged += staminaUpdate;
        }

        isHooked = true;
    }

    public override void OnStopLocalPlayer()
    {
        Debug.Log("[PlayerUI] Local player stopped");

        if (playerUICanvas != null)
            playerUICanvas.enabled = false;

        if (crosshair != null)
            crosshair.SetActive(false);

        if (statsManager != null)
        {
            statsManager.OnStatsReady -= TryHookSliders;

            if (healthUpdate != null)
                statsManager.health.OnValueChanged -= healthUpdate;

            if (staminaUpdate != null)
                statsManager.stamina.OnValueChanged -= staminaUpdate;
        }
    }
}