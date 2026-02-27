using UnityEngine;
using Mirror;

public class OngloMovement : BossMovementBase
{
    [Header("Chase (Phase 1 & 2)")]
    public float chaseSpeed = 1.5f;
    public float chaseStopDistance = 3f;
    public float chaseStartDistance = 6f;

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

    [Header("Phase Feedback")]
    public float burstShakeIntensity = 2f;

    private bool movementEnabled = true;
    private float nextTremorTime;
    private float burstEndTime;
    private float nextBurstTime;
    private bool isPhase3;
    private bool isChasing;

    public override void OnStartServer()
    {
        nextTremorTime = Time.time + tremorInterval;
        nextBurstTime = Time.time + burstCooldown;

        if (arenaCenter == null)
            Debug.LogWarning($"[OngloMovement] Arena Center is not assigned on {gameObject.name}. Center-control movement will be disabled.");
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

        if (isPhase3)
        {
            Server_HandlePhase3Bursts();
        }
        else
        {
            Server_HandleStandardMovement();
        }

        if (enableFootstepTremor && Time.time >= nextTremorTime)
        {
            nextTremorTime = Time.time + tremorInterval;
            Server_SpawnFootstepTremor();
        }
    }

    [Server]
    void Server_HandleStandardMovement()
    {
        Transform target = Server_FindClosestPlayer();
        
        // If no player, go home
        if (target == null)
        {
            isChasing = false;
            Server_HandleCenterControl();
            return;
        }

        float distToPlayer = Vector3.Distance(transform.position, target.position);

        // State Machine: Chase vs Return to Center
        if (isChasing)
        {
            // Chase logic: stay chasing until very close
            if (distToPlayer < chaseStopDistance)
            {
                isChasing = false;
            }
            else
            {
                // Chase player
                Vector3 dir = (target.position - transform.position).normalized;
                dir.y = 0f;
                transform.position += dir * chaseSpeed * Time.deltaTime;
                transform.forward = Vector3.Slerp(transform.forward, dir, 8f * Time.deltaTime);
                
                syncedMovementSpeed = chaseSpeed;
                if (animator != null && animator.runtimeAnimatorController != null) 
                    animator.SetFloat(SpeedHash, chaseSpeed);
                return;
            }
        }
        else
        {
            // Territorial logic: If player enters chase range, prioritize them
            if (distToPlayer > chaseStartDistance && distToPlayer < (chaseStartDistance * 2f))
            {
                isChasing = true;
                return; 
            }
            
            // If player is quite close (within chase/attack range zone), don't go back to center!
            // Just look at the player and wait for attack manager to pick one.
            if (distToPlayer < chaseStartDistance)
            {
                Vector3 lookDir = (target.position - transform.position).normalized;
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.001f)
                    transform.forward = Vector3.Slerp(transform.forward, lookDir, 5f * Time.deltaTime);
                
                syncedMovementSpeed = 0f;
                if (animator != null && animator.runtimeAnimatorController != null) 
                    animator.SetFloat(SpeedHash, 0f);
                return;
            }
        }

        // If player is very far away or target is null, return to center
        Server_HandleCenterControl();
    }

    [Server]
    void Server_HandleCenterControl()
    {
        if (arenaCenter == null)
        {
            if (animator != null) animator.SetFloat(SpeedHash, 0f);
            return;
        }

        Vector3 toCenter = arenaCenter.position - transform.position;
        toCenter.y = 0f;
        float dist = toCenter.magnitude;

        if (dist <= preferredRadius)
        {
            if (animator != null) animator.SetFloat(SpeedHash, 0f);
            return;
        }

        Vector3 dir = toCenter.normalized;
        transform.position += dir * walkSpeed * Time.deltaTime;

        syncedMovementSpeed = walkSpeed;
        if (animator != null && animator.runtimeAnimatorController != null) 
            animator.SetFloat(SpeedHash, walkSpeed);

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
            
            syncedMovementSpeed = burstSpeed;
            if (animator != null && animator.runtimeAnimatorController != null) 
                animator.SetFloat(SpeedHash, burstSpeed);
            
            // Thrilling: Faster turn speed during enrage burst
            transform.forward = Vector3.Slerp(transform.forward, dir, 15f * Time.deltaTime);
            return;
        }

        // Post-burst recovery delay
        if (Time.time < burstEndTime + 0.5f)
            return;

        burstEndTime = Time.time + burstDuration;
        nextBurstTime = Time.time + burstCooldown;
        
        Rpc_OnBurstStarted();
    }

    [ClientRpc]
    void Rpc_OnBurstStarted()
    {
        // Feedback: Dust clouds, screen rumble, etc.
        Debug.Log("[OngloMovement] Enrage burst started!");
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

