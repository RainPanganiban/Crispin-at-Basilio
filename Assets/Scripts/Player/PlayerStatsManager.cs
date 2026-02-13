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

    private void Start()
    {
        // Initialize current values
        health.SetValue(health.maxValue);
        stamina.SetValue(stamina.maxValue);

        OnStatsReady?.Invoke();
    }

    private void Update()
    {
        // Regenerate stamina over time
        if (stamina.currentValue < stamina.maxValue)
            stamina.ChangeValue(staminaRegenRate * Time.deltaTime);

        // Optional health regen
        if (health.currentValue < health.maxValue)
            health.ChangeValue(healthRegenRate * Time.deltaTime);
    }

    // Public methods
    [Server]
    public void TakeDamage(float amount)
    {
        health.ChangeValue(-amount);

        Debug.Log("Player took damage: " + amount);

        if (health.currentValue <= 0)
        {
            Die();
        }
    }

    public void UseStamina(float amount)
    {
        stamina.ChangeValue(-amount);
    }

    public void RestoreHealth(float amount)
    {
        health.ChangeValue(amount);
    }

    public void RestoreStamina(float amount)
    {
        stamina.ChangeValue(amount);
    }

    [Server]
    void Die()
    {
        Debug.Log("Player died");

        // For now just log
        // Later we add respawn logic
    }
}
