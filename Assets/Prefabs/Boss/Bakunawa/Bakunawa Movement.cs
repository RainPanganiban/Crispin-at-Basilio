using UnityEngine;
using Mirror;

/// <summary>
/// Optimized Serpentine movement for Bakunawa.
/// Fixed Y-axis dipping during Phase 2 and 3.
/// Targets only living players.
/// </summary>
public class BakunawaMovement : BossMovementBase
{
    [Header("Serpentine Settings")]
    public float moveSpeed = 3f;
    public float turnSpeed = 4f;
    public float waveAmplitude = 1.5f;
    public float waveFrequency = 2f;

    [Header("Targeting")]
    public float detectionRange = 25f;
    public float stopDistance = 5f;

    [Header("Arena Constraints")]
    public Transform arenaCenter;
    public float maxRangeFromCenter = 20f;
    public float fixedYHeight = 2f;

    private bool movementEnabled = true;
    private float phaseMultiplier = 1f;
    private float waveTimer;

    public override void OnStartServer()
    {
        if (fixedYHeight == 0) fixedYHeight = transform.position.y;

        if (arenaCenter == null)
            Debug.LogWarning($"<color=orange>[Bakunawa]</color> Warning: Arena Center is missing!");

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

        // Kung walang player o patay na lahat, bumalik sa gitna
        if (target == null)
        {
            Server_MoveTowards(arenaCenter != null ? arenaCenter.position : transform.position);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, target.position);

        if (distanceToPlayer > detectionRange)
        {
            Server_MoveTowards(arenaCenter != null ? arenaCenter.position : transform.position);
        }
        else if (distanceToPlayer > stopDistance)
        {
            Server_MoveTowards(target.position);
            Server_FaceTarget(target.position);
        }
        else
        {
            Server_FaceTarget(target.position);
            UpdateAnimation(0);
        }
    }

    [Server]
    void Server_MoveTowards(Vector3 targetPos)
    {
        waveTimer += Time.deltaTime * waveFrequency;

        Vector3 direction = (targetPos - transform.position);
        direction.y = 0;
        direction.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, direction);
        Vector3 offset = right * Mathf.Sin(waveTimer) * waveAmplitude;

        Vector3 finalDirection = (direction + offset).normalized;
        float currentSpeed = moveSpeed * phaseMultiplier;

        Vector3 velocity = finalDirection * currentSpeed * Time.deltaTime;

        transform.position = new Vector3(
            transform.position.x + velocity.x,
            fixedYHeight,
            transform.position.z + velocity.z
        );

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

    // --- UPDATED: Chine-check na kung buhay ang player ---
    [Server]
    Transform Server_FindClosestPlayer()
    {
        Transform best = null;
        float bestSqr = float.PositiveInfinity;

        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn?.identity == null) continue;

            // Kunin ang Health component (PlayerStatsManager)
            PlayerStatsManager stats = conn.identity.GetComponent<PlayerStatsManager>();

            // LALAKTAWAN kung patay na (health <= 0)
            if (stats != null && stats.health.currentValue <= 0) continue;

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
    public override void Server_SetMovementEnabled(bool enabled) => movementEnabled = enabled;

    [Server]
    public override void Server_ApplyPhaseModifier(BossPhaseManager.BossPhase phase)
    {
        if (phase == null) return;

        phaseMultiplier = phase.movementSpeedMultiplier;

        if (phase.specialBehaviorFlag)
        {
            waveFrequency *= 1.3f;
            waveAmplitude *= 0.8f;
            Debug.Log($"<color=red>[Bakunawa]</color> Phase Shift Active.");
        }
    }
}