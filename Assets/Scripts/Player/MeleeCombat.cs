using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

public class MeleeCombat : NetworkBehaviour, ICombatHandler
{
    [Header("Attack Settings")]
    public float lightDamage = 10f;
    public float heavyDamage = 25f;

    public float attackRadius = 1.5f;
    public float attackRange = 1.5f;
    public LayerMask hitLayers;

    [Header("Combo Settings")]
    public float comboResetTime = 1.2f;

    private int comboStep = 0;
    private float lastAttackTime;

    [Header("References")]
    public Transform attackPoint;
    private BasilioAnimation basilioAnimation;
    private PlayerStatsManager statsManager;

    void Awake()
    {
        basilioAnimation = GetComponent<BasilioAnimation>();
        statsManager = GetComponent<PlayerStatsManager>();
    }

    public void OnLightAttack(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer) return; // Make sure only local player triggers it
        if (!context.started) return;

        // Call your existing LightAttack logic
        GetComponent<CharacterAnimationController>()?.PlayAttack();
    }

    // Called by the Animation Event "AnimationEvent_Hit"
    public void ApplyDamageLocalClient(int currentComboStep)
    {
        if (isLocalPlayer)
        {
            CmdApplyDamage(currentComboStep);
        }
    }

    // ===============================
    // SERVER AUTHORITATIVE ATTACK
    // ===============================
    [Command]
    void CmdApplyDamage(int animationComboStep)
    {
        // Sync combo step based on client's animation to ensure damage matches visuals.
        comboStep = animationComboStep;
        lastAttackTime = Time.time; 

        float bonus = statsManager != null ? statsManager.BonusAttackDamage : 0f;
        float damage = ((comboStep == 3) ? heavyDamage : lightDamage) + bonus;

        Vector3 center =
            attackPoint.position +
            attackPoint.forward * attackRange;

        Collider[] hits = Physics.OverlapSphere(
            center,
            attackRadius,
            hitLayers
        );

        bool hitAnything = false;

        foreach (Collider hit in hits)
        {
            if (hit.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(damage, transform);
                hitAnything = true;
            }
        }

        RpcOnAttack(comboStep, hitAnything);
    }

    // ===============================
    // COMBO LOGIC (SERVER)
    // ===============================
    // Note: comboStep is now purely driven by the client's animation comboCount to stay perfectly synced.

    // ===============================
    // VISUAL FEEDBACK (ALL CLIENTS)
    // ===============================
    [ClientRpc]
    void RpcOnAttack(int step, bool hitAnything)
    {
        Debug.Log($"Melee Attack Hit Event (Step: {step}, Hit Anything: {hitAnything})");

        // Hit Stop / Freeze Frame
        if (hitAnything)
        {
            StartCoroutine(HitStopRoutine(0.08f)); // Freeze for 80ms for nice crunchy impact
        }
    }

    private System.Collections.IEnumerator HitStopRoutine(float duration)
    {
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            float originalSpeed = anim.speed;
            anim.speed = 0f;
            yield return new WaitForSecondsRealtime(duration);
            
            // Only unfreeze if it hasn't been destroyed
            if (anim != null)
            {
                anim.speed = originalSpeed;
            }
        }
    }

    // ===============================
    // DEBUG GIZMOS
    // ===============================
    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            attackPoint.position + attackPoint.forward * attackRange,
            attackRadius
        );
    }
}
