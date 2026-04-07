using UnityEngine;
using Mirror;

public class BossAnimationRelay : NetworkBehaviour
{
    [Header("References (Manual assignment avoids search errors)")]
    [SerializeField] private BossAttackManager attackManager;
    [SerializeField] private BossController controller;

    void Awake()
    {
        // Awtomatikong hahanapin ang scripts sa parent kung sakaling makalimutang i-drag sa Inspector
        if (attackManager == null) attackManager = GetComponentInParent<BossAttackManager>();
        if (controller == null) controller = GetComponentInParent<BossController>();
    }

    // PINALITAN: Dati ay AnimationEvent, ngayon ay Server_OnAnimationEvent para mag-match sa Animator
    public void Server_OnAnimationEvent(string eventName)
    {
        Debug.Log($"[BossAnimationRelay] Received event: {eventName} (isServer: {isServer})");

        if (!isServer)
            return;

        if (controller != null && controller.State == BossState.Dead)
            return;

        if (attackManager != null)
            attackManager.Server_OnAnimationEvent(eventName);
    }

    // PINALITAN: Dati ay AttackAnimationComplete, ngayon ay Server_OnAttackAnimationComplete
    public void Server_OnAttackAnimationComplete()
    {
        Debug.Log($"[BossAnimationRelay] AttackAnimationComplete fired (isServer: {isServer})");

        if (!isServer)
            return;

        // Siguraduhin na may reference pa rin bago tawagin
        if (attackManager == null) attackManager = GetComponentInParent<BossAttackManager>();
        if (controller == null) controller = GetComponentInParent<BossController>();

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