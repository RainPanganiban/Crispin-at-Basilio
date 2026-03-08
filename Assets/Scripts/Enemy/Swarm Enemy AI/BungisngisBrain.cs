using UnityEngine;
using UnityEngine.AI;
using Mirror;

public class BungisngisBrain : EnemyBrain
{
    public enum BungisngisState
    {
        Advancing,
        Repositioning,
        Dead
    }

    [Header("Bungisngis Artillery Settings")]
    public float repositionTime = 2f;     
    public float repositionDistance = 5f;
    [Tooltip("Preferred distance to maintain from player for attacks")]
    public float preferredAttackDistance = 12f;

    private BungisngisState currentBungisngisState;
    private Transform target;
    private float stateTimer;
    private bool isAttacking;

    void Start()
    {
        if (!isServer) return;
        
        Debug.Log($"[BungisngisBrain] {name} starting AI...");

        if (aggroSystem == null) aggroSystem = GetComponent<EnemyAggro>();
        if (attackController == null) attackController = GetComponent<EnemyAttackController>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        
        currentBungisngisState = BungisngisState.Advancing;
        
        if (agent != null)
        {
            // Set a long stopping distance for artillery
            // If attackRange is too small (e.g. default 2), use preferredAttackDistance
            float effectiveRange = Mathf.Max(attackRange, preferredAttackDistance);
            agent.stoppingDistance = effectiveRange * 0.7f;
            agent.updateRotation = false; 
            
            Debug.Log($"[BungisngisBrain] Agent configured with stopping distance {agent.stoppingDistance}");
        }
        else
        {
            Debug.LogError($"[BungisngisBrain] NavMeshAgent missing on {name}!");
        }

        if (aggroSystem == null) Debug.LogError($"[BungisngisBrain] EnemyAggro missing on {name}!");
        if (attackController == null) Debug.LogError($"[BungisngisBrain] EnemyAttackController missing on {name}!");
    }

    void Update()
    {
        if (!isServer || currentBungisngisState == BungisngisState.Dead) return;

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

        // If currently in the middle of executing an attack, don't move or change state
        if (isAttacking)
        {
            FaceTarget();
            return;
        }

        switch (currentBungisngisState)
        {
            case BungisngisState.Advancing:
                HandleAdvancing();
                break;
            case BungisngisState.Repositioning:
                HandleRepositioning();
                break;
        }
    }

    void HandleAdvancing()
    {
        FaceTarget();
        float distance = Vector3.Distance(transform.position, target.position);
        float effectiveRange = Mathf.Max(attackRange, preferredAttackDistance);

        if (distance > effectiveRange)
        {
            if (agent.isStopped) agent.isStopped = false;
            
            // Move into long-range attack position
            Vector3 surroundPos = aggroSystem.GetSurroundPosition(effectiveRange);
            agent.SetDestination(surroundPos);
        }
        else
        {
            if (!agent.isStopped) agent.isStopped = true;
            if (agent.isOnNavMesh) agent.ResetPath();
            
            if (attackController.CanAttack())
            {
                Debug.Log($"[BungisngisBrain] Triggering Attack on {target.name}");
                isAttacking = true;
                attackController.ExecuteRandomAttack();
            }
            else
            {
                // Can't attack yet, just reposition slightly if we've been standing here too long
                // In Advancing state, we just wait for CD or reposition
                if (distance < effectiveRange * 0.5f) // Too close?
                {
                    StartReposition();
                }
            }
        }
    }

    [Server]
    public override void OnAttackFinished()
    {
        Debug.Log("[BungisngisBrain] Attack Finished");
        isAttacking = false;
        StartReposition();
    }

    void StartReposition()
    {
        currentBungisngisState = BungisngisState.Repositioning;
        stateTimer = repositionTime;

        // Slight lateral reposition
        Vector3 toPlayer = target != null ? (target.position - transform.position).normalized : transform.forward;
        toPlayer.y = 0;
        
        Vector3 perp = Vector3.Cross(Vector3.up, toPlayer).normalized;
        if (Random.value > 0.5f) perp *= -1f;

        Vector3 idealBackPoint = transform.position + perp * repositionDistance;
        
        NavMeshHit hit;
        Vector3 backPoint = idealBackPoint;
        if (NavMesh.SamplePosition(idealBackPoint, out hit, repositionDistance, NavMesh.AllAreas))
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

        if (stateTimer <= 0f)
        {
            currentBungisngisState = BungisngisState.Advancing;
        }
    }

    void FaceTarget()
    {
        if (target == null) return;
        Vector3 dir = (target.position - transform.position).normalized;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
        }
    }

    [Server]
    public override void Die()
    {
        base.Die();
        currentBungisngisState = BungisngisState.Dead;
    }
}
