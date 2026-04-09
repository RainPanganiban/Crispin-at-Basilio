using UnityEngine;
using Mirror;

/// <summary>
/// Serpentine movement for Bakunawa (Sea Dragon).
/// Moves in an S-pattern towards the player and tracks their position.
/// Includes visual logs in the console to check tracking status.
/// </summary>
public class BakunawaMovement : BossMovementBase
{
    [Header("Serpentine Settings")]
    public float moveSpeed = 3f;
    public float turnSpeed = 4f;
    public float waveAmplitude = 1.5f; // Lawak ng slither (S-pattern)
    public float waveFrequency = 2f;  // Bilis ng pagkumpas ng katawan

    [Header("Targeting")]
    public float detectionRange = 25f; // Distansya bago ka niya mapansin
    public float stopDistance = 5f;    // Distansya kung saan hihinto siya para tumitig/umatake

    [Header("Arena Constraints")]
    public Transform arenaCenter;
    public float maxRangeFromCenter = 20f;

    private bool movementEnabled = true;
    private float phaseMultiplier = 1f;
    private float waveTimer;

    public override void OnStartServer()
    {
        if (arenaCenter == null)
            Debug.LogWarning($"<color=orange>[Bakunawa]</color> Warning: Arena Center is missing on {gameObject.name}!");

        Debug.Log("<color=cyan>[Bakunawa]</color> Movement System Initialized.");
    }

    [ServerCallback]
    void Update()
    {
        if (!movementEnabled)
        {
            UpdateAnimation(0);
            return;
        }

        Server_HandleMovement();
    }

    [Server]
    void Server_HandleMovement()
    {
        Transform target = Server_FindClosestPlayer();

        // 1. Check kung may player sa paligid
        if (target == null)
        {
            Server_MoveTowards(arenaCenter != null ? arenaCenter.position : transform.position);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, target.position);

        // 2. Logic depende sa layo ng player
        if (distanceToPlayer > detectionRange)
        {
            // Sobrang layo ng player, balik sa gitna
            Server_MoveTowards(arenaCenter != null ? arenaCenter.position : transform.position);
        }
        else if (distanceToPlayer > stopDistance)
        {
            // Habulin ang player
            Server_MoveTowards(target.position);
            Server_FaceTarget(target.position);
        }
        else
        {
            // Malapit na, titig na lang (Attack Range)
            Server_FaceTarget(target.position);
            UpdateAnimation(0);
        }
    }

    [Server]
    void Server_MoveTowards(Vector3 targetPos)
    {
        waveTimer += Time.deltaTime * waveFrequency;

        // --- SERPENTINE MATH (The Slither) ---
        Vector3 direction = (targetPos - transform.position).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, direction);
        Vector3 offset = right * Mathf.Sin(waveTimer) * waveAmplitude;

        Vector3 finalDirection = (direction + offset).normalized;
        float currentSpeed = moveSpeed * phaseMultiplier;

        // Apply Movement
        transform.position += finalDirection * currentSpeed * Time.deltaTime;

        // Rotation towards movement direction
        if (finalDirection != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(finalDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }

        UpdateAnimation(currentSpeed);
    }

    [Server]
    void Server_FaceTarget(Vector3 targetPos)
    {
        Vector3 dir = (targetPos - transform.position).normalized;
        dir.y = 0;

        if (dir.sqrMagnitude > 0.001f)
        {
            float angleToPlayer = Vector3.Angle(transform.forward, dir);
            float dist = Vector3.Distance(transform.position, targetPos);

            // --- TRACKING LOGS ---
            if (angleToPlayer > 10f)
            {
                Debug.Log($"<color=yellow>[Bakunawa]</color> Tracking Player... (Angle: {angleToPlayer:F1}°, Dist: {dist:F1}m)");
            }
            else
            {
                Debug.Log($"<color=green>[Bakunawa]</color> Locked on Player! (Dist: {dist:F1}m)");
            }

            // Humarap sa player
            Quaternion lookRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, turnSpeed * Time.deltaTime);
        }
    }

    void UpdateAnimation(float speed)
    {
        syncedMovementSpeed = speed;
        if (animator != null)
        {
            animator.SetFloat(SpeedHash, speed);
        }
    }

    [Server]
    Transform Server_FindClosestPlayer()
    {
        Transform best = null;
        float bestSqr = float.PositiveInfinity;

        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn?.identity == null) continue;
            float d = (conn.identity.transform.position - transform.position).sqrMagnitude;
            if (d < bestSqr)
            {
                bestSqr = d;
                best = conn.identity.transform;
            }
        }
        return best;
    }

    [Server]
    public override void Server_SetMovementEnabled(bool enabled) => movementEnabled = enabled;

    [Server]
    public override void Server_ApplyPhaseModifier(BossPhaseManager.BossPhase phase)
    {
        if (phase == null) return;
        phaseMultiplier = phase.movementSpeedMultiplier;

        if (phase.specialBehaviorFlag)
        {
            waveFrequency *= 1.5f;
            Debug.Log("<color=red>[Bakunawa]</color> PHASE CHANGE: Movement speed increased!");
        }
    }
}