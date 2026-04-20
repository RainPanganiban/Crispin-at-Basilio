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
    public float knockbackForce = 25f;
    public float upwardForce = 60f;

    [Header("Timing & Visuals")]
    public float damageDelay = 0.8f;
    public float attackDuration = 2.0f;
    public GameObject slamVFXPrefab;

    [Header("Sound Effects")] // --- DAGDAG: Sound Setup ---
    [SerializeField] private AudioClip slamSFX; // Ang malakas na "THUD" o paghampas

    private BossController bossController;
    private BakunawaMovement movement;
    private NetworkAnimator networkAnimator;
    private EnemySoundManager soundManager; // --- DAGDAG: Reference ---
    private bool isExecuting = false;

    void Awake()
    {
        bossController = GetComponent<BossController>();
        movement = GetComponent<BakunawaMovement>();
        networkAnimator = GetComponent<NetworkAnimator>();
        soundManager = GetComponent<EnemySoundManager>(); // Kunin ang SoundManager
    }

    public override void Initialize(BossController controller)
    {
        base.Initialize(controller);
        this.bossController = controller;
        this.soundManager = controller.GetComponent<EnemySoundManager>();
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

        // --- AUDIO: Patunugin ang Slam SFX sa lahat ---
        RpcPlaySlamSFX();

        // --- HIT DETECTION & KNOCKBACK ---
        Collider[] hits = Physics.OverlapSphere(slamPos, slamRadius, playerLayer);

        foreach (Collider hit in hits)
        {
            if (hit.transform == transform) continue;

            // 1. Damage gamit ang IDamageable (Standard para sa boss scripts mo)
            if (hit.TryGetComponent<IDamageable>(out var dmg))
            {
                dmg.TakeDamage(damage, transform);
            }
            else if (hit.TryGetComponent<PlayerStatsManager>(out var stats))
            {
                stats.TakeDamage(damage, transform);
            }

            // 2. Knockback Logic
            var playerMove = hit.GetComponent<PlayerMovement>();
            if (playerMove != null)
            {
                Vector3 pushDir = (hit.transform.position - transform.position).normalized;
                pushDir.y = 0;
                Vector3 finalForce = (pushDir * knockbackForce) + (Vector3.up * upwardForce);

                playerMove.ApplyKnockback(finalForce, 0.5f);
                Debug.Log($"<color=yellow>[TailSlam]</color> Uppercut applied to {hit.name}");
            }
        }

        yield return new WaitForSeconds(attackDuration - damageDelay);
        Server_Stop();
    }

    // --- AUDIO RPC ---
    [ClientRpc]
    private void RpcPlaySlamSFX()
    {
        // Gagamitin ang soundManager ng boss para sa 3D spatial sound
        if (soundManager != null && slamSFX != null)
        {
            soundManager.PlaySpecificAttack(slamSFX);
        }
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