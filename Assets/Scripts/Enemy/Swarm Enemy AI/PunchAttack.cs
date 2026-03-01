using UnityEngine;
using Mirror;

public class PunchAttack : EnemyAttack
{
    [Header("Punch Settings")]
    public float damage = 10f;
    public float hitRadius = 1.5f;
    public LayerMask playerLayer;

    [Header("Animation")]
    public NetworkAnimator networkAnimator; 
    public string punchTrigger = "Punch";

    protected override void OnExecute()
    {
        // Execute the animation, the actual damage is dealt via Animation Event
        if (networkAnimator != null)
        {
            networkAnimator.SetTrigger(punchTrigger); 
        }
    }

    [ServerCallback]
    public void DealDamageEvent()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            hitRadius,
            playerLayer
        );

        Debug.Log($"[PunchAttack] DealDamageEvent triggered! Found {hits.Length} colliders in range on layer {LayerMask.LayerToName((int)Mathf.Log(playerLayer.value, 2))}");

        foreach (Collider hit in hits)
        {
            IDamageable damageable = hit.GetComponent<IDamageable>();
            
            if (damageable != null)
            {
                Debug.Log($"[PunchAttack] Successfully dealt {damage} damage to {hit.name}!");
                damageable.TakeDamage(damage, transform);
            }
            else
            {
                Debug.LogWarning($"[PunchAttack] Hit {hit.name} but it does not have an IDamageable component!");
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.5f); // Semi-transparent red
        Gizmos.DrawSphere(transform.position, hitRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}