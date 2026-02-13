using UnityEngine;
using Mirror;

public abstract class EnemyAttack : NetworkBehaviour
{
    [Header("Attack Settings")]
    public float cooldown = 2f;
    public float duration = 1f;

    protected float lastUsedTime = -Mathf.Infinity;

    public bool CanUse()
    {
        return Time.time >= lastUsedTime + cooldown;
    }

    [Server]
    public void Execute()
    {
        lastUsedTime = Time.time;
        OnExecute();
    }

    protected abstract void OnExecute();
}
