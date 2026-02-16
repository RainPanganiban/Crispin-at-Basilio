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
        Dead,
        Combat
    }

    [Header("References")]
    public NavMeshAgent agent;
    public Animator animator;
    public EnemyAggro aggroSystem;
    public EnemyAttackController attackController;

    [Header("Distance Settings")]
    public float attackRange = 2f;
    public float repositionDistance = 1.5f;

    [Header("Combat Settings")]
    public float combatDistance = 2.5f;
    public float strafeSpeed = 2f;
    public float strafeDuration = 1.5f;
    public float backOffDistance = 3f;

    float strafeTimer;
    int strafeDirection;

    [Header("Attack Settings")]
    int consecutiveAttacks = 0;
    public int attacksBeforeReposition = 2;

    public EnemyState currentState;

    private Transform currentTarget;
    private bool isRepositioning = false;

    void Start()
    {
        if (!isServer) return;

        currentState = EnemyState.Idle;
    }

    void Update()
    {
        if (!isServer) return;
        if (currentState == EnemyState.Dead) return;
        if (currentState == EnemyState.Attacking && !agent.isStopped)
        {
            currentState = EnemyState.Chasing;
        }

        Debug.Log("Stopped: " + agent.isStopped +
          " | HasPath: " + agent.hasPath +
          " | Velocity: " + agent.velocity +
          " | Remaining: " + agent.remainingDistance);

        Debug.Log("Current State: " + currentState);

        UpdateTarget();
        HandleState();
    }

    void UpdateTarget()
    {
        currentTarget = aggroSystem.GetCurrentTarget();
    }

    void HandleState()
    {
        if (currentTarget == null)
        {
            currentState = EnemyState.Idle;
            agent.ResetPath();
            return;
        }

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
    }

    void HandleChase(float distance)
    {
        FaceTarget();

        if (distance > attackRange)
        {
            agent.isStopped = false;
            agent.SetDestination(currentTarget.position);
        }
        else
        {
            agent.ResetPath();
            StartAttack();
        }
    }


    void HandleReposition()
    {
        if (!agent.pathPending && agent.remainingDistance <= 0.2f)
        {
            currentState = EnemyState.Chasing;
        }
    }

    void HandleCombat()
    {
        if (currentTarget == null)
            return;

        FaceTarget();

        float distance = Vector3.Distance(transform.position, currentTarget.position);
        Debug.Log("Distance: " + distance);

        // Too far → chase
        if (distance > combatDistance)
        {
            currentState = EnemyState.Chasing;
            return;
        }

        // Slightly outside attack range → step inward
        if (distance > attackRange)
        {
            Vector3 dir = (transform.position - currentTarget.position).normalized;
            Vector3 attackPoint = currentTarget.position + dir * attackRange;

            agent.SetDestination(attackPoint);
            return;
        }

        // Inside attack range
        agent.ResetPath();

        if (attackController.CanAttack())
        {
            StartAttack();
            return;
        }

        // After attacking cooldown → strafe
        if (!agent.hasPath)
        {
            Vector3 strafeDir = (Random.value > 0.5f ? transform.right : -transform.right);
            Vector3 strafeTarget = transform.position + strafeDir * 2f;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(strafeTarget, out hit, 2f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
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

    void StartBackOff()
    {
        currentState = EnemyState.Repositioning;

        Vector3 dirAway = (transform.position - currentTarget.position).normalized;
        Vector3 backPoint = transform.position + dirAway * 2.5f;

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
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                lookRotation,
                Time.deltaTime * 10f
            );
        }
    }

    [Server]
    public void OnAttackFinished()
    {
        StartBackOff();
    }

    [Server]
    void StartReposition()
    {
        if (currentTarget == null)
        {
            currentState = EnemyState.Chasing;
            return;
        }

        currentState = EnemyState.Repositioning;

        agent.isStopped = false;
        agent.ResetPath();

        Vector3 directionAway = (transform.position - currentTarget.position).normalized;
        Vector3 rawPoint = transform.position + directionAway * repositionDistance;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(rawPoint, out hit, 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            Debug.Log("Valid NavMesh reposition set.");
        }
        else
        {
            Debug.Log("No valid NavMesh point found. Switching to chase.");
            currentState = EnemyState.Chasing;
        }
    }

    [Server]
    public void Die()
    {
        currentState = EnemyState.Dead;
        agent.isStopped = true;
    }
}
