using UnityEngine;
using Mirror;

public abstract class BaseAttack : NetworkBehaviour
{
    [Header("Attack Settings")]
    public string attackName = "Attack";
    public float cooldown = 2f;
    public float maxRange = 5f; // New: Range check
    public string animationTriggerName = "";
    public bool requiresMovementLock = false;

    protected BossController boss;

    public virtual void Initialize(BossController bossController)
    {
        boss = bossController;
    }

    public virtual bool Server_CanExecute()
    {
        if (boss == null || !boss.isServer) return false;

        // Find closest player to check range
        Transform target = Server_FindClosestPlayer();
        if (target == null) return false;

        float distSqr = (target.position - transform.position).sqrMagnitude;
        return distSqr <= (maxRange * maxRange);
    }

    protected Transform Server_FindClosestPlayer()
    {
        Transform best = null;
        float bestSqr = float.PositiveInfinity;
        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn?.identity == null) continue;
            float d = (conn.identity.transform.position - transform.position).sqrMagnitude;
            if (d < bestSqr)
            {
                bestSqr = d;
                best = conn.identity.transform;
            }
        }
        return best;
    }

    public abstract void Server_Execute();
    public abstract void Server_OnAnimationEvent(string eventName);
    public abstract void Server_Stop();
}

