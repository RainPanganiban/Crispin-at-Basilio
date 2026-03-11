using UnityEngine;
using Mirror;

public class TyanakClawSwipe : EnemyAttack
{
    [Header("Claw Settings")]
    public float damage = 5f; 
    public float hitRadius = 1.2f;
    public LayerMask playerLayer;
    [Tooltip("Vertical offset for the damage check to target the player's body instead of their feet")]
    public float targetVerticalOffset = 1.0f;
    [Tooltip("How far in front of the Tyanak the hit sphere should be centered")]
    public float hitForwardOffset = 1.0f;

    [Header("Animation")]
    public NetworkAnimator networkAnimator; 
    public string attackTrigger = "ClawSwipe";

    void Awake()
    {
        if (networkAnimator == null) 
        {
            networkAnimator = GetComponent<NetworkAnimator>();
            if (networkAnimator == null) networkAnimator = GetComponentInParent<NetworkAnimator>();
        }
    }

    protected override void OnExecute()
    {
        Debug.Log($"[TyanakClawSwipe] Executing Claw Swipe on {name}");

        // Stop movement during swipe
        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (networkAnimator != null)
        {
            networkAnimator.SetTrigger(attackTrigger); 
        }
        else
        {
            Debug.LogError($"[TyanakClawSwipe] NetworkAnimator missing on {name}!");
        }
    }

    [ServerCallback]
    public void ClawHitEvent()
    {
        // Calculate hit position: height offset + forward offset
        Vector3 checkPos = transform.position + transform.forward * hitForwardOffset + Vector3.up * targetVerticalOffset;

        Collider[] hits = Physics.OverlapSphere(
            checkPos,
            hitRadius,
            playerLayer
        );

        if (hits.Length > 0)
        {
            Debug.Log($"[TyanakClawSwipe] {name} hit {hits.Length} potential targets!");
        }

        foreach (Collider hit in hits)
        {
            IDamageable damageable = hit.GetComponent<IDamageable>();
            
            if (damageable != null)
            {
                damageable.TakeDamage(damage, transform);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.5f); 
        Vector3 checkPos = transform.position + transform.forward * hitForwardOffset + Vector3.up * targetVerticalOffset;
        Gizmos.DrawSphere(checkPos, hitRadius);
    }
}
