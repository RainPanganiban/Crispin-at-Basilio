using UnityEngine;
using Mirror;
using System.Collections.Generic;

/// <summary>
/// Diwata summons swarm enemies to pressure ranged players.
/// Spawns Tyanak (Phase 1-2) or Bungisngis (Phase 2-3) enemies at spawn points.
/// Registers spawned enemies with DiwataVulnerabilityManager for kill tracking.
/// </summary>
public class SwarmSummonAttack : BaseAttack
{
    public const string Event_SpawnSwarm = "SpawnSwarm";

    [Header("Swarm Prefabs")]
    [Tooltip("Tyanak enemy prefab (Phase 1-2 swarm type)")]
    public GameObject tyanakPrefab;
    [Tooltip("Bungisngis enemy prefab (Phase 2-3 swarm type)")]
    public GameObject bungisngisAnimrefab;

    [Header("Spawn Points")]
    public List<Transform> swarmSpawnPoints = new List<Transform>();

    [Header("Phase Scaling")]
    public int phase1SpawnCount = 4;
    public int phase2SpawnCount = 6;
    public int phase3SpawnCount = 8;

    [Header("Spawn Settings")]
    public float spawnRadius = 2f;

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
        // Swarm summon should always be available regardless of range
        if (boss == null || !boss.isServer) return false;
        return true;
    }

    public override void Server_Execute()
    {
        // Animation-driven
    }

    public override void Server_OnAnimationEvent(string eventName)
    {
        if (eventName == Event_SpawnSwarm)
        {
            Server_SpawnSwarmEnemies();
        }
    }

    [Server]
    void Server_SpawnSwarmEnemies()
    {
        int phaseIndex = phaseManager != null ? phaseManager.GetCurrentPhaseIndex() : 0;
        int count = Server_GetSpawnCount(phaseIndex);
        GameObject prefab = Server_GetPrefabForPhase(phaseIndex);

        if (prefab == null)
        {
            Debug.LogWarning("[SwarmSummon] No swarm enemy prefab assigned!");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = Server_GetSpawnPosition(i);
            GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
            NetworkServer.Spawn(enemy);

            // Register with vulnerability manager for kill tracking
            if (vulnerabilityManager != null)
            {
                vulnerabilityManager.Server_RegisterSwarmEnemy(enemy);

                // Hook into the enemy's death to notify the vulnerability manager
                EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    // Use a simple callback — when this enemy dies, notify manager
                    var manager = vulnerabilityManager;
                    enemyHealth.OnDeath += () =>
                    {
                        if (manager != null)
                            manager.Server_OnSwarmEnemyKilled();
                    };
                }
            }
        }

        Rpc_OnSwarmSummoned(count);
    }

    int Server_GetSpawnCount(int phaseIndex)
    {
        return phaseIndex switch
        {
            0 => phase1SpawnCount,
            1 => phase2SpawnCount,
            _ => phase3SpawnCount,
        };
    }

    GameObject Server_GetPrefabForPhase(int phaseIndex)
    {
        // Phase 1: Tyanak only
        // Phase 2: Mix (Bungisngis if available, else Tyanak)
        // Phase 3: Bungisngis primarily
        return phaseIndex switch
        {
            0 => tyanakPrefab,
            1 => bungisngisAnimrefab != null ? bungisngisAnimrefab : tyanakPrefab,
            _ => bungisngisAnimrefab != null ? bungisngisAnimrefab : tyanakPrefab,
        };
    }

    Vector3 Server_GetSpawnPosition(int index)
    {
        if (swarmSpawnPoints != null && swarmSpawnPoints.Count > 0)
        {
            Transform point = swarmSpawnPoints[index % swarmSpawnPoints.Count];
            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            return point.position + new Vector3(offset.x, 0f, offset.y);
        }

        // Fallback: spawn around the boss
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float radius = Random.Range(5f, 10f);
        return transform.position + new Vector3(
            Mathf.Cos(angle) * radius,
            0f,
            Mathf.Sin(angle) * radius
        );
    }

    [ClientRpc]
    void Rpc_OnSwarmSummoned(int count)
    {
        Debug.Log($"[SwarmSummon] Diwata summoned {count} swarm enemies!");
    }

    public override void Server_Stop()
    {
        // Nothing persistent to stop — spawned enemies are independent
    }
}
