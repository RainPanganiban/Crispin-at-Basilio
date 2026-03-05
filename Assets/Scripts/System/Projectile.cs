using UnityEngine;
using Mirror;

public class Projectile : NetworkBehaviour
{
    [Header("Projectile Settings")]
    [SyncVar] public float speed = 15f;
    [SyncVar] public float maxLifetime = 5f;
    [SyncVar] public int damage = 20;

    private float lifetime;
    private NetworkIdentity ownerIdentity; // Who fired this projectile
    private Vector3 moveDirection;
    private Collider ownerCollider;

    [Server]
    public void Initialize(int dmg, float spd, float life, Vector3 direction, Collider ownerCol, NetworkIdentity ownerId)
    {
        damage = dmg;
        speed = spd;
        maxLifetime = life;
        moveDirection = direction.normalized;
        ownerCollider = ownerCol;
        ownerIdentity = ownerId; // store the attacker
        lifetime = 0f;
    }

    public override void OnStartServer()
    {
        lifetime = 0f;
    }

    [ServerCallback]
    void Update()
    {
        transform.position += moveDirection * speed * Time.deltaTime;

        lifetime += Time.deltaTime;
        if (lifetime >= maxLifetime)
            NetworkServer.Destroy(gameObject);
    }

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (other == ownerCollider) return;

        if (other.TryGetComponent<IDamageable>(out var target))
        {
            // Friendly Fire Protection
            bool isAttackerPlayer = ownerCollider != null && ownerCollider.GetComponent<PlayerMovement>() != null;
            bool isTargetPlayer = other.GetComponent<PlayerMovement>() != null;

            if (isAttackerPlayer && isTargetPlayer)
            {
                // It's a player hitting another player, ignore the damage but destroy the projectile
                NetworkServer.Destroy(gameObject);
                return;
            }

            Transform attackerTransform = ownerIdentity != null ? ownerIdentity.transform : null;
            target.TakeDamage(damage, attackerTransform);
        }

        NetworkServer.Destroy(gameObject);
    }
}
