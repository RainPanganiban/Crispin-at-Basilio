using UnityEngine;
using UnityEngine.AI;
using Mirror;
using System.Collections;
using System.Collections.Generic;

public class ChargePunchAttack : EnemyAttack
{
    [Header("Charge Punch Settings")]
    public float damage = 10f;
    public float chargeTime = 0.5f;
    public float dashSpeed = 10f;
    public float dashDistance = 4f;
    public float hitRadius = 1.5f;
    public LayerMask playerLayer;

    [Header("Animation")]
    public NetworkAnimator networkAnimator;
    public string chargePunchTrigger = "ChargePunch";

    private HashSet<GameObject> hitTargets = new HashSet<GameObject>();
    private bool isDashing = false;

    protected override void OnExecute()
    {
        if (!isServer) return;

        if (networkAnimator != null)
        {
            networkAnimator.SetTrigger(chargePunchTrigger);
        }

        // Clear previous hits at the start of a new attack
        hitTargets.Clear();
        StartCoroutine(ChargeAndDash());
    }

    [ServerCallback]
    public void DealDamageEvent()
    {
        // This can be called forcefully via animation event if we want a specific hit frame
        // Otherwise, the coroutine handles the sweeping dash damage
        ApplyDashDamage();
    }

    private IEnumerator ChargeAndDash()
    {
        NavMeshAgent agent = GetComponent<NavMeshAgent>();

        // Stop the agent so it doesn't fight the manual dash movement
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        // Try to get current target direction and use that for the dash when available
        Vector3 dashDirection = transform.forward.normalized;
        Vector3 targetDir = Vector3.zero;
        EnemyAggro aggro = GetComponent<EnemyAggro>();
        Transform target = aggro != null ? aggro.GetCurrentTarget() : null;
        if (target != null)
        {
            targetDir = (target.position - transform.position).normalized;
            targetDir.y = 0f;

            if (targetDir.sqrMagnitude > 0.0001f)
            {
                dashDirection = targetDir.normalized;

                // Hard-align rotation so the mesh also faces the dash direction
                transform.rotation = Quaternion.LookRotation(dashDirection);
            }
        }

        // Charge / windup phase
        float chargeTimer = 0f;
        while (chargeTimer < chargeTime)
        {
            chargeTimer += Time.deltaTime;
            yield return null;
        }

        // Dash phase
        float travelled = 0f;
        isDashing = true;
        
        while (travelled < dashDistance)
        {
            float step = dashSpeed * Time.deltaTime;
            travelled += step;

            Vector3 nextPosition = transform.position + dashDirection * step;

            if (agent != null)
            {
                agent.Warp(nextPosition);
            }
            else
            {
                transform.position = nextPosition;
            }

            // Check for player hits continuously during the dash movement
            ApplyDashDamage();

            yield return null;
        }

        isDashing = false;

        // Optionally let the NavMeshAgent resume after the dash
        if (agent != null)
        {
            agent.isStopped = false;
        }
    }

    private void ApplyDashDamage()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            hitRadius,
            playerLayer
        );

        foreach (Collider hit in hits)
        {
            // Skip if we already hit this object during this dash
            if (hitTargets.Contains(hit.gameObject)) continue;

            IDamageable damageable = hit.GetComponent<IDamageable>();

            if (damageable != null)
            {
                hitTargets.Add(hit.gameObject); // Remember we hit them
                Debug.Log($"[ChargePunchAttack] Dealt {damage} damage to {hit.name} during dash!");
                damageable.TakeDamage(damage, transform);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 1, 0.5f); // Semi-transparent cyan
        Gizmos.DrawSphere(transform.position, hitRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}
