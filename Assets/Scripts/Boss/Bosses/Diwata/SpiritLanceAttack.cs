using UnityEngine;
using Mirror;

/// <summary>
/// Light spears appear around arena edges and fire toward the center.
/// Forces players to dodge between projectile lanes.
/// </summary>
public class SpiritLanceAttack : BaseAttack
{
    public const string Event_SpawnLances = "SpawnLances";
    public const string Event_FireLances = "FireLances";

    [Header("Lance Settings")]
    public DiwataSpiritLance lancePrefab;
    public Transform arenaCenter;
    public LayerMask playerLayer;

    [Header("Spawn")]
    public float arenaEdgeRadius = 14f;
    public float spawnHeight = 2f;

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

    public override void Server_Execute()
    {
        // Animation-driven
    }

    public override void Server_OnAnimationEvent(string eventName)
    {
        if (eventName == Event_FireLances)
        {
            int count = Server_GetLanceCount();
            Server_SpawnLances(count);

            if (vulnerabilityManager != null)
                vulnerabilityManager.Server_OnAttackCompleted();
        }
    }

    int Server_GetLanceCount()
    {
        int idx = phaseManager != null ? phaseManager.GetCurrentPhaseIndex() : 0;
        return idx switch
        {
            0 => phase2Lances, // In case used early
            1 => phase2Lances,
            _ => phase3Lances,
        };
    }

    [Server]
    void Server_SpawnLances(int count)
    {
        if (lancePrefab == null) return;

        Vector3 center = arenaCenter != null ? arenaCenter.position : transform.position;
        float angleStep = 360f / count;
        float startAngle = Random.Range(0f, 360f);

        for (int i = 0; i < count; i++)
        {
            float angle = (startAngle + angleStep * i) * Mathf.Deg2Rad;
            Vector3 edgePos = center + new Vector3(
                Mathf.Cos(angle) * arenaEdgeRadius,
                spawnHeight,
                Mathf.Sin(angle) * arenaEdgeRadius
            );

            Vector3 direction = (center - edgePos).normalized;
            direction.y = 0f;

            DiwataSpiritLance lance = Instantiate(lancePrefab, edgePos, Quaternion.identity);
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

        Rpc_OnLancesFired();
    }

    [ClientRpc]
    void Rpc_OnLancesFired()
    {
        Debug.Log("[SpiritLance] Lances fired from arena edges!");
    }

    public override void Server_Stop()
    {
        // Nothing persistent to stop
    }
}
