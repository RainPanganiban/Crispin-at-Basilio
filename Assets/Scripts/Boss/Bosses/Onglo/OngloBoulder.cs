using UnityEngine;
using Mirror;
using UnityEngine.VFX;

[RequireComponent(typeof(SphereCollider))]
public class OngloBoulder : NetworkBehaviour
{
    [Header("Visuals")]
    public VisualEffect vfxTrail;
    public VisualEffect vfxImpact;
    public ParticleSystem psTrail;
    public ParticleSystem psImpact;

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

        Rpc_TriggerImpact();
        
        // Disable immediate gameplay components
        trigger.enabled = false;
        // Optionally disable model renderer here if it was a separate component
        
        Invoke(nameof(DestroyBoulder), 2.0f);
    }

    [ClientRpc]
    void Rpc_TriggerImpact()
    {
        if (vfxTrail != null) vfxTrail.Stop();
        if (vfxImpact != null) vfxImpact.Play();

        if (psTrail != null) psTrail.Stop();
        if (psImpact != null) psImpact.Play();
        
        // Hide the model if it exists
        MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
        if (renderer != null) renderer.enabled = false;
    }

    void DestroyBoulder()
    {
        NetworkServer.Destroy(gameObject);
    }
}

