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

    // --- ITO ANG DINAGDAG NA FIX ---
    public override bool Server_CanExecute()
    {
        if (boss == null || !isServer) return false;

        // Pipiliin lang ito kung may players sa server
        bool hasPlayers = false;
        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn != null && conn.identity != null)
            {
                hasPlayers = true;
                break;
            }
        }

        // Pwedeng lagyan ng distance check kung gusto mo
        // Pero ang Vine Snare ay karaniwang "Global" attack sa arena
        return hasPlayers;
    }

    public override void Server_Execute()
    {
        // Tinatawag ang trigger para mag-play ang animation
        if (boss != null && !string.IsNullOrEmpty(animationTriggerName))
        {
            boss.Server_PlayTrigger(animationTriggerName);
        }
    }

    public override void Server_OnAnimationEvent(string eventName)
    {
        // Siguraduhin na ang string sa Animation Event ay "TriggerVineSnap"
        if (eventName == Event_TriggerVineSnap || eventName == Event_SpawnVines)
        {
            Server_SpawnVines();

            if (vulnerabilityManager != null)
                vulnerabilityManager.Server_OnAttackCompleted();
        }
    }

    [Server]
    void Server_SpawnVines()
    {
        if (vineAttackPrefab == null)
        {
            Debug.LogWarning($"[VineSnare] Prefab missing on {gameObject.name}");
            return;
        }

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
                targetPos = playerPositions[Random.Range(0, playerPositions.Count)];
                Vector2 offset = Random.insideUnitCircle * spawnOffsetRadius;
                targetPos += new Vector3(offset.x, 0f, offset.y);
            }
            else
            {
                targetPos = transform.position + Random.insideUnitSphere * 5f;
                targetPos.y = transform.position.y;
            }

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
        // Raycast pababa para lumitaw ang vines sa floor (Layer 0 o Ground)
        if (Physics.Raycast(position + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f))
            return hit.point.y;
        return position.y;
    }

    public override void Server_Stop()
    {
    }
}