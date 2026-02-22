using UnityEngine;
using Mirror;

public abstract class BaseAttack : MonoBehaviour
{
    [Header("Attack")]
    public string attackName = "Attack";
    public float cooldown = 2f;
    public string animationTriggerName = "";
    public bool requiresMovementLock = false;

    protected BossController boss;

    public virtual void Initialize(BossController bossController)
    {
        boss = bossController;
    }

    public virtual bool Server_CanExecute()
    {
        return boss != null && boss.isServer;
    }

    public abstract void Server_Execute();
    public abstract void Server_OnAnimationEvent(string eventName);
    public abstract void Server_Stop();
}

