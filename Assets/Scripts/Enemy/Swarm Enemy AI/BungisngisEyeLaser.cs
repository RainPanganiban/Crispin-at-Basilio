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
        if (laserBeamPrefab == null || firePoint == null) return;

        // Ensure enemy faces current target when firing
        EnemyAggro aggro = GetComponent<EnemyAggro>();
        if (aggro != null && aggro.GetCurrentTarget() != null)
        {
             Vector3 toTarget = aggro.GetCurrentTarget().position - transform.position;
             toTarget.y = 0;
             if (toTarget.sqrMagnitude > 0.001f)
             {
                 transform.rotation = Quaternion.LookRotation(toTarget.normalized);
             }
        }

        activeLaser = Instantiate(laserBeamPrefab, firePoint.position, firePoint.rotation);
        
        var beam = activeLaser.GetComponent<BungisngisLaserBeam>();
        if (beam != null)
        {
            beam.Initialize(laserDamage, laserDistance, laserLifetime, laserWidth, tickInterval, ownerCollider, ownerIdentity, playerLayer);
        }

        NetworkServer.Spawn(activeLaser);
        StartCoroutine(UpdateLaserPosition());
    }

    [ServerCallback]
    public void EndLaser()
    {
        if (activeLaser != null)
        {
            NetworkServer.Destroy(activeLaser);
        }
    }

    private IEnumerator UpdateLaserPosition()
    {
        while (activeLaser != null && firePoint != null)
        {
            activeLaser.transform.position = firePoint.position;
            activeLaser.transform.rotation = firePoint.rotation;
            yield return null;
        }
    }
}
