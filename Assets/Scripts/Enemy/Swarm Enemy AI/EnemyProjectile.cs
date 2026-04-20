using UnityEngine;
using Mirror;

public class EnemyProjectile : NetworkBehaviour
{
    [Header("Projectile Settings")]
    [SyncVar] public float speed = 15f;
    [SyncVar] public float maxLifetime = 5f;
    [SyncVar] public int damage = 20;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip impactSFX; // Tunog kapag tumama
    [Range(0f, 1f)][SerializeField] private float volume = 0.8f;

    private float lifetime;
    private NetworkIdentity ownerIdentity;
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
        ownerIdentity = ownerId;
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

        // Sabihan ang lahat ng clients na patugtugin ang sound bago ma-destroy ang object
        RpcPlayImpactSound();

        // Do not damage other enemies
        if (other.GetComponent<EnemyHealth>() != null)
        {
            NetworkServer.Destroy(gameObject);
            return;
        }

        if (other.TryGetComponent<IDamageable>(out var target))
        {
            Transform attackerTransform = ownerIdentity != null ? ownerIdentity.transform : null;
            target.TakeDamage(damage, attackerTransform);
        }

        NetworkServer.Destroy(gameObject);
    }

    // CORRECTED RPC: Wala na itong AudioClip parameter para iwas error sa Mirror
    [ClientRpc]
    private void RpcPlayImpactSound()
    {
        if (impactSFX != null)
        {
            // Gagamit ng transform.position ng projectile sa oras ng impact
            AudioSource.PlayClipAtPoint(impactSFX, transform.position, GetEffectiveVolume());
        }
    }

    private float GetEffectiveVolume()
    {
        if (SoundManager.Instance != null)
            return SoundManager.Instance.EffectiveSFXVolume * volume;

        return volume;
    }
}