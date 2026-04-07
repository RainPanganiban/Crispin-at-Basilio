using UnityEngine;
using Mirror;
using System.Collections;

public class AtomicBreath : EnemyAttack
{
    [Header("Laser Setup")]
    [SerializeField] private GameObject laserBeamPrefab;   // Drag your laser prefab here
    [SerializeField] private Transform firePoint;          // Drag your FirePoint here

    [Header("Laser Stats")]
    [SerializeField] private float laserDamage = 15f;
    [SerializeField] private float laserDistance = 25f;
    [SerializeField] private float laserLifetime = 2f;
    [SerializeField] private float laserWidth = 0.5f;
    [SerializeField] private float tickInterval = 0.25f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Tracking")]
    [SerializeField] private float trackingSpeed = 5f;

    [Header("Animation")]
    [SerializeField] private NetworkAnimator networkAnimator;
    [SerializeField] private string attackTrigger = "EyeLaser";

    private GameObject activeLaser;
    private Collider ownerCollider;
    private NetworkIdentity ownerIdentity;

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

    // ?? Animation Event (optional charge phase)
    [ServerCallback]
    public void StartCharge()
    {
        Debug.Log("Laser charging...");
    }

    // ?? Animation Event (spawn laser)
    [ServerCallback]
    public void FireLaser()
    {
        if (laserBeamPrefab == null || firePoint == null)
        {
            Debug.LogError("Missing laser prefab or firepoint!");
            return;
        }

        // Spawn laser at firepoint
        activeLaser = Instantiate(
            laserBeamPrefab,
            firePoint.position,
            firePoint.rotation
        );

        // Initialize laser if script exists
        var beam = activeLaser.GetComponent<BungisngisLaserBeam>();
        if (beam != null)
        {
            beam.Initialize(
                laserDamage,
                laserDistance,
                laserLifetime,
                laserWidth,
                tickInterval,
                ownerCollider,
                ownerIdentity,
                playerLayer
            );
        }

        // Network spawn
        NetworkServer.Spawn(activeLaser);

        // Start tracking
        StartCoroutine(TrackLaser());
    }

    // ?? Animation Event (destroy laser)
    [ServerCallback]
    public void EndLaser()
    {
        if (activeLaser != null)
        {
            NetworkServer.Destroy(activeLaser);
        }
    }

    // ?? Simple tracking
    private IEnumerator TrackLaser()
    {
        EnemyAggro aggro = GetComponent<EnemyAggro>();

        while (activeLaser != null && firePoint != null)
        {
            // Stick to firepoint
            activeLaser.transform.position = firePoint.position;

            // Track player
            if (aggro != null && aggro.GetCurrentTarget() != null)
            {
                Transform target = aggro.GetCurrentTarget();

                Vector3 dir = (target.position - firePoint.position).normalized;
                Quaternion targetRot = Quaternion.LookRotation(dir);

                activeLaser.transform.rotation = Quaternion.Lerp(
                    activeLaser.transform.rotation,
                    targetRot,
                    trackingSpeed * Time.deltaTime
                );
            }

            yield return null;
        }
    }
}