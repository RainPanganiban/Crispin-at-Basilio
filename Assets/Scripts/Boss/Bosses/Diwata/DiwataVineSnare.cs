using UnityEngine;
using Mirror;

/// <summary>
/// Stationary ground hazard. Appears at a position, telegraphs with a delay,
/// then erupts dealing AoE damage to players in radius.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class DiwataVineSnare : NetworkBehaviour
{
    [Header("Visuals")]
    public ParticleSystem psTelegraph;
    public ParticleSystem psErupt;

    [Header("Runtime (server initialized)")]
    [SyncVar] private float damage;
    [SyncVar] private float eruptRadius;
    [SyncVar] private float telegraphDuration;

    private NetworkIdentity owner;
    private LayerMask playerLayer;
    private float eruptAt;
    private bool erupted;
    private SphereCollider trigger;

    void Awake()
    {
        trigger = GetComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.enabled = false; // Disabled until eruption
    }

    [Server]
    public void Server_Initialize(
        NetworkIdentity owner,
        float damage,
        float eruptRadius,
        float telegraphDuration,
        LayerMask playerLayer
    )
    {
        this.owner = owner;
        this.damage = damage;
        this.eruptRadius = eruptRadius;
        this.telegraphDuration = telegraphDuration;
        this.playerLayer = playerLayer;
        this.eruptAt = Time.time + telegraphDuration;
        this.erupted = false;

        trigger.radius = eruptRadius;

        Rpc_ShowTelegraph();
    }

    [ClientRpc]
    void Rpc_ShowTelegraph()
    {
        if (psTelegraph != null) psTelegraph.Play();
    }

    [ServerCallback]
    void Update()
    {
        if (erupted) return;

        if (Time.time >= eruptAt)
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
            if (!hit.TryGetComponent<IDamageable>(out var dmg))
                continue;

            dmg.TakeDamage(damage, owner != null ? owner.transform : null);
        }

        Rpc_OnErupt();
        Invoke(nameof(DestroySnare), 2.0f);
    }

    [ClientRpc]
    void Rpc_OnErupt()
    {
        if (psTelegraph != null) psTelegraph.Stop();
        if (psErupt != null) psErupt.Play();
    }

    void DestroySnare()
    {
        NetworkServer.Destroy(gameObject);
    }
}
