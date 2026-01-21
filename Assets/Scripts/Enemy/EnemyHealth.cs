using UnityEngine;
using Mirror;

public class EnemyHealth : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnHealthChanged))]
    public int health = 100;

    public void TakeDamage(int damage)
    {
        if (!isServer) return;

        health -= damage;

        if (health <= 0)
        {
            NetworkServer.Destroy(gameObject);
        }
    }

    void OnHealthChanged(int oldValue, int newValue)
    {
        Debug.Log($"{gameObject.name} Health: {newValue}");
    }
}
