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
    public float initialTrackingSpeed = 5f;
    public float trackingAcceleration = 15f;
    public float initialAimOffsetAngle = 25f;
    public float targetVerticalOffset = 1.0f;

    [Header("Audio Settings")]
    [Tooltip("Ang looping sound ng laser.")]
    public AudioClip laserLoopClip;
    private EnemySoundManager soundManager;

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
        // Kunin ang inyong existing Sound Manager
        soundManager = GetComponent<EnemySoundManager>();
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

        Quaternion spawnRotation = firePoint.rotation;
        EnemyAggro aggro = GetComponent<EnemyAggro>();

        if (aggro != null && aggro.GetCurrentTarget() != null)
        {
            Transform t = aggro.GetCurrentTarget();
            Vector3 targetCenter = t.position + Vector3.up * targetVerticalOffset;
            Vector3 dirToTarget = (targetCenter - firePoint.position).normalized;
            
            float randomSign = Random.value > 0.5f ? 1f : -1f;
            Quaternion offsetRotation = Quaternion.AngleAxis(initialAimOffsetAngle * randomSign, Vector3.up);
            dirToTarget = offsetRotation * dirToTarget;
            
            spawnRotation = Quaternion.LookRotation(dirToTarget);
        }

        activeLaser = Instantiate(laserBeamPrefab, firePoint.position, spawnRotation);
        
        // I-sync ang pag-play ng sound sa lahat ng clients
        RpcToggleLaserSound(true);

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
        // Itigil ang sound sa lahat ng clients
        RpcToggleLaserSound(false);

        if (activeLaser != null)
        {
            NetworkServer.Destroy(activeLaser);
        }
    }

    // --- AUDIO SYNC LOGIC ---
    [ClientRpc]
    private void RpcToggleLaserSound(bool play)
    {
        if (soundManager == null) return;

        // Kinukuha natin ang AudioSource na ginawa ng EnemySoundManager niyo
        AudioSource source = soundManager.GetComponent<AudioSource>();
        if (source == null) return;

        if (play)
        {
            if (laserLoopClip != null)
            {
                source.clip = laserLoopClip;
                source.loop = true; // Gawing loop dahil mahaba ang laser beam
                source.Play();
            }
        }
        else
        {
            source.Stop();
            source.loop = false; // Ibalik sa false para sa ibang sounds (footsteps, etc.)
        }
    }

    private IEnumerator UpdateLaserPosition()
    {
        EnemyAggro aggro = GetComponent<EnemyAggro>();
        float currentSpeed = initialTrackingSpeed;
        if (activeLaser == null) yield break;
        
        Quaternion currentRotation = activeLaser.transform.rotation;

        while (activeLaser != null && firePoint != null)
        {
            activeLaser.transform.position = firePoint.position;
            Transform target = aggro != null ? aggro.GetCurrentTarget() : null;

            if (target != null)
            {
                Vector3 targetCenter = target.position + Vector3.up * targetVerticalOffset;
                Vector3 laserDirection = currentRotation * Vector3.forward;
                bool isHittingPlayer = Physics.SphereCast(firePoint.position, laserWidth / 2f, laserDirection, out RaycastHit hit, laserDistance, playerLayer);

                if (!isHittingPlayer)
                    currentSpeed += trackingAcceleration * Time.deltaTime;
                else
                    currentSpeed = initialTrackingSpeed;

                Vector3 dirToTarget = (targetCenter - firePoint.position).normalized;
                Quaternion desiredRotation = Quaternion.LookRotation(dirToTarget);
                currentRotation = Quaternion.RotateTowards(currentRotation, desiredRotation, currentSpeed * Time.deltaTime);
            }
            
            activeLaser.transform.rotation = currentRotation;
            yield return null;
        }
    }
}