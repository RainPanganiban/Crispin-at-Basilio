using UnityEngine;
using Mirror;

public class BossAnimationRelay : NetworkBehaviour
{
    private BossAttackManager attackManager;
    private BossController controller;

    void Awake()
    {
        attackManager = GetComponent<BossAttackManager>();
        controller = GetComponent<BossController>();
    }

    // Called by Unity Animation Events.
    // IMPORTANT: Animation events can fire on clients too; only the server applies gameplay results.
    public void AnimationEvent(string eventName)
    {
        if (!isServer)
            return;

        if (controller != null && controller.State == BossState.Dead)
            return;

        if (attackManager != null)
            attackManager.Server_OnAnimationEvent(eventName);
    }

    // Optional helper for “animation finished” events (attack end).
    public void AttackAnimationComplete()
    {
        if (!isServer)
            return;

        if (attackManager != null)
            attackManager.Server_OnAttackAnimationComplete();

        if (controller != null && controller.State == BossState.Attacking)
            controller.Server_EndAttack();
    }
}

