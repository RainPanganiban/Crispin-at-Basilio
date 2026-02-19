using UnityEngine;
using Mirror;

public class BossController : NetworkBehaviour
{
    [Header("Core refs (auto-wired if left empty)")]
    [SerializeField] private BossHealth health;
    [SerializeField] private BossPhaseManager phaseManager;
    [SerializeField] private BossAttackManager attackManager;
    [SerializeField] private BossAnimationRelay animationRelay;
    [SerializeField] private BossMovementBase movement;
    [SerializeField] private Animator animator;

    [Header("Tick")]
    public float thinkInterval = 0.2f;

    [SyncVar(hook = nameof(OnStateChanged))]
    private BossState state = BossState.Idle;

    public BossState State => state;

    private float nextThinkTime;
    private BaseAttack currentAttack;

    void Awake()
    {
        if (health == null) health = GetComponent<BossHealth>();
        if (phaseManager == null) phaseManager = GetComponent<BossPhaseManager>();
        if (attackManager == null) attackManager = GetComponent<BossAttackManager>();
        if (animationRelay == null) animationRelay = GetComponent<BossAnimationRelay>();
        if (movement == null) movement = GetComponent<BossMovementBase>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    public override void OnStartServer()
    {
        state = BossState.Idle;
        nextThinkTime = Time.time + thinkInterval;
    }

    [ServerCallback]
    void Update()
    {
        if (state == BossState.Dead)
            return;

        if (Time.time < nextThinkTime)
            return;

        nextThinkTime = Time.time + thinkInterval;

        if (state == BossState.Idle && attackManager != null)
            attackManager.Server_TrySelectAndStartAttack();
    }

    void OnStateChanged(BossState oldValue, BossState newValue)
    {
        // Hook for UI/VFX later (intentionally empty in MVP).
    }

    // ---- Server-only state transitions ----

    [Server]
    public void Server_BeginAttack(BaseAttack attack)
    {
        if (state != BossState.Idle)
            return;

        if (attack == null)
            return;

        currentAttack = attack;
        state = BossState.Attacking;

        if (movement != null && attack.requiresMovementLock)
            movement.Server_SetMovementEnabled(false);

        Server_PlayTrigger(attack.animationTriggerName);
        attack.Server_Execute();
    }

    [Server]
    public void Server_EndAttack()
    {
        if (state != BossState.Attacking)
            return;

        if (movement != null)
            movement.Server_SetMovementEnabled(true);

        currentAttack = null;
        state = BossState.Idle;
    }

    [Server]
    public void Server_BeginPhaseTransition()
    {
        if (state == BossState.Dead)
            return;

        if (attackManager != null)
            attackManager.Server_StopCurrentAttack();

        if (movement != null)
            movement.Server_SetMovementEnabled(false);

        state = BossState.Transitioning;
    }

    [Server]
    public void Server_EndPhaseTransition()
    {
        if (state != BossState.Transitioning)
            return;

        if (movement != null)
            movement.Server_SetMovementEnabled(true);

        state = BossState.Idle;
    }

    [Server]
    public void Server_Die()
    {
        if (state == BossState.Dead)
            return;

        if (attackManager != null)
            attackManager.Server_StopCurrentAttack();

        if (movement != null)
            movement.Server_SetMovementEnabled(false);

        state = BossState.Dead;

        Server_PlayTrigger("Die");
        // Collider disable / rewards / NetworkServer.Destroy should be handled by a dedicated death handler later.
    }

    // ---- Animation triggering (server authoritative) ----

    [Server]
    public void Server_PlayTrigger(string triggerName)
    {
        if (string.IsNullOrWhiteSpace(triggerName))
            return;

        if (animator != null)
            animator.SetTrigger(triggerName);

        Rpc_PlayTrigger(triggerName);
    }

    [ClientRpc]
    void Rpc_PlayTrigger(string triggerName)
    {
        if (isServer)
            return; // server already played it locally

        if (animator != null)
            animator.SetTrigger(triggerName);
    }
}

