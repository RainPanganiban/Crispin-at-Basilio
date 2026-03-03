using UnityEngine;
using Mirror;
using UnityEngine.VFX;

[RequireComponent(typeof(BoxCollider))]
public class OngloFissure : NetworkBehaviour
{
    [Header("Visuals")]
    public VisualEffect vfxTravel;
    public VisualEffect vfxErupt;
    public ParticleSystem psTravel;
    public ParticleSystem psErupt;

    [Header("Runtime (server initialized)")]
    [SyncVar] private float speed;
    [SyncVar] private float maxDistance;
    [SyncVar] private float eruptDelay;
    [SyncVar] private float eruptRadius;
    [SyncVar] private float damage;

    private Vector3 direction;
    private Vector3 startPos;
    private NetworkIdentity owner;
    private LayerMask playerLayer;

    private bool erupted;
    private float eruptAt;

    private BoxCollider trigger;

    void Awake()
    {
        trigger = GetComponent<BoxCollider>();
        trigger.isTrigger = true;
    }

    [Server]
    public void Server_Initialize(
        NetworkIdentity owner,
        Vector3 direction,
        float speed,
        float maxDistance,
        float eruptDelay,
        float eruptRadius,
        float damage,
        LayerMask playerLayer
    )
    {
        this.owner = owner;
        this.direction = direction.normalized;
        this.speed = speed;
        this.maxDistance = maxDistance;
        this.eruptDelay = eruptDelay;
        this.eruptRadius = eruptRadius;
        this.damage = damage;
        this.playerLayer = playerLayer;

        startPos = transform.position;
        eruptAt = Time.time + eruptDelay;
        erupted = false;
    }

    [ServerCallback]
    void Update()
    {
        if (erupted)
            return;

        transform.position += direction * speed * Time.deltaTime;

        float traveled = Vector3.Distance(startPos, transform.position);
        if (traveled >= maxDistance || Time.time >= eruptAt)
        {
            Server_Erupt();
        }
    }

    [Server]
    void Server_Erupt()
    {
        erupted = true;

        Collider[] hits = Physics.OverlapSphere(transform.position, eruptRadius, playerLayer);
        foreach (Collider hit in hits)
        {
            if (!hit.TryGetComponent<IDamageable>(out var dmgable))
                continue;

            dmgable.TakeDamage(damage, owner != null ? owner.transform : null);
        }

        Rpc_TriggerErupt();
        
        // Destroy after a small delay to let VFX play out, 
        // but hide the object/colliders immediately.
        trigger.enabled = false;
        Invoke(nameof(DestroyFissure), 2.0f);
    }

    [ClientRpc]
    void Rpc_TriggerErupt()
    {
        if (vfxTravel != null) vfxTravel.Stop();
        if (vfxErupt != null) vfxErupt.Play();
        
        if (psTravel != null) psTravel.Stop();
        if (psErupt != null) psErupt.Play();
    }

    void DestroyFissure()
    {
        NetworkServer.Destroy(gameObject);
    }
}

