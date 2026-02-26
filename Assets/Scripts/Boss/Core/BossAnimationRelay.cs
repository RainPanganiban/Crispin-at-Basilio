using UnityEngine;
using Mirror;

public class BossAnimationRelay : NetworkBehaviour
{
    [Header("References (Manual assignment avoids search errors)")]
    [SerializeField] private BossAttackManager attackManager;
    [SerializeField] private BossController controller;

    void Awake()
    {
        if (attackManager == null) attackManager = GetComponentInParent<BossAttackManager>();
        if (controller == null) controller = GetComponentInParent<BossController>();
    }

    // Called by Unity Animation Events.
    // IMPORTANT: Animation events can fire on clients too; only the server applies gameplay results.
    public void AnimationEvent(string eventName)
    {
        Debug.Log($"[BossAnimationRelay] Received event: {eventName} (isServer: {isServer})");
        
        if (!isServer)
            return;

        if (controller != null && controller.State == BossState.Dead)
            return;

        if (attackManager != null)
            attackManager.Server_OnAnimationEvent(eventName);
    }

    public void AttackAnimationComplete()
    {
        Debug.Log($"[BossAnimationRelay] AttackAnimationComplete fired (isServer: {isServer})");
        
        if (!isServer)
            return;

        // Force refresh if null
        if (attackManager == null) attackManager = GetComponentInParent<BossAttackManager>();
        if (controller == null) controller = GetComponentInParent<BossController>();

        Debug.Log($"[BossAnimationRelay] Refs: controller={controller != null}, attackManager={attackManager != null}");

        if (attackManager != null)
        {
            attackManager.Server_OnAttackAnimationComplete();
        }

        if (controller != null)
        {
            Debug.Log($"[BossAnimationRelay] Controller State: {controller.State}");
            controller.Server_EndAttack();
        }
        else
        {
            Debug.LogError("[BossAnimationRelay] Controller not found! Cannot end attack state.");
        }
    }
}

