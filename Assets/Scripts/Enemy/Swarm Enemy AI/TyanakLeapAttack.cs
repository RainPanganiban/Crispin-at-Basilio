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
    [Tooltip("Extra distance to travel past the player's position to ensure we 'jump through' them")]
    public float pounceOvershoot = 2.5f;
    [Tooltip("Time to wait (telegraph) before the physical jump starts. Use this to sync with your animation.")]
    public float leapTelegraphDuration = 0.2f;
    public LayerMask playerLayer;
    [Tooltip("Vertical offset to aim the leap at the player's body instead of their feet")]
    public float targetVerticalOffset = 1.0f;

    [Header("Animation")]
    public NetworkAnimator networkAnimator;
    public string leapTrigger = "LeapPounce";

    private Collider ownerCollider;
    private HashSet<GameObject> hitTargets = new HashSet<GameObject>();
    private bool isLeaping = false;

    void Awake()
    {
        ownerCollider = GetComponent<Collider>();
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
        else
        {
            Debug.LogError($"[TyanakLeapAttack] NetworkAnimator missing on {name}!");
        }

        hitTargets.Clear();
        StartCoroutine(LeapRoutine());
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
            // Aim at the player's body center, not feet
            Vector3 targetCenter = targetTransform.position + Vector3.up * targetVerticalOffset;
            Vector3 diff = targetCenter - transform.position;
            float distToTarget = new Vector3(diff.x, 0, diff.z).magnitude;
            
            // Set leap distance to go past the player
            actualLeapDistance = distToTarget + pounceOvershoot;
            
            Vector3 targetDir = diff.normalized;
            targetDir.y = 0f; 

            if (targetDir.sqrMagnitude > 0.0001f)
            {
                dashDirection = targetDir.normalized;
                transform.rotation = Quaternion.LookRotation(dashDirection);
            }
        }

        // Brief telegraph (crouch) before launching
        yield return new WaitForSeconds(leapTelegraphDuration);
        
        float travelled = 0f;
        isLeaping = true;
        Debug.Log($"[TyanakLeapAttack] {name} launching pounce! Passing through player.");
        
        // Temporarily make collider a trigger so we pass through the player
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

        // Restore collider
        if (ownerCollider != null) ownerCollider.isTrigger = false;

        isLeaping = false;

        // Brief recovery
        yield return new WaitForSeconds(0.2f);

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
    }

    private void ApplyLeapDamage()
    {
        // Use the vertical offset for the damage sphere too
        Vector3 checkPos = transform.position + Vector3.up * targetVerticalOffset;

        Collider[] hits = Physics.OverlapSphere(
            checkPos,
            hitRadius,
            playerLayer
        );

        foreach (Collider hit in hits)
        {
            if (hitTargets.Contains(hit.gameObject)) continue;

            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                Debug.Log($"[TyanakLeapAttack] {name} hit {hit.gameObject.name} during leap!");
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
        Gizmos.color = new Color(1, 0.5f, 0);
        Gizmos.DrawWireSphere(checkPos, hitRadius);
    }
}
