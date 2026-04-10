using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class BubbleBarrageAttack : BaseAttack
{
    public const string Event_FireBubbles = "FireBubbles";

    [Header("Bubble Settings")]
    public ShokoyBubble bubblePrefab;

    [Tooltip("Dito ilalagay ang mga Transforms kung saan pwedeng lumabas ang bubbles.")]
    public List<Transform> spawnOrigins = new List<Transform>();

    public LayerMask playerLayer;

    [Header("Multi-Spawn Logic")]
    [Tooltip("Ilan sa mga spawn points ang gagamitin nang sabay-sabay?")]
    [Range(1, 5)]
    public int spawnPointCountToUse = 1;

    [Header("Bubble Stats (Overrides)")]
    public float bubbleDamage = 15f;
    public float bubbleExplosionRadius = 2.5f;
    public float bubbleLifetime = 1.5f;

    [Header("Phase Scaling")]
    public int phase1Bubbles = 6;
    public int phase2Bubbles = 10;
    public int phase3Bubbles = 16;

    private BossPhaseManager phaseManager;
    private DiwataVulnerabilityManager vulnerabilityManager;

    public override void Initialize(BossController bossController)
    {
        base.Initialize(bossController);
        phaseManager = bossController != null ? bossController.GetComponent<BossPhaseManager>() : null;
        vulnerabilityManager = bossController != null ? bossController.GetComponent<DiwataVulnerabilityManager>() : null;
    }

    public override bool Server_CanExecute()
    {
        if (!isServer || boss == null || !NetworkServer.active) return false;

        Transform targetPlayer = Server_FindClosestPlayer();
        if (targetPlayer != null)
        {
            float dist = Vector3.Distance(transform.position, targetPlayer.position);
            return dist <= maxRange;
        }
        return false;
    }

    public override void Server_Execute()
    {
        if (boss != null && !string.IsNullOrEmpty(animationTriggerName))
        {
            boss.Server_PlayTrigger(animationTriggerName);
        }
    }

    public override void Server_OnAnimationEvent(string eventName)
    {
        if (eventName == Event_FireBubbles)
        {
            int totalBubbleCount = Server_GetBubbleCount();
            List<Transform> selectedOrigins = Server_GetRandomSpawnPoints();

            foreach (Transform origin in selectedOrigins)
            {
                int bubblesPerPoint = totalBubbleCount / selectedOrigins.Count;
                // Siguraduhin na "SpawnBubbles" ang itatawag mo dito:
                SpawnBubbles(origin, bubblesPerPoint);
            }

            if (NetworkServer.active)
            {
                Rpc_OnBubblesFired();
            }

            if (vulnerabilityManager != null)
                vulnerabilityManager.Server_OnAttackCompleted();
        }
    }

    [Server]
    void SpawnBubbles(Transform origin, int count) // Sinigurado nating may parameters dito
    {
        if (bubblePrefab == null) return;

        for (int i = 0; i < count; i++)
        {
            // Random offset para hindi magkakapatong ang bubbles
            Vector3 randomOffset = new Vector3(Random.Range(-1.5f, 1.5f), 0, Random.Range(-1.5f, 1.5f));
            Vector3 spawnPos = origin.position + randomOffset;

            ShokoyBubble bubble = Instantiate(bubblePrefab, spawnPos, Quaternion.identity);

            // Siniguradong tumutugma sa Initialize ng ShokoyBubble mo
            bubble.Initialize(boss != null ? boss.GetComponent<Collider>() : null);

            NetworkServer.Spawn(bubble.gameObject);
        }
    }
    List<Transform> Server_GetRandomSpawnPoints()
    {
        List<Transform> picked = new List<Transform>();
        if (spawnOrigins == null || spawnOrigins.Count == 0)
        {
            picked.Add(transform);
            return picked;
        }

        List<Transform> pool = new List<Transform>(spawnOrigins);
        int countToPick = Mathf.Min(spawnPointCountToUse, pool.Count);

        for (int i = 0; i < countToPick; i++)
        {
            int randomIndex = Random.Range(0, pool.Count);
            picked.Add(pool[randomIndex]);
            pool.RemoveAt(randomIndex);
        }
        return picked;
    }

    int Server_GetBubbleCount()
    {
        int idx = phaseManager != null ? phaseManager.GetCurrentPhaseIndex() : 0;
        return idx switch
        {
            0 => phase1Bubbles,
            1 => phase2Bubbles,
            _ => phase3Bubbles,
        };
    }

    [ClientRpc]
    void Rpc_OnBubblesFired()
    {
        Debug.Log("[BubbleBarrage] Bubbles spawned and rising!");
    }

    Transform Server_FindClosestPlayer()
    {
        Transform best = null;
        float bestSqr = float.PositiveInfinity;

        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn == null || conn.identity == null) continue;
            float d = (conn.identity.transform.position - transform.position).sqrMagnitude;
            if (d < bestSqr)
            {
                bestSqr = d;
                best = conn.identity.transform;
            }
        }
        return best;
    }

    public override void Server_Stop() { }
}