using UnityEngine;
using UnityEngine.AI;
using Mirror;
using System.Collections;
using System.Collections.Generic;

public class TyanakLeapAttack : EnemyAttack
{
    [Header("Leap Settings")]
    public float damage = 10f;
    public float leapSpeed = 15f;
    public float leapDistance = 5f;
    public float hitRadius = 1.5f;
    public float pounceOvershoot = 2.5f;
    public float leapTelegraphDuration = 0.2f;
    public LayerMask playerLayer;
    public float targetVerticalOffset = 1.0f;

    [Header("Audio Settings")]
    [Tooltip("I-assign dito ang sound ng pagtalon o pounce.")]
    public AudioClip leapSound;
    private EnemySoundManager soundManager;

    [Header("Animation")]
    public NetworkAnimator networkAnimator;
    public string leapTrigger = "LeapPounce";

    private Collider ownerCollider;
    private HashSet<GameObject> hitTargets = new HashSet<GameObject>();
    private bool isLeaping = false;

    void Awake()
    {
        ownerCollider = GetComponent<Collider>();
        soundManager = GetComponent<EnemySoundManager>(); // Kunin ang Sound Manager

        if (networkAnimator == null)
        {
            networkAnimator = GetComponent<NetworkAnimator>();
            if (networkAnimator == null) networkAnimator = GetComponentInParent<NetworkAnimator>();
        }
    }

    protected override void OnExecute()
    {
        if (!isServer) return;

        Debug.Log($"[TyanakLeapAttack] Executing Leap on {name}");

        if (networkAnimator != null)
        {
            networkAnimator.SetTrigger(leapTrigger);
        }

        // I-sync ang sound sa lahat ng players kapag nagsimula ang attack
        RpcPlayLeapSound();

        hitTargets.Clear();
        StartCoroutine(LeapRoutine());
    }

    [ClientRpc]
    private void RpcPlayLeapSound()
    {
        // Reference check para sa Client side
        if (soundManager == null) soundManager = GetComponent<EnemySoundManager>();

        if (soundManager != null && leapSound != null)
        {
            // Gagamitin ang PlaySpecificAttack dahil hindi ito looping sound
            soundManager.PlaySpecificAttack(leapSound);
        }
    }

    [ServerCallback]
    public void DealDamageEvent()
    {
        ApplyLeapDamage();
    }

    private IEnumerator LeapRoutine()
    {
        NavMeshAgent agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

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

        yield return new WaitForSeconds(leapTelegraphDuration);

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

            ApplyLeapDamage();
            yield return null;
        }

        if (ownerCollider != null) ownerCollider.isTrigger = false;
        isLeaping = false;

        yield return new WaitForSeconds(0.2f);

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
    }

    private void ApplyLeapDamage()
    {
        Vector3 checkPos = transform.position + Vector3.up * targetVerticalOffset;

        Collider[] hits = Physics.OverlapSphere(checkPos, hitRadius, playerLayer);

        foreach (Collider hit in hits)
        {
            if (hitTargets.Contains(hit.gameObject)) continue;

            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                hitTargets.Add(hit.gameObject);
                damageable.TakeDamage(damage, transform);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 checkPos = transform.position + Vector3.up * (targetVerticalOffset != 0 ? targetVerticalOffset : 1.0f);
        Gizmos.color = new Color(1, 0.5f, 0, 0.5f);
        Gizmos.DrawSphere(checkPos, hitRadius);
    }
}