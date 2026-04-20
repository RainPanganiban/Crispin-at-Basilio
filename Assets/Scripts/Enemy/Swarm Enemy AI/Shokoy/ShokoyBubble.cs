using UnityEngine;
using Mirror;
using System.Collections;

public class ShokoyBubble : NetworkBehaviour
{
    [Header("Bubble Settings")]
    public float damage = 15f;
    public float explosionRadius = 2.5f;
    public float delayBeforeExplosion = 1.5f;
    public LayerMask targetLayer;
    public float bubbleLifetime;
    public LayerMask playerLayer;
    public GameObject explosionVFXPrefab;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip risingLoopSFX; // Tunog habang lumulutang
    [SerializeField] private AudioClip popSFX;        // Tunog ng pagsabog
    [Range(0f, 1f)][SerializeField] private float volume = 0.7f;
    private AudioSource loopingSource;

    [Header("Visual Indicator Settings")]
    public float floatSpeed = 1f;
    public float floatAmplitude = 0.1f;
    public float riseSpeed = 0.5f;
    public float popScaleMultiplier = 1.8f;
    public float enlargementSpeed = 2f;

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

        // Tawagin ang RPC para sa lahat ng players
        RpcPlayRisingSound();
    }

    [ClientRpc]
    private void RpcPlayRisingSound()
    {
        // Gagamitin ang 'risingLoopSFX' na naka-assign na sa prefab
        if (risingLoopSFX != null)
        {
            loopingSource = gameObject.AddComponent<AudioSource>();
            loopingSource.clip = risingLoopSFX;
            loopingSource.loop = true;
            loopingSource.spatialBlend = 1f; // 3D Sound
            loopingSource.volume = GetEffectiveVolume();
            loopingSource.Play();
        }
    }

    void Update()
    {
        float timeElapsed = (float)NetworkTime.time - serverStartTime;
        Vector3 verticalOffset = Vector3.up * (riseSpeed * timeElapsed);
        float wobbleOffset = Mathf.Sin(timeElapsed * floatSpeed) * floatAmplitude;
        transform.position = spawnPosition + verticalOffset + Vector3.up * wobbleOffset;

        // Dynamic Pitch Logic
        if (loopingSource != null)
        {
            float progress = Mathf.Clamp01(timeElapsed / delayBeforeExplosion);
            loopingSource.pitch = Mathf.Lerp(1f, 1.5f, progress);
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

            if (t > 0.7f)
            {
                float surge = (t - 0.7f) / 0.3f;
                transform.localScale = initialScale * Mathf.Lerp(1f, popScaleMultiplier, surge);
            }
            yield return null;
        }

        // Explode Logic
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, targetLayer);
        foreach (var hit in hits)
        {
            if (hit == ownerCollider) continue;
            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null) damageable.TakeDamage(damage, transform);
        }

        // Tawagin ang Pop RPC bago i-destroy
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
        // Gagamitin ang 'popSFX' na naka-assign na sa prefab
        if (popSFX != null)
        {
            AudioSource.PlayClipAtPoint(popSFX, pos, GetEffectiveVolume());
        }
    }

    private float GetEffectiveVolume()
    {
        if (SoundManager.Instance != null)
            return SoundManager.Instance.EffectiveSFXVolume * volume;
        return volume;
    }
}