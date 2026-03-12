using UnityEngine;
using UnityEngine.AI;
using Mirror;

public class BalbalBrain : EnemyBrain
{
    public enum BalbalState
    {
        Chasing,
        Repositioning,
        Dead
    }

    [Header("Balbal Fast Melee Settings")]
    public float repositionTime = 0.5f;     
    public float repositionDistance = 2f;

    private BalbalState currentBalbalState;
    private Transform target;
    private float stateTimer;
    private bool isAttacking;

    void Start()
    {
        if (!isServer) return;

        if (aggroSystem == null) aggroSystem = GetComponent<EnemyAggro>();
        if (attackController == null) attackController = GetComponent<EnemyAttackController>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        
        currentBalbalState = BalbalState.Chasing;
        
        if (agent != null)
        {
            agent.stoppingDistance = attackRange * 0.5f; 
            agent.updateRotation = false; 
        }
    }

    void Update()
    {
        if (!isServer || currentBalbalState == BalbalState.Dead) return;

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
            FaceTarget();
            return; 
        }

        switch (currentBalbalState)
        {
            case BalbalState.Chasing:
                HandleChasing();
                break;
            case BalbalState.Repositioning:
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
            
            Vector3 surroundPos = aggroSystem.GetSurroundPosition(attackRange);
            agent.SetDestination(surroundPos);
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
                StartReposition();
            }
        }
    }

    void TryTriggerAttack()
    {
        isAttacking = true;
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
        currentBalbalState = BalbalState.Repositioning;
        stateTimer = repositionTime;

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
            currentBalbalState = BalbalState.Chasing;
        }
    }

    void FaceTarget()
    {
        if (target == null) return;
        Vector3 dir = (target.position - transform.position).normalized;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 15f); 
        }
    }

    [Server]
    public override void Die()
    {
        base.Die();
        currentBalbalState = BalbalState.Dead;
    }
}
