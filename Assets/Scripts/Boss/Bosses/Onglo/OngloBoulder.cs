using UnityEngine;
using Mirror;

[RequireComponent(typeof(SphereCollider))]
public class OngloBoulder : NetworkBehaviour
{
    [Header("Runtime (server initialized)")]
    [SyncVar] private float damage;
    [SyncVar] private float gravity;
    [SyncVar] private float lifetime;
    [SyncVar] private float impactRadius;

    private Vector3 velocity;
    private float dieAt;
    private NetworkIdentity owner;
    private LayerMask playerLayer;
    private SphereCollider trigger;

    void Awake()
    {
        trigger = GetComponent<SphereCollider>();
        trigger.isTrigger = true;
    }

    [Server]
    public void Server_Initialize(
        NetworkIdentity owner,
        Vector3 initialVelocity,
        float gravity,
        float damage,
        float impactRadius,
        float lifetime,
        LayerMask playerLayer
    )
    {
        this.owner = owner;
        this.velocity = initialVelocity;
        this.gravity = gravity;
        this.damage = damage;
        this.impactRadius = impactRadius;
        this.lifetime = lifetime;
        this.playerLayer = playerLayer;
        dieAt = Time.time + lifetime;
    }

    [ServerCallback]
    void Update()
    {
        velocity += Vector3.down * gravity * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;

        if (Time.time >= dieAt)
            NetworkServer.Destroy(gameObject);
    }

    [ServerCallback]
    void OnTriggerEnter(Collider other)
    {
        // Impact on anything that's not a trigger volume.
        if (other.isTrigger)
            return;

        Server_Impact();
    }

    [Server]
    void Server_Impact()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, impactRadius, playerLayer);
        foreach (Collider hit in hits)
        {
            if (!hit.TryGetComponent<IDamageable>(out var dmgable))
                continue;

            dmgable.TakeDamage(damage, owner != null ? owner.transform : null);
        }

        NetworkServer.Destroy(gameObject);
    }
}

