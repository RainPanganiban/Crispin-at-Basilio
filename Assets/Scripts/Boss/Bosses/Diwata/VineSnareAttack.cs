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

    [Header("Spawn & Range")]
    public int vineCount = 3;
    public float spawnOffsetRadius = 2f;
    [Tooltip("Dapat pasok ang player sa range na ito bago lumabas ang vines.")]
    public float maxAttackRange = 15f;

    [Header("Sound Effects")] // --- DAGDAG: Sound Clips ---
    [SerializeField] private AudioClip vineTelegraphClip; // Tunog bago lumabas (e.g., Ground Rumbling/Roots Growing)
    [SerializeField] private AudioClip vineSnapClip;      // Tunog pag-erupt (e.g., Wood Snap/Dirt Explosion)

    [Header("Phase Requirement")]
    [Tooltip("I-set sa 1 para magsimulang lumitaw sa Phase 2 at Phase 3.")]
    public int startFromPhaseIndex = 1;

    private DiwataVulnerabilityManager vulnerabilityManager;
    private BossPhaseManager phaseManager;
    private EnemySoundManager soundManager; // --- DAGDAG: Reference ---

    public override void Initialize(BossController bossController)
    {
        base.Initialize(bossController);
        vulnerabilityManager = bossController != null ? bossController.GetComponent<DiwataVulnerabilityManager>() : null;
        phaseManager = bossController != null ? bossController.GetComponent<BossPhaseManager>() : null;
        soundManager = bossController != null ? bossController.GetComponent<EnemySoundManager>() : null;
    }

    public override bool Server_CanExecute()
    {
        if (!isServer || boss == null || !NetworkServer.active) return false;

        // --- PHASE CHECK ---
        if (phaseManager != null)
        {
            if (phaseManager.GetCurrentPhaseIndex() < startFromPhaseIndex)
                return false;
        }

        // --- DISTANCE CHECK ---
        bool isAnyPlayerInRange = false;
        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn != null && conn.identity != null)
            {
                float distance = Vector3.Distance(transform.position, conn.identity.transform.position);
                if (distance <= maxAttackRange)
                {
                    isAnyPlayerInRange = true;
                    break;
                }
            }
        }

        return isAnyPlayerInRange;
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

        // Fail-safe para hindi mag-trigger sa Phase 1
        if (phaseManager != null && phaseManager.GetCurrentPhaseIndex() < startFromPhaseIndex)
            return;

        // Visual/Audio preparation (Rumbling)
        if (eventName == Event_SpawnVines)
        {
            Rpc_PlayTelegraphSound();
        }

        // Actual attack (Snapping)
        if (eventName == Event_TriggerVineSnap)
        {
            Server_SpawnVines();
            Rpc_PlaySnapSound();

            if (vulnerabilityManager != null)
                vulnerabilityManager.Server_OnAttackCompleted();
        }
    }

    [Server]
    void Server_SpawnVines()
    {
        if (vineAttackPrefab == null) return;

        List<Vector3> validTargets = new List<Vector3>();
        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn == null || conn.identity == null) continue;

            float dist = Vector3.Distance(transform.position, conn.identity.transform.position);
            if (dist <= maxAttackRange + 10f)
            {
                validTargets.Add(conn.identity.transform.position);
            }
        }

        if (validTargets.Count == 0) return;

        for (int i = 0; i < vineCount; i++)
        {
            Vector3 targetPos = validTargets[Random.Range(0, validTargets.Count)];
            Vector2 offset = Random.insideUnitCircle * spawnOffsetRadius;
            targetPos += new Vector3(offset.x, 0f, offset.y);

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
        return position.y;
    }

    // --- AUDIO RPCs ---

    [ClientRpc]
    void Rpc_PlayTelegraphSound()
    {
        if (soundManager != null && vineTelegraphClip != null)
        {
            soundManager.PlaySpecificAttack(vineTelegraphClip);
        }
    }

    [ClientRpc]
    void Rpc_PlaySnapSound()
    {
        if (soundManager != null && vineSnapClip != null)
        {
            soundManager.PlaySpecificAttack(vineSnapClip);
        }
    }

    public override void Server_Stop() { }
}