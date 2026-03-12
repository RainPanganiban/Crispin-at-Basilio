using UnityEngine;
using Mirror;

public class ShokoyWaterProjectile : NetworkBehaviour
{
    public GameObject hazardPuddlePrefab;
    
    private Vector3 startPos;
    private Vector3 targetPos;
    private float arcHeight;
    private float speed;
    private float damage;
    private Collider ownerCollider;
    private NetworkIdentity ownerIdentity;
    
    private float progress = 0f;
    private System.Collections.Generic.HashSet<IDamageable> damagedTargets = new System.Collections.Generic.HashSet<IDamageable>();

    [Server]
    public void InitializeArc(Vector3 target, float arcZ, float spd, float dmg, Collider ownerCol, NetworkIdentity ownerId)
    {
        startPos = transform.position;
        targetPos = target;
        arcHeight = arcZ;
        speed = spd;
        damage = dmg;
        ownerCollider = ownerCol;
        ownerIdentity = ownerId;
        progress = 0f;
    }

    [ServerCallback]
    void Update()
    {
        float totalDist = Vector3.Distance(startPos, targetPos);
        if (totalDist < 0.001f) totalDist = 1f;

        progress += speed * Time.deltaTime / totalDist;
        
        Vector3 currentPos = Vector3.Lerp(startPos, targetPos, progress);
        
        // Add arc
        float yOffset = Mathf.Sin(progress * Mathf.PI) * arcHeight;
        currentPos.y += yOffset;
        
        transform.position = currentPos;

        // If we missed or went past target without hitting anything, destroy after some time
        if (progress >= 2.0f)
        {
            NetworkServer.Destroy(gameObject);
        }
    }
    
    [ServerCallback]
    void OnTriggerEnter(Collider other)
    {
        if(other == ownerCollider) return;
        
        bool isGrounded = other.CompareTag("Ground") || other.gameObject.layer == LayerMask.NameToLayer("Ground");
        
        if (other.TryGetComponent<IDamageable>(out var hitTarget))
        {
            if (!damagedTargets.Contains(hitTarget))
            {
                hitTarget.TakeDamage(damage, ownerIdentity != null ? ownerIdentity.transform : null);
                damagedTargets.Add(hitTarget);
            }
            // Do NOT SpawnPuddleAndDestroy here, let it continue to ground
        }
        else if (isGrounded)
        {
            SpawnPuddleAndDestroy();
        }
    }

    [Server]
    private void SpawnPuddleAndDestroy()
    {
        if (hazardPuddlePrefab != null)
        {
            Vector3 spawnPos = transform.position;
            
            // Cast a ray from slightly above the projectile straight down 
            // This is more robust than relying single point collision with thin meshes
            if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 10f))
            {
                // Place puddle slightly above the hit point to prevent z-fighting
                spawnPos = hit.point + Vector3.up * 0.05f;
            }
            else
            {
                // Fallback: spawn at the projectile's current height (approx ground level if it trigger hit)
                spawnPos.y = 0.05f; 
            }
            
            GameObject puddle = Instantiate(hazardPuddlePrefab, spawnPos, Quaternion.identity);
            NetworkServer.Spawn(puddle);
        }
        
        // Always destroy the projectile
        NetworkServer.Destroy(gameObject);
    }
}
