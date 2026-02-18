using UnityEngine;
using System;
using Mirror;

public class PlayerStatsManager : NetworkBehaviour, IDamageable
{
    [Header("Stats")]
    public Stat health = new Stat("Health", 100f);
    public Stat stamina = new Stat("Stamina", 100f);

    [Header("Regeneration")]
    public float staminaRegenRate = 10f; // per second
    public float healthRegenRate = 0f;   // optional

    public event Action OnStatsReady;

    // SyncVars for multiplayer UI syncing
    [SyncVar(hook = nameof(OnHealthChanged))] private float syncedHealth;
    [SyncVar(hook = nameof(OnStaminaChanged))] private float syncedStamina;

    private void Start()
    {
        // Initialize stats
        health.SetValue(health.maxValue);
        stamina.SetValue(stamina.maxValue);

        syncedHealth = health.currentValue;
        syncedStamina = stamina.currentValue;

        OnStatsReady?.Invoke();
    }

    private void Update()
    {
        if (!isServer) return; // Only server modifies values

        // Regenerate stamina
        if (stamina.currentValue < stamina.maxValue)
        {
            stamina.ChangeValue(staminaRegenRate * Time.deltaTime);
            syncedStamina = stamina.currentValue; // Sync
        }

        // Optional health regen
        if (health.currentValue < health.maxValue)
        {
            health.ChangeValue(healthRegenRate * Time.deltaTime);
            syncedHealth = health.currentValue; // Sync
        }
    }

    // ===============================
    // IDamageable Implementation
    // ===============================
    [Server]
    public void TakeDamage(float amount, Transform attacker)
    {
        if (health.currentValue <= 0) return;

        health.ChangeValue(-amount);
        syncedHealth = health.currentValue; // sync with clients

        if (health.currentValue <= 0)
        {
            health.SetValue(0);
            syncedHealth = 0;
            Die();
        }
    }

    // ===============================
    // SyncVar Hooks
    // These are called on all clients when the server updates the SyncVar
    // ===============================
    void OnHealthChanged(float oldValue, float newValue)
    {
        health.SetValue(newValue); // updates UI via Stat events
    }

    void OnStaminaChanged(float oldValue, float newValue)
    {
        // Fix: Update the Stat AND trigger OnValueChanged
        float previous = stamina.currentValue;
        stamina.SetValue(newValue); // fires OnValueChanged
    }

    // ===============================
    // Other Methods
    // ===============================
    [Server]
    public void UseStamina(float amount)
    {
        if (stamina.currentValue <= 0) return;

        stamina.ChangeValue(-amount);
        syncedStamina = stamina.currentValue; // sync
    }

    [Server]
    public void RestoreHealth(float amount)
    {
        health.ChangeValue(amount);
        syncedHealth = health.currentValue; // sync
    }

    [Server]
    public void RestoreStamina(float amount)
    {
        stamina.ChangeValue(amount);
        syncedStamina = stamina.currentValue; // sync
    }

    [Server]
    void Die()
    {
        Debug.Log("Player died");
        // Add respawn or death logic here
    }

    // Applied when spawning a player from persistent session data.
    [Server]
    public void ServerApplyPersistentData(int coins, System.Collections.Generic.List<string> purchasedUpgrades)
    {
        // Hook for persistent stats/upgrades if needed.
        // For now, health/stamina already start at max on spawn.
    }
}
