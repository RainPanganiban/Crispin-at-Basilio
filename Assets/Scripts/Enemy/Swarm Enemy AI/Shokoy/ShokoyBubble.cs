using UnityEngine;
using Mirror;
using System.Collections;

public class ShokoyBubble : NetworkBehaviour
{
    [Header("Bubble Settings")]
    public float damage = 15f;
    public float explosionRadius = 2.5f;
    public float delayBeforeExplosion = 1.5f;
    public LayerMask targetLayer;

    public GameObject explosionVFXPrefab;

    private Collider ownerCollider;

    [Server]
    public void Initialize(Collider owner)
    {
        ownerCollider = owner;
        StartCoroutine(ExplodeRoutine());
    }

    [Server]
    private IEnumerator ExplodeRoutine()
    {
        yield return new WaitForSeconds(delayBeforeExplosion);

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, targetLayer);
        foreach (var hit in hits)
        {
            if (hit == ownerCollider) continue;

            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage, transform);
            }
        }

        if (explosionVFXPrefab != null)
        {
            GameObject vfx = Instantiate(explosionVFXPrefab, transform.position, Quaternion.identity);
            NetworkServer.Spawn(vfx);
        }

        NetworkServer.Destroy(gameObject);
    }
}
