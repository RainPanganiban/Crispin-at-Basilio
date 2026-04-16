using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class BubbleBarrageAttack : BaseAttack
{
    public const string Event_FireBubbles = "FireBubbles";

    [Header("Bubble Settings")]
    public BakunawaBubble bubblePrefab;

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

    // BAGO: Gaano katagal bago mag-reset ang AI pagka-atake?
    public float attackDuration = 2.5f;

    [Header("Phase Scaling")]
    public int phase1Bubbles = 6;
    public int phase2Bubbles = 10;
    public int phase3Bubbles = 16;

    private BossPhaseManager phaseManager;
    private DiwataVulnerabilityManager vulnerabilityManager;
    private BossController controller;

    public override void Initialize(BossController bossController)
    {
        base.Initialize(bossController);
        controller = bossController;
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

            // --- ETO ANG DETALYE ---
            // Kung ang player ay masyadong malapit (halimbawa < 7 units), 
            // mag-re-return tayo ng FALSE para mapilitan ang AI na piliin ang Tidal Bite.
            if (dist < 7.0f)
            {
                return false;
            }

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

        // BAGO: Simulan ang timer para i-reset ang AI
        StopAllCoroutines();
        StartCoroutine(AutoResetRoutine());
    }

    // BAGO: Maghihintay ito bago sabihan ang AI na "Tapos na ako!"
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
                Rpc_OnBubblesFired();
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

            // --- BAGO: IPASA ANG DAMAGE PARA MAKABAWAS ---
            // Kailangan may public variables na 'damage' at 'explosionRadius' sa ShokoyBubble.cs mo
            // Kung iba ang pangalan ng variable doon, palitan mo lang ito.
            bubble.damage = bubbleDamage;
            // bubble.explosionRadius = bubbleExplosionRadius; // Uncomment kung meron

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

    // --- BAGO: LALAGYAN NA NATIN NG LAMAN ITO PARA DI MAG-STUCK ---
    public override void Server_Stop()
    {
        if (!isServer) return;

        // Reset BossController
        if (controller != null)
        {
            controller.Server_EndAttack();
        }

        // Reset Attack Manager
        if (TryGetComponent<BossAttackManager>(out var attackManager))
        {
            attackManager.Server_OnAttackAnimationComplete();
        }

        Debug.Log("<color=yellow>[BubbleBarrage]</color> Attack Ended & AI Reset!");
    }
}