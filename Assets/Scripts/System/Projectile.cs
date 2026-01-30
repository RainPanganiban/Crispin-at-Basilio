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
    private Vector3 moveDirection;
    private Collider ownerCollider;

    [Server]
    public void Initialize(int dmg, float spd, float life, Vector3 direction, Collider ownerCol)
    {
        damage = dmg;
        speed = spd;
        maxLifetime = life;
        moveDirection = direction.normalized;
        ownerCollider = ownerCol;
        lifetime = 0f;
    }

    public override void OnStartServer()
    {
        lifetime = 0f;
    }

    [ServerCallback]
    void Update()
    {
        // Move using the precomputed direction
        transform.position += moveDirection * speed * Time.deltaTime;

        lifetime += Time.deltaTime;
        if (lifetime >= maxLifetime)
        {
            NetworkServer.Destroy(gameObject);
        }
    }

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (other == ownerCollider) return;
        
        if (other.TryGetComponent<IDamageable>(out var target))
        {
            target.TakeDamage(damage);
        }

        NetworkServer.Destroy(gameObject);
    }
}
