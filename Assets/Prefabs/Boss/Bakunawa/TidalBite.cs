using UnityEngine;
using Mirror;
using System.Collections;

public class TidalBite : BaseAttack
{
    public const string Event_BiteSnap = "BiteSnap"; // Event name sa Animation tab

    [Header("Bite Settings")]
    public float damage = 50f;
    public float attackFullDuration = 1.2f;
    public Transform mouthPoint;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip biteSFX; // Ang malakas na tunog ng kagat

    [Header("AOE Settings")]
    public float aoeRadius = 8.0f;
    public float aoeOffsetForward = 3.0f;
    public LayerMask playerLayer;

    private Animator animator;
    private BakunawaMovement movement;
    private BossController bossController;
    private EnemySoundManager soundManager;

    private bool isExecuting = false;
    private bool damageDealt = false;
    private float attackStartTime;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        movement = GetComponent<BakunawaMovement>();
        bossController = GetComponent<BossController>();
        soundManager = GetComponent<EnemySoundManager>();
    }

    public override void Initialize(BossController bossController)
    {
        base.Initialize(bossController);
        // Sinisiguro na ang references ay laging up to date
        this.bossController = bossController;
        this.soundManager = bossController.GetComponent<EnemySoundManager>();
    }

    [Server]
    public override void Server_Execute()
    {
        if (boss != null)
        {
            isExecuting = true;
            damageDealt = false;
            attackStartTime = Time.time;

            // I-play ang animation (dapat may "BiteSnap" event itong animation na ito)
            boss.Server_PlayTrigger("TidalBite");

            // Disable movement habang kumakagat
            if (movement != null) movement.Server_SetMovementEnabled(false);
        }
    }

    public override void Server_OnAnimationEvent(string eventName)
    {
        if (!isServer) return;

        // Dito papasok ang sound at damage sabay sa animation event
        if (eventName == Event_BiteSnap || eventName.Contains("Damage"))
        {
            if (!damageDealt)
            {
                DealBiteDamage();
                RpcPlayBiteSFX(); // Sabay ang tunog sa kagat
            }
        }
    }

    [ClientRpc]
    private void RpcPlayBiteSFX()
    {
        // Gagamitin ang soundManager ng Bakunawa para sa 3D sound
        if (soundManager != null && biteSFX != null)
        {
            soundManager.PlaySpecificAttack(biteSFX);
        }
    }

    [Server]
    private void DealBiteDamage()
    {
        if (damageDealt) return;

        Vector3 basePos = mouthPoint != null ? mouthPoint.position : transform.position + (transform.forward * 2.0f);
        Vector3 aoeCenter = basePos + (transform.forward * aoeOffsetForward);

        Collider[] hits = Physics.OverlapSphere(aoeCenter, aoeRadius, playerLayer);
        foreach (Collider h in hits)
        {
            if (h.transform == transform) continue;

            if (h.TryGetComponent<IDamageable>(out var dmg))
            {
                damageDealt = true;
                dmg.TakeDamage(damage, transform);
            }
        }
    }

    [ServerCallback]
    void Update()
    {
        if (!isExecuting) return;

        // Fail-safe para ibalik ang movement kung sakaling hindi nag-trigger ang event
        if (Time.time - attackStartTime >= attackFullDuration)
        {
            Server_Stop();
        }
    }

    [Server]
    public override void Server_Stop()
    {
        if (!isExecuting) return;
        isExecuting = false;

        if (movement != null) movement.Server_SetMovementEnabled(true);
        if (bossController != null) bossController.Server_EndAttack();

        // I-notify ang manager na tapos na ang attack
        if (TryGetComponent<BossAttackManager>(out var attackManager))
        {
            attackManager.Server_OnAttackAnimationComplete();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 basePos = mouthPoint != null ? mouthPoint.position : transform.position + (transform.forward * 2.0f);
        Vector3 aoeCenter = basePos + (transform.forward * aoeOffsetForward);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(aoeCenter, aoeRadius);
    }
}