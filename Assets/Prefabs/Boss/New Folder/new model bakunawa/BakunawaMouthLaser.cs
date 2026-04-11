using UnityEngine;
using Mirror;
using System.Collections;

public class BakunawaMouthLaser : BaseAttack
{
    [Header("Laser Settings")]
    public GameObject laserBeamPrefab;
    public Transform mouthFirePoint;
    public float laserDamage = 20f;
    public float laserDistance = 40f;
    public float laserLifetime = 3f;
    public float laserWidth = 1.2f;
    public float tickInterval = 0.2f;
    public LayerMask playerLayer;

    [Header("Tracking Settings")]
    public float initialTrackingSpeed = 15f;
    public float trackingAcceleration = 25f;
    public float targetVerticalOffset = 1.8f; // Itaas ito para tumama sa dibdib ng player

    [Header("Animation & Logic")]
    public string attackTrigger = "MouthLaser";

    private BossController bossController;
    private BakunawaMovement movement;
    private NetworkAnimator networkAnimator;
    private GameObject activeLaser;
    private bool isExecuting = false;

    void Awake()
    {
        bossController = GetComponent<BossController>();
        movement = GetComponent<BakunawaMovement>();
        networkAnimator = GetComponent<NetworkAnimator>();
    }

    public override void Initialize(BossController controller)
    {
        base.Initialize(controller);
        this.bossController = controller;
    }

    [Server]
    public override void Server_Execute()
    {
        if (isExecuting) return;
        isExecuting = true;

        if (movement != null) movement.Server_SetMovementEnabled(false);
        if (networkAnimator != null) networkAnimator.SetTrigger(attackTrigger);
    }

    [ServerCallback]
    public void FireLaser() // Tinatawag ng Animation Event (Start)
    {
        if (!isServer || activeLaser != null || mouthFirePoint == null) return;

        EnemyAggro aggro = GetComponent<EnemyAggro>();
        Vector3 initialDir = mouthFirePoint.forward;

        if (aggro != null && aggro.GetCurrentTarget() != null)
        {
            Vector3 targetCenter = aggro.GetCurrentTarget().position + Vector3.up * targetVerticalOffset;
            initialDir = (targetCenter - mouthFirePoint.position).normalized;
        }

        activeLaser = Instantiate(laserBeamPrefab, mouthFirePoint.position, Quaternion.LookRotation(initialDir));

        var beam = activeLaser.GetComponent<BakunawaLaserBeam>();
        if (beam != null)
        {
            beam.Initialize(laserDamage, laserDistance, laserLifetime, laserWidth, tickInterval, GetComponent<Collider>(), netIdentity, playerLayer);
        }

        NetworkServer.Spawn(activeLaser);
        StartCoroutine(UpdateLaserPosition());
    }

    [ServerCallback]
    public void EndLaser() // Tinatawag ng Animation Event (End)
    {
        if (!isServer) return;

        if (activeLaser != null)
        {
            NetworkServer.Destroy(activeLaser);
        }

        Server_Stop();
    }

    // ISA LANG DAPAT ITO. Burahin yung ibang version nito sa script.
    private IEnumerator UpdateLaserPosition()
    {
        EnemyAggro aggro = GetComponent<EnemyAggro>();
        float currentSpeed = initialTrackingSpeed;

        while (activeLaser != null && mouthFirePoint != null)
        {
            activeLaser.transform.position = mouthFirePoint.position;
            Transform target = aggro != null ? aggro.GetCurrentTarget() : null;

            if (target != null)
            {
                Vector3 targetBodyPos = target.position + (Vector3.up * targetVerticalOffset);
                Vector3 dirToTarget = (targetBodyPos - activeLaser.transform.position).normalized;

                if (dirToTarget != Vector3.zero)
                {
                    Quaternion desiredRotation = Quaternion.LookRotation(dirToTarget);
                    activeLaser.transform.rotation = Quaternion.RotateTowards(
                        activeLaser.transform.rotation,
                        desiredRotation,
                        currentSpeed * Time.deltaTime
                    );
                    currentSpeed += trackingAcceleration * Time.deltaTime;
                }
            }
            yield return null;
        }
    }

    [Server]
    public override void Server_Stop()
    {
        if (!isExecuting) return;
        isExecuting = false;

        if (movement != null) movement.Server_SetMovementEnabled(true);
        if (bossController != null) bossController.Server_EndAttack();

        if (TryGetComponent<BossAttackManager>(out var am))
            am.Server_OnAttackAnimationComplete();
    }

    [Server]
    public override void Server_OnAnimationEvent(string eventName) { }
}