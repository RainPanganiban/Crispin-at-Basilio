using UnityEngine;
using Mirror;

public class ShokoyWaterProjectile : NetworkBehaviour
{
    public GameObject hazardPuddlePrefab;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip impactSFX; // I-drag dito ang "Splash" o "Water Hit" sound
    [Range(0f, 1f)][SerializeField] private float volume = 0.8f;

    private Vector3 startPos;
    private Vector3 targetPos;
    private float arcHeight;
    private float speed;
    private float damage;
    private Collider ownerCollider;
    private NetworkIdentity ownerIdentity;

    private float progress = 0f;
    private System.Collections.Generic.HashSet<IDamageable> damagedTargets = new System.Collections.Generic.HashSet<IDamageable>();

    [Server]
    public void InitializeArc(Vector3 target, float arcZ, float spd, float dmg, Collider ownerCol, NetworkIdentity ownerId)
    {
        startPos = transform.position;
        targetPos = target;
        arcHeight = arcZ;
        speed = spd;
        damage = dmg;
        ownerCollider = ownerCol;
        ownerIdentity = ownerId;
        progress = 0f;
    }

    [ServerCallback]
    void Update()
    {
        float totalDist = Vector3.Distance(startPos, targetPos);
        if (totalDist < 0.001f) totalDist = 1f;

        progress += speed * Time.deltaTime / totalDist;

        Vector3 currentPos = Vector3.Lerp(startPos, targetPos, progress);

        // Add arc
        float yOffset = Mathf.Sin(progress * Mathf.PI) * arcHeight;
        currentPos.y += yOffset;

        transform.position = currentPos;

        if (progress >= 2.0f)
        {
            NetworkServer.Destroy(gameObject);
        }
    }

    [ServerCallback]
    void OnTriggerEnter(Collider other)
    {
        if (other == ownerCollider) return;

        bool isGrounded = other.CompareTag("Ground") || other.gameObject.layer == LayerMask.NameToLayer("Ground");

        if (other.TryGetComponent<IDamageable>(out var hitTarget))
        {
            if (!damagedTargets.Contains(hitTarget))
            {
                hitTarget.TakeDamage(damage, ownerIdentity != null ? ownerIdentity.transform : null);
                damagedTargets.Add(hitTarget);

                // MAG-PLAY NG SOUND KAPAG TUMAMA SA PLAYER
                RpcPlayImpactSound(transform.position);
            }
        }
        else if (isGrounded)
        {
            SpawnPuddleAndDestroy();
        }
    }

    [Server]
    private void SpawnPuddleAndDestroy()
    {
        // PATUGTUGIN ANG SOUND SA LAHAT NG CLIENTS BAGO MA-DESTROY
        RpcPlayImpactSound(transform.position);

        if (hazardPuddlePrefab != null)
        {
            Vector3 spawnPos = transform.position;
            if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 10f))
            {
                spawnPos = hit.point + Vector3.up * 0.05f;
            }
            else
            {
                spawnPos.y = 0.05f;
            }

            GameObject puddle = Instantiate(hazardPuddlePrefab, spawnPos, Quaternion.identity);
            NetworkServer.Spawn(puddle);
        }

        NetworkServer.Destroy(gameObject);
    }

    [ClientRpc]
    private void RpcPlayImpactSound(Vector3 position)
    {
        if (impactSFX != null)
        {
            // Ang PlayClipAtPoint ay gumagawa ng temporary AudioSource sa world space
            // para kahit ma-destroy itong projectile, tuloy pa rin ang tunog.
            AudioSource.PlayClipAtPoint(impactSFX, position, GetEffectiveVolume());
        }
    }

    private float GetEffectiveVolume()
    {
        // Kung meron kang SoundManager na nag-handle ng global volume
        if (SoundManager.Instance != null)
            return SoundManager.Instance.EffectiveSFXVolume * volume;

        return volume;
    }
}