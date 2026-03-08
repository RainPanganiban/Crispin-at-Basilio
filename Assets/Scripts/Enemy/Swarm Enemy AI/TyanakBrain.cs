using UnityEngine;
using UnityEngine.AI;
using Mirror;

public class TyanakBrain : EnemyBrain
{
    public enum TyanakState
    {
        Chasing,
        Repositioning,
        Dead
    }

    [Header("Tyanak Fast Melee Settings")]
    public float repositionTime = 0.5f;     
    public float repositionDistance = 2f;
    [Tooltip("Distance threshold to prefer Leap over Claw")]
    public float leapThreshold = 3f;

    private TyanakState currentTyanakState;
    private Transform target;
    private float stateTimer;
    private bool isAttacking;

    void Start()
    {
        if (!isServer) return;
        if (aggroSystem == null) aggroSystem = GetComponent<EnemyAggro>();
        if (attackController == null) attackController = GetComponent<EnemyAttackController>();
        
        currentTyanakState = TyanakState.Chasing;
        
        if (agent != null)
        {
            // Close range for fast melee
            agent.stoppingDistance = attackRange * 0.5f; 
            agent.updateRotation = false; 
        }
    }

    void Update()
    {
        if (!isServer || currentTyanakState == TyanakState.Dead) return;

        target = aggroSystem != null ? aggroSystem.GetCurrentTarget() : null;
        
        if (target == null)
        {
            if (agent != null && agent.isOnNavMesh) agent.ResetPath();
            return;
        }

        if (isAttacking)
        {
            return; 
        }

        switch (currentTyanakState)
        {
            case TyanakState.Chasing:
                HandleChasing();
                break;
            case TyanakState.Repositioning:
                HandleRepositioning();
                break;
        }
    }

    void HandleChasing()
    {
        FaceTarget();
        float distance = Vector3.Distance(transform.position, target.position);

        if (distance > attackRange)
        {
            agent.isStopped = false;
            
            // Fast chase straight at target (swarm surround)
            Vector3 surroundPos = aggroSystem.GetSurroundPosition(attackRange);
            agent.SetDestination(surroundPos);
            
            // Try leap attack if in range
            if (distance <= leapThreshold && attackController.CanAttack())
            {
                TryTriggerAttack();
            }
        }
        else
        {
            agent.isStopped = true;
            if (agent.isOnNavMesh) agent.ResetPath();
            
            if (attackController.CanAttack())
            {
                TryTriggerAttack();
            }
            else
            {
                // Dance around player waiting for cooldown
                StartReposition();
            }
        }
    }

    void TryTriggerAttack()
    {
        isAttacking = true;
        
        // Tyanak prefers leap when far, claw when close. 
        // Relies on EnemyAttackController picking an available attack randomly.
        attackController.ExecuteRandomAttack();
    }

    [Server]
    public override void OnAttackFinished()
    {
        isAttacking = false;
        StartReposition();
    }

    void StartReposition()
    {
        currentTyanakState = TyanakState.Repositioning;
        stateTimer = repositionTime;

        // Quick dart to the side
        Vector3 toPlayer = target != null ? (target.position - transform.position).normalized : transform.forward;
        toPlayer.y = 0;
        
        Vector3 perp = Vector3.Cross(Vector3.up, toPlayer).normalized;
        if (Random.value > 0.5f) perp *= -1f;

        Vector3 idealPoint = transform.position + perp * repositionDistance;
        
        NavMeshHit hit;
        Vector3 backPoint = idealPoint;
        if (NavMesh.SamplePosition(idealPoint, out hit, repositionDistance * 0.5f, NavMesh.AllAreas))
        {
            backPoint = hit.position;
        }

        agent.isStopped = false;
        agent.SetDestination(backPoint);
    }

    void HandleRepositioning()
    {
        stateTimer -= Time.deltaTime;
        FaceTarget();

        // Continue tracking
        Vector3 toPlayer = target != null ? (target.position - transform.position).normalized : transform.forward;
        toPlayer.y = 0;
        Vector3 perp = Vector3.Cross(Vector3.up, toPlayer).normalized;
        if (Vector3.Dot(perp, agent.velocity) < 0) perp *= -1f;
        
        Vector3 idealPoint = transform.position + perp * repositionDistance;
        
        if (agent.isOnNavMesh)
        {
             agent.SetDestination(idealPoint);
        }

        if (stateTimer <= 0f)
        {
            currentTyanakState = TyanakState.Chasing;
        }
    }

    void FaceTarget()
    {
        if (target == null) return;
        Vector3 dir = (target.position - transform.position).normalized;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.001f)
        {
            // Fast turning for fast enemy
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 15f); 
        }
    }

    [Server]
    public override void Die()
    {
        base.Die();
        currentTyanakState = TyanakState.Dead;
    }
}
