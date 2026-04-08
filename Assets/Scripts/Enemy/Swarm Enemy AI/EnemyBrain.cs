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
    public float attackRange = 2f;
    public float combatDistance = 2.5f;
    public float backOffDistance = 3f;
    public float repositionTimeout = 2f;

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

        if (agent != null)
        {
            agent.stoppingDistance = Mathf.Min(attackRange * 0.4f, 0.6f);
            agent.updateRotation = false;
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
        if (aggroSystem == null) return;

        currentTarget = aggroSystem.GetCurrentTarget();

        if (currentTarget != null)
        {
            PlayerStatsManager targetStats = currentTarget.GetComponent<PlayerStatsManager>();
            if (targetStats != null && targetStats.IsDead)
            {
                currentTarget = null;
            }
        }

        if (currentTarget == null)
        {
            currentState = EnemyState.Idle;
            if (agent != null && agent.hasPath) agent.ResetPath();
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
                break;

            case EnemyState.Repositioning:
                HandleReposition();
                break;
        }

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
            if (agent != null)
            {
                agent.isStopped = false;
                Vector3 surroundPos = aggroSystem.GetSurroundPosition(attackRange);
                agent.SetDestination(surroundPos);
            }
        }
        else
        {
            if (agent != null) agent.ResetPath();
            currentState = EnemyState.Attacking;
            combatPhase = CombatPhase.Positioning;
        }
    }

    void HandleCombat()
    {
        FaceTarget();

        if (currentTarget == null) return;
        float distance = Vector3.Distance(transform.position, currentTarget.position);

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
            if (agent != null)
            {
                agent.isStopped = false;
                agent.SetDestination(surroundPos);
            }
        }
        else
        {
            if (agent != null) agent.ResetPath();
            combatPhase = CombatPhase.Attacking;
        }
    }

    void HandleAttacking()
    {
        if (agent != null) agent.ResetPath();

        if (attackController != null && attackController.CanAttack())
        {
            StartAttack();
            return;
        }

        if (combatPhase != CombatPhase.Strafing)
        {
            BeginStrafe();
            combatPhase = CombatPhase.Strafing;
        }
    }

    void BeginStrafe()
    {
        if (currentTarget == null) return;

        Vector3 toPlayer = (currentTarget.position - transform.position).normalized;
        strafeDirection = Vector3.Cross(Vector3.up, toPlayer).normalized;

        if (Random.value > 0.5f)
            strafeDirection *= -1f;

        strafeTimer = Random.Range(strafeDurationMin, strafeDurationMax);
    }

    void HandleStrafing()
    {
        strafeTimer -= Time.deltaTime;

        if (agent != null)
        {
            agent.isStopped = false;
            Vector3 targetPos = transform.position + strafeDirection * strafeDistance;
            agent.SetDestination(targetPos);
        }

        if (strafeTimer <= 0f)
        {
            combatPhase = CombatPhase.Positioning;
        }
    }

    void HandleReposition()
    {
        if (agent == null) return;

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

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        FaceTarget();
        if (attackController != null) attackController.ExecuteRandomAttack();
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
        // FIX: Proteksyon kung wala nang target sa frame na ito
        if (currentTarget == null)
        {
            currentState = EnemyState.Idle;
            return;
        }

        currentState = EnemyState.Repositioning;
        repositionStartTime = Time.time;

        Vector3 dirAway = (transform.position - currentTarget.position).normalized;
        dirAway.y = 0;

        if (dirAway.sqrMagnitude < 0.001f)
            dirAway = -transform.forward;

        Vector3 perp = Vector3.Cross(Vector3.up, dirAway).normalized;
        perp *= (Random.value - 0.5f) * 1.5f;
        Vector3 dirWithJitter = (dirAway + perp).normalized;

        Vector3 idealBackPoint = transform.position + dirWithJitter * backOffDistance;

        NavMeshHit hit;
        Vector3 backPoint = idealBackPoint;
        if (NavMesh.SamplePosition(idealBackPoint, out hit, backOffDistance * 0.5f, NavMesh.AllAreas))
            backPoint = hit.position;

        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = false;
            agent.SetDestination(backPoint);
        }
    }

    void FaceTarget()
    {
        // FIX: Proteksyon para sa FaceTarget
        if (currentTarget == null) return;

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
        if (agent != null) agent.isStopped = true;
    }
}