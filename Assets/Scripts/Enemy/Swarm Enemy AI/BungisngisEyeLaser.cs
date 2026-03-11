using UnityEngine;
using Mirror;
using System.Collections;

public class BungisngisEyeLaser : EnemyAttack
{
    [Header("Laser Settings")]
    public GameObject laserBeamPrefab;
    public Transform firePoint;
    public float laserDamage = 15f;
    public float laserDistance = 25f;
    public float laserLifetime = 2f;
    public float laserWidth = 0.5f;
    public float tickInterval = 0.25f;
    public LayerMask playerLayer;

    [Header("Tracking Settings")]
    [Tooltip("Initial rotation speed when the laser starts tracking the player")]
    public float initialTrackingSpeed = 5f;
    [Tooltip("How much the tracking speed increases per second while firing")]
    public float trackingAcceleration = 15f;
    [Tooltip("Angle in degrees to offset the initial aim so it spawns near the player, not directly on them")]
    public float initialAimOffsetAngle = 25f;
    [Tooltip("Vertical offset to aim the laser higher up on the player's body instead of their feet")]
    public float targetVerticalOffset = 1.0f;

    [Header("Animation")]
    public NetworkAnimator networkAnimator;
    public string attackTrigger = "EyeLaser";

    private Collider ownerCollider;
    private NetworkIdentity ownerIdentity;
    private GameObject activeLaser;

    void Awake()
    {
        ownerCollider = GetComponent<Collider>();
        ownerIdentity = netIdentity;
    }

    protected override void OnExecute()
    {
        if (networkAnimator != null)
        {
            networkAnimator.SetTrigger(attackTrigger);
        }
    }

    [ServerCallback]
    public void StartCharge()
    {
        // Animation event placeholder for telegraph effect
    }

    [ServerCallback]
    public void FireLaser()
    {
        Debug.Log($"[BungisngisEyeLaser] FireLaser event received on {name}");

        if (laserBeamPrefab == null)
        {
            Debug.LogError($"[BungisngisEyeLaser] laserBeamPrefab is NULL on {name}");
            return;
        }
        if (firePoint == null)
        {
            Debug.LogError($"[BungisngisEyeLaser] firePoint is NULL on {name}");
            return;
        }

        Quaternion spawnRotation = firePoint.rotation;
        
        EnemyAggro aggro = GetComponent<EnemyAggro>();
        if (aggro != null && aggro.GetCurrentTarget() != null)
        {
             Transform t = aggro.GetCurrentTarget();
             
             // Ensure enemy body faces current target horizontally
             Vector3 toTarget = t.position - transform.position;
             toTarget.y = 0;
             if (toTarget.sqrMagnitude > 0.001f)
             {
                 transform.rotation = Quaternion.LookRotation(toTarget.normalized);
             }

             // Aim the laser specifically at the player to ensure it tilts downward
             Vector3 targetCenter = t.position + Vector3.up * targetVerticalOffset;
             Vector3 dirToTarget = (targetCenter - firePoint.position).normalized;
             
             // Add a random offset so it spawns to the side of the player
             float randomSign = Random.value > 0.5f ? 1f : -1f;
             Quaternion offsetRotation = Quaternion.AngleAxis(initialAimOffsetAngle * randomSign, Vector3.up);
             dirToTarget = offsetRotation * dirToTarget;
             
             spawnRotation = Quaternion.LookRotation(dirToTarget);
        }

        activeLaser = Instantiate(laserBeamPrefab, firePoint.position, spawnRotation);
        Debug.Log($"[BungisngisEyeLaser] Laser object instantiated: {activeLaser.name}");
        
        var beam = activeLaser.GetComponent<BungisngisLaserBeam>();
        if (beam != null)
        {
            beam.Initialize(laserDamage, laserDistance, laserLifetime, laserWidth, tickInterval, ownerCollider, ownerIdentity, playerLayer);
        }
        else
        {
            Debug.LogError($"[BungisngisEyeLaser] BungisngisLaserBeam component missing on prefab!");
        }

        NetworkServer.Spawn(activeLaser);
        Debug.Log($"[BungisngisEyeLaser] Laser spawned on network.");
        StartCoroutine(UpdateLaserPosition());
    }

    [ServerCallback]
    public void EndLaser()
    {
        Debug.Log($"[BungisngisEyeLaser] EndLaser event received on {name}");
        if (activeLaser != null)
        {
            NetworkServer.Destroy(activeLaser);
        }
    }

    private IEnumerator UpdateLaserPosition()
    {
        EnemyAggro aggro = GetComponent<EnemyAggro>();
        float currentSpeed = initialTrackingSpeed;
        
        // Wait until activeLaser is actually available
        if (activeLaser == null) yield break;
        
        Quaternion currentRotation = activeLaser.transform.rotation;

        while (activeLaser != null && firePoint != null)
        {
            activeLaser.transform.position = firePoint.position;

            Transform target = aggro != null ? aggro.GetCurrentTarget() : null;

            if (target != null)
            {
                // Account for the vertical offset to aim at body, not feet
                Vector3 targetCenter = target.position + Vector3.up * targetVerticalOffset;

                // Check if the laser is currently hitting a player
                Vector3 laserDirection = currentRotation * Vector3.forward;
                bool isHittingPlayer = Physics.SphereCast(firePoint.position, laserWidth / 2f, laserDirection, out RaycastHit hit, laserDistance, playerLayer);

                if (!isHittingPlayer)
                {
                    // Accelerate the tracking over time if we haven't caught them
                    currentSpeed += trackingAcceleration * Time.deltaTime;
                }
                else
                {
                    // If it hits the player, drop the speed back to initial so they can escape it
                    currentSpeed = initialTrackingSpeed;
                }

                // Determine where we should aim
                Vector3 dirToTarget = (targetCenter - firePoint.position).normalized;
                Quaternion desiredRotation = Quaternion.LookRotation(dirToTarget);

                // Rotate smoothly towards the player
                currentRotation = Quaternion.RotateTowards(currentRotation, desiredRotation, currentSpeed * Time.deltaTime);
            }
            else
            {
                // Fallback to pointing forward if we lose the target
                currentRotation = firePoint.rotation;
            }

            activeLaser.transform.rotation = currentRotation;

            yield return null;
        }
    }
}
