using UnityEngine;
using Mirror;

public class BakunawaLaserBeam : NetworkBehaviour
{
    [SyncVar] public float maxDistance = 40f;
    [SyncVar] public float width = 1.2f;
    [SyncVar] public float maxLifetime = 3f;
    [SyncVar] public float damage = 20f;
    [SyncVar] public float damageInterval = 0.2f;
    public LayerMask hitMask;

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
        lastDamageTime = Time.time;
    }

    private void Start()
    {
        if (lineRenderer != null)
        {
            lineRenderer.useWorldSpace = true;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    void Update()
    {
        // Visuals update for everyone
        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, transform.position + transform.forward * maxDistance);
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
        }

        if (isServer)
        {
            lifetime += Time.deltaTime;
            if (lifetime >= maxLifetime) NetworkServer.Destroy(gameObject);

            if (Time.time >= lastDamageTime + damageInterval)
            {
                lastDamageTime = Time.time;
                ApplyDamage();
            }
        }
    }

    [Server]
    void ApplyDamage()
    {
        // SphereCast para mas madaling tamaan ang player kahit manipis ang laser
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, width / 2f, transform.forward, maxDistance, hitMask);

        foreach (RaycastHit hit in hits)
        {
            if (ownerCollider != null && hit.collider == ownerCollider) continue;

            if (hit.collider.TryGetComponent<IDamageable>(out var target))
            {
                Transform attacker = ownerIdentity != null ? ownerIdentity.transform : null;
                target.TakeDamage(damage, attacker);
            }
        }
    }
}