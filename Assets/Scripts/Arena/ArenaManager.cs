using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// Server-authoritative arena wave controller.
/// Place in the arena scene alongside spawn point transforms.
/// Spawns enemies wave-by-wave, tracks deaths, and fires OnArenaCompleted
/// when all waves are cleared.
/// </summary>
public class ArenaManager : NetworkBehaviour
{
    public static ArenaManager Instance { get; private set; }

    [Header("Wave Configuration")]
    [Tooltip("ScriptableObject defining enemy waves.")]
    public ArenaWaveData waveData;

    [Header("Spawn Points")]
    [Tooltip("Transforms in the scene where enemies can spawn. Enemies are distributed round-robin.")]
    public Transform[] spawnPoints;

    // ── Synced state (clients read these for UI) ──────────────────────
    [SyncVar] private int currentWave;       // 0-indexed internally, displayed as 1-based
    [SyncVar] private int totalWaves;
    [SyncVar] private float interWaveCountdown;
    [SyncVar] private bool arenaActive;

    // ── Server-only tracking ─────────────────────────────────────────
    private readonly HashSet<GameObject> aliveEnemies = new HashSet<GameObject>();
    private bool arenaCompleted;

    /// <summary>Fired on the server when all waves are cleared.</summary>
    public static event Action OnArenaCompleted;

    // ── Public getters for ArenaUI ────────────────────────────────────
    public int CurrentWave => currentWave + 1;   // 1-based for display
    public int TotalWaves => totalWaves;
    public float InterWaveCountdown => interWaveCountdown;
    public bool IsArenaActive => arenaActive;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (waveData == null)
        {
            Debug.LogError("[ArenaManager] No ArenaWaveData assigned!");
            return;
        }

        totalWaves = waveData.WaveCount;
        currentWave = 0;
        arenaCompleted = false;
        arenaActive = true;

        Debug.Log($"[ArenaManager] OnStartServer: Starting arena with {totalWaves} waves.");
        StartCoroutine(RunArena());
    }

    // ══════════════════════════════════════════════════════════════════
    // Core Loop
    // ══════════════════════════════════════════════════════════════════

    [Server]
    IEnumerator RunArena()
    {
        Debug.Log("[ArenaManager] RunArena coroutine started.");

        for (int w = 0; w < waveData.WaveCount; w++)
        {
            currentWave = w;
            WaveDefinition wave = waveData.waves[w];

            Debug.Log($"[ArenaManager] Beginning Wave {w + 1}/{totalWaves} (delay: {wave.delayBeforeWave}s).");

            // Inter-wave countdown
            float countdown = wave.delayBeforeWave;
            while (countdown > 0f)
            {
                interWaveCountdown = countdown;
                yield return new WaitForSeconds(0.1f);
                countdown -= 0.1f;
            }
            interWaveCountdown = 0f;

            // Announce wave to clients
            RpcAnnounceWave(w + 1, totalWaves);

            // Spawn enemies
            SpawnWave(wave);

            if (aliveEnemies.Count == 0)
            {
                Debug.LogWarning($"[ArenaManager] Wave {w + 1} spawned zero enemies! Check WaveData configuration.");
            }

            // Wait until all enemies from this wave are dead
            Debug.Log($"[ArenaManager] Waiting for {aliveEnemies.Count} enemies to be defeated...");
            yield return new WaitUntil(() => aliveEnemies.Count == 0);

            Debug.Log($"[ArenaManager] Wave {w + 1}/{totalWaves} cleared.");
        }

        // All waves cleared
        CompleteArena();
    }

    // ══════════════════════════════════════════════════════════════════
    // Spawning
    // ══════════════════════════════════════════════════════════════════

    [Server]
    void SpawnWave(WaveDefinition wave)
    {
        int totalToSpawn = 0;
        foreach (WaveEntry entry in wave.enemies) totalToSpawn += Mathf.Max(0, entry.count);
        Debug.Log($"[ArenaManager] Spawning Wave {currentWave + 1}: Total {totalToSpawn} enemies across {wave.enemies.Count} entries.");

        int spawnIndex = 0;

        foreach (WaveEntry entry in wave.enemies)
        {
            if (entry.enemyPrefab == null)
            {
                Debug.LogWarning("[ArenaManager] Null enemy prefab in wave entry, skipping.");
                continue;
            }

            if (entry.count <= 0)
            {
                Debug.LogWarning($"[ArenaManager] Enemy count is {entry.count} for prefab {entry.enemyPrefab.name}, skipping.");
                continue;
            }

            for (int i = 0; i < entry.count; i++)
            {
                Transform point = GetSpawnPoint(spawnIndex);
                spawnIndex++;

                Vector3 pos = point.position;
                Quaternion rot = point.rotation;

                GameObject enemy = Instantiate(entry.enemyPrefab, pos, rot);
                NetworkServer.Spawn(enemy);

                // Track this enemy
                aliveEnemies.Add(enemy);

                // Listen for destruction to decrement count
                // Use MonoBehaviour for runtime-added component safety
                EnemyDeathTracker tracker = enemy.AddComponent<EnemyDeathTracker>();
                tracker.Initialize(this);
            }
        }

        Debug.Log($"[ArenaManager] SpawnWave complete. Current active enemies: {aliveEnemies.Count}.");
    }

    Transform GetSpawnPoint(int index)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[ArenaManager] No spawn points configured, using ArenaManager position.");
            return transform;
        }

        return spawnPoints[index % spawnPoints.Length];
    }

    // ══════════════════════════════════════════════════════════════════
    // Enemy Death Tracking
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by EnemyDeathTracker when an enemy is destroyed.
    /// </summary>
    [Server]
    public void NotifyEnemyDead(GameObject enemy)
    {
        aliveEnemies.Remove(enemy);
    }

    // ══════════════════════════════════════════════════════════════════
    // Completion
    // ══════════════════════════════════════════════════════════════════

    [Server]
    void CompleteArena()
    {
        if (arenaCompleted) return;
        arenaCompleted = true;
        arenaActive = false;

        Debug.Log("[ArenaManager] Arena completed! All waves cleared.");
        OnArenaCompleted?.Invoke();
    }

    [ClientRpc]
    void RpcAnnounceWave(int wave, int total)
    {
        // ArenaUI listens for this or polls SyncVars — this RPC is for
        // triggering one-shot animations/sounds on the client.
        Debug.Log($"[ArenaManager] Wave {wave}/{total} starting!");
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
