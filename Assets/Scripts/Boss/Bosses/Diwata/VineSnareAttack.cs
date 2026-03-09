using UnityEngine;
using Mirror;
using System.Collections.Generic;

/// <summary>
/// Roots erupt beneath players and trap areas of the arena.
/// Spawns VineSnare hazards at or near player positions.
/// </summary>
public class VineSnareAttack : BaseAttack
{
    public const string Event_SpawnVines = "SpawnVines";
    public const string Event_TriggerVineSnap = "TriggerVineSnap";

    [Header("Vine Settings")]
    public DiwataVineSnare vineAttackPrefab;
    public LayerMask playerLayer;

    [Header("Damage & Timing")]
    public float vineDamage = 20f;
    public float vineRadius = 2.5f;
    public float telegraphDuration = 1.2f;

    [Header("Spawn")]
    public int vineCount = 3;
    public float spawnOffsetRadius = 2f;

    private DiwataVulnerabilityManager vulnerabilityManager;

    public override void Initialize(BossController bossController)
    {
        base.Initialize(bossController);
        vulnerabilityManager = bossController != null ? bossController.GetComponent<DiwataVulnerabilityManager>() : null;
    }

    public override void Server_Execute()
    {
        // Animation-driven
    }

    public override void Server_OnAnimationEvent(string eventName)
    {
        if (eventName == Event_TriggerVineSnap)
        {
            Server_SpawnVines();

            if (vulnerabilityManager != null)
                vulnerabilityManager.Server_OnAttackCompleted();
        }
    }

    [Server]
    void Server_SpawnVines()
    {
        if (vineAttackPrefab == null) return;

        // Collect all player positions
        List<Vector3> playerPositions = new List<Vector3>();
        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn == null || conn.identity == null) continue;
            playerPositions.Add(conn.identity.transform.position);
        }

        for (int i = 0; i < vineCount; i++)
        {
            Vector3 targetPos;
            if (playerPositions.Count > 0)
            {
                // Pick a random player position and offset slightly
                targetPos = playerPositions[Random.Range(0, playerPositions.Count)];
                Vector2 offset = Random.insideUnitCircle * spawnOffsetRadius;
                targetPos += new Vector3(offset.x, 0f, offset.y);
            }
            else
            {
                targetPos = transform.position + Random.insideUnitSphere * 5f;
                targetPos.y = transform.position.y;
            }

            // Ground the position
            targetPos.y = GetGroundY(targetPos);

            DiwataVineSnare vine = Instantiate(vineAttackPrefab, targetPos, Quaternion.identity);
            vine.Server_Initialize(
                owner: boss != null ? boss.netIdentity : null,
                damage: vineDamage,
                eruptRadius: vineRadius,
                telegraphDuration: telegraphDuration,
                playerLayer: playerLayer
            );

            NetworkServer.Spawn(vine.gameObject);
        }
    }

    float GetGroundY(Vector3 position)
    {
        if (Physics.Raycast(position + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f))
            return hit.point.y;
        return 0f;
    }

    public override void Server_Stop()
    {
        // Nothing persistent to stop
    }
}
