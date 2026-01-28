using UnityEngine;
using Mirror;

public class Projectile : NetworkBehaviour
{
    [Header("Projectile Settings")]
    [SyncVar] public float speed = 15f;
    [SyncVar] public float maxLifetime = 5f;
    [SyncVar] public int damage = 20;

    private float lifetime;
    private NetworkIdentity owner;

    /// <summary>
    /// Initialize projectile values dynamically (call this before NetworkServer.Spawn)
    /// </summary>
    [Server]
    public void Initialize(int dmg, float spd, float life, NetworkIdentity ownerIdentity = null)
    {
        damage = dmg;                 // now int
        speed = spd;
        maxLifetime = life;
        owner = ownerIdentity;        // optional, can track who fired
        lifetime = 0f;
    }

    public override void OnStartServer()
    {
        lifetime = 0f;
    }

    [ServerCallback]
    void Update()
    {
        // Move forward
        transform.position += transform.forward * speed * Time.deltaTime;

        // Lifetime check
        lifetime += Time.deltaTime;
        if (lifetime >= maxLifetime)
        {
            NetworkServer.Destroy(gameObject);
        }
    }

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IDamageable>(out var target))
        {
            target.TakeDamage(damage);
        }

        NetworkServer.Destroy(gameObject);
    }
}
