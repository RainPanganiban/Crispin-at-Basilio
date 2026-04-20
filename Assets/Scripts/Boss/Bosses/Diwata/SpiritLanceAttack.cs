using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class SpiritLanceAttack : BaseAttack
{
    public const string Event_SpawnLances = "SpawnLances";
    public const string Event_FireLances = "FireLances";

    [Header("Lance Settings")]
    public DiwataSpiritLance lancePrefab;
    public Transform arenaCenter;
    public LayerMask playerLayer;

    [Tooltip("Dito ilalagay ang mga Transforms kung saan magsisimula ang lances.")]
    public List<Transform> spawnOrigins = new List<Transform>();

    [Header("Sound Effects")] // --- DAGDAG: Sound Clips ---
    [SerializeField] private AudioClip spawnLancesClip;
    [SerializeField] private AudioClip fireLancesClip;

    [Header("Spawn Logic")]
    [Tooltip("Ilang spawn points ang gagamitin nang sabay-sabay?")]
    [Range(1, 10)]
    public int spawnPointCountToUse = 4;

    [Header("Projectile")]
    public float lanceDamage = 25f;
    public float lanceSpeed = 15f;
    public float lanceMaxDistance = 30f;

    [Header("Phase Scaling")]
    public int phase2Lances = 6;
    public int phase3Lances = 10;

    private BossPhaseManager phaseManager;
    private DiwataVulnerabilityManager vulnerabilityManager;
    private EnemySoundManager soundManager; // --- DAGDAG: Reference ---

    public override void Initialize(BossController bossController)
    {
        base.Initialize(bossController);
        phaseManager = bossController != null ? bossController.GetComponent<BossPhaseManager>() : null;
        vulnerabilityManager = bossController != null ? bossController.GetComponent<DiwataVulnerabilityManager>() : null;

        // Kunin ang SoundManager mula sa boss
        soundManager = bossController != null ? bossController.GetComponent<EnemySoundManager>() : null;
    }

    public override bool Server_CanExecute()
    {
        if (!isServer || boss == null || !NetworkServer.active) return false;

        // Gagamit na ito ng Server_FindClosestPlayer mula sa BaseAttack (No more warning!)
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
        if (!isServer || !NetworkServer.active) return;

        // Pag-spawn ng lances (visual preparation)
        if (eventName == Event_SpawnLances)
        {
            Rpc_PlaySpawnSound();
        }

        // Pag-fire na ng lances
        if (eventName == Event_FireLances)
        {
            List<Transform> selectedOrigins = Server_GetRandomSpawnPoints();

            foreach (Transform origin in selectedOrigins)
            {
                Server_SpawnLanceAtPoint(origin);
            }

            if (NetworkServer.active)
            {
                Rpc_OnLancesFired(); // Dito tutunog ang fire sound
            }

            if (vulnerabilityManager != null)
                vulnerabilityManager.Server_OnAttackCompleted();
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

    [Server]
    void Server_SpawnLanceAtPoint(Transform origin)
    {
        if (lancePrefab == null) return;

        Vector3 center = arenaCenter != null ? arenaCenter.position : transform.position;

        Vector3 direction = (center - origin.position).normalized;
        direction.y = 0f;

        DiwataSpiritLance lance = Instantiate(lancePrefab, origin.position, Quaternion.identity);
        lance.Server_Initialize(
            owner: boss != null ? boss.netIdentity : null,
            direction: direction,
            speed: lanceSpeed,
            damage: lanceDamage,
            maxDistance: lanceMaxDistance,
            playerLayer: playerLayer
        );

        NetworkServer.Spawn(lance.gameObject);
    }

    // --- AUDIO RPCs ---

    [ClientRpc]
    void Rpc_PlaySpawnSound()
    {
        if (soundManager != null && spawnLancesClip != null)
        {
            soundManager.PlaySpecificAttack(spawnLancesClip);
        }
    }

    [ClientRpc]
    void Rpc_OnLancesFired()
    {
        Debug.Log("[SpiritLance] Lances fired!");
        if (soundManager != null && fireLancesClip != null)
        {
            soundManager.PlaySpecificAttack(fireLancesClip);
        }
    }

    public override void Server_Stop() { }
}