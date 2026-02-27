using UnityEngine;
using Mirror;
using System;

public class BossHealth : NetworkBehaviour, IDamageable
{
    [Header("Health")]
    public float maxHealth = 500f;

    [SyncVar(hook = nameof(OnHealthChanged))]
    private float currentHealth;

    public event Action<float, float> OnHealthChangedUI;

    private BossPhaseManager phaseManager;
    private BossController controller;

    public override void OnStartServer()
    {
        currentHealth = maxHealth;
        phaseManager = GetComponent<BossPhaseManager>();
        controller = GetComponent<BossController>();
    }


    [Server]
    public void Server_TakeDamage(float amount)
    {
        if (controller != null && controller.State == BossState.Dead)
            return;

        if (currentHealth <= 0)
            return;

        float oldHealth = currentHealth;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        
        if (phaseManager != null)
            phaseManager.Server_OnHealthChanged(currentHealth, maxHealth);

        if (currentHealth <= 0f && controller != null)
            controller.Server_Die();
    }

    public void TakeDamage(float amount, Transform attacker)
    {
        Server_TakeDamage(amount);
    }

    void OnHealthChanged(float oldValue, float newValue)
    {
        OnHealthChangedUI?.Invoke(newValue, maxHealth);
    }

    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
}

