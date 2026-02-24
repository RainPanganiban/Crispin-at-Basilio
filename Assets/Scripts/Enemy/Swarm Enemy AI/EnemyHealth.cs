using UnityEngine;
using Mirror;
using System;

public class EnemyHealth : NetworkBehaviour, IDamageable
{
    [Header("Health Settings")]
    public float maxHealth = 50f;

    [SyncVar(hook = nameof(OnHealthChanged))]
    private float currentHealth;

    public event Action<float, float> OnHealthChangedUI;
    public event Action OnDamaged;

    private EnemyBrain brain;
    private EnemyAggro aggro;

    public override void OnStartServer()
    {
        currentHealth = maxHealth;
        brain = GetComponent<EnemyBrain>();
        aggro = GetComponent<EnemyAggro>(); // ✅ FIXED
    }

    // Interface-required method
    [Server]
    public void TakeDamage(float amount)
    {
        TakeDamage(amount, null);
    }

    [Server]
    public void TakeDamage(float amount, Transform attacker)
    {
        if (currentHealth <= 0) return;

        currentHealth -= amount;

        // Damage Pull Aggro
        if (aggro != null && attacker != null)
        {
            aggro.ForceTarget(attacker);
        }

        RpcNotifyDamaged();

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    [ClientRpc]
    void RpcNotifyDamaged()
    {
        OnDamaged?.Invoke();
    }

    void OnHealthChanged(float oldValue, float newValue)
    {
        OnHealthChangedUI?.Invoke(newValue, maxHealth);
    }

    [Server]
    void Die()
    {
        brain.Die();

        // Drop coins before the object is destroyed
        GetComponent<EnemyCoinDrop>()?.DropCoins();

        NetworkServer.Destroy(gameObject);
    }

    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
}
