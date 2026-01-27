using UnityEngine;
using Mirror;
using UnityEngine.AI;

public class EnemyAI : NetworkBehaviour
{
    [SerializeField] private float detectionRadius = 15f;

    private NavMeshAgent agent;
    private Transform currentTarget;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public override void OnStartServer()
    {
        agent.enabled = true; // Only server moves agent
        InvokeRepeating(nameof(UpdateTarget), 0f, 0.5f); // detect nearest player every 0.5s
    }

    public override void OnStartClient()
    {
        if (!isServer) agent.enabled = false; // clients do not simulate AI
    }

    [Server]
    void UpdateTarget()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        float closestDistance = Mathf.Infinity;
        Transform closestPlayer = null;

        foreach (GameObject player in players)
        {
            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist < closestDistance && dist <= detectionRadius)
            {
                closestDistance = dist;
                closestPlayer = player.transform;
            }
        }

        if (closestPlayer != null && closestPlayer != currentTarget)
            currentTarget = closestPlayer;
    }

    [ServerCallback]
    void Update()
    {
        if (currentTarget == null) return;
        agent.SetDestination(currentTarget.position);
    }

    [Server]
    public void SetAggroTarget(Transform target)
    {
        if (target != null)
            currentTarget = target;
    }
}
