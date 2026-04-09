using UnityEngine;
using Mirror;

/// <summary>
/// Aerial floating movement for Diwata.
/// Drifts around an arena center at a configurable hover height,
/// occasionally teleports to new positions on phase changes.
/// Includes an activation trigger to enable attacks only when player is near.
/// </summary>
public class DiwataMovement : BossMovementBase
{
    [Header("Activation Settings")]
    public float activationDistance = 12f; // Gaano kalapit ang player bago siya mag-start
    private bool isPlayerDetected = false;

    [Header("Hover")]
    public float hoverHeight = 6f;
    public float hoverBobAmplitude = 0.3f;
    public float hoverBobFrequency = 1.2f;

    [Header("Drift")]
    public float driftSpeed = 1.5f;
    public float driftChangeInterval = 3f;

    [Header("Arena")]
    public Transform arenaCenter;
    public float arenaRadius = 12f;

    [Header("Teleport (phase transition)")]
    public float teleportCooldown = 8f;

    [Header("Facing")]
    public float turnSpeed = 5f;

    private bool movementEnabled = true;
    private Vector3 driftDirection;
    private float nextDriftChangeTime;
    private float currentSpeed;
    private float phaseSpeedMultiplier = 1f;
    private float bobPhase;

    public override void OnStartServer()
    {
        nextDriftChangeTime = Time.time + driftChangeInterval;
        Server_PickNewDriftDirection();
        bobPhase = Random.Range(0f, Mathf.PI * 2f);

        if (arenaCenter == null)
            Debug.LogWarning($"[DiwataMovement] Arena Center not assigned on {gameObject.name}.");

        // Siguraduhin na naka-reset ang detection sa simula ng server
        Server_ResetDetection();
    }

    [ServerCallback]
    void Update()
    {
        // Force animator speed to 1 regardless of movement
        if (animator != null)
        {
            animator.speed = 1f;
        }

        if (!movementEnabled) return;

        // --- PROXIMITY CHECK & AUTO-ACTIVATION ---
        if (!isPlayerDetected)
        {
            Transform targetPlayer = Server_FindClosestPlayer();
            if (targetPlayer != null)
            {
                float dist = Vector3.Distance(transform.position, targetPlayer.position);
                if (dist <= activationDistance)
                {
                    isPlayerDetected = true;
                    Server_ActivateBossLogic();
                }
                else
                {
                    // Look at player maski malayo, pero wag gagalaw o aatake
                    Server_FaceClosestPlayer();
                    return;
                }
            }
            else
            {
                return; // Wait hanggang may player
            }
        }

        // --- MOVEMENT LOGIC (Gagana lang kapag Detected na) ---
        Server_HandleDrift();
        Server_HandleHoverBob();
        Server_FaceClosestPlayer();
    }

    [Server]
    public void Server_ResetDetection()
    {
        isPlayerDetected = false;

        // I-disable ang controller para huminto ang pag-atake kapag malayo
        if (TryGetComponent<BossController>(out var controller))
        {
            controller.enabled = false;
        }

        Debug.Log($"[DiwataMovement] Detection reset for {gameObject.name}. Waiting for players.");
    }

