using UnityEngine;
using Mirror;

public class bossRotation : BossMovementBase
{
    [Header("Rotation")]
    public float rotationSpeed = 5f;

    [Header("Movement")]
    public float moveSpeed = 1.2f; // Slow movement speed

    [Header("Footstep tremor (optional)")]
    public bool enableFootstepTremor = true;
    public float tremorInterval = 1.1f;
    public OngloShockwaveRing tremorRingPrefab;
    public Transform tremorOrigin;
    public float tremorDamage = 5f;
    public float tremorMaxRadius = 2.5f;
    public float tremorExpandSpeed = 8f;
    public LayerMask playerLayer;

    [Header("Phase Feedback")]
    public float burstShakeIntensity = 2f;

    private bool movementEnabled = true;
    private float nextTremorTime;
    private bool isPhase3;

    public override void OnStartServer()
    {
        nextTremorTime = Time.time + tremorInterval;
    }

    [ServerCallback]
    void Update()
    {
        if (!movementEnabled)
        {
            syncedMovementSpeed = 0f;

            if (animator != null && animator.runtimeAnimatorController != null)
                animator.SetFloat(SpeedHash, 0f);

            return;
        }

        Server_HandleRotation();

        if (enableFootstepTremor && Time.time >= nextTremorTime)
        {
            nextTremorTime = Time.time + tremorInterval;
            Server_SpawnFootstepTremor();
        }
    }

    [Server]
    void Server_HandleRotation()
    {
        Transform target = Server_FindClosestPlayer();

        if (target == null)
        {
            syncedMovementSpeed = 0f;

            if (animator != null && animator.runtimeAnimatorController != null)
                animator.SetFloat(SpeedHash, 0f);

            return;
        }

        Vector3 lookDir = target.position - transform.position;
        lookDir.y = 0f;

        if (lookDir.sqrMagnitude > 0.001f)
        {
            // Rotate toward player
            transform.forward = Vector3.Slerp(
                transform.forward,
                lookDir.normalized,
                rotationSpeed * Time.deltaTime
            );

            // Slow forward movement
            transform.position += transform.forward * moveSpeed * Time.deltaTime;
        }

        syncedMovementSpeed = moveSpeed;

        if (animator != null && animator.runtimeAnimatorController != null)
            animator.SetFloat(SpeedHash, syncedMovementSpeed);
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

        OngloShockwaveRing ring = Instantiate(
            tremorRingPrefab,
            origin.position,
            Quaternion.identity
        );

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
        isPhase3 = phase != null && phase.specialBehaviorFlag;
    }
}