using UnityEngine;
using Mirror;
using System.Collections;

/// <summary>
/// Diwata spins while emitting continuous spiral projectiles.
/// Between StartSpiral and EndSpiral animation events, continuously fires
/// petal projectiles in an expanding spiral pattern.
/// Phase 3 attack.
/// </summary>
public class SacredSpiralAttack : BaseAttack
{
    public const string Event_StartSpiral = "StartSpiral";
    public const string Event_SpawnProjectiles = "SpawnProjectiles";
    public const string Event_EndSpiral = "EndSpiral";

    [Header("Projectile Settings")]
    public DiwataPetal petalPrefab;
    public Transform spawnOrigin;
    public LayerMask playerLayer;

    [Header("Spiral Pattern")]
    public float petalSpeed = 10f;
    public float petalDamage = 10f;
    public float petalLifetime = 4f;
    public float fireInterval = 0.15f;   // time between each petal
    public float rotationSpeed = 90f;    // degrees per second for spiral rotation
    public int petalsPerBurst = 2;       // petals spawned per interval (opposite directions)

    [Header("Duration")]
    public float spiralDuration = 4f;

    private Coroutine spiralCoroutine;
    private DiwataVulnerabilityManager vulnerabilityManager;

    public override void Initialize(BossController bossController)
    {
        base.Initialize(bossController);
        vulnerabilityManager = bossController != null ? bossController.GetComponent<DiwataVulnerabilityManager>() : null;
    }

    public override void Server_Execute()
    {
        // Animation-driven
    }

    public override void Server_OnAnimationEvent(string eventName)
    {
        if (eventName == Event_StartSpiral)
        {
            if (spiralCoroutine != null)
                StopCoroutine(spiralCoroutine);
            spiralCoroutine = StartCoroutine(Server_SpiralRoutine());
        }
        else if (eventName == Event_EndSpiral)
        {
            if (spiralCoroutine != null)
            {
                StopCoroutine(spiralCoroutine);
                spiralCoroutine = null;
            }

            if (vulnerabilityManager != null)
                vulnerabilityManager.Server_OnAttackCompleted();
        }
    }

    [Server]
    IEnumerator Server_SpiralRoutine()
    {
        float elapsed = 0f;
        float currentAngle = 0f;

        while (elapsed < spiralDuration)
        {
            Server_FireSpiralBurst(currentAngle);
            currentAngle += rotationSpeed * fireInterval;
            elapsed += fireInterval;
            yield return new WaitForSeconds(fireInterval);
        }

        spiralCoroutine = null;

        if (vulnerabilityManager != null)
            vulnerabilityManager.Server_OnAttackCompleted();
    }

    [Server]
    void Server_FireSpiralBurst(float baseAngle)
    {
        if (petalPrefab == null) return;

        Transform origin = spawnOrigin != null ? spawnOrigin : transform;
        float angleStep = 360f / petalsPerBurst;

        for (int i = 0; i < petalsPerBurst; i++)
        {
            float angle = (baseAngle + angleStep * i) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

            DiwataPetal petal = Instantiate(petalPrefab, origin.position, Quaternion.identity);
            petal.Server_Initialize(
                owner: boss != null ? boss.netIdentity : null,
                direction: dir,
                speed: petalSpeed,
                damage: petalDamage,
                lifetime: petalLifetime,
                playerLayer: playerLayer
            );

            NetworkServer.Spawn(petal.gameObject);
        }
    }

    public override void Server_Stop()
    {
        if (spiralCoroutine != null)
        {
            StopCoroutine(spiralCoroutine);
            spiralCoroutine = null;
        }
    }
}
