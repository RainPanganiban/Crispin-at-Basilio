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
    private float lastAttackStartTime;
    private const float AttackTimeout = 10f; // Seconds before we force-reset

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
        if (state == BossState.Dead || state == BossState.Stunned)
            return;

        if (Time.time < nextThinkTime)
            return;

        nextThinkTime = Time.time + thinkInterval;

        if (state == BossState.Idle && attackManager != null)
        {
            attackManager.Server_TrySelectAndStartAttack();
        }
        else if (state == BossState.Attacking)
        {
            // Failsafe: If stuck in Attacking state too long, reset to Idle.
            if (Time.time > lastAttackStartTime + AttackTimeout)
            {
                Debug.LogWarning($"[BossController][{gameObject.name}] Attack timeout! Forcing reset from {currentAttack?.attackName ?? "Unknown"}.");
                Server_EndAttack();
            }
            else if (Time.frameCount % 120 == 0)
            {
                Debug.Log($"[BossController][{gameObject.name}] Current state: {state}. Waiting to return to Idle.");
            }
        }
        else if (state != BossState.Idle && Time.frameCount % 60 == 0)
        {
            Debug.Log($"[BossController] Current state: {state}. Waiting to return to Idle.");
        }
    }

    void OnStateChanged(BossState oldValue, BossState newValue)
    {
        // Clients and Server react to state changes.
        if (newValue == BossState.Dead)
        {
            // Stop logic on clients (VFX/SFX).
        }

        if (isServer)
            Rpc_OnStateChanged(oldValue, newValue);
    }

    [ClientRpc]
    void Rpc_OnStateChanged(BossState oldValue, BossState newValue)
    {
        if (isServer) return; // Hook already fired on server.
        // Hook for client-only state reactions.
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
        lastAttackStartTime = Time.time;

        if (movement != null && attack.requiresMovementLock)
            movement.Server_SetMovementEnabled(false);

        Server_PlayTrigger(attack.animationTriggerName);
        attack.Server_Execute();
    }

    [Server]
    public void Server_EndAttack()
    {
        Debug.Log($"[BossController][{gameObject.name}] Server_EndAttack called. current state: {state}");

        if (state != BossState.Attacking)
        {
            // No warning here, just return silently to avoid log spam from legitimate double-calls (Relay + Timeout)
            return;
        }

        if (movement != null)
            movement.Server_SetMovementEnabled(true);

        currentAttack = null;
        state = BossState.Idle;
        Debug.Log($"[BossController][{gameObject.name}] State reverted to Idle.");
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
    public void Server_Stun()
    {
        if (state == BossState.Dead)
            return;

        if (attackManager != null)
            attackManager.Server_StopCurrentAttack();

        if (movement != null)
            movement.Server_SetMovementEnabled(false);

        currentAttack = null;
        state = BossState.Stunned;
    }

    [Server]
    public void Server_EndStun()
    {
        if (state != BossState.Stunned)
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
        
        // Notify death handler if one exists
        if (TryGetComponent<BossDeathHandler>(out var deathHandler))
            deathHandler.Server_OnBossDeath();
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

