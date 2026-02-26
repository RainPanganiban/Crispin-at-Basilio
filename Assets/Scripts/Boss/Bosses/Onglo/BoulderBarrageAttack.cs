using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class BoulderBarrageAttack : BaseAttack
{
    public const string Event_Throw = "BoulderThrow";

    [Header("Spawn")]
    public List<Transform> boulderSpawnPoints = new List<Transform>();
    public OngloBoulder boulderPrefab;
    public LayerMask playerLayer;

    [Header("Throw")]
    public float boulderDamage = 30f;
    public float impactRadius = 2.5f;
    public float gravity = 18f;
    public float lifetime = 6f;

    [Header("Arc (tuned for big boss throws)")]
    public float forwardSpeed = 10f;
    public float upSpeed = 9f;

    [Header("Feedback")]
    public float throwShakeIntensity = 1.5f;

    [Header("Phase scaling")]
    public int phase1Throws = 3;
    public int phase2Throws = 5;
    public int phase3Throws = 8; // “rain”

    private BossPhaseManager phaseManager;

    public override void Initialize(BossController bossController)
    {
        base.Initialize(bossController);
        phaseManager = bossController != null ? bossController.GetComponent<BossPhaseManager>() : null;
    }

    public override void Server_Execute()
    {
        // Telegraph is animation-driven.
    }

    public override void Server_OnAnimationEvent(string eventName)
    {
        if (eventName != Event_Throw)
            return;

        int throws = Server_GetThrowsForCurrentPhase();
        for (int i = 0; i < throws; i++)
            Server_SpawnBoulder(i);
            
        Rpc_OnBoulderThrow();
    }

    [ClientRpc]
    void Rpc_OnBoulderThrow()
    {
        Debug.Log($"[BoulderBarrage] Boulder thrown feedback. Intensity: {throwShakeIntensity}");
    }

    int Server_GetThrowsForCurrentPhase()
    {
        int idx = phaseManager != null ? phaseManager.GetCurrentPhaseIndex() : 0;
        return idx switch
        {
            0 => phase1Throws,
            1 => phase2Throws,
            _ => phase3Throws,
        };
    }

    [Server]
    void Server_SpawnBoulder(int index)
    {
        if (boulderPrefab == null)
            return;

        Transform spawn = (boulderSpawnPoints != null && boulderSpawnPoints.Count > 0)
            ? boulderSpawnPoints[index % boulderSpawnPoints.Count]
            : transform;

        Vector3 dir = transform.forward;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f)
            dir = Vector3.forward;

        Vector3 vel = dir.normalized * forwardSpeed + Vector3.up * upSpeed;
        vel += new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));

        OngloBoulder b = Instantiate(boulderPrefab, spawn.position, Quaternion.identity);
        b.Server_Initialize(
            owner: boss != null ? boss.netIdentity : null,
            initialVelocity: vel,
            gravity: gravity,
            damage: boulderDamage,
            impactRadius: impactRadius,
            lifetime: lifetime,
            playerLayer: playerLayer
        );

        NetworkServer.Spawn(b.gameObject);
    }

    public override void Server_Stop()
    {
        // Nothing persistent to stop in MVP.
    }
}

