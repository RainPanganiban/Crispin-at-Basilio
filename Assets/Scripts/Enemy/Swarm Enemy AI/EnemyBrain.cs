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

    [Header("References")]
    public NavMeshAgent agent;
    public Animator animator;
    public EnemyAggro aggroSystem;
    public EnemyAttackController attackController;

    [Header("Distance Settings")]
    public float attackRange = 2f;
    public float repositionDistance = 1.5f;

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
            agent.isStopped = true;
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
                // Wait for animation event to exit attack state
                break;

            case EnemyState.Repositioning:
                HandleReposition();
                break;
        }
    }

    void HandleChase(float distance)
    {
        if (currentTarget == null)
            return;

        FaceTarget();

        if (distance > attackRange)
        {
            agent.isStopped = false;

            Vector3 dir = (transform.position - currentTarget.position).normalized;
            Vector3 targetPoint = currentTarget.position + dir * attackRange;

            agent.SetDestination(targetPoint);
        }
        else
        {
            agent.ResetPath(); // stop micro pushing

            if (attackController.CanAttack())
            {
                StartAttack();
            }
        }
    }

    void HandleReposition()
    {
        if (currentTarget == null)
        {
            currentState = EnemyState.Chasing;
            return;
        }

        FaceTarget();

        if (!agent.pathPending && agent.remainingDistance <= 0.2f)
        {
            Debug.Log("Reposition Complete → Chasing");

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
        consecutiveAttacks++;

        if (consecutiveAttacks >= attacksBeforeReposition)
        {
            consecutiveAttacks = 0;
            StartReposition();
        }
        else
        {
            currentState = EnemyState.Chasing;
        }
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
