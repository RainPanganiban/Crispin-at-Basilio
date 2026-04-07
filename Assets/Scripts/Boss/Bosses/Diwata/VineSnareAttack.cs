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

    public override bool Server_CanExecute()
    {
        // FIX: Idinagdag ang NetworkServer.active check para iwas error sa transition
        if (!isServer || boss == null || !NetworkServer.active) return false;

        bool hasPlayers = false;
        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn != null && conn.identity != null)
            {
                hasPlayers = true;
                break;
            }
        }
        return hasPlayers;
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
        // FIX: Sinigurado na server-only ang execution nito
        if (!isServer || !NetworkServer.active) return;

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
                // Pumili ng random na player para tubuan ng vines
                targetPos = playerPositions[Random.Range(0, playerPositions.Count)];
                Vector2 offset = Random.insideUnitCircle * spawnOffsetRadius;
                targetPos += new Vector3(offset.x, 0f, offset.y);
            }
            else
            {
                // Fallback kung biglang nawala ang players
                targetPos = transform.position + (Random.insideUnitSphere * 5f);
            }

            targetPos.y = GetGroundY(targetPos);

            DiwataVineSnare vine = Instantiate(vineAttackPrefab, targetPos, Quaternion.identity);

            // Siguraduhin na ang vine prefab ay may Network Identity
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
        // Raycast pababa para lumitaw ang vines sa floor (Dapat may Collider ang ground mo)
        if (Physics.Raycast(position + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f))
            return hit.point.y;
        return position.y;
    }

    public override void Server_Stop() { }
}