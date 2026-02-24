using UnityEngine;
using System;
using System.Collections.Generic;
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

    // Death & Revive
    [SyncVar(hook = nameof(OnDeadChanged))]
    private bool isDead = false;
    public bool IsDead => isDead;
    public event Action<bool> OnDeadStateChanged;

    // Bonus damage from upgrades (synced so combat scripts can read it)
    [SyncVar]
    private float bonusAttackDamage = 0f;
    public float BonusAttackDamage => bonusAttackDamage;

    private PlayerMovement playerMovement;

    private void Start()
    {
        // Initialize stats
        health.SetValue(health.maxValue);
        stamina.SetValue(stamina.maxValue);

        syncedHealth = health.currentValue;
        syncedStamina = stamina.currentValue;

        playerMovement = GetComponent<PlayerMovement>();

        OnStatsReady?.Invoke();
    }

    private void Update()
    {
        if (!isServer) return; // Only server modifies values
        if (isDead) return;    // No regen while dead

        bool running = playerMovement != null && playerMovement.IsRunning;

        // Drain stamina on server while running (authoritative drain)
        if (running && stamina.currentValue > 0f)
        {
            float drainRate = playerMovement != null ? playerMovement.staminaCostPerSecondRunning : staminaRegenRate;
            stamina.ChangeValue(-drainRate * Time.deltaTime);
            syncedStamina = stamina.currentValue;
        }
        // Only regen when NOT running
        else if (!running && stamina.currentValue < stamina.maxValue)
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
        if (isDead) return;
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
    // ===============================
    void OnHealthChanged(float oldValue, float newValue)
    {
        health.SetValue(newValue); // updates UI via Stat events
    }

    void OnStaminaChanged(float oldValue, float newValue)
    {
        float previous = stamina.currentValue;
        stamina.SetValue(newValue); // fires OnValueChanged
    }

    void OnDeadChanged(bool oldValue, bool newValue)
    {
        OnDeadStateChanged?.Invoke(newValue);
    }

    // ===============================
    // Stamina Methods
    // ===============================
    public void UseStamina(float amount)
    {
        stamina.ChangeValue(-amount);

        if (isServer)
        {
            syncedStamina = stamina.currentValue; // sync to all clients
        }
    }

    public void RestoreHealth(float amount)
    {
        health.ChangeValue(amount);
        if (isServer) syncedHealth = health.currentValue;
    }

    public void RestoreStamina(float amount)
    {
        stamina.ChangeValue(amount);
        if (isServer) syncedStamina = stamina.currentValue;
    }

    [Command]
    public void CmdUseStamina(float amount)
    {
        UseStamina(amount);
    }

    [Command]
    public void CmdRestoreHealth(float amount)
    {
        RestoreHealth(amount);
    }

    [Command]
    public void CmdRestoreStamina(float amount)
    {
        RestoreStamina(amount);
    }

    // ===============================
    // Death & Revive
    // ===============================
    [Server]
    void Die()
    {
        isDead = true;
        Debug.Log($"Player {gameObject.name} died.");
        RpcOnPlayerDied();
    }

    [ClientRpc]
    void RpcOnPlayerDied()
    {
        // Disable movement and combat on all clients
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = false;

        // Disable combat handlers
        MeleeCombat melee = GetComponent<MeleeCombat>();
        if (melee != null) melee.enabled = false;

        RangedAttack ranged = GetComponent<RangedAttack>();
        if (ranged != null) ranged.enabled = false;
    }

    [Server]
    public void ServerRevive()
    {
        if (!isDead) return;

        isDead = false;
        health.SetValue(health.maxValue);
        syncedHealth = health.currentValue;

        Debug.Log($"Player {gameObject.name} revived.");
        RpcOnPlayerRevived();
    }

    [ClientRpc]
    void RpcOnPlayerRevived()
    {
        // Re-enable movement and combat on all clients
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = true;

        MeleeCombat melee = GetComponent<MeleeCombat>();
        if (melee != null) melee.enabled = true;

        RangedAttack ranged = GetComponent<RangedAttack>();
        if (ranged != null) ranged.enabled = true;
    }

    // ===============================
    // Upgrades
    // ===============================

    /// <summary>
    /// Apply a single upgrade by stat name.
    /// Called by ShopManager when purchasing an upgrade.
    /// </summary>
    [Server]
    public void ServerApplyUpgrade(string statName, float amount)
    {
        switch (statName)
        {
            case "MaxHealth":
                health.maxValue += amount;
                health.SetValue(health.currentValue); // re-clamp
                syncedHealth = health.currentValue;
                break;

            case "MaxStamina":
                stamina.maxValue += amount;
                stamina.SetValue(stamina.currentValue); // re-clamp
                syncedStamina = stamina.currentValue;
                break;

            case "AttackDamage":
                bonusAttackDamage += amount;
                break;

            default:
                Debug.LogWarning($"[PlayerStatsManager] Unknown upgrade stat: {statName}");
                break;
        }
    }

    /// <summary>
    /// Applied when spawning a player from persistent session data.
    /// Re-applies all previously purchased upgrades.
    /// </summary>
    [Server]
    public void ServerApplyPersistentData(int coins, List<string> purchasedUpgrades)
    {
        if (purchasedUpgrades == null) return;

        // Find the shop manager to look up upgrade values
        ShopManager shop = ShopManager.Instance;
        if (shop == null) return;

        foreach (string upgradeId in purchasedUpgrades)
        {
            ShopItemData item = shop.GetItemById(upgradeId);
            if (item != null && item.type == ShopItemType.Upgrade)
            {
                ServerApplyUpgrade(item.statToUpgrade, item.upgradeAmount);
            }
        }
    }
}
