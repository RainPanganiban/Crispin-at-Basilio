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
    public event Action OnDeath;

    private EnemyBrain brain;
    private EnemyAggro aggro;
    private EnemySoundManager soundManager;

    public override void OnStartServer()
    {
        currentHealth = maxHealth;
        brain = GetComponent<EnemyBrain>();
        aggro = GetComponent<EnemyAggro>(); // ✅ FIXED
        soundManager = GetComponent<EnemySoundManager>();
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
        
        // Play hurt sound for all clients
        if (soundManager == null) 
            soundManager = GetComponent<EnemySoundManager>();
        
        if (soundManager != null)
            soundManager.PlayHurt();
    }

    void OnHealthChanged(float oldValue, float newValue)
    {
        OnHealthChangedUI?.Invoke(newValue, maxHealth);
    }

    [Server]
    void Die()
    {
        // Play death sound before destroying the object
        RpcNotifyDeath();

        brain.Die();

        // Notify listeners (e.g. DiwataVulnerabilityManager for swarm kill tracking)
        OnDeath?.Invoke();

        // Drop coins before the object is destroyed
        GetComponent<EnemyCoinDrop>()?.DropCoins();

        NetworkServer.Destroy(gameObject);
    }

    [ClientRpc]
    void RpcNotifyDeath()
    {
        if (soundManager == null) 
            soundManager = GetComponent<EnemySoundManager>();

        if (soundManager != null)
            soundManager.PlayDeath();
    }

    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
}
