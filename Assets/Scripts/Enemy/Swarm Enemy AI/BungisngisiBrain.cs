using UnityEngine;
using UnityEngine.AI;
using Mirror;

public class BungisngisiBrain : EnemyBrain
{
    public enum BungisngisState
    {
        Advancing,
        Repositioning,
        Dead
    }

    [Header("Bungisngis Artilliary Settings")]
    public float repositionTime = 2f;     
    public float repositionDistance = 3f;

    private BungisngisState currentBungisngisState;
    private Transform target;
    private float stateTimer;
    private bool isAttacking;

    void Start()
    {
        if (!isServer) return;
        if (aggroSystem == null) aggroSystem = GetComponent<EnemyAggro>();
        if (attackController == null) attackController = GetComponent<EnemyAttackController>();
        
        currentBungisngisState = BungisngisState.Advancing;
        
        if (agent != null)
        {
            // Set a long stopping distance for artillery
            agent.stoppingDistance = attackRange * 0.8f;
            agent.updateRotation = false; 
        }
    }

    void Update()
    {
        if (!isServer || currentBungisngisState == BungisngisState.Dead) return;

        target = aggroSystem != null ? aggroSystem.GetCurrentTarget() : null;
        
        if (target == null)
        {
            if (agent != null && agent.isOnNavMesh) agent.ResetPath();
            return;
        }

        // If currently in the middle of executing an attack, don't move or change state
        if (isAttacking)
        {
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

        if (distance > attackRange)
        {
            agent.isStopped = false;
            
            // Move into long-range attack position
            Vector3 surroundPos = aggroSystem.GetSurroundPosition(attackRange);
            agent.SetDestination(surroundPos);
        }
        else
        {
            agent.isStopped = true;
            if (agent.isOnNavMesh) agent.ResetPath();
            
            if (attackController.CanAttack())
            {
                isAttacking = true;
                attackController.ExecuteRandomAttack();
            }
            else
            {
                // Can't attack yet, just reposition slightly
                StartReposition();
            }
        }
    }

    [Server]
    public override void OnAttackFinished()
    {
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
        if (NavMesh.SamplePosition(idealBackPoint, out hit, repositionDistance * 0.5f, NavMesh.AllAreas))
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

        // Continue tracking and adjusting reposition path if target moves
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
