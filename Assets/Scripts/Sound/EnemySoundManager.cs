using UnityEngine;

/// <summary>
/// Manages sound effects for an individual enemy.
/// Attach this component to each enemy prefab so every instance
/// plays its own positional audio independently.
/// 
/// Uses PlayOneShot so multiple SFX can overlap (e.g. attack + death).
/// Volume is driven by SoundManager's Master × SFX volume settings.
/// 
/// Usage:
///   Assign AudioClips in the Inspector per enemy type, then call
///   the Play___() methods from AI scripts or animation events.
/// </summary>
public class EnemySoundManager : MonoBehaviour
{
    // ============================
    // SFX Clips (assign in Inspector)
    // ============================
    [Header("Movement")]
    [Tooltip("Sound played when the enemy walks / patrols.")]
    [SerializeField] private AudioClip walkClip;
    [Range(0f, 1f)] [SerializeField] private float walkVolume = 1f;

    [Header("Combat")]
    [Tooltip("Global multiplier for all attack sounds triggered by attack scripts.")]
    [Range(0f, 1f)] [SerializeField] private float attackVolume = 1f;

    [Tooltip("Sound played when the enemy takes damage.")]
    [SerializeField] private AudioClip hurtClip;
    [Range(0f, 1f)] [SerializeField] private float hurtVolume = 1f;

    [Tooltip("Sound played when the enemy dies.")]
    [SerializeField] private AudioClip deathClip;
    [Range(0f, 1f)] [SerializeField] private float deathVolume = 1f;

    [Header("Interval Settings")]
    [Tooltip("How often to play the walk sound (seconds) while moving.")]
    [SerializeField] private float walkInterval = 0.5f;

    // ============================
    // State Tracking
    // ============================
    private AudioSource sfxSource;
    private Vector3 lastPosition;
    private float footstepTimer;

    // ============================
    // Unity Lifecycle
    // ============================
    private void Awake()
    {
        // Use an existing AudioSource or add one automatically
        sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        ConfigureSFXSource(sfxSource);
        
        lastPosition = transform.position;
    }

    private void Update()
    {
        // Automatic Movement Sounds
        Vector3 displacement = transform.position - lastPosition;
        displacement.y = 0;
        float velocity = displacement.magnitude / Time.deltaTime;
        lastPosition = transform.position;

        // Only play if moving faster than a tiny threshold
        if (velocity > 0.1f)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0)
            {
                PlayWalk();
                footstepTimer = walkInterval;
            }
        }
        else
        {
            footstepTimer = 0; // Ready to play immediately when starting to move
        }
    }

    // ============================
    // Public Play Methods
    // ============================

    /// <summary>Plays the walking / patrol sound.</summary>
    public void PlayWalk()
    {
        PlayClip(walkClip, walkVolume);
    }

    /// <summary>Plays a specific attack sound provided by the calling attack script.</summary>
    public void PlaySpecificAttack(AudioClip clip)
    {
        PlayClip(clip, attackVolume);
    }

    /// <summary>Plays the hurt / damage-taken sound.</summary>
    public void PlayHurt()
    {
        PlayClip(hurtClip, hurtVolume);
    }

    /// <summary>Plays the death sound.</summary>
    public void PlayDeath()
    {
        PlayClip(deathClip, deathVolume);
    }

    // ============================
    // Internal Helpers
    // ============================

    /// <summary>
    /// Plays a clip via PlayOneShot so multiple sounds can overlap.
    /// Volume is scaled by the global SFX volume from SoundManager AND the local multiplier.
    /// </summary>
    private void PlayClip(AudioClip clip, float localVolumeMultiplier = 1f)
    {
        if (clip == null) return;

        float volume = GetEffectiveSFXVolume() * localVolumeMultiplier;
        sfxSource.PlayOneShot(clip, volume);
    }

    /// <summary>
    /// Returns the combined Master × SFX volume from the global SoundManager.
    /// Falls back to 1.0 if SoundManager hasn't been set up yet.
    /// </summary>
    private float GetEffectiveSFXVolume()
    {
        if (SoundManager.Instance != null)
            return SoundManager.Instance.EffectiveSFXVolume;

        return 1f;
    }

    /// <summary>Configures the AudioSource for short, non-looping 3D sound effects.</summary>
    private void ConfigureSFXSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f; // 3D — sound originates from enemy's world position
    }
}
