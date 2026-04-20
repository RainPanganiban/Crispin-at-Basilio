using UnityEngine;
using Mirror;
using System.Collections;

public class BakunawaBubble : NetworkBehaviour
{
    [Header("Bubble Settings")]
    public float damage = 15f;
    public float explosionRadius = 2.5f;
    public float delayBeforeExplosion = 1.5f;
    public LayerMask targetLayer;
    public GameObject explosionVFXPrefab;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip risingLoopSFX; // Tunog habang lumulutang (looping)
    [SerializeField] private AudioClip popSFX;        // Tunog ng pagsabog
    [Range(0f, 1f)][SerializeField] private float volume = 0.7f;
    private AudioSource loopingSource;

    [Header("Visual Indicator Settings")]
    public float floatSpeed = 1f;
    public float floatAmplitude = 0.1f;
    public float riseSpeed = 0.5f;
    public float popScaleMultiplier = 1.8f;

    private Collider ownerCollider;
    [SyncVar] private Vector3 spawnPosition;
    [SyncVar] private float serverStartTime;

    [Server]
    public void Initialize(Collider owner)
    {
        ownerCollider = owner;
        spawnPosition = transform.position;
        serverStartTime = (float)NetworkTime.time;

        StartCoroutine(ExplodeRoutine());

        // Sabihan ang lahat ng clients na patunugin ang loop sound
        RpcPlayRisingSound();
    }

    [ClientRpc]
    private void RpcPlayRisingSound()
    {
        if (risingLoopSFX != null)
        {
            // Gumawa ng temporary AudioSource para sa loop
            loopingSource = gameObject.AddComponent<AudioSource>();
            loopingSource.clip = risingLoopSFX;
            loopingSource.loop = true;
            loopingSource.spatialBlend = 1f; // Full 3D Sound
            loopingSource.minDistance = 2f;
            loopingSource.maxDistance = 15f;
            loopingSource.volume = GetEffectiveVolume();
            loopingSource.Play();
        }
    }

    void Update()
    {
        // Visual movement sync
        float timeElapsed = (float)NetworkTime.time - serverStartTime;
        Vector3 verticalOffset = Vector3.up * (riseSpeed * timeElapsed);
        float wobbleOffset = Mathf.Sin(timeElapsed * floatSpeed) * floatAmplitude;
        transform.position = spawnPosition + verticalOffset + Vector3.up * wobbleOffset;

        // --- AUDIO LOGIC: Dynamic Pitch ---
        // Mas bumibilis/tataas ang tono ng bubble habang palapit sa putok
        if (loopingSource != null)
        {
            float progress = Mathf.Clamp01(timeElapsed / delayBeforeExplosion);
            loopingSource.pitch = Mathf.Lerp(1f, 1.6f, progress);
        }
    }

    [Server]
    private IEnumerator ExplodeRoutine()
    {
        float timer = 0f;
        Vector3 initialScale = transform.localScale;

        while (timer < delayBeforeExplosion)
        {
            timer += Time.deltaTime;
            float t = timer / delayBeforeExplosion;

            // Visual swelling effect
            if (t > 0.7f)
            {
                float surge = (t - 0.7f) / 0.3f;
                transform.localScale = initialScale * Mathf.Lerp(1f, popScaleMultiplier, surge);
            }

            yield return null;
        }

        // Damage Calculation
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, targetLayer);
        foreach (var hit in hits)
        {
            if (hit == ownerCollider) continue;

            // Gumamit ng IDamageable para standard sa lahat ng boss attacks mo
            if (hit.TryGetComponent<IDamageable>(out var dmg))
            {
                dmg.TakeDamage(damage, transform);
            }
            // Fallback sa StatsManager kung wala pang IDamageable
            else if (hit.TryGetComponent<PlayerStatsManager>(out var stats))
            {
                stats.TakeDamage(damage, transform);
            }
        }

        // Patunugin ang POP sound bago i-destroy
        RpcPlayPopSound(transform.position);

        if (explosionVFXPrefab != null)
        {
            GameObject vfx = Instantiate(explosionVFXPrefab, transform.position, Quaternion.identity);
            NetworkServer.Spawn(vfx);
        }

        NetworkServer.Destroy(gameObject);
    }

    [ClientRpc]
    private void RpcPlayPopSound(Vector3 pos)
    {
        if (popSFX != null)
        {
            // PlayClipAtPoint ay maganda rito dahil mananatili ang tunog 
            // kahit i-destroy na yung bubble object
            AudioSource.PlayClipAtPoint(popSFX, pos, GetEffectiveVolume());
        }
    }

    private float GetEffectiveVolume()
    {
        // Check kung may SoundManager ka na may Master Volume, otherwise gamitin yung default
        if (SoundManager.Instance != null)
            return SoundManager.Instance.EffectiveSFXVolume * volume;

        return volume;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}