using UnityEngine;
using Mirror;

/// <summary>
/// A fast directional light spear projectile.
/// Fires from the arena edge toward the center, 
/// damages players on contact, destroys after maxDistance.
/// </summary>
[RequireComponent(typeof(CapsuleCollider))]
public class DiwataSpiritLance : NetworkBehaviour
{
    [Header("Visuals")]
    public ParticleSystem psTrail;
    public ParticleSystem psImpact;

    [Header("Runtime (server initialized)")]
    [SyncVar] private float damage;
    [SyncVar] private float speed;
    [SyncVar] private float maxDistance;

    private Vector3 direction;
    private Vector3 startPos;
    private NetworkIdentity owner;
    private LayerMask playerLayer;
    private CapsuleCollider trigger;
    private bool hit;

    void Awake()
    {
        trigger = GetComponent<CapsuleCollider>();
        trigger.isTrigger = true;
    }

    [Server]
    public void Server_Initialize(
        NetworkIdentity owner,
        Vector3 direction,
        float speed,
        float damage,
        float maxDistance,
        LayerMask playerLayer
    )
    {
        this.owner = owner;
        this.direction = direction.normalized;
        this.speed = speed;
        this.damage = damage;
        this.maxDistance = maxDistance;
        this.playerLayer = playerLayer;
        this.startPos = transform.position;
        this.hit = false;

        // Orient the lance along direction
        if (direction.sqrMagnitude > 0.001f)
            transform.forward = direction;
    }

    [ServerCallback]
    void Update()
    {
        if (hit) return;

        transform.position += direction * speed * Time.deltaTime;

        float traveled = Vector3.Distance(startPos, transform.position);
        if (traveled >= maxDistance)
            NetworkServer.Destroy(gameObject);
    }

    [ServerCallback]
    void OnTriggerEnter(Collider other)
    {
        if (hit) return;
        if (other.isTrigger) return;
        if (((1 << other.gameObject.layer) & playerLayer.value) == 0) return;

        if (other.TryGetComponent<IDamageable>(out var dmg))
        {
            dmg.TakeDamage(damage, owner != null ? owner.transform : null);
        }

        hit = true;
        Rpc_OnHit();
        trigger.enabled = false;
        Invoke(nameof(DestroyLance), 1.5f);
    }

    [ClientRpc]
    void Rpc_OnHit()
    {
        if (psTrail != null) psTrail.Stop();
        if (psImpact != null) psImpact.Play();

        MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
        if (renderer != null) renderer.enabled = false;
    }

    void DestroyLance()
    {
        NetworkServer.Destroy(gameObject);
    }
}
