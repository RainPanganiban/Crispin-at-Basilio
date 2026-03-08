using UnityEngine;
using Mirror;

public class TyanakClawSwipe : EnemyAttack
{
    [Header("Claw Settings")]
    public float damage = 5f; 
    public float hitRadius = 1.2f;
    public LayerMask playerLayer;

    [Header("Animation")]
    public NetworkAnimator networkAnimator; 
    public string attackTrigger = "ClawSwipe";

    protected override void OnExecute()
    {
        if (networkAnimator != null)
        {
            networkAnimator.SetTrigger(attackTrigger); 
        }
    }

    [ServerCallback]
    public void ClawHitEvent()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            hitRadius,
            playerLayer
        );

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
        Gizmos.DrawSphere(transform.position, hitRadius);
    }
}
