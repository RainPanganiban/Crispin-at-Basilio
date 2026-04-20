using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class SwarmSummonAttack : BaseAttack
{
    public const string Event_SpawnSwarm = "SpawnSwarm";

    [Header("Swarm Prefabs")]
    public GameObject tyanakPrefab;
    public GameObject bungisngisAnimrefab;

    [Header("Spawn Points")]
    public List<Transform> swarmSpawnPoints = new List<Transform>();

    [Header("Sound Effects")] // --- DAGDAG: Sound Clips ---
    [SerializeField] private AudioClip summonVoiceClip; // Sigaw ni Diwata (e.g., "Sugod!")
    [SerializeField] private AudioClip summonMagicClip; // Magic effect sound (e.g., Dark Poof/Portal)

    [Header("Phase Scaling")]
    public int phase1SpawnCount = 4;
    public int phase2SpawnCount = 6;
    public int phase3SpawnCount = 8;

    [Header("Spawn Settings")]
    public float spawnRadius = 2f;

    private BossPhaseManager phaseManager;
    private DiwataVulnerabilityManager vulnerabilityManager;
    private EnemySoundManager soundManager; // --- DAGDAG: Reference ---

    public override void Initialize(BossController bossController)
    {
        base.Initialize(bossController);
        phaseManager = bossController != null ? bossController.GetComponent<BossPhaseManager>() : null;
        vulnerabilityManager = bossController != null ? bossController.GetComponent<DiwataVulnerabilityManager>() : null;
        soundManager = bossController != null ? bossController.GetComponent<EnemySoundManager>() : null;
    }

    public override bool Server_CanExecute()
    {
        if (!isServer || boss == null || !NetworkServer.active) return false;
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
        if (!isServer || !NetworkServer.active) return;

        if (eventName == Event_SpawnSwarm)
        {
            Server_SpawnSwarmEnemies();

            // I-play ang sound sa lahat ng clients
            Rpc_PlaySummonSounds();
        }
    }

    [Server]
    void Server_SpawnSwarmEnemies()
    {
        int phaseIndex = phaseManager != null ? phaseManager.GetCurrentPhaseIndex() : 0;
        int count = Server_GetSpawnCount(phaseIndex);
        GameObject prefab = Server_GetPrefabForPhase(phaseIndex);

        if (prefab == null) return;

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = Server_GetSpawnPosition(i);
            GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);

            NetworkServer.Spawn(enemy);

            if (vulnerabilityManager != null)
            {
                vulnerabilityManager.Server_RegisterSwarmEnemy(enemy);

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

        if (NetworkServer.active)
        {
            Rpc_OnSwarmSummoned(count);
        }
    }

    // --- AUDIO RPC ---
    [ClientRpc]
    void Rpc_PlaySummonSounds()
    {
        if (soundManager != null)
        {
            // Play voice clip kung meron (e.g. Diwata's shout)
            if (summonVoiceClip != null)
                soundManager.PlaySpecificAttack(summonVoiceClip);

            // Play magic effect (e.g. spell sound)
            if (summonMagicClip != null)
                soundManager.PlaySpecificAttack(summonMagicClip);
        }
    }

    // ... (Keep other helper methods like Server_HasActivePlayers, Server_GetSpawnCount, etc. the same)

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
            return point.position + new Vector3(offset.x, 0.1f, offset.y);
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