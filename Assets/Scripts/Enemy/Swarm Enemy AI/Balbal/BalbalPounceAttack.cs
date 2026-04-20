using UnityEngine;
using UnityEngine.AI;
using Mirror;
using System.Collections;
using System.Collections.Generic;

public class BalbalPounceAttack : EnemyAttack
{
    [Header("Leap Settings")]
    public float directDamage = 10f;
    public float leapSpeed = 15f;
    public float leapDistance = 5f;
    public float directHitRadius = 1.5f;
    public float pounceOvershoot = 2.5f;
    public float leapTelegraphDuration = 0.2f;
    public LayerMask playerLayer;
    public float targetVerticalOffset = 1.0f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip pounceRoarSFX; // Sigaw bago tumalon
    [SerializeField] private AudioClip leapSFX;       // Whoosh sound
    [SerializeField] private AudioClip landingSFX;    // Kalabog pagbagsak
    private EnemySoundManager soundManager;

    [Header("Shockwave Settings")]
    public GameObject shockwavePrefab;

    [Header("Animation")]
    public NetworkAnimator networkAnimator;
    public string leapTrigger = "Pounce";

    private Collider ownerCollider;
    private HashSet<GameObject> hitTargets = new HashSet<GameObject>();
    private bool isLeaping = false;

    void Awake()
    {
        ownerCollider = GetComponent<Collider>();
        soundManager = GetComponent<EnemySoundManager>();

        if (networkAnimator == null)
        {
            networkAnimator = GetComponent<NetworkAnimator>();
            if (networkAnimator == null) networkAnimator = GetComponentInParent<NetworkAnimator>();
        }
    }

    protected override void OnExecute()
    {
        if (!isServer) return;

        // I-play ang roar warning sa lahat ng clients
        RpcPlayRoar();

        if (networkAnimator != null)
        {
            networkAnimator.SetTrigger(leapTrigger);
        }

        hitTargets.Clear();

        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    [ServerCallback]
    public void LaunchPounceEvent()
    {
        StartCoroutine(LeapRoutine());

        // I-play ang leap sound sa lahat ng clients
        RpcPlayLeap();
    }

    private IEnumerator LeapRoutine()
    {
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        Vector3 dashDirection = transform.forward.normalized;
        float actualLeapDistance = leapDistance;

        EnemyAggro aggro = GetComponent<EnemyAggro>();
        Transform targetTransform = aggro != null ? aggro.GetCurrentTarget() : null;

        if (targetTransform != null)
        {
            Vector3 targetCenter = targetTransform.position + Vector3.up * targetVerticalOffset;
            Vector3 diff = targetCenter - transform.position;
            float distToTarget = new Vector3(diff.x, 0, diff.z).magnitude;

            actualLeapDistance = distToTarget + pounceOvershoot;

            Vector3 targetDir = diff.normalized;
            targetDir.y = 0f;

            if (targetDir.sqrMagnitude > 0.0001f)
            {
                dashDirection = targetDir.normalized;
                transform.rotation = Quaternion.LookRotation(dashDirection);
            }
        }

        float travelled = 0f;
        isLeaping = true;

        if (ownerCollider != null) ownerCollider.isTrigger = true;

        while (travelled < actualLeapDistance)
        {
            float step = leapSpeed * Time.deltaTime;
            travelled += step;

            Vector3 nextPosition = transform.position + dashDirection * step;

            if (agent != null && agent.isOnNavMesh)
            {
                agent.Warp(nextPosition);
            }
            else
            {
                transform.position = nextPosition;
            }

            ApplyDirectDamage();
            yield return null;
        }

        if (ownerCollider != null) ownerCollider.isTrigger = false;
        isLeaping = false;

        // I-play ang landing sound sa lahat ng clients
        RpcPlayLanding();
        SpawnShockwave();

        yield return new WaitForSeconds(0.2f);

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
    }

    // ==========================================
    // corrected RPCs (Inalis ang AudioClip parameters)
    // ==========================================

    [ClientRpc]
    private void RpcPlayRoar()
    {
        if (soundManager != null && pounceRoarSFX != null)
            soundManager.PlaySpecificAttack(pounceRoarSFX);
    }

    [ClientRpc]
    private void RpcPlayLeap()
    {
        if (soundManager != null && leapSFX != null)
            soundManager.PlaySpecificAttack(leapSFX);
    }

    [ClientRpc]
    private void RpcPlayLanding()
    {
        if (soundManager != null && landingSFX != null)
            soundManager.PlaySpecificAttack(landingSFX);
    }

    // ==========================================

    private void ApplyDirectDamage()
    {
        Vector3 checkPos = transform.position + Vector3.up * targetVerticalOffset;
        Collider[] hits = Physics.OverlapSphere(checkPos, directHitRadius, playerLayer);

        foreach (Collider hit in hits)
        {
            if (hitTargets.Contains(hit.gameObject)) continue;

            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                hitTargets.Add(hit.gameObject);
                damageable.TakeDamage(directDamage, transform);
            }
        }
    }

    [Server]
    private void SpawnShockwave()
    {
        if (shockwavePrefab == null) return;

        Vector3 spawnPos = transform.position;
        GameObject shockwave = Instantiate(shockwavePrefab, spawnPos, Quaternion.identity);
        NetworkServer.Spawn(shockwave);

        BalbalShockwave comp = shockwave.GetComponent<BalbalShockwave>();
        if (comp != null)
        {
            comp.Initialize(ownerCollider);
        }
    }
}