using UnityEngine;
using Mirror;
using System.Collections;

public class BakunawaBubble : NetworkBehaviour
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

    private Collider ownerCollider;
    [SyncVar] private Vector3 spawnPosition;
    [SyncVar] private float serverStartTime;

    [Server]
    public void Initialize(Collider owner)
    {
        ownerCollider = owner;
        spawnPosition = transform.position;
        serverStartTime = (float)NetworkTime.time;
        StartCoroutine(ExplodeRoutine());
    }

    void Update()
    {
        // Visual movement sync
        float timeElapsed = (float)NetworkTime.time - serverStartTime;
        Vector3 verticalOffset = Vector3.up * (riseSpeed * timeElapsed);
        float wobbleOffset = Mathf.Sin(timeElapsed * floatSpeed) * floatAmplitude;
        transform.position = spawnPosition + verticalOffset + Vector3.up * wobbleOffset;
    }

    [Server]
    private IEnumerator ExplodeRoutine()
    {
        float timer = 0f;
        Vector3 initialScale = transform.localScale;

        // ETO ANG MAHALAGA: Kailangan may loop at yield return dito
        while (timer < delayBeforeExplosion)
        {
            timer += Time.deltaTime;
            float t = timer / delayBeforeExplosion;

            // Visual swelling effect
            if (t > 0.7f)
            {
                float surge = (t - 0.7f) / 0.3f;
                transform.localScale = initialScale * Mathf.Lerp(1f, popScaleMultiplier, surge);
            }

            yield return null; // Eto ang "return" na hinahanap ng error
        }

        // --- PAGSABOG LOGIC (Nasa screenshot mo na ito) ---
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, targetLayer);

        foreach (var hit in hits)
        {
            if (hit == ownerCollider) continue;

            Vector3 closestPoint = hit.ClosestPoint(transform.position);
            float distance = Vector3.Distance(transform.position, closestPoint);

            if (distance <= explosionRadius)
            {
                var stats = hit.GetComponent<PlayerStatsManager>();
                if (stats == null) stats = hit.GetComponentInParent<PlayerStatsManager>();

                if (stats != null)
                {
                    stats.TakeDamage(damage, transform);
                }
            }
        }

        // Huwag kalimutan ang VFX at Destroy sa dulo!
        if (explosionVFXPrefab != null)
        {
            GameObject vfx = Instantiate(explosionVFXPrefab, transform.position, Quaternion.identity);
            NetworkServer.Spawn(vfx);
        }

        NetworkServer.Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}