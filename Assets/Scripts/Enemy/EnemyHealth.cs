using UnityEngine;
using Mirror;

public class EnemyHealth : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnHealthChanged))]
    public int health = 100;

    public void TakeDamage(int damage, Transform attacker)
    {
        if (!isServer) return;

        health -= damage;

        // switch aggro to attacker
        EnemyAI ai = GetComponent<EnemyAI>();
        if (ai != null) ai.SetAggroTarget(attacker);

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
