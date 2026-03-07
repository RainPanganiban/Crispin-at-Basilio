using UnityEngine;
using Mirror;

/// <summary>
/// A spirit wisp that can orbit a target Transform, then detach
/// and fire outward as a directional projectile.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class DiwataWisp : NetworkBehaviour
{
    [Header("Visuals")]
    public ParticleSystem psGlow;
    public ParticleSystem psImpact;

    [Header("Runtime (server initialized)")]
    [SyncVar] private float damage;
    [SyncVar] private float orbitSpeed;
    [SyncVar] private float orbitRadius;
    [SyncVar] private float projectileSpeed;
    [SyncVar] private float lifetime;
    [SyncVar] private bool isOrbiting;

    private Transform orbitCenter;
    private float orbitAngle;
    private float orbitHeight;
    private Vector3 fireDirection;
    private float dieAt;
    private NetworkIdentity owner;
    private LayerMask playerLayer;
    private SphereCollider trigger;
    private bool hit;

    void Awake()
    {
        trigger = GetComponent<SphereCollider>();
        trigger.isTrigger = true;
    }

    [Server]
    public void Server_Initialize(
        NetworkIdentity owner,
        Transform orbitCenter,
        float startAngle,
        float orbitRadius,
        float orbitHeight,
        float orbitSpeed,
        float damage,
        float projectileSpeed,
        float lifetime,
        LayerMask playerLayer
    )
    {
        this.owner = owner;
        this.orbitCenter = orbitCenter;
        this.orbitAngle = startAngle;
        this.orbitRadius = orbitRadius;
        this.orbitHeight = orbitHeight;
        this.orbitSpeed = orbitSpeed;
        this.damage = damage;
        this.projectileSpeed = projectileSpeed;
        this.lifetime = lifetime;
        this.playerLayer = playerLayer;
        this.isOrbiting = true;
        this.hit = false;
        dieAt = Time.time + lifetime;
    }

    /// <summary>
    /// Called by the attack script to release the wisp as a fired projectile.
    /// </summary>
    [Server]
    public void Server_Release()
    {
        isOrbiting = false;

        // Fire direction is outward from center
        if (orbitCenter != null)
        {
            fireDirection = (transform.position - orbitCenter.position).normalized;
            fireDirection.y = 0f;
            if (fireDirection.sqrMagnitude < 0.001f)
                fireDirection = Vector3.forward;
        }
        else
        {
            fireDirection = transform.forward;
        }

        dieAt = Time.time + lifetime;
    }

    [ServerCallback]
    void Update()
    {
        if (hit) return;

        if (isOrbiting)
        {
            Server_HandleOrbit();
        }
        else
        {
            Server_HandleProjectile();
        }

        if (Time.time >= dieAt)
            NetworkServer.Destroy(gameObject);
    }

    [Server]
    void Server_HandleOrbit()
    {
        if (orbitCenter == null)
        {
            NetworkServer.Destroy(gameObject);
            return;
        }

        orbitAngle += orbitSpeed * Time.deltaTime;
        float rad = orbitAngle * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            Mathf.Cos(rad) * orbitRadius,
            orbitHeight,
            Mathf.Sin(rad) * orbitRadius
        );

        transform.position = orbitCenter.position + offset;
    }

    [Server]
    void Server_HandleProjectile()
    {
        transform.position += fireDirection * projectileSpeed * Time.deltaTime;
    }

    [ServerCallback]
    void OnTriggerEnter(Collider other)
    {
        if (hit || isOrbiting) return;
        if (other.isTrigger) return;
        if (((1 << other.gameObject.layer) & playerLayer.value) == 0) return;

        if (other.TryGetComponent<IDamageable>(out var dmg))
        {
            dmg.TakeDamage(damage, owner != null ? owner.transform : null);
        }

        hit = true;
        Rpc_OnHit();
        trigger.enabled = false;
        Invoke(nameof(DestroyWisp), 1.5f);
    }

    [ClientRpc]
    void Rpc_OnHit()
    {
        if (psGlow != null) psGlow.Stop();
        if (psImpact != null) psImpact.Play();

        MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
        if (renderer != null) renderer.enabled = false;
    }

    void DestroyWisp()
    {
        NetworkServer.Destroy(gameObject);
    }
}
