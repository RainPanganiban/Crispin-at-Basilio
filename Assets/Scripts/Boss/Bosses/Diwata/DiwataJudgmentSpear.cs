using UnityEngine;
using Mirror;

/// <summary>
/// Lock-on then fire projectile. Records a target position on spawn,
/// waits a brief delay (telegraph), then fires toward the locked position. 
/// Damages on hit.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class DiwataJudgmentSpear : NetworkBehaviour
{
    [Header("Visuals")]
    public ParticleSystem psCharge;
    public ParticleSystem psTrail;
    public ParticleSystem psImpact;

    [Header("Runtime (server initialized)")]
    [SyncVar] private float damage;
    [SyncVar] private float projectileSpeed;
    [SyncVar] private float lockOnDuration;
    [SyncVar] private float maxDistance;

    private Vector3 targetPosition;
    private Vector3 fireDirection;
    private Vector3 startPos;
    private NetworkIdentity owner;
    private LayerMask playerLayer;
    private SphereCollider trigger;
    private bool fired;
    private bool hit;
    private float fireAt;

    void Awake()
    {
        trigger = GetComponent<SphereCollider>();
        trigger.isTrigger = true;
    }

    [Server]
    public void Server_Initialize(
        NetworkIdentity owner,
        Vector3 targetPosition,
        float damage,
        float projectileSpeed,
        float lockOnDuration,
        float maxDistance,
        LayerMask playerLayer
    )
    {
        this.owner = owner;
        this.targetPosition = targetPosition;
        this.damage = damage;
        this.projectileSpeed = projectileSpeed;
        this.lockOnDuration = lockOnDuration;
        this.maxDistance = maxDistance;
        this.playerLayer = playerLayer;
        this.fired = false;
        this.hit = false;
        this.fireAt = Time.time + lockOnDuration;
        this.startPos = transform.position;

        // Don't enable collision until fired
        trigger.enabled = false;

        Rpc_ShowCharge();
    }

    [ClientRpc]
    void Rpc_ShowCharge()
    {
        if (psCharge != null) psCharge.Play();
    }

    [ServerCallback]
    void Update()
    {
        if (hit) return;

        if (!fired)
        {
            if (Time.time >= fireAt)
            {
                Server_Fire();
            }
            return;
        }

        // Move as projectile
        transform.position += fireDirection * projectileSpeed * Time.deltaTime;

        float traveled = Vector3.Distance(startPos, transform.position);
        if (traveled >= maxDistance)
            NetworkServer.Destroy(gameObject);
    }

    [Server]
    void Server_Fire()
    {
        fired = true;
        trigger.enabled = true;

        fireDirection = (targetPosition - transform.position).normalized;
        if (fireDirection.sqrMagnitude < 0.001f)
            fireDirection = Vector3.down;

        startPos = transform.position;

        // Orient along fire direction
        if (fireDirection.sqrMagnitude > 0.001f)
            transform.forward = fireDirection;

        Rpc_OnFired();
    }

    [ClientRpc]
    void Rpc_OnFired()
    {
        if (psCharge != null) psCharge.Stop();
        if (psTrail != null) psTrail.Play();
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
        Invoke(nameof(DestroySpear), 1.5f);
    }

    [ClientRpc]
    void Rpc_OnHit()
    {
        if (psTrail != null) psTrail.Stop();
        if (psImpact != null) psImpact.Play();

        MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
        if (renderer != null) renderer.enabled = false;
    }

    void DestroySpear()
    {
        NetworkServer.Destroy(gameObject);
    }
}
