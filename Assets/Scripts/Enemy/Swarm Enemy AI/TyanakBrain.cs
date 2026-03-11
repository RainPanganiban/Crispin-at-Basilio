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

        Debug.Log($"[TyanakBrain] {name} starting AI...");

        if (aggroSystem == null) aggroSystem = GetComponent<EnemyAggro>();
        if (attackController == null) attackController = GetComponent<EnemyAttackController>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        
        currentTyanakState = TyanakState.Chasing;
        
        if (agent != null)
        {
            // Close range for fast melee
            agent.stoppingDistance = attackRange * 0.5f; 
            agent.updateRotation = false; 
            Debug.Log($"[TyanakBrain] Agent configured with stopping distance {agent.stoppingDistance}");
        }
        else
        {
            Debug.LogError($"[TyanakBrain] NavMeshAgent missing on {name}!");
        }

        if (aggroSystem == null) Debug.LogError($"[TyanakBrain] EnemyAggro missing on {name}!");
        if (attackController == null) Debug.LogError($"[TyanakBrain] EnemyAttackController missing on {name}!");
    }

    void Update()
    {
        if (!isServer || currentTyanakState == TyanakState.Dead) return;

        target = aggroSystem != null ? aggroSystem.GetCurrentTarget() : null;
        
        if (target == null)
        {
            if (agent != null && agent.isOnNavMesh && !agent.isStopped) 
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
            return;
        }

        if (isAttacking)
        {
            FaceTarget(); // Still face target while mid-attack animation if applicable
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
            if (agent.isStopped) agent.isStopped = false;
            
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
            if (!agent.isStopped) agent.isStopped = true;
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
        Debug.Log($"[TyanakBrain] Triggering Attack on {target.name}");
        isAttacking = true;
        
        // Tyanak prefers leap when far, claw when close. 
        // Relies on EnemyAttackController picking an available attack randomly.
        attackController.ExecuteRandomAttack();
    }

    [Server]
    public override void OnAttackFinished()
    {
        Debug.Log("[TyanakBrain] Attack Finished");
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
        if (NavMesh.SamplePosition(idealPoint, out hit, repositionDistance, NavMesh.AllAreas))
        {
            backPoint = hit.position;
        }

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(backPoint);
        }
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
