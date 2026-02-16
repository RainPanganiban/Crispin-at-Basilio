using UnityEngine;
using Mirror;

public class PunchAttack : EnemyAttack
{
    [Header("Punch Settings")]
    public float damage = 10f;
    public float hitRadius = 1.5f;
    public LayerMask playerLayer;

    protected override void OnExecute()
    {
        Debug.Log("Punch executed");

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
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}