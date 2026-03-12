using UnityEngine;
using Mirror;
using UnityEngine.AI;

public class ShokoyBubbleBurstAttack : EnemyAttack
{
    [Header("Bubble Burst Settings")]
    public GameObject bubblePrefab;
    public int bubbleCount = 4;
    public float spawnRadius = 5f;
    
    [Header("Animation")]
    public NetworkAnimator networkAnimator;
    public string attackTrigger = "BubbleBurst";

    private EnemyAggro aggroSystem;

    void Awake()
    {
        aggroSystem = GetComponent<EnemyAggro>();
        if (networkAnimator == null) 
        {
            networkAnimator = GetComponent<NetworkAnimator>();
            if (networkAnimator == null) networkAnimator = GetComponentInParent<NetworkAnimator>();
        }
    }

    protected override void OnExecute()
    {
        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (networkAnimator != null)
        {
            networkAnimator.SetTrigger(attackTrigger);
        }
    }

    [ServerCallback]
    public void SpawnBubblesEvent()
    {
        Transform target = aggroSystem != null ? aggroSystem.GetCurrentTarget() : null;
        if (target == null) return;

        for (int i = 0; i < bubbleCount; i++)
        {
            Vector3 randomOffset = Random.insideUnitSphere * spawnRadius;
            randomOffset.y = 0f; 
            
            Vector3 spawnPos = target.position + randomOffset;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(spawnPos, out hit, 2f, NavMesh.AllAreas))
            {
                spawnPos = hit.position;
            }

            GameObject bubble = Instantiate(bubblePrefab, spawnPos, Quaternion.identity);
            NetworkServer.Spawn(bubble);
            
            ShokoyBubble script = bubble.GetComponent<ShokoyBubble>();
            if(script != null)
            {
                script.Initialize(GetComponent<Collider>());
            }
        }
    }
}
