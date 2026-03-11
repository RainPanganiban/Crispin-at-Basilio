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
        
        Debug.Log($"[BungisngisLaserBeam] Initialized on Server. Damage: {damage}, Lifetime: {maxLifetime}");

        // Immediate first damage tick
        lastDamageTime = Time.time - tickInterval; 
    }

    private void Start()
    {
        if (lineRenderer != null)
        {
            lineRenderer.useWorldSpace = true;
            // Ensure shadow casting is off for performance/visibility
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
        }
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
            Debug.Log("[BungisngisLaserBeam] Lifetime expired, destroying.");
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
        
        if (hits.Length > 0)
        {
            Debug.Log($"[BungisngisLaserBeam] Applying damage tick to {hits.Length} potential targets.");
        }

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
        
        // Use World Space positions
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, transform.position + transform.forward * maxDistance);
        
        // Ensure width is set
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
        
        // Ensure it's enabled
        if (!lineRenderer.enabled) lineRenderer.enabled = true;
    }
}
