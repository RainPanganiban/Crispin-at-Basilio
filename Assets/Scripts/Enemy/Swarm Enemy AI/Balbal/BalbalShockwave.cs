using UnityEngine;
using Mirror;

public class BalbalShockwave : NetworkBehaviour
{
    [Header("Shockwave Settings")]
    [SyncVar] public float damage = 15f;
    [SyncVar] public float maxRadius = 4f;
    [SyncVar] public float expansionSpeed = 8f;
    public LayerMask targetLayer;
    
    public Transform visualTransform; 

    private float currentRadius = 0f;
    private Collider ownerCollider;

    [Server]
    public void Initialize(Collider owner)
    {
        ownerCollider = owner;
        currentRadius = 0f;
    }

    [ServerCallback]
    void Update()
    {
        currentRadius += expansionSpeed * Time.deltaTime;
        
        if (visualTransform != null)
        {
            visualTransform.localScale = Vector3.one * currentRadius * 2f;
        }
        
        ApplyExpansionDamage();

        if (currentRadius >= maxRadius)
        {
            NetworkServer.Destroy(gameObject);
        }
    }

    [Server]
    private void ApplyExpansionDamage()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, currentRadius, targetLayer);

        foreach (Collider hit in hits)
        {
            if (hit == ownerCollider) continue;

            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage, transform);
            }
        }
    }
}
