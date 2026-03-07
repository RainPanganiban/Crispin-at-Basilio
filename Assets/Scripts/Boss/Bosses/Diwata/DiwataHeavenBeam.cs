using UnityEngine;
using Mirror;

/// <summary>
/// Delayed divine light column AoE. Spawns an indicator at a position,
/// waits for a telegraph period, then fires a beam dealing damage in radius.
/// </summary>
public class DiwataHeavenBeam : NetworkBehaviour
{
    [Header("Visuals")]
    public ParticleSystem psIndicator;
    public ParticleSystem psBeam;

    [Header("Runtime (server initialized)")]
    [SyncVar] private float damage;
    [SyncVar] private float beamRadius;
    [SyncVar] private float telegraphDuration;

    private NetworkIdentity owner;
    private LayerMask playerLayer;
    private float fireAt;
    private bool fired;

    [Server]
    public void Server_Initialize(
        NetworkIdentity owner,
        float damage,
        float beamRadius,
        float telegraphDuration,
        LayerMask playerLayer
    )
    {
        this.owner = owner;
        this.damage = damage;
        this.beamRadius = beamRadius;
        this.telegraphDuration = telegraphDuration;
        this.playerLayer = playerLayer;
        this.fireAt = Time.time + telegraphDuration;
        this.fired = false;

        Rpc_ShowIndicator();
    }

    [ClientRpc]
    void Rpc_ShowIndicator()
    {
        if (psIndicator != null) psIndicator.Play();
    }

    [ServerCallback]
    void Update()
    {
        if (fired) return;

        if (Time.time >= fireAt)
        {
            Server_Fire();
        }
    }

    [Server]
    void Server_Fire()
    {
        fired = true;

        Collider[] hits = Physics.OverlapSphere(transform.position, beamRadius, playerLayer);
        foreach (Collider hit in hits)
        {
            if (!hit.TryGetComponent<IDamageable>(out var dmg))
                continue;

            dmg.TakeDamage(damage, owner != null ? owner.transform : null);
        }

        Rpc_OnFire();
        Invoke(nameof(DestroyBeam), 2.5f);
    }

    [ClientRpc]
    void Rpc_OnFire()
    {
        if (psIndicator != null) psIndicator.Stop();
        if (psBeam != null) psBeam.Play();

        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(0.3f, 0.08f);
    }

    void DestroyBeam()
    {
        NetworkServer.Destroy(gameObject);
    }
}
