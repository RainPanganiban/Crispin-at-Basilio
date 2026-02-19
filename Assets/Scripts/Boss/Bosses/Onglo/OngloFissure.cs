using UnityEngine;
using Mirror;

[RequireComponent(typeof(BoxCollider))]
public class OngloFissure : NetworkBehaviour
{
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

        NetworkServer.Destroy(gameObject);
    }
}

