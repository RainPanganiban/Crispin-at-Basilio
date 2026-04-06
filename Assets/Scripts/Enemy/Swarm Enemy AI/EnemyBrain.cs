using UnityEngine;
using UnityEngine.AI;
using Mirror;

public class EnemyBrain : NetworkBehaviour
{
    public enum EnemyState
    {
        Idle,
        Chasing,
        Attacking,
        Repositioning,
        Dead
    }

    protected enum CombatPhase
    {
        Positioning,
        Attacking,
        Strafing
    }

    [Header("References")]
    public NavMeshAgent agent;
    public EnemyAggro aggroSystem;
    public EnemyAttackController attackController;

    [Header("Distance Settings")]
    public float attackRange = 2f;        // distance to start attack
    public float combatDistance = 2.5f;   // distance to start combat phase
    public float backOffDistance = 3f;     // distance to step back after attack
    public float repositionTimeout = 2f;  // max time in Repositioning before giving up

    [Header("Strafing Settings")]
    public float strafeDurationMin = 1f;
    public float strafeDurationMax = 2f;
    public float strafeDistance = 2f;

    [Header("Attack Settings")]
    public int attacksBeforeReposition = 2;

    protected EnemyState currentState;
    protected CombatPhase combatPhase;

    protected Transform currentTarget;
    private int consecutiveAttacks;

    private Vector3 strafeDirection;
    private float strafeTimer;
    private float repositionStartTime;

    void Start()
    {
        if (!isServer) return;
        currentState = EnemyState.Idle;

        // Sync stopping distance with attack range so we don't overshoot when closing in
        if (agent != null)
        {
            agent.stoppingDistance = Mathf.Min(attackRange * 0.4f, 0.6f);
            agent.updateRotation = false; // We handle rotation manually in FaceTarget()
        }
    }

    void Update()
    {
        if (!isServer || currentState == EnemyState.Dead) return;

        UpdateTarget();
        HandleState();
    }

    protected void UpdateTarget()
    {
        currentTarget = aggroSystem.GetCurrentTarget();

        // FIX: Kung may target, i-check kung patay na ito sa PlayerStatsManager
        if (currentTarget != null)
        {
            PlayerStatsManager targetStats = currentTarget.GetComponent<PlayerStatsManager>();
            if (targetStats != null && targetStats.IsDead)
            {
                currentTarget = null; // Bitawan ang target para huminto ang enemy
            }
        }

        if (currentTarget == null)
        {
            currentState = EnemyState.Idle;
            if (agent.hasPath) agent.ResetPath();
        }
    }

    void HandleState()
    {
        if (currentTarget == null) return;

        float distance = Vector3.Distance(transform.position, currentTarget.position);

        switch (currentState)
        {
            case EnemyState.Idle:
                currentState = EnemyState.Chasing;
                break;

            case EnemyState.Chasing:
                HandleChase(distance);
                break;

            case EnemyState.Attacking:
                // Agent is stopped during attack; handled in HandleCombat
                break;

            case EnemyState.Repositioning:
                HandleReposition();
                break;
        }

        // Combat logic triggers automatically when within combatDistance
        if (currentState == EnemyState.Chasing && distance <= combatDistance)
        {
            currentState = EnemyState.Attacking;
            combatPhase = CombatPhase.Positioning;
        }

        if (currentState == EnemyState.Attacking)
            HandleCombat();
    }

    void HandleChase(float distance)
    {
        FaceTarget();

        if (distance > attackRange)
        {
            agent.isStopped = false;
            Vector3 surroundPos = aggroSystem.GetSurroundPosition(attackRange);
            agent.SetDestination(surroundPos);
        }
        else
        {
            agent.ResetPath();
            currentState = EnemyState.Attacking;
            combatPhase = CombatPhase.Positioning;
        }
    }

