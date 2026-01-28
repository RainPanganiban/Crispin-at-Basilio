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

    public void OnLightAttack(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer) return; // Make sure only local player triggers it
        if (!context.performed) return;

        // Call your existing LightAttack logic
        LightAttack();
    }

    // ===============================
    // INPUT ENTRY POINT (CLIENT)
    // ===============================
    public void LightAttack()
    {
        CmdPerformLightAttack();
    }

    // ===============================
    // SERVER AUTHORITATIVE ATTACK
    // ===============================
    [Command]
    void CmdPerformLightAttack()
    {
        HandleCombo();

        float damage = (comboStep == 3) ? heavyDamage : lightDamage;

        Vector3 center =
            attackPoint.position +
            attackPoint.forward * attackRange;

        Collider[] hits = Physics.OverlapSphere(
            center,
            attackRadius,
            hitLayers
        );

        foreach (Collider hit in hits)
        {
            if (hit.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(damage);
            }
        }

        RpcOnAttack(comboStep);
    }

    // ===============================
    // COMBO LOGIC (SERVER)
    // ===============================
    void HandleCombo()
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
    void RpcOnAttack(int step)
    {
        // Hook animations, VFX, sound later
        Debug.Log($"Melee Attack Step: {step}");
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