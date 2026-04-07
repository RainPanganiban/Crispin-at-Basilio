using UnityEngine;
using Mirror;

public class PlayerHealth : NetworkBehaviour, IDamageable
{
    [SyncVar(hook = nameof(OnHealthChanged))]
    public float currentHealth = 100f;
    public float maxHealth = 100f;

    // This function is called by the Laser Beam script
    public void TakeDamage(float amount, Transform attacker)
    {
        // Only the server should calculate damage
        if (!isServer) return;

        currentHealth -= amount;
        Debug.Log($"Player hit! Remaining health: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void OnHealthChanged(float oldHealth, float newHealth)
    {
        // Use this to update your UI Health Bar in the future
    }

    void Die()
    {
        Debug.Log("Player has died!");
        // Add respawn or death logic here
    }
}