using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class BubbleBarrageAttack : BaseAttack
{
    public const string Event_FireBubbles = "FireBubbles";

    [Header("Bubble Settings")]
    public BakunawaBubble bubblePrefab;
    public List<Transform> spawnOrigins = new List<Transform>();
    public LayerMask playerLayer;

    [Header("Sound Effects")] // --- DAGDAG: Sound Setup ---
    [SerializeField] private AudioClip bubbleSpawnSFX; // Tunog ng bula (e.g., Bubble Pop/Water Gurgle)

    [Header("Multi-Spawn Logic")]
    [Tooltip("Ilan sa mga spawn points ang gagamitin nang sabay-sabay?")]
    [Range(1, 5)]
    public int spawnPointCountToUse = 1;

    [Header("Bubble Stats (Overrides)")]
    public float bubbleDamage = 15f;
    public float bubbleExplosionRadius = 2.5f;
    public float bubbleLifetime = 1.5f;
    public float attackDuration = 2.5f;

    [Header("Phase Scaling")]
    public int phase1Bubbles = 6;
    public int phase2Bubbles = 10;
    public int phase3Bubbles = 16;

    private BossPhaseManager phaseManager;
    private DiwataVulnerabilityManager vulnerabilityManager;
    private BossController controller;
    private EnemySoundManager soundManager; // --- DAGDAG: Reference ---

    public override void Initialize(BossController bossController)
    {
        base.Initialize(bossController);
        controller = bossController;
        phaseManager = bossController != null ? bossController.GetComponent<BossPhaseManager>() : null;
        vulnerabilityManager = bossController != null ? bossController.GetComponent<DiwataVulnerabilityManager>() : null;

        // Kunin ang SoundManager mula sa boss
        soundManager = bossController != null ? bossController.GetComponent<EnemySoundManager>() : null;
    }

    public override bool Server_CanExecute()
    {
        if (!isServer || boss == null || !NetworkServer.active) return false;

        Transform targetPlayer = Server_FindClosestPlayer();
        if (targetPlayer != null)
        {
            float dist = Vector3.Distance(transform.position, targetPlayer.position);

            // AI Logic: Kung masyadong malapit, bawal ang bubbles (pilitin mag-Tidal Bite)
            if (dist < 7.0f) return false;

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

        StopAllCoroutines();
        StartCoroutine(AutoResetRoutine());
    }

    private System.Collections.IEnumerator AutoResetRoutine()
    {
        yield return new WaitForSeconds(attackDuration);
        Server_Stop();
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
                SpawnBubbles(origin, bubblesPerPoint);
            }

            if (NetworkServer.active)
            {
                Rpc_OnBubblesFired(); // Dito tutunog ang bubbles
            }

            if (vulnerabilityManager != null)
                vulnerabilityManager.Server_OnAttackCompleted();
        }
    }

    [Server]
    void SpawnBubbles(Transform origin, int count)
    {
        if (bubblePrefab == null) return;

        for (int i = 0; i < count; i++)
        {
            Vector3 randomOffset = new Vector3(Random.Range(-1.5f, 1.5f), 0, Random.Range(-1.5f, 1.5f));
            Vector3 spawnPos = origin.position + randomOffset;

            BakunawaBubble bubble = Instantiate(bubblePrefab, spawnPos, Quaternion.identity);
            bubble.Initialize(boss != null ? boss.GetComponent<Collider>() : null);

            bubble.damage = bubbleDamage;
            // bubble.explosionRadius = bubbleExplosionRadius; 

            NetworkServer.Spawn(bubble.gameObject);
        }
    }

    // --- AUDIO RPC ---
    [ClientRpc]
    void Rpc_OnBubblesFired()
    {
        Debug.Log("[BubbleBarrage] Bubbles spawned and rising!");

        // Patunugin ang bubble SFX sa pwesto ni Bakunawa (o mouth origins)
        if (soundManager != null && bubbleSpawnSFX != null)
        {
            soundManager.PlaySpecificAttack(bubbleSpawnSFX);
        }
    }

    // ... (Keep the rest of helper methods: Server_GetRandomSpawnPoints, Server_GetBubbleCount, Server_FindClosestPlayer)

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

    public override void Server_Stop()
    {
        if (!isServer) return;

        if (controller != null)
        {
            controller.Server_EndAttack();
        }

        if (TryGetComponent<BossAttackManager>(out var attackManager))
        {
            attackManager.Server_OnAttackAnimationComplete();
        }
    }
}