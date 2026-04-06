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
    public float staminaRegenRate = 10f;
    public float healthRegenRate = 0f;

    public event Action OnStatsReady;

    [SyncVar(hook = nameof(OnHealthChanged))] private float syncedHealth;
    [SyncVar(hook = nameof(OnMaxHealthChanged))] private float syncedMaxHealth;
    [SyncVar(hook = nameof(OnStaminaChanged))] private float syncedStamina;

    [SyncVar(hook = nameof(OnDeadChanged))]
    private bool isDead = false;
    public bool IsDead => isDead;
    public event Action<bool> OnDeadStateChanged;

    [SyncVar]
    private float bonusAttackDamage = 0f;
    public float BonusAttackDamage => bonusAttackDamage;

    private PlayerMovement playerMovement;
    private PlayerSoundManager playerSoundManager;
    private bool healthSetFromSession = false;
    private Coroutine activeStaggerCoroutine;
    private Dictionary<Renderer, Color[]> originalColorsMap = new Dictionary<Renderer, Color[]>();
    private bool colorsCached = false;

    private void Start()
    {
        if (isServer)
        {
            // PERSISTENT DEATH CHECK:
            if (syncedHealth <= 0 && healthSetFromSession)
            {
                isDead = true;
                health.SetValue(0);
                syncedHealth = 0;
                RpcNotifyDeath();
            }
            else if (!healthSetFromSession || syncedHealth <= 0)
            {
                health.SetValue(health.maxValue);
                syncedHealth = health.maxValue;
                isDead = false;
            }

            stamina.SetValue(stamina.maxValue);
            syncedMaxHealth = health.maxValue;
            syncedStamina = stamina.currentValue;
        }

        playerMovement = GetComponent<PlayerMovement>();
        playerSoundManager = GetComponent<PlayerSoundManager>();

        health.SetMax(syncedMaxHealth > 0 ? syncedMaxHealth : 100f);
        health.SetValue(syncedHealth);
        stamina.SetValue(syncedStamina);

        OnStatsReady?.Invoke();

        // FIX (1 & 2): Force visual state base sa current synced value ng isDead.
        // Ito ang sisiguro na itatago agad ang body pagka-load ng scene.
        UpdateVisualState(isDead);
    }

    private void Update()
    {
        if (!isServer) return;
        if (isDead) return;

        bool running = playerMovement != null && playerMovement.IsRunning;

        if (running && stamina.currentValue > 0f)
        {
            float drainRate = playerMovement != null ? playerMovement.staminaCostPerSecondRunning : staminaRegenRate;
            stamina.ChangeValue(-drainRate * Time.deltaTime);
            syncedStamina = stamina.currentValue;
        }
        else if (!running && stamina.currentValue < stamina.maxValue)
        {
            stamina.ChangeValue(staminaRegenRate * Time.deltaTime);
            syncedStamina = stamina.currentValue;
        }

        if (health.currentValue < health.maxValue)
        {
            health.ChangeValue(healthRegenRate * Time.deltaTime);
            syncedHealth = health.currentValue;
        }
    }

    [Server]
    public void TakeDamage(float amount, Transform attacker)
    {
        if (isDead) return;
        if (health.currentValue <= 0) return;

        health.ChangeValue(-amount);
        syncedHealth = health.currentValue;

        if (health.currentValue <= 0)
        {
            health.SetValue(0);
            syncedHealth = 0;
            Die();
        }
        else
        {
            Vector3 attackerPos = attacker != null ? attacker.position : transform.position;
            RpcOnTookDamage(attackerPos);
        }
    }

    [ClientRpc]
    void RpcOnTookDamage(Vector3 attackerPos)
    {
        if (!colorsCached) CacheOriginalColors();

        if (activeStaggerCoroutine != null) StopCoroutine(activeStaggerCoroutine);
        activeStaggerCoroutine = StartCoroutine(HitStaggerRoutine(attackerPos));

        if (playerSoundManager != null) playerSoundManager.PlayHurt();
    }

    // --- DEATH & REVIVE LOGIC ---

    [Server]
    void Die()
    {
        if (isDead) return;
        isDead = true;

        RpcNotifyDeath();
        CheckGameOver();
    }

    [ClientRpc]
    void RpcNotifyDeath()
    {
        UpdateVisualState(true);
        if (playerSoundManager != null) playerSoundManager.PlayDeath();
    }

    // NEW FUNCTION: Centralized visual toggle para sa death/revive
    private void UpdateVisualState(bool dead)
    {
        if (dead)
        {
            gameObject.tag = "Untagged";
            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            CharacterController cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            Transform modelTransform = transform.Find("Model");
            if (modelTransform != null) modelTransform.gameObject.SetActive(false);

            if (playerMovement != null) playerMovement.enabled = false;
        }
        else
        {
            gameObject.tag = "Player";
            gameObject.layer = LayerMask.NameToLayer("Player");

            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = true;

            CharacterController cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = true;

            Transform modelTransform = transform.Find("Model");
            if (modelTransform != null) modelTransform.gameObject.SetActive(true);

            if (playerMovement != null) playerMovement.enabled = true;
        }
    }

    [Server]
    void CheckGameOver()
    {
        PlayerStatsManager[] allPlayers = FindObjectsByType<PlayerStatsManager>(FindObjectsSortMode.None);
        int deadCount = 0;

        foreach (var p in allPlayers)
        {
            if (p.isDead) deadCount++;
        }

        if (deadCount >= allPlayers.Length && allPlayers.Length > 0)
        {
            GameOverUI ui = FindFirstObjectByType<GameOverUI>();
            if (ui != null) ui.RpcShowGameOver();
        }
    }

    [Server]
    public void ServerRevive()
    {
        isDead = false;
        health.SetValue(health.maxValue);
        syncedHealth = health.maxValue;
        stamina.SetValue(stamina.maxValue);
        syncedStamina = stamina.maxValue;

        RpcNotifyRevive();
    }

    [ClientRpc]
    void RpcNotifyRevive()
    {
        UpdateVisualState(false);

        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.enabled = true;
            anim.speed = 1f;
        }
    }

    // --- SYNCVAR HOOKS ---

    void OnHealthChanged(float oldValue, float newValue)
    {
        health.SetValue(newValue);
    }

    void OnMaxHealthChanged(float oldValue, float newValue)
    {
        health.SetMax(newValue);
    }

    void OnStaminaChanged(float oldValue, float newValue)
    {
        stamina.SetValue(newValue);
    }

    void OnDeadChanged(bool oldValue, bool newValue)
    {
        OnDeadStateChanged?.Invoke(newValue);
        UpdateVisualState(newValue);
    }

    // --- UTILITIES ---

    private void CacheOriginalColors()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            Color[] colors = new Color[r.materials.Length];
            for (int i = 0; i < r.materials.Length; i++)
            {
                if (r.materials[i].HasProperty("_Color")) colors[i] = r.materials[i].color;
                else if (r.materials[i].HasProperty("_BaseColor")) colors[i] = r.materials[i].GetColor("_BaseColor");
            }
            originalColorsMap[r] = colors;
        }
        colorsCached = true;
    }

    private System.Collections.IEnumerator HitStaggerRoutine(Vector3 attackerPos)
    {
        foreach (var kvp in originalColorsMap)
        {
            if (kvp.Key != null)
            {
                for (int i = 0; i < kvp.Key.materials.Length; i++)
                {
                    Color originalColor = kvp.Value[i];
                    Color flashColor = Color.Lerp(originalColor, Color.white, 0.5f);
                    if (kvp.Key.materials[i].HasProperty("_Color")) kvp.Key.materials[i].color = flashColor;
                    else if (kvp.Key.materials[i].HasProperty("_BaseColor")) kvp.Key.materials[i].SetColor("_BaseColor", flashColor);
                }
            }
        }

        if (playerMovement != null)
        {
            Vector3 pushDir = (transform.position - attackerPos).normalized;
            pushDir.y = 0;
            if (pushDir.sqrMagnitude < 0.01f) pushDir = -transform.forward;
            playerMovement.ApplyKnockback(pushDir * 3f, 0.15f);
        }

        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.speed = 0.5f;

        yield return new WaitForSeconds(0.15f);

        foreach (var kvp in originalColorsMap)
        {
            if (kvp.Key != null)
            {
                for (int i = 0; i < kvp.Key.materials.Length; i++)
                {
                    if (kvp.Key.materials[i].HasProperty("_Color")) kvp.Key.materials[i].color = kvp.Value[i];
                    else if (kvp.Key.materials[i].HasProperty("_BaseColor")) kvp.Key.materials[i].SetColor("_BaseColor", kvp.Value[i]);
                }
            }
        }
        if (anim != null) anim.speed = 1f;
        activeStaggerCoroutine = null;
        yield return null;
    }

    public void UseStamina(float amount)
    {
        stamina.ChangeValue(-amount);
        if (isServer) syncedStamina = stamina.currentValue;
    }

    public void RestoreHealth(float amount)
    {
        health.ChangeValue(amount);
        if (isServer) syncedHealth = health.currentValue;
    }

    [Server]
    public void ServerSetHealth(float current, float max)
    {
        healthSetFromSession = true;
        health.SetMax(max);
        health.SetValue(current);
        syncedMaxHealth = health.maxValue;
        syncedHealth = health.currentValue;
    }

    public void RestoreStamina(float amount)
    {
        stamina.ChangeValue(amount);
        if (isServer) syncedStamina = stamina.currentValue;
    }

    [Command] public void CmdUseStamina(float amount) { UseStamina(amount); }
    [Command] public void CmdRestoreHealth(float amount) { RestoreHealth(amount); }
    [Command] public void CmdRestoreStamina(float amount) { RestoreStamina(amount); }

    [Server]
    public void ServerApplyUpgrade(string statName, float amount)
    {
        switch (statName)
        {
            case "MaxHealth":
                health.SetMax(health.maxValue + amount);
                syncedMaxHealth = health.maxValue;
                syncedHealth = health.currentValue;
                break;
            case "MaxStamina":
                stamina.maxValue += amount;
                stamina.SetValue(stamina.currentValue);
                syncedStamina = stamina.currentValue;
                break;
            case "AttackDamage":
                bonusAttackDamage += amount;
                break;
        }
    }

    [Server]
    public void ServerApplyPersistentData(int coins, List<string> purchasedUpgrades)
    {
        if (purchasedUpgrades == null) return;
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