    [Server]
    void Server_ActivateBossLogic()
    {
        // 1. I-enable ang BossController (Brain)
        if (TryGetComponent<BossController>(out var controller))
        {
            controller.enabled = true;
            Debug.Log($"[DiwataMovement] Boss Controller activated for {gameObject.name}");
        }

        // 2. I-enable lahat ng script na may "Attack" o "Health" sa pangalan
        MonoBehaviour[] allScripts = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in allScripts)
        {
            string scriptName = script.GetType().Name;
            if (scriptName.Contains("Attack") || scriptName.Contains("Health"))
            {
                script.enabled = true;
                Debug.Log($"[DiwataMovement] Script activated: {scriptName}");
            }
        }
    }

    // --- FIX: ANIMATION EVENT RECEIVERS ---
    // Inilagay ito dito para mawala ang error sa console mo
    public void Server_OnAnimationEvent(string eventName)
    {
        if (!isServer) return;
        Debug.Log($"[Diwata Animation Event] {eventName} triggered.");
    }

    public void Server_OnAttackAnimationComplete()
    {
        if (!isServer) return;
        Debug.Log("[Diwata Animation] Attack animation completed.");
    }

    [Server]
    void Server_HandleDrift()
    {
        if (Time.time >= nextDriftChangeTime)
        {
            Server_PickNewDriftDirection();
            nextDriftChangeTime = Time.time + driftChangeInterval;
        }

        currentSpeed = driftSpeed * phaseSpeedMultiplier;
        Vector3 move = driftDirection * currentSpeed * Time.deltaTime;
        Vector3 newPos = transform.position + move;

        // Clamp within arena radius (XZ only)
        if (arenaCenter != null)
        {
            Vector3 centerXZ = new Vector3(arenaCenter.position.x, 0f, arenaCenter.position.z);
            Vector3 posXZ = new Vector3(newPos.x, 0f, newPos.z);
            if (Vector3.Distance(posXZ, centerXZ) > arenaRadius)
            {
                // Redirect toward center
                Vector3 toCenter = (centerXZ - posXZ).normalized;
                driftDirection = toCenter;
                move = driftDirection * currentSpeed * Time.deltaTime;
                newPos = transform.position + move;
            }
        }

        transform.position = newPos;

        syncedMovementSpeed = currentSpeed;
        if (animator != null && animator.runtimeAnimatorController != null)
            animator.SetFloat(SpeedHash, currentSpeed);
    }

    [Server]
    void Server_HandleHoverBob()
    {
        bobPhase += Time.deltaTime * hoverBobFrequency * Mathf.PI * 2f;
        float baseY = arenaCenter != null ? arenaCenter.position.y + hoverHeight : hoverHeight;
        float bob = Mathf.Sin(bobPhase) * hoverBobAmplitude;

        Vector3 pos = transform.position;
        pos.y = baseY + bob;
        transform.position = pos;
    }

    [Server]
    void Server_FaceClosestPlayer()
    {
        Transform target = Server_FindClosestPlayer();
        if (target == null)
            return;

        Vector3 lookDir = (target.position - transform.position);
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.001f)
            transform.forward = Vector3.Slerp(transform.forward, lookDir.normalized, turnSpeed * Time.deltaTime);
    }

    [Server]
    void Server_PickNewDriftDirection()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        driftDirection = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
    }

    [Server]
    public void Server_TeleportToRandomPosition()
    {
        if (arenaCenter == null)
            return;

        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float radius = Random.Range(arenaRadius * 0.3f, arenaRadius * 0.7f);
        Vector3 newPos = arenaCenter.position + new Vector3(
            Mathf.Cos(angle) * radius,
            hoverHeight,
            Mathf.Sin(angle) * radius
        );

        transform.position = newPos;
        Rpc_OnTeleport(newPos);
    }

    [ClientRpc]
    void Rpc_OnTeleport(Vector3 position)
    {
        Debug.Log($"[DiwataMovement] Diwata teleported to {position}");
    }

    [Server]
    public void Server_FallToGround()
    {
        float groundY = arenaCenter != null ? arenaCenter.position.y : 0f;
        Vector3 pos = transform.position;
        pos.y = groundY;
        transform.position = pos;
    }

    [Server]
    public void Server_ReturnToAir()
    {
        float airY = arenaCenter != null ? arenaCenter.position.y + hoverHeight : hoverHeight;
        Vector3 pos = transform.position;
        pos.y = airY;
        transform.position = pos;
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
    public override void Server_SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
    }

    [Server]
    public override void Server_ApplyPhaseModifier(BossPhaseManager.BossPhase phase)
    {
        if (phase == null) return;
        phaseSpeedMultiplier = phase.movementSpeedMultiplier;

        if (phase.specialBehaviorFlag)
            Server_TeleportToRandomPosition();
    }
}