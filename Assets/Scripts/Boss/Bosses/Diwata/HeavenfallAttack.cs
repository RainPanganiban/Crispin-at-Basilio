using UnityEngine;
using Mirror;
using System.Collections.Generic;

/// <summary>
/// Columns of divine light strike random areas of the arena.
/// Spawns indicators before firing the beams.
/// Phase 3 attack.
/// </summary>
public class HeavenfallAttack : BaseAttack
{
    public const string Event_SpawnIndicators = "SpawnIndicators";
    public const string Event_FireBeams = "FireBeams";

    [Header("Beam Settings")]
    public DiwataHeavenBeam beamPrefab;
    public Transform arenaCenter;
    public LayerMask playerLayer;

    [Header("Arena")]
    public float arenaRadius = 12f;

    [Header("Damage & Timing")]
    public float beamDamage = 30f;
    public float beamRadius = 3f;
    public float telegraphDuration = 1.5f;

    [Header("Phase Scaling")]
    public int phase3Beams = 5;
    public int defaultBeams = 3;

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
        if (eventName == Event_FireBeams)
        {
            int count = Server_GetBeamCount();
            Server_SpawnBeams(count);

            if (vulnerabilityManager != null)
                vulnerabilityManager.Server_OnAttackCompleted();
        }
    }

    int Server_GetBeamCount()
    {
        int idx = phaseManager != null ? phaseManager.GetCurrentPhaseIndex() : 0;
        return idx >= 2 ? phase3Beams : defaultBeams;
    }

    [Server]
    void Server_SpawnBeams(int count)
    {
        if (beamPrefab == null) return;

        Vector3 center = arenaCenter != null ? arenaCenter.position : transform.position;

        // Mix: some beams on player positions, some random
        List<Vector3> playerPositions = new List<Vector3>();
        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn == null || conn.identity == null) continue;
            playerPositions.Add(conn.identity.transform.position);
        }

        for (int i = 0; i < count; i++)
        {
            Vector3 targetPos;

            // First beams target near players, rest are random
            if (i < playerPositions.Count)
            {
                targetPos = playerPositions[i];
                // Slight offset so it's dodgeable
                Vector2 offset = Random.insideUnitCircle * 2f;
                targetPos += new Vector3(offset.x, 0f, offset.y);
            }
            else
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float radius = Random.Range(0f, arenaRadius);
                targetPos = center + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius
                );
            }

            // Ground position
            targetPos.y = GetGroundY(targetPos);

            DiwataHeavenBeam beam = Instantiate(beamPrefab, targetPos, Quaternion.identity);
            beam.Server_Initialize(
                owner: boss != null ? boss.netIdentity : null,
                damage: beamDamage,
                beamRadius: beamRadius,
                telegraphDuration: telegraphDuration,
                playerLayer: playerLayer
            );

            NetworkServer.Spawn(beam.gameObject);
        }

        Rpc_OnHeavenfall();
    }

    float GetGroundY(Vector3 position)
    {
        if (Physics.Raycast(position + Vector3.up * 20f, Vector3.down, out RaycastHit hit, 40f))
            return hit.point.y;
        return 0f;
    }

    [ClientRpc]
    void Rpc_OnHeavenfall()
    {
        Debug.Log("[Heavenfall] Divine beams incoming!");
        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(0.4f, 0.1f);
    }

    public override void Server_Stop()
    {
        // Nothing persistent to stop
    }
}
