using UnityEngine;
using Mirror;

[RequireComponent(typeof(SphereCollider))]
public class DiwataPetal : NetworkBehaviour
{
    [Header("Visuals")]
    public ParticleSystem psTrail;
    public ParticleSystem psImpact;

    [Header("Sync Variables")]
    [SyncVar] private float damage;
    [SyncVar] private float speed;
    [SyncVar] private float lifetime;
    [SyncVar] private Vector3 direction; // Ginawang SyncVar para alam ng Client ang direksyon

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
    public void Server_Initialize(NetworkIdentity owner, Vector3 direction, float speed, float damage, float lifetime, LayerMask playerLayer)
    {
        this.owner = owner;
        this.direction = direction.normalized;
        this.speed = speed;
        this.damage = damage;
        this.lifetime = lifetime;
        this.playerLayer = playerLayer;
        dieAt = Time.time + lifetime;
    }

    // Inalis ang [ServerCallback] para gumalaw din sa side ng Client
    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;

        // Server pa rin ang may hawak ng destruction logic
        if (isServer && Time.time >= dieAt)
            NetworkServer.Destroy(gameObject);
    }

    [ServerCallback]
    void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger) return;

        // Siguraduhin na ang playerLayer ay naka-sync o nase-set din sa client kung gagamitin sa client logic
        if (((1 << other.gameObject.layer) & playerLayer.value) == 0) return;

        if (other.TryGetComponent<IDamageable>(out var dmg))
        {
            dmg.TakeDamage(damage, owner != null ? owner.transform : null);
        }

        Rpc_OnHit();
        trigger.enabled = false;
        Invoke(nameof(DestroyPetal), 1.5f);
    }

    [ClientRpc]
    void Rpc_OnHit()
    {
        if (psTrail != null) psTrail.Stop();
        if (psImpact != null) psImpact.Play();

        MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
        if (renderer != null) renderer.enabled = false;
    }

    [Server]
    void DestroyPetal()
    {
        NetworkServer.Destroy(gameObject);
    }
}