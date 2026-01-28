using UnityEngine;
using Mirror;
using System;

public class EnemyHealth : NetworkBehaviour , IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;

    [SyncVar(hook = nameof(OnHealthChanged))]
    private int currentHealth;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    public event Action<float, float> OnHealthChangedUI;
    public event Action OnDamaged;

    public override void OnStartServer()
    {
        currentHealth = maxHealth;
    }

    // REQUIRED by IDamageable
    public void TakeDamage(float damage)
    {
        if (!isServer) return;

        ApplyDamage(Mathf.RoundToInt(damage), null);
    }

    // Your existing version (used by melee / ranged)
    [Server]
    public void TakeDamage(int damage, Transform attacker)
    {
        ApplyDamage(damage, attacker);
    }

    // Single source of truth
    [Server]
    private void ApplyDamage(int damage, Transform attacker)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (attacker != null)
        {
            EnemyAI ai = GetComponent<EnemyAI>();
            if (ai != null) ai.SetAggroTarget(attacker);
        }

        OnDamaged?.Invoke();

        if (currentHealth <= 0)
        {
            NetworkServer.Destroy(gameObject);
        }
    }

    void OnHealthChanged(int oldValue, int newValue)
    {
        OnHealthChangedUI?.Invoke(newValue, maxHealth);
    }
}