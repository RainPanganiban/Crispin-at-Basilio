using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Spirit lights orbit Diwata then fire outward.
/// Two-phase attack: SpawnWisps starts orbit, ReleaseWisps fires them.
/// </summary>
public class OrbitingWispsAttack : BaseAttack
{
    public const string Event_SpawnWisps = "SpawnWisps";
    public const string Event_ReleaseWisps = "ReleaseWisps";

    [Header("Wisp Settings")]
    public DiwataWisp wispPrefab;
    public LayerMask playerLayer;

    [Header("Orbit")]
    public float orbitRadius = 3f;
    public float orbitHeight = 1f;
    public float orbitSpeed = 180f; // degrees per second
    public float wispDamage = 15f;
    public float wispProjectileSpeed = 12f;
    public float wispLifetime = 6f;

    [Header("Phase Scaling")]
    public int phase2Wisps = 5;
    public int phase3Wisps = 8;

    private readonly List<DiwataWisp> activeWisps = new List<DiwataWisp>();
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
        if (eventName == Event_SpawnWisps)
        {
            int count = Server_GetWispCount();
            Server_SpawnOrbitingWisps(count);
        }
        else if (eventName == Event_ReleaseWisps)
        {
            Server_ReleaseAllWisps();

            if (vulnerabilityManager != null)
                vulnerabilityManager.Server_OnAttackCompleted();
        }
    }

    int Server_GetWispCount()
    {
        int idx = phaseManager != null ? phaseManager.GetCurrentPhaseIndex() : 0;
        return idx switch
        {
            0 => phase2Wisps,
            1 => phase2Wisps,
            _ => phase3Wisps,
        };
    }

    [Server]
    void Server_SpawnOrbitingWisps(int count)
    {
        if (wispPrefab == null) return;

        activeWisps.Clear();
        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float startAngle = angleStep * i;

            DiwataWisp wisp = Instantiate(wispPrefab, transform.position, Quaternion.identity);
            wisp.Server_Initialize(
                owner: boss != null ? boss.netIdentity : null,
                orbitCenter: transform,
                startAngle: startAngle,
                orbitRadius: orbitRadius,
                orbitHeight: orbitHeight,
                orbitSpeed: orbitSpeed,
                damage: wispDamage,
                projectileSpeed: wispProjectileSpeed,
                lifetime: wispLifetime,
                playerLayer: playerLayer
            );

            NetworkServer.Spawn(wisp.gameObject);
            activeWisps.Add(wisp);
        }

        Rpc_OnWispsSpawned();
    }

    [Server]
    void Server_ReleaseAllWisps()
    {
        foreach (var wisp in activeWisps)
        {
            if (wisp != null)
                wisp.Server_Release();
        }

        activeWisps.Clear();
        Rpc_OnWispsReleased();
    }

    [ClientRpc]
    void Rpc_OnWispsSpawned()
    {
        Debug.Log("[OrbitingWisps] Wisps spawned and orbiting!");
    }

    [ClientRpc]
    void Rpc_OnWispsReleased()
    {
        Debug.Log("[OrbitingWisps] Wisps released!");
    }

    public override void Server_Stop()
    {
        foreach (var wisp in activeWisps)
        {
            if (wisp != null)
                NetworkServer.Destroy(wisp.gameObject);
        }
        activeWisps.Clear();
    }
}
