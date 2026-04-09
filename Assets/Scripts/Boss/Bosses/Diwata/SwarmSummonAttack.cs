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
        // FIX: Added NetworkServer.active check
        if (!isServer || boss == null || !NetworkServer.active) return false;

        // Siguraduhin na may player muna bago mag-summon ng minions
        return Server_HasActivePlayers();
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
        // FIX: Guard for server execution
        if (!isServer || !NetworkServer.active) return;

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

            // FIX: Spawn using Mirror networking
            NetworkServer.Spawn(enemy);

            // Register with vulnerability manager for kill tracking
            if (vulnerabilityManager != null)
            {
                vulnerabilityManager.Server_RegisterSwarmEnemy(enemy);

                // Hook into the enemy's death
                if (enemy.TryGetComponent<EnemyHealth>(out var enemyHealth))
                {
                    var manager = vulnerabilityManager;
                    enemyHealth.OnDeath += () =>
                    {
                        if (manager != null)
                            manager.Server_OnSwarmEnemyKilled();
                    };
                }
            }
        }

        // FIX: RPC Guard
        if (NetworkServer.active)
        {
            Rpc_OnSwarmSummoned(count);
        }
    }

    bool Server_HasActivePlayers()
    {
        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn != null && conn.identity != null) return true;
        }
        return false;
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
            return point.position + new Vector3(offset.x, 0.1f, offset.y); // Bahagyang angat sa floor
        }

        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float radius = Random.Range(5f, 10f);
        return transform.position + new Vector3(Mathf.Cos(angle) * radius, 0.1f, Mathf.Sin(angle) * radius);
    }

    [ClientRpc]
    void Rpc_OnSwarmSummoned(int count)
    {
        Debug.Log($"[SwarmSummon] Diwata summoned {count} swarm enemies!");
    }

    public override void Server_Stop() { }
}