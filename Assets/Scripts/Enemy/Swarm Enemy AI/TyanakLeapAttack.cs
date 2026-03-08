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
    public LayerMask playerLayer;

    [Header("Animation")]
    public NetworkAnimator networkAnimator;
    public string leapTrigger = "LeapPounce";

    private HashSet<GameObject> hitTargets = new HashSet<GameObject>();
    private bool isLeaping = false;

    protected override void OnExecute()
    {
        if (!isServer) return;

        if (networkAnimator != null)
        {
            networkAnimator.SetTrigger(leapTrigger);
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
        EnemyAggro aggro = GetComponent<EnemyAggro>();
        Transform target = aggro != null ? aggro.GetCurrentTarget() : null;
        if (target != null)
        {
            Vector3 targetDir = (target.position - transform.position).normalized;
            targetDir.y = 0f;

            if (targetDir.sqrMagnitude > 0.0001f)
            {
                dashDirection = targetDir.normalized;
                transform.rotation = Quaternion.LookRotation(dashDirection);
            }
        }

        // Brief telegraph (crouch) before launching
        yield return new WaitForSeconds(0.2f);
        
        float travelled = 0f;
        isLeaping = true;
        
        while (travelled < leapDistance)
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
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            hitRadius,
            playerLayer
        );

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
        Gizmos.color = new Color(1, 0.5f, 0, 0.5f);
        Gizmos.DrawSphere(transform.position, hitRadius);
        Gizmos.color = new Color(1, 0.5f, 0);
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}
