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

    // PINALITAN: List para sa maraming spawn points
    [Tooltip("Dito ilalagay ang mga Transforms kung saan magsisimula ang lances.")]
    public List<Transform> spawnOrigins = new List<Transform>();

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

    public override void Initialize(BossController bossController)
    {
        base.Initialize(bossController);
        phaseManager = bossController != null ? bossController.GetComponent<BossPhaseManager>() : null;
        vulnerabilityManager = bossController != null ? bossController.GetComponent<DiwataVulnerabilityManager>() : null;
    }

    // FIX: Dinagdagan ng check para hindi umatake kung malayo o kung hindi active ang server
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
        // FIX: Siniguro na server-only at active ang network bago mag-fire
        if (!isServer || !NetworkServer.active) return;

        if (eventName == Event_FireLances)
        {
            // Pumili ng random spawn points mula sa listahan
            List<Transform> selectedOrigins = Server_GetRandomSpawnPoints();

            foreach (Transform origin in selectedOrigins)
            {
                Server_SpawnLanceAtPoint(origin);
            }

            // FIX: Guard for ClientRpc
            if (NetworkServer.active)
            {
                Rpc_OnLancesFired();
            }

            if (vulnerabilityManager != null)
                vulnerabilityManager.Server_OnAttackCompleted();
        }
    }

    // Logic para pumili ng random points sa listahan
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

        // Ang direction ay laging papunta sa center mula sa spawn point
        Vector3 direction = (center - origin.position).normalized;
        direction.y = 0f; // Panatilihing horizontal ang lipad

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

    [ClientRpc]
    void Rpc_OnLancesFired()
    {
        Debug.Log("[SpiritLance] Lances fired from selected spawn points!");
    }

    // Helper para mahanap ang pinakamalapit na player para sa distance check
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