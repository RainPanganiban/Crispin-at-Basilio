using UnityEngine;

/// <summary>
/// Manages sound effects for the player character.
/// Attach this component to the Player prefab alongside its other scripts.
/// 
/// Provides specific integer passing functionality so Animation Events
/// can trigger precise sequence hits (Melee 1, 2, 3) or Ranged Charge/Release.
/// </summary>
public class PlayerSoundManager : MonoBehaviour
{
    // ============================
    // Movement & Generic SFX 
    // ============================
    [Header("Movement")]
    [SerializeField] private AudioClip walkClip;
    [SerializeField] private AudioClip runClip;
    [SerializeField] private AudioClip rollClip;

    [Header("Status")]
    [SerializeField] private AudioClip hurtClip;
    [SerializeField] private AudioClip deathClip;

    // ============================
    // Combat SFX (Melee)
    // ============================
    [Header("Melee Combat")]
    [Tooltip("Assign the 3 attack sounds for the Melee combo here in order (Index 0, 1, 2).")]
    [SerializeField] private AudioClip[] meleeAttackClips;

    // ============================
    // Combat SFX (Ranged)
    // ============================
    [Header("Ranged Combat")]
    [Tooltip("Sound played when starting to draw the bow/charge attack.")]
    [SerializeField] private AudioClip rangedChargeClip;
    [Tooltip("Sound played when the projectile is released.")]
    [SerializeField] private AudioClip rangedReleaseClip;

    [Header("Movement Intervals")]
    [Tooltip("How often to play the walk sound (in seconds) while moving.")]
    [SerializeField] private float walkInterval = 0.5f;

    [Tooltip("How often to play the run sound (in seconds) while running.")]
    [SerializeField] private float runInterval = 0.3f;

    private AudioSource sfxSource;
    private AudioSource movementSource;
    private PlayerMovement playerMovement;
    private float footstepTimer;

    private void Awake()
    {
        sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        ConfigureSFXSource(sfxSource);

        // Dedicated source for movement to allow immediate stopping
        movementSource = gameObject.AddComponent<AudioSource>();
        ConfigureSFXSource(movementSource);

        playerMovement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        // Only run this logic on the local player to avoid hearing everyone's footsteps 
        // if they are non-positional, but since we use 1.0 spatial blend, 
        // we can run it for everyone or just owner. Usually footsteps are better handled per-client.
        // For Mirror, we check isLocalPlayer if we only want OUR sounds, 
        // but here we'll let it run so we hear others too (3D audio handles the distance).

        if (playerMovement == null) return;

        // Check if player is moving on the ground and not performing an action like rolling
        bool isMoving = playerMovement.MoveInput.sqrMagnitude > 0.01f && 
                        playerMovement.IsGrounded && 
                        !playerMovement.IsRolling;

        if (isMoving)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                if (playerMovement.IsRunning)
                {
                    PlayMovementClip(runClip);
                    footstepTimer = runInterval;
                }
                else
                {
                    PlayMovementClip(walkClip);
                    footstepTimer = walkInterval;
                }
            }
        }
        else
        {
            // Immediately cut off any playing footstep sounds
            if (movementSource != null && movementSource.isPlaying)
            {
                movementSource.Stop();
            }
            // Reset timer so the first step plays immediately when we start moving again
            footstepTimer = 0f;
        }
    }

    // ============================
    // Generic Play Methods
    // ============================
    public void PlayWalk() => PlayClip(walkClip);
    public void PlayRun() => PlayClip(runClip);
    public void PlayRoll() => PlayClip(rollClip);
    public void PlayHurt() => PlayClip(hurtClip);
    public void PlayDeath() => PlayClip(deathClip);

    // ============================
    // Animation Event Receivers
    // ============================

    /// <summary>
    /// Call this from Melee Attack Animation Events.
    /// In the Animation Event inspector, set the 'Int' parameter to 0, 1, or 2.
    /// </summary>
    public void PlayMeleeAttack(int comboIndex)
    {
        if (meleeAttackClips == null || meleeAttackClips.Length == 0) return;

        // Ensure we don't go out of bounds if the inspector array isn't 3 yet
        if (comboIndex >= 0 && comboIndex < meleeAttackClips.Length)
        {
            PlayClip(meleeAttackClips[comboIndex]);
        }
        else
        {
            Debug.LogWarning($"[PlayerSoundManager] Melee Attack index {comboIndex} is out of bounds!");
        }
    }

    /// <summary>Call this from the Ranged Attack charging animation event.</summary>
    public void PlayRangedCharge() => PlayClip(rangedChargeClip);

    /// <summary>Call this from the Ranged Attack firing/release animation event.</summary>
    public void PlayRangedRelease() => PlayClip(rangedReleaseClip);


    // ============================
    // Internal Helpers
    // ============================
    private void PlayClip(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, GetEffectiveSFXVolume());
    }

    private void PlayMovementClip(AudioClip clip)
    {
        if (clip == null || movementSource == null) return;
        
        // Using standard Play() instead of PlayOneShot() for footsteps
        // so movementSource.Stop() can cut the sound off instantly.
        movementSource.clip = clip;
        movementSource.volume = GetEffectiveSFXVolume();
        movementSource.Play();
    }

    private float GetEffectiveSFXVolume()
    {
        return SoundManager.Instance != null ? SoundManager.Instance.EffectiveSFXVolume : 1f;
    }

    private void ConfigureSFXSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;
    }
}
