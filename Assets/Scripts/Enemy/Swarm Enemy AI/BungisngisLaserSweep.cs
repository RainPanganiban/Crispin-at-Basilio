using UnityEngine;
using Mirror;
using System.Collections;

public class BungisngisLaserSweep : EnemyAttack
{
    [Header("Sweep Settings")]
    public GameObject laserBeamPrefab;
    public Transform firePoint;
    public float laserDamage = 10f;
    public float laserDistance = 25f;
    public float laserLifetime = 3f;
    public float laserWidth = 0.8f;
    public float tickInterval = 0.2f;
    public LayerMask playerLayer;
    
    public float sweepAngle = 90f; 
    public float sweepSpeed = 45f; 

    [Header("Animation")]
    public NetworkAnimator networkAnimator;
    public string attackTrigger = "LaserSweep";

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
    public void FireLaser()
    {
        if (laserBeamPrefab == null || firePoint == null) return;

        EnemyAggro aggro = GetComponent<EnemyAggro>();
        if (aggro != null && aggro.GetCurrentTarget() != null)
        {
             Vector3 toTarget = aggro.GetCurrentTarget().position - transform.position;
             toTarget.y = 0;
             if (toTarget.sqrMagnitude > 0.001f)
             {
                 // Start rotating before reaching center target
                 Quaternion lookRot = Quaternion.LookRotation(toTarget.normalized);
                 transform.rotation = lookRot * Quaternion.Euler(0, -sweepAngle / 2f, 0);
             }
        }

        activeLaser = Instantiate(laserBeamPrefab, firePoint.position, firePoint.rotation);
        
        var beam = activeLaser.GetComponent<BungisngisLaserBeam>();
        if (beam != null)
        {
            beam.Initialize(laserDamage, laserDistance, laserLifetime, laserWidth, tickInterval, ownerCollider, ownerIdentity, playerLayer);
        }

        NetworkServer.Spawn(activeLaser);
        StartCoroutine(SweepRoutine());
    }

    [ServerCallback]
    public void EndLaser()
    {
        if (activeLaser != null)
        {
            NetworkServer.Destroy(activeLaser);
        }
    }

    private IEnumerator SweepRoutine()
    {
        float swept = 0f;
        while (activeLaser != null && swept < sweepAngle)
        {
            float step = sweepSpeed * Time.deltaTime;
            swept += step;
            
            transform.Rotate(0, step, 0);

            if (activeLaser != null && firePoint != null)
            {
                activeLaser.transform.position = firePoint.position;
                activeLaser.transform.rotation = firePoint.rotation;
            }

            yield return null;
        }

        EndLaser();
    }
}