    void HandleCombat()
    {
        FaceTarget();
        float distance = Vector3.Distance(transform.position, currentTarget.position);

        // Too far → go back to chasing
        if (distance > combatDistance)
        {
            currentState = EnemyState.Chasing;
            return;
        }

        switch (combatPhase)
        {
            case CombatPhase.Positioning:
                HandlePositioning(distance);
                break;

            case CombatPhase.Attacking:
                HandleAttacking();
                break;

            case CombatPhase.Strafing:
                HandleStrafing();
                break;
        }
    }

    void HandlePositioning(float distance)
    {
        if (distance > attackRange)
        {
            Vector3 surroundPos = aggroSystem.GetSurroundPosition(attackRange);
            agent.isStopped = false;
            agent.SetDestination(surroundPos);
        }
        else
        {
            agent.ResetPath();
            combatPhase = CombatPhase.Attacking;
        }
    }

    void HandleAttacking()
    {
        agent.ResetPath();

        if (attackController.CanAttack())
        {
            StartAttack();
            return;
        }

        // If attack is on cooldown → begin strafing
        if (combatPhase != CombatPhase.Strafing)
        {
            BeginStrafe();
            combatPhase = CombatPhase.Strafing;
        }
    }

    void BeginStrafe()
    {
        Vector3 toPlayer = (currentTarget.position - transform.position).normalized;
        strafeDirection = Vector3.Cross(Vector3.up, toPlayer).normalized;

        if (Random.value > 0.5f)
            strafeDirection *= -1f;

        strafeTimer = Random.Range(strafeDurationMin, strafeDurationMax);
    }

    void HandleStrafing()
    {
        strafeTimer -= Time.deltaTime;

        agent.isStopped = false;
        Vector3 targetPos = transform.position + strafeDirection * strafeDistance;
        agent.SetDestination(targetPos);

        if (strafeTimer <= 0f)
        {
            combatPhase = CombatPhase.Positioning;
        }
    }

    void HandleReposition()
    {
        bool arrived = !agent.pathPending && agent.remainingDistance <= 0.2f;
        bool timeout = Time.time >= repositionStartTime + repositionTimeout;

        if (arrived || timeout)
        {
            currentState = EnemyState.Chasing;
        }
    }

    void StartAttack()
    {
        Debug.Log("Starting Attack");
        currentState = EnemyState.Attacking;
        agent.isStopped = true;
        agent.ResetPath();
        FaceTarget();
        attackController.ExecuteRandomAttack();
    }

    [Server]
    public virtual void OnAttackFinished()
    {
        consecutiveAttacks++;

        if (consecutiveAttacks >= attacksBeforeReposition)
        {
            consecutiveAttacks = 0;
            StartBackOff();
        }
        else
        {
            combatPhase = CombatPhase.Strafing;
        }
    }

    void StartBackOff()
    {
        currentState = EnemyState.Repositioning;
        repositionStartTime = Time.time;

        Vector3 dirAway = (transform.position - currentTarget.position).normalized;
        dirAway.y = 0;
        if (dirAway.sqrMagnitude < 0.001f)
            dirAway = -transform.forward;

        // Add slight perpendicular offset so swarm doesn't all back off in same line
        Vector3 perp = Vector3.Cross(Vector3.up, dirAway).normalized;
        perp *= (Random.value - 0.5f) * 1.5f;
        Vector3 dirWithJitter = (dirAway + perp).normalized;

        Vector3 idealBackPoint = transform.position + dirWithJitter * backOffDistance;

        // Find valid NavMesh position (avoids backing through walls / off mesh)
        NavMeshHit hit;
        Vector3 backPoint = idealBackPoint;
        if (NavMesh.SamplePosition(idealBackPoint, out hit, backOffDistance * 0.5f, NavMesh.AllAreas))
            backPoint = hit.position;

        agent.isStopped = false;
        agent.SetDestination(backPoint);
    }

    void FaceTarget()
    {
        Vector3 direction = (currentTarget.position - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f);
        }
    }

    [Server]
    public virtual void Die()
    {
        currentState = EnemyState.Dead;
        agent.isStopped = true;
    }
}
