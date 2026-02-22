using UnityEngine;
using Mirror;

public class OngloMovement : BossMovementBase
{
    [Header("Center control")]
    public Transform arenaCenter;
    public float preferredRadius = 4f;

    [Header("Walk")]
    public float walkSpeed = 1.2f;

    [Header("Enrage burst (Phase 3)")]
    public float burstSpeed = 4.5f;
    public float burstDuration = 0.8f;
    public float burstCooldown = 6f;

    [Header("Footstep tremor (optional)")]
    public bool enableFootstepTremor = true;
    public float tremorInterval = 1.1f;
    public OngloShockwaveRing tremorRingPrefab;
    public Transform tremorOrigin;
    public float tremorDamage = 5f;
    public float tremorMaxRadius = 2.5f;
    public float tremorExpandSpeed = 8f;
    public LayerMask playerLayer;

    private bool movementEnabled = true;
    private float nextTremorTime;
    private float burstEndTime;
    private float nextBurstTime;
    private bool isPhase3;

    public override void OnStartServer()
    {
        nextTremorTime = Time.time + tremorInterval;
        nextBurstTime = Time.time + burstCooldown;
    }

    [ServerCallback]
    void Update()
    {
        if (!movementEnabled)
            return;

        Server_HandleCenterControl();

        if (enableFootstepTremor && Time.time >= nextTremorTime)
        {
            nextTremorTime = Time.time + tremorInterval;
            Server_SpawnFootstepTremor();
        }

        if (isPhase3)
            Server_HandlePhase3Bursts();
    }

    [Server]
    void Server_HandleCenterControl()
    {
        if (arenaCenter == null)
            return;

        Vector3 toCenter = arenaCenter.position - transform.position;
        toCenter.y = 0f;
        float dist = toCenter.magnitude;

        if (dist <= preferredRadius)
            return;

        Vector3 dir = toCenter.normalized;
        transform.position += dir * walkSpeed * Time.deltaTime;

        if (dir.sqrMagnitude > 0.001f)
            transform.forward = Vector3.Slerp(transform.forward, dir, 8f * Time.deltaTime);
    }

    [Server]
    void Server_HandlePhase3Bursts()
    {
        if (Time.time < nextBurstTime)
            return;

        if (Time.time < burstEndTime)
        {
            Transform target = Server_FindClosestPlayer();
            if (target == null)
                return;

            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.25f)
                return;

            Vector3 dir = toTarget.normalized;
            transform.position += dir * burstSpeed * Time.deltaTime;
            transform.forward = Vector3.Slerp(transform.forward, dir, 12f * Time.deltaTime);
            return;
        }

        burstEndTime = Time.time + burstDuration;
        nextBurstTime = Time.time + burstCooldown;
    }

    [Server]
    Transform Server_FindClosestPlayer()
    {
        Transform best = null;
        float bestSqr = float.PositiveInfinity;

        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn == null || conn.identity == null)
                continue;

            Transform t = conn.identity.transform;
            float d = (t.position - transform.position).sqrMagnitude;
            if (d < bestSqr)
            {
                bestSqr = d;
                best = t;
            }
        }

        return best;
    }

    [Server]
    void Server_SpawnFootstepTremor()
    {
        if (tremorRingPrefab == null)
            return;

        Transform origin = tremorOrigin != null ? tremorOrigin : transform;
        OngloShockwaveRing ring = Instantiate(tremorRingPrefab, origin.position, Quaternion.identity);
        ring.Server_Initialize(
            owner: netIdentity,
            damage: tremorDamage,
            expandSpeed: tremorExpandSpeed,
            maxRadius: tremorMaxRadius,
            playerLayer: playerLayer
        );

        NetworkServer.Spawn(ring.gameObject);
    }

    [Server]
    public override void Server_SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
    }

    [Server]
    public override void Server_ApplyPhaseModifier(BossPhaseManager.BossPhase phase)
    {
        // Phase 3 is expected to be the final phase in Onglo's setup.
        // We infer it by the designer setting specialBehaviorFlag = true for phase 3.
        isPhase3 = phase != null && phase.specialBehaviorFlag;
    }
}

