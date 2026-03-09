using UnityEngine;
using Mirror;
using System.Collections.Generic;

/// <summary>
/// Divine spears lock onto players before firing.
/// Spawns spears that track player positions during a lock-on period,
/// then fire in a straight line toward the locked position.
/// Phase 3 attack.
/// </summary>
public class JudgmentSpearAttack : BaseAttack
{
    public const string Event_SpawnSpears = "SpawnSpears";
    public const string Event_FireSpears = "FireSpears";

    [Header("Spear Settings")]
    public DiwataJudgmentSpear spearPrefab;
    public LayerMask playerLayer;

    [Header("Spawn")]
    public float spawnHeight = 8f;
    public float spawnSpread = 3f;

    [Header("Projectile")]
    public float spearDamage = 30f;
    public float spearSpeed = 18f;
    public float lockOnDuration = 1.2f;
    public float maxDistance = 30f;

    [Header("Phase Scaling")]
    public int phase3Spears = 4;
    public int defaultSpears = 2;

    private BossPhaseManager phaseManager;
    private DiwataVulnerabilityManager vulnerabilityManager;

    public override void Initialize(BossController bossController)
    {
        base.Initialize(bossController);
        phaseManager = bossController != null ? bossController.GetComponent<BossPhaseManager>() : null;
        vulnerabilityManager = bossController != null ? bossController.GetComponent<DiwataVulnerabilityManager>() : null;
    }

    public override void Server_Execute()
    {
        // Animation-driven
    }

    public override void Server_OnAnimationEvent(string eventName)
    {
        if (eventName == Event_FireSpears)
        {
            Server_SpawnJudgmentSpears();

            if (vulnerabilityManager != null)
                vulnerabilityManager.Server_OnAttackCompleted();
        }
    }

    [Server]
    void Server_SpawnJudgmentSpears()
    {
        if (spearPrefab == null) return;

        // Gather player targets
        List<Transform> players = new List<Transform>();
        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn == null || conn.identity == null) continue;
            players.Add(conn.identity.transform);
        }

        if (players.Count == 0) return;

        int count = Server_GetSpearCount();

        for (int i = 0; i < count; i++)
        {
            // Target players in round-robin
            Transform target = players[i % players.Count];
            Vector3 targetPos = target.position;

            // Spawn above the target with some spread
            Vector2 offset = Random.insideUnitCircle * spawnSpread;
            Vector3 spawnPos = targetPos + new Vector3(offset.x, spawnHeight, offset.y);

            DiwataJudgmentSpear spear = Instantiate(spearPrefab, spawnPos, Quaternion.identity);
            spear.Server_Initialize(
                owner: boss != null ? boss.netIdentity : null,
                targetPosition: targetPos,
                damage: spearDamage,
                projectileSpeed: spearSpeed,
                lockOnDuration: lockOnDuration,
                maxDistance: maxDistance,
                playerLayer: playerLayer
            );

            NetworkServer.Spawn(spear.gameObject);
        }

        Rpc_OnJudgmentSpears();
    }

    int Server_GetSpearCount()
    {
        int idx = phaseManager != null ? phaseManager.GetCurrentPhaseIndex() : 0;
        return idx >= 2 ? phase3Spears : defaultSpears;
    }

    [ClientRpc]
    void Rpc_OnJudgmentSpears()
    {
        Debug.Log("[JudgmentSpear] Divine spears locked on!");
    }

    public override void Server_Stop()
    {
        // Nothing persistent to stop
    }
}
