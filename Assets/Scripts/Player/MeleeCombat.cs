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

    [Header("Combo Settings")]
    public float comboResetTime = 1.2f;

    private int comboStep = 0;
    private float lastAttackTime;

    [Header("References")]
    public Transform attackPoint;

    // Called by input system
    public void OnLightAttack(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer || !context.performed) return;
        LightAttack();
    }

    // ===============================
    // CLIENT -> SERVER
    // ===============================
    public void LightAttack()
    {
        CmdPerformLightAttack();
    }

    [Command]
    private void CmdPerformLightAttack()
    {
        HandleCombo();

        float damage = (comboStep == 3) ? heavyDamage : lightDamage;

        Vector3 center = attackPoint.position + attackPoint.forward * attackRange;

        // OverlapSphere without layers → hits everything
        Collider[] hits = Physics.OverlapSphere(center, attackRadius);

        int hitCount = 0;

        foreach (Collider hit in hits)
        {
            if (hit.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(damage, transform); // Pass this player as attacker

                // If enemy has aggro, force target on this player
                if (hit.TryGetComponent(out EnemyAggro aggro))
                {
                    aggro.ForceTarget(transform);
                }

                hitCount++;
            }
        }

        Debug.Log($"Melee Attack Step {comboStep} hit {hitCount} targets.");

        RpcOnAttack(comboStep, hitCount);
    }

    // ===============================
    // COMBO LOGIC
    // ===============================
    private void HandleCombo()
    {
        if (Time.time - lastAttackTime > comboResetTime)
            comboStep = 0;

        comboStep++;
        comboStep = Mathf.Clamp(comboStep, 1, 3);

        lastAttackTime = Time.time;
    }

    // ===============================
    // VISUAL FEEDBACK (ALL CLIENTS)
    // ===============================
    [ClientRpc]
    private void RpcOnAttack(int step, int hitCount)
    {
        // TODO: Play animation, VFX, sound here
        Debug.Log($"Melee Attack Step: {step}, hit {hitCount} enemies");
    }

    // ===============================
    // DEBUG GIZMOS
    // ===============================
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position + attackPoint.forward * attackRange, attackRadius);
    }
}
