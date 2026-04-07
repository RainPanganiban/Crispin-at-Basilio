using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

public class MeleeCombat : NetworkBehaviour, ICombatHandler
{
    [Header("Attack Settings")]
    public float lightDamage = 10f;
    public float heavyDamage = 25f;
    public float attackRadius = 5f;
    public float attackRange = 1.5f;
    public LayerMask hitLayers;

    [Header("References")]
    public Transform attackPoint;
    private PlayerStatsManager statsManager;

    void Awake()
    {
        statsManager = GetComponent<PlayerStatsManager>();
    }

    // ================================================================
    // ANIMATION EVENT BRIDGES
    // ================================================================

    public void AnimationEvent_Hit(int currentComboStep)
    {
        // Gagamit lang ng Log para sa local tracking
        if (isLocalPlayer)
        {
            CmdApplyDamage(currentComboStep);
        }
    }

    public void ApplyDamageLocalClient(int currentComboStep)
    {
        AnimationEvent_Hit(currentComboStep);
    }

    // ================================================================
    // SERVER-SIDE DAMAGE LOGIC
    // ================================================================
    [Command]
    void CmdApplyDamage(int animationComboStep)
    {
        // Ang center ay nasa harap ni Basilio
        Vector3 center = transform.position + (transform.forward * attackRange);

        // Gagamit ng Physics filter para optimized
        Collider[] hits = Physics.OverlapSphere(center, attackRadius, hitLayers);

        foreach (Collider hit in hits)
        {
            // Siguraduhin na hindi tinatamaan ang sarili
            if (hit.transform.root == transform.root) continue;

            // 1. Hanapin ang BossHealth
            BossHealth bossHP = hit.GetComponentInParent<BossHealth>();

            if (bossHP != null)
            {
                var vulnMgr = bossHP.GetComponent<DiwataVulnerabilityManager>();
                float multiplier = (vulnMgr != null) ? vulnMgr.GetDamageMultiplier() : 1f;

                float bonus = statsManager != null ? statsManager.BonusAttackDamage : 0f;
                float finalDamage = ((animationComboStep == 3) ? heavyDamage : lightDamage) + bonus;
                finalDamage *= multiplier;

                bossHP.Server_TakeDamage(finalDamage);

                // GINAWANG LOG LANG PARA HINDI PULA SA CONSOLE
                Debug.Log($"[SERVER] Damage Success: {finalDamage} HP to {hit.name}");
                continue;
            }

            // 2. Ordinaryong Enemy/Swarm
            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                float bonus = statsManager != null ? statsManager.BonusAttackDamage : 0f;
                float damageValue = ((animationComboStep == 3) ? heavyDamage : lightDamage) + bonus;

                damageable.TakeDamage(damageValue, transform);
                Debug.Log($"[SERVER] Swarm Damage: {damageValue} to {hit.name}");
            }
        }
    }

    public void OnLightAttack(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer || !context.started) return;

        // Iharap ang player sa camera direction
        Transform camTransform = GetComponent<PlayerMovement>()?.PlayerCamera;
        if (camTransform != null)
        {
            Vector3 camForward = camTransform.forward;
            camForward.y = 0;
            if (camForward.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(camForward);
        }

        GetComponent<CharacterAnimationController>()?.PlayAttack();
    }

    [ClientRpc]
    void RpcOnAttack(bool hitAnything)
    {
        // Iniwan nating blanko para smooth ang animation speed
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 center = transform.position + (transform.forward * attackRange);
        Gizmos.DrawWireSphere(center, attackRadius);
    }
}