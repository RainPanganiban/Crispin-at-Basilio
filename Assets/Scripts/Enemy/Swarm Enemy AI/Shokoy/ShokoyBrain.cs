using UnityEngine;
using UnityEngine.AI;
using Mirror;

public class ShokoyBrain : EnemyBrain
{
    [Header("Shokoy Ranged Settings")]
    public float minRetreatDistance = 5f;
    public float maxChaseDistance = 12f;
    public float repositionCooldown = 2f;

    private float nextRepositionTime;
    private bool isAttacking;

    void Start()
    {
        if (!isServer) return;

        if (aggroSystem == null) aggroSystem = GetComponent<EnemyAggro>();
        if (attackController == null) attackController = GetComponent<EnemyAttackController>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.updateRotation = false;
            // Default stopping distance for ranged should be higher
            agent.stoppingDistance = 2f;
        }
    }

    void Update()
    {
        if (!isServer || currentState == EnemyState.Dead) return;

        UpdateTarget();
        if (currentTarget == null) return;

        if (isAttacking)
        {
            FaceTarget();
            return;
        }

        HandleRangedLogic();
    }

    void HandleRangedLogic()
    {
        float distance = Vector3.Distance(transform.position, currentTarget.position);

        if (distance < minRetreatDistance)
        {
            // Too close, back away
            BackOffFromPlayer();
        }
        else if (distance > attackRange)
        {
            // Too far, chase until in range
            ChasePlayer();
        }
        else
        {
            // In sweet spot
            if (attackController.CanAttack())
            {
                StartAttack();
            }
            else if (Time.time >= nextRepositionTime)
            {
                // Strafe while waiting for cooldown
                StrafeAroundPlayer();
                nextRepositionTime = Time.time + repositionCooldown;
            }
            else
            {
                FaceTarget();
                if (!agent.isStopped) agent.isStopped = true;
            }
        }
    }

    void BackOffFromPlayer()
    {
        FaceTarget();
        Vector3 dirAway = (transform.position - currentTarget.position).normalized;
        Vector3 targetPos = transform.position + dirAway * 3f;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPos, out hit, 3f, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.SetDestination(hit.position);
        }
    }

    void ChasePlayer()
    {
        FaceTarget();
        agent.isStopped = false;
        Vector3 surroundPos = aggroSystem.GetSurroundPosition(attackRange * 0.8f);
        agent.SetDestination(surroundPos);
    }

    void StrafeAroundPlayer()
    {
        FaceTarget();
        Vector3 toPlayer = (currentTarget.position - transform.position).normalized;
        Vector3 strafeDir = Vector3.Cross(Vector3.up, toPlayer).normalized;
        
        if (Random.value > 0.5f) strafeDir *= -1f;

        Vector3 targetPos = transform.position + strafeDir * 4f;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPos, out hit, 4f, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.SetDestination(hit.position);
        }
    }

    void StartAttack()
    {
        isAttacking = true;
        agent.isStopped = true;
        agent.ResetPath();
        FaceTarget();
        attackController.ExecuteRandomAttack();
    }

    [Server]
    public override void OnAttackFinished()
    {
        isAttacking = false;
        nextRepositionTime = Time.time + 0.5f;
    }

    void FaceTarget()
    {
        if (currentTarget == null) return;
        Vector3 direction = (currentTarget.position - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f);
        }
    }
}
