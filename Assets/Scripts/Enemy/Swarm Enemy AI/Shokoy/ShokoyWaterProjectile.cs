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
        
        float yOffset = Mathf.Sin(progress * Mathf.PI) * arcHeight;
        currentPos.y += yOffset;
        
        transform.position = currentPos;

        if (progress >= 1f)
        {
            SpawnPuddleAndDestroy();
        }
    }
    
    [ServerCallback]
    void OnTriggerEnter(Collider other)
    {
        if(other == ownerCollider) return;
        
        if (other.TryGetComponent<IDamageable>(out var hitTarget))
        {
            hitTarget.TakeDamage(damage, ownerIdentity != null ? ownerIdentity.transform : null);
            SpawnPuddleAndDestroy();
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
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
            spawnPos.y = 0.1f; 
            
            GameObject puddle = Instantiate(hazardPuddlePrefab, spawnPos, Quaternion.identity);
            NetworkServer.Spawn(puddle);
        }
        NetworkServer.Destroy(gameObject);
    }
}
