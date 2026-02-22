using UnityEngine;
using Mirror;
using System.Collections.Generic;

[RequireComponent(typeof(SphereCollider))]
public class OngloShockwaveRing : NetworkBehaviour
{
    [Header("Runtime (server initialized)")]
    [SyncVar] private float damage;
    [SyncVar] private float expandSpeed;
    [SyncVar] private float maxRadius;

    private SphereCollider trigger;
    private NetworkIdentity owner;
    private LayerMask playerLayer;
    private readonly HashSet<uint> hitNetIds = new HashSet<uint>();

    void Awake()
    {
        trigger = GetComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.1f;
    }

    [Server]
    public void Server_Initialize(NetworkIdentity owner, float damage, float expandSpeed, float maxRadius, LayerMask playerLayer)
    {
        this.owner = owner;
        this.damage = damage;
        this.expandSpeed = expandSpeed;
        this.maxRadius = maxRadius;
        this.playerLayer = playerLayer;
        trigger.radius = 0.1f;
    }

    [ServerCallback]
    void Update()
    {
        if (trigger == null)
            return;

        trigger.radius += expandSpeed * Time.deltaTime;

        if (trigger.radius >= maxRadius)
            NetworkServer.Destroy(gameObject);
    }

    [ServerCallback]
    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & playerLayer.value) == 0)
            return;

        if (!other.TryGetComponent<IDamageable>(out var dmg))
            return;

        NetworkIdentity victimId = other.GetComponentInParent<NetworkIdentity>();
        if (victimId != null)
        {
            if (!hitNetIds.Add(victimId.netId))
                return;
        }

        dmg.TakeDamage(damage, owner != null ? owner.transform : null);
    }
}

