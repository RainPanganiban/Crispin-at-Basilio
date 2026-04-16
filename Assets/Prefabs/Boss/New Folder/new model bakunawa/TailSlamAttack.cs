using UnityEngine;
using Mirror;
using System.Collections;

public class TailSlamAttack : BaseAttack
{
    [Header("Tail Slam (Uppercut) Settings")]
    public float damage = 40f;
    public float slamRadius = 6.0f;

    [Tooltip("Positive Z para sa harap ng boss. I-adjust sa Inspector para tumama sa pula na bilog.")]
    public Vector3 slamOffsetByParent = new Vector3(0, 0f, 6.0f);

    public LayerMask playerLayer;

    [Header("Knockback Settings")]
    [Tooltip("Lakas ng talsik paatras.")]
    public float knockbackForce = 25f;
    [Tooltip("Lakas ng talsik pataas. Dahil mabigat si Basilio, subukan ang 50-80.")]
    public float upwardForce = 60f;

    [Header("Timing & Visuals")]
    public float damageDelay = 0.8f;
    public float attackDuration = 2.0f;
    public GameObject slamVFXPrefab;

    private BossController bossController;
    private BakunawaMovement movement;
    private NetworkAnimator networkAnimator;
    private bool isExecuting = false;

    void Awake()
    {
        bossController = GetComponent<BossController>();
        movement = GetComponent<BakunawaMovement>();
        networkAnimator = GetComponent<NetworkAnimator>();
    }

    public override void Initialize(BossController controller)
    {
        base.Initialize(controller);
        this.bossController = controller;
    }

    [Server]
    public override void Server_Execute()
    {
        if (isExecuting) return;
        isExecuting = true;

        if (movement != null) movement.Server_SetMovementEnabled(false);
        if (networkAnimator != null) networkAnimator.SetTrigger("TailSlam");

        StartCoroutine(SlamRoutine());
    }

    private IEnumerator SlamRoutine()
    {
        yield return new WaitForSeconds(damageDelay);
        if (!isExecuting) yield break;

        // Kunin ang position sa harap ni Bakunawa
        Vector3 slamPos = transform.TransformPoint(slamOffsetByParent);

        // --- SHOCKWAVE VFX ---
        if (slamVFXPrefab != null)
        {
            GameObject vfx = Instantiate(slamVFXPrefab, slamPos, transform.rotation);
            NetworkServer.Spawn(vfx);
        }

        // --- HIT DETECTION & KNOCKBACK OVERRIDE ---
        Collider[] hits = Physics.OverlapSphere(slamPos, slamRadius, playerLayer);

        foreach (Collider hit in hits)
        {
            // 1. Damage
            var stats = hit.GetComponent<PlayerStatsManager>();
            if (stats == null) stats = hit.GetComponentInParent<PlayerStatsManager>();
            if (stats != null) stats.TakeDamage(damage, transform);

            // 2. Knockback Logic (Direct to Basilio's script)
            var playerMove = hit.GetComponent<PlayerMovement>();
            if (playerMove != null)
            {
                // Kalkulahin ang direksyon palayo sa boss
                Vector3 pushDir = (hit.transform.position - transform.position).normalized;
                pushDir.y = 0; // Horizontal base

                // Gagawa tayo ng "Super Force" vector para malabanan ang -2f gravity lock ni Basilio
                Vector3 finalForce = (pushDir * knockbackForce) + (Vector3.up * upwardForce);

                // Tinatawag natin yung existing function sa PlayerMovement.cs mo
                playerMove.ApplyKnockback(finalForce, 0.5f);

                Debug.Log($"<color=yellow>[TailSlam]</color> Uppercut applied to {hit.name} with {upwardForce} vertical force.");
            }
        }

        yield return new WaitForSeconds(attackDuration - damageDelay);
        Server_Stop();
    }

    [Server]
    public override void Server_Stop()
    {
        if (!isExecuting) return;
        isExecuting = false;

        if (movement != null) movement.Server_SetMovementEnabled(true);
        if (bossController != null) bossController.Server_EndAttack();

        if (TryGetComponent<BossAttackManager>(out var am))
            am.Server_OnAttackAnimationComplete();
    }

    [Server]
    public override void Server_OnAnimationEvent(string eventName) { }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 slamPos = transform.TransformPoint(slamOffsetByParent);
        Gizmos.DrawWireSphere(slamPos, slamRadius);
        Gizmos.DrawLine(transform.position, slamPos);
    }
}