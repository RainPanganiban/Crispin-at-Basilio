using UnityEngine;
using Mirror;
using System.Collections;

public class AtomicBreath : EnemyAttack
{
    [Header("Laser Setup")]
    [SerializeField] private GameObject laserBeamPrefab;
    [SerializeField] private Transform firePoint;

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

    // =========================
    // ANIMATION EVENT: CHARGE
    // =========================
    public void StartCharge()
    {
        Debug.Log("Laser charging...");
    }

    // =========================
    // ANIMATION EVENT: FIRE
    // =========================
    public void FireLaser()
    {
        Debug.Log("FireLaser called | isServer: " + isServer);

        if (isServer)
        {
            SpawnLaser();
        }
        else
        {
            CmdFireLaser();
        }
    }

    [Command]
    private void CmdFireLaser()
    {
        SpawnLaser();
    }

    private void SpawnLaser()
    {
        if (laserBeamPrefab == null || firePoint == null)
        {
            Debug.LogError("Missing laser prefab or firepoint!");
            return;
        }

        activeLaser = Instantiate(
            laserBeamPrefab,
            firePoint.position,
            firePoint.rotation
        );

        NetworkServer.Spawn(activeLaser);

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

        StartCoroutine(TrackLaser());
    }

    // =========================
    // ANIMATION EVENT: END
    // =========================
    public void EndLaser()
    {
        if (isServer)
        {
            DestroyLaser();
        }
        else
        {
            CmdEndLaser();
        }
    }

    [Command]
    private void CmdEndLaser()
    {
        DestroyLaser();
    }

    private void DestroyLaser()
    {
        if (activeLaser != null)
        {
            NetworkServer.Destroy(activeLaser);
        }
    }

    // =========================
    // NEAREST PLAYER FINDER
    // =========================
    private Transform GetNearestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        Transform nearest = null;
        float minDist = Mathf.Infinity;

        foreach (GameObject p in players)
        {
            float dist = Vector3.Distance(transform.position, p.transform.position);

            if (dist < minDist)
            {
                minDist = dist;
                nearest = p.transform;
            }
        }

        return nearest;
    }

    // =========================
    // TRACKING + AIM FIX
    // =========================
    private IEnumerator TrackLaser()
    {
        while (activeLaser != null && firePoint != null)
        {
            activeLaser.transform.position = firePoint.position;

            Transform target = GetNearestPlayer();

            if (target != null)
            {
                Vector3 dir = (target.position - firePoint.position).normalized;

                Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);

                activeLaser.transform.rotation = Quaternion.Slerp(
                    activeLaser.transform.rotation,
                    targetRot,
                    trackingSpeed * Time.deltaTime
                );
            }

            yield return null;
        }
    }
}