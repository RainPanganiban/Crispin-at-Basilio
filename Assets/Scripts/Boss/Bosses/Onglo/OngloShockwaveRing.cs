using UnityEngine;
using Mirror;
using System.Collections.Generic;
using UnityEngine.VFX;

[RequireComponent(typeof(SphereCollider))]
public class OngloShockwaveRing : NetworkBehaviour
{
    [Header("Visuals")]
    public VisualEffect vfxRing;
    public ParticleSystem psRing;
    public string vfxRadiusParam = "Radius";

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

    void LateUpdate()
    {
        if (vfxRing != null)
        {
            // Calculate radius locally on clients using the syncvars
            // We use a local timer to match server's trigger.radius expansion
            float currentRadius = Mathf.Min(maxRadius, expandSpeed * age);
            vfxRing.SetFloat(vfxRadiusParam, currentRadius);
            
            if (currentRadius >= maxRadius)
                vfxRing.Stop();
        }

        if (psRing != null && !psRing.isPlaying && age < 0.1f)
        {
            psRing.Play();
        }

        age += Time.deltaTime;
    }

    private float age;

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

