using UnityEngine;
using Mirror;
using System;

public class EnemyHealth : NetworkBehaviour, IDamageable
{
    [Header("Health Settings")]
    public float maxHealth = 50f;

    [SyncVar(hook = nameof(OnHealthChanged))]
    private float currentHealth;

    public event Action<float, float> OnHealthChangedUI; // current, max
    public event Action OnDamaged;

    private EnemyBrain brain;

    public override void OnStartServer()
    {
        currentHealth = maxHealth;
        brain = GetComponent<EnemyBrain>();
    }

    [Server]
    public void TakeDamage(float amount)
    {
        if (currentHealth <= 0) return;

        currentHealth -= amount;

        OnDamaged?.Invoke();

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    void OnHealthChanged(float oldValue, float newValue)
    {
        OnHealthChangedUI?.Invoke(newValue, maxHealth);
    }

    [Server]
    void Die()
    {
        brain.Die();
        NetworkServer.Destroy(gameObject);
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    public float GetMaxHealth()
    {
        return maxHealth;
    }
}
