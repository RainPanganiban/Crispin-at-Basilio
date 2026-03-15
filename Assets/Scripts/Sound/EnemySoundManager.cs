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

    [Header("Combat")]
    [Tooltip("Sound played when the enemy attacks.")]
    [SerializeField] private AudioClip attackClip;

    [Tooltip("Sound played when the enemy takes damage.")]
    [SerializeField] private AudioClip hurtClip;

    [Tooltip("Sound played when the enemy dies.")]
    [SerializeField] private AudioClip deathClip;

    // ============================
    // Audio Source
    // ============================
    private AudioSource sfxSource;

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
    }

    // ============================
    // Public Play Methods
    // ============================

    /// <summary>Plays the walking / patrol sound.</summary>
    public void PlayWalk()
    {
        PlayClip(walkClip);
    }

    /// <summary>Plays the attack sound.</summary>
    public void PlayAttack()
    {
        PlayClip(attackClip);
    }

    /// <summary>Plays the hurt / damage-taken sound.</summary>
    public void PlayHurt()
    {
        PlayClip(hurtClip);
    }

    /// <summary>Plays the death sound.</summary>
    public void PlayDeath()
    {
        PlayClip(deathClip);
    }

    // ============================
    // Internal Helpers
    // ============================

    /// <summary>
    /// Plays a clip via PlayOneShot so multiple sounds can overlap.
    /// Volume is scaled by the global SFX volume from SoundManager.
    /// </summary>
    private void PlayClip(AudioClip clip)
    {
        if (clip == null) return;

        float volume = GetEffectiveSFXVolume();
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
