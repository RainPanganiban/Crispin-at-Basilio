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

    [Header("Visual Indicator Settings")]
    public float floatSpeed = 1f;
    public float floatAmplitude = 0.1f;
    public float riseSpeed = 0.5f; 
    public float popScaleMultiplier = 1.8f;
    public float enlargementSpeed = 2f;

    private Collider ownerCollider;
    [SyncVar] private Vector3 spawnPosition;
    [SyncVar] private float serverStartTime;

    [Server]
    public void Initialize(Collider owner)
    {
        ownerCollider = owner;
        spawnPosition = transform.position; // Keep ground position
        serverStartTime = (float)NetworkTime.time;
        StartCoroutine(ExplodeRoutine());
    }

    void Update()
    {
        // Visuals only: Use NetworkTime for consistent sync across all clients
        float timeElapsed = (float)NetworkTime.time - serverStartTime;
        
        // Upward movement
        Vector3 verticalOffset = Vector3.up * (riseSpeed * timeElapsed);
        
        // Slight wobble
        float wobbleOffset = Mathf.Sin(timeElapsed * floatSpeed) * floatAmplitude;
        
        // Use localPosition to avoid fighting NetworkTransform if it's on the root
        // If there's no parent, this is the same as position
        transform.position = spawnPosition + verticalOffset + Vector3.up * wobbleOffset;
    }

    [Server]
    private IEnumerator ExplodeRoutine()
    {
        float timer = 0f;
        Vector3 initialScale = transform.localScale;

        // Wait and enlarge
        while (timer < delayBeforeExplosion)
        {
            timer += Time.deltaTime;
            float t = timer / delayBeforeExplosion;
            
            // Enlarge significantly in the last 30% of lifetime
            if (t > 0.7f)
            {
                float surge = (t - 0.7f) / 0.3f;
                transform.localScale = initialScale * Mathf.Lerp(1f, popScaleMultiplier, surge);
            }
            
            yield return null;
        }

        // Explode
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
