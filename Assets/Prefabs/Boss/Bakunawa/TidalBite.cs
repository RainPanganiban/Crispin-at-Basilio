using UnityEngine;
using Mirror;
using System.Collections;

public class TidalBite : BaseAttack
{
    [Header("Bite Settings")]
    public float damage = 50f;
    public float lungeForce = 7f;
    public float lungeDuration = 0.3f;
    public float attackFullDuration = 1.2f;
    public Transform mouthPoint;

    [Header("AOE Settings")]
    public float aoeRadius = 8.0f;
    public float aoeOffsetForward = 3.0f;
    public LayerMask playerLayer;

    private Animator animator;
    private NetworkAnimator networkAnimator;
    private BakunawaMovement movement;
    private BossController bossController;

    private bool isExecuting = false;
    private bool damageDealt = false;
    private float attackStartTime;

    private static readonly int AttackTrigger = Animator.StringToHash("TidalBite");

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        networkAnimator = GetComponent<NetworkAnimator>();
        movement = GetComponent<BakunawaMovement>();
        bossController = GetComponent<BossController>();
    }

    [Server]
    public override void Server_Execute()
    {
        // Eto ang trigger para magsimula ang kagat
        if (boss != null)
        {
            isExecuting = true;
            damageDealt = false;
            attackStartTime = Time.time;

            // I-play ang animation
            boss.Server_PlayTrigger("TidalBite");

            if (movement != null) movement.Server_SetMovementEnabled(false);
        }
    }

    void Update()
    {
        if (!isServer || !isExecuting) return;

        float elapsed = Time.time - attackStartTime;

        // Damage Timing
        if (!damageDealt && elapsed >= 0.6f)
        {
            DealBiteDamage();
        }

        // Auto-End Attack
        if (elapsed >= attackFullDuration)
        {
            Server_Stop();
        }
    }

    [Server]
    public override void Server_Stop()
    {
        if (!isExecuting) return;

        isExecuting = false;
        damageDealt = false;

        if (movement != null) movement.Server_SetMovementEnabled(true);
        if (animator != null) animator.ResetTrigger(AttackTrigger);

        // 1. I-reset ang BossController (Gawa na natin 'to)
        if (bossController != null) bossController.Server_EndAttack();

        // 2. ETO ANG BAGO: I-reset ang Attack Manager
        // Para malaman ng manager na pwede na siyang pumili ng bagong attack
        if (TryGetComponent<BossAttackManager>(out var attackManager))
        {
            attackManager.Server_OnAttackAnimationComplete();
        }

        Debug.Log("<color=cyan>[TidalBite]</color> Attack Finished and Manager Notified.");
    }

    // Para mawala ang CS0534 Error
    public override void Server_OnAnimationEvent(string eventName)
    {
        if (eventName.Contains("Damage") && !damageDealt)
        {
            DealBiteDamage();
        }
    }

    private IEnumerator LungeRoutine()
    {
        float timer = 0;
        while (timer < lungeDuration)
        {
            transform.position += transform.forward * lungeForce * Time.deltaTime;
            timer += Time.deltaTime;
            yield return null;
        }
    }

    [Server]
    private void DealBiteDamage()
    {
        if (damageDealt) return;

        // Kunin ang CURRENT position (mahalaga dahil sa lunge)
        Vector3 basePos = mouthPoint != null ? mouthPoint.position : transform.position + (transform.forward * 2.0f);
        Vector3 aoeCenter = basePos + (transform.forward * aoeOffsetForward);

        // Scan for player
        Collider[] hits = Physics.OverlapSphere(aoeCenter, aoeRadius, playerLayer);

        // Debug Log para makita kung may nasasagap ba ang sphere
        Debug.Log($"<color=orange>[TidalBite]</color> Sphere Check: Found {hits.Length} objects in Layer.");

        foreach (Collider h in hits)
        {
            if (h.transform == transform) continue;

            var stats = h.GetComponent<PlayerStatsManager>();
            if (stats == null) stats = h.GetComponentInParent<PlayerStatsManager>();

            if (stats != null)
            {
                damageDealt = true;
                stats.TakeDamage(damage, transform);
                Debug.Log("<color=green>[TidalBite]</color> SUCCESS! Damaged: " + h.name);
            }
        }
    }

    // Visual helper sa Scene View
    private void OnDrawGizmosSelected()
    {
        Vector3 basePos = mouthPoint != null ? mouthPoint.position : transform.position + (transform.forward * 2.0f);
        Vector3 aoeCenter = basePos + (transform.forward * aoeOffsetForward);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(aoeCenter, aoeRadius);
    }
}