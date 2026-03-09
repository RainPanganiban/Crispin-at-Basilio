using UnityEngine;
using Mirror;

public class BungisngisLaserBeam : NetworkBehaviour
{
    [Header("Laser Settings")]
    [SyncVar] public float maxDistance = 20f;
    [SyncVar] public float width = 0.5f;
    [SyncVar] public float maxLifetime = 2f;
    [SyncVar] public float damage = 10f;
    [SyncVar] public float damageInterval = 0.2f;
    public LayerMask hitMask;
    
    [Header("Visuals")]
    public LineRenderer lineRenderer;

    private float lifetime;
    private float lastDamageTime;
    private Collider ownerCollider;
    private NetworkIdentity ownerIdentity;

    [Server]
    public void Initialize(float dmg, float distance, float life, float widthVal, float tickInterval, Collider ownerCol, NetworkIdentity ownerId, LayerMask mask)
    {
        damage = dmg;
        maxDistance = distance;
        maxLifetime = life;
        width = widthVal;
        damageInterval = tickInterval;
        ownerCollider = ownerCol;
        ownerIdentity = ownerId;
        hitMask = mask;
        lifetime = 0f;
        
        // Immediate first damage tick
        lastDamageTime = Time.time - tickInterval; 
    }

    void Update()
    {
        UpdateVisuals();

        if (isServer)
        {
            ServerUpdate();
        }
    }

    [Server]
    void ServerUpdate()
    {
        lifetime += Time.deltaTime;
        
        if (lifetime >= maxLifetime)
        {
            NetworkServer.Destroy(gameObject);
            return;
        }

        if (Time.time >= lastDamageTime + damageInterval)
        {
            lastDamageTime = Time.time;
            ApplyDamage();
        }
    }

    [Server]
    void ApplyDamage()
    {
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, width / 2f, transform.forward, maxDistance, hitMask);
        
        foreach (RaycastHit hit in hits)
        {
            if (ownerCollider != null && hit.collider == ownerCollider)
                continue;

            // Damage player
            if (hit.collider.TryGetComponent<IDamageable>(out var target))
            {
                Transform attacker = ownerIdentity != null ? ownerIdentity.transform : null;
                target.TakeDamage(damage, attacker);
            }
        }
    }

    void UpdateVisuals()
    {
        if (lineRenderer == null) return;
        
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, transform.position + transform.forward * maxDistance);
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
    }
}
