using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public struct LevelMusicProfile
{
    [Tooltip("Identifier for this level (e.g., 'Level1', 'Forest').")]
    public string levelName;
    public AudioClip explorationMusic;
    public AudioClip swarmMusic;
    public AudioClip bossMusic;
}

[System.Serializable]
public struct OptionalLevelMusicProfile
{
    [Tooltip("Identifier for the optional level.")]
    public string levelName;
    [Tooltip("The single background track for this optional area.")]
    public AudioClip backgroundMusic;
}

/// <summary>
/// Global singleton that manages background music playback.
/// Supports distinct music profiles per normal level (Exploration, Swarm, Boss)
/// and single-track profiles for optional levels.
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Menu Music")]
    [SerializeField] private AudioClip mainMenuMusic;
    [Tooltip("If true, the Main Menu music will start playing automatically on Awake.")]
    [SerializeField] private bool autoPlayMenuMusic = true;

    [Header("Overworld Music")]
    [SerializeField] private AudioClip overworldMusic;

    [Header("Normal Level Music Profiles")]
    [Tooltip("Define the 3 tracks for each main level here.")]
    [SerializeField] private List<LevelMusicProfile> normalLevels = new List<LevelMusicProfile>();

    [Header("Optional Level Music Profiles")]
    [Tooltip("Define the single track for each optional level here.")]
    [SerializeField] private List<OptionalLevelMusicProfile> optionalLevels = new List<OptionalLevelMusicProfile>();

    [Header("Volume Settings")]
    [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.7f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;

    [Header("Transition Settings")]
    [SerializeField] private float fadeDuration = 1.5f;

    // The active clips for the CURRENTLY loaded normal level
    private AudioClip currentExplorationMusic;
    private AudioClip currentSwarmMusic;
    private AudioClip currentBossMusic;

    // Crossfading State
    private AudioSource musicSourceA;
    private AudioSource musicSourceB;
    private bool isSourceAActive = true;
    private Coroutine fadeCoroutine;

    public float MasterVolume { get => masterVolume; set { masterVolume = Mathf.Clamp01(value); ApplyMusicVolume(); } }
    public float MusicVolume { get => musicVolume; set { musicVolume = Mathf.Clamp01(value); ApplyMusicVolume(); } }
    public float SFXVolume { get => sfxVolume; set => sfxVolume = Mathf.Clamp01(value); }
    public float EffectiveMusicVolume => masterVolume * musicVolume;
    public float EffectiveSFXVolume => masterVolume * sfxVolume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicSourceA = gameObject.AddComponent<AudioSource>();
        musicSourceB = gameObject.AddComponent<AudioSource>();
        ConfigureMusicSource(musicSourceA);
        ConfigureMusicSource(musicSourceB);
    }

    private void Start()
    {
        if (autoPlayMenuMusic)
        {
            PlayMainMenuMusic();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnValidate()
    {
        // This ensures the volume changes in the Inspector 
        // take effect immediately while the game is running.
        if (Application.isPlaying && Instance == this)
        {
            ApplyMusicVolume();
        }
    }

    // ============================
    // Profile Loading
    // ============================

    /// <summary>
    /// Loads the 3 music tracks for a specific normal level so they are ready to play.
    /// Call this when entering a new normal level (e.g., from a LevelManager).
    /// </summary>
    public void LoadNormalLevelProfile(string levelName)
    {
        foreach (var profile in normalLevels)
        {
            if (profile.levelName == levelName)
            {
                currentExplorationMusic = profile.explorationMusic;
                currentSwarmMusic = profile.swarmMusic;
                currentBossMusic = profile.bossMusic;
                Debug.Log($"[SoundManager] Loaded Normal Level Profile: {levelName}");
                return;
            }
        }
        Debug.LogWarning($"[SoundManager] Could not find Normal Level Profile for: {levelName}");
    }

    /// <summary>
    /// Loads and immediately plays the single track for an optional level.
    /// Call this when entering an optional level.
    /// </summary>
    public void PlayOptionalLevelMusic(string levelName)
    {
        foreach (var profile in optionalLevels)
        {
            if (profile.levelName == levelName)
            {
                CrossfadeTo(profile.backgroundMusic);
                Debug.Log($"[SoundManager] Playing Optional Level Music: {levelName}");
                return;
            }
        }
        Debug.LogWarning($"[SoundManager] Could not find Optional Level Profile for: {levelName}");
    }

    // ============================
    // Standard Play Methods
    // ============================

    public void PlayMainMenuMusic() => CrossfadeTo(mainMenuMusic);

    /// <summary>Immediately crossfades to the Overworld music track.</summary>
    public void PlayOverworldMusic() => CrossfadeTo(overworldMusic);

    // These rely on LoadNormalLevelProfile() having been called first
    public void PlayExplorationMusic() => CrossfadeTo(currentExplorationMusic);
    public void PlaySwarmMusic() => CrossfadeTo(currentSwarmMusic);
    public void PlayBossMusic() => CrossfadeTo(currentBossMusic);
    public void StopMusic() => CrossfadeTo(null);

    // ============================
    // Crossfade Logic
    // ============================

    private void CrossfadeTo(AudioClip newClip)
    {
        AudioSource activeSource = isSourceAActive ? musicSourceA : musicSourceB;
        AudioSource inactiveSource = isSourceAActive ? musicSourceB : musicSourceA;

        if (activeSource.clip == newClip && activeSource.isPlaying && newClip != null) return;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(CrossfadeRoutine(activeSource, inactiveSource, newClip));
    }

    private IEnumerator CrossfadeRoutine(AudioSource fadeOutSource, AudioSource fadeInSource, AudioClip newClip)
    {
        float targetVolume = EffectiveMusicVolume;
        float elapsed = 0f;

        if (newClip != null)
        {
            fadeInSource.clip = newClip;
            fadeInSource.volume = 0f;
            fadeInSource.Play();
        }

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float currentMaxVolume = EffectiveMusicVolume;

            if (fadeOutSource.isPlaying) fadeOutSource.volume = Mathf.Lerp(currentMaxVolume, 0f, t);
            if (newClip != null) fadeInSource.volume = Mathf.Lerp(0f, currentMaxVolume, t);

            yield return null;
        }

        fadeOutSource.Stop();
        fadeOutSource.clip = null;
        fadeOutSource.volume = 0f;

        if (newClip != null) fadeInSource.volume = targetVolume;

        isSourceAActive = !isSourceAActive;
        fadeCoroutine = null;
    }

    private void ConfigureMusicSource(AudioSource source)
    {
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0f;
        source.spatialBlend = 0f;
    }

    private void ApplyMusicVolume()
    {
        float vol = EffectiveMusicVolume;
        
        // Apply to both sources so crossfades are also updated in real-time
        if (musicSourceA != null)
        {
            // If the source is fading out, its volume shouldn't be clamped 
            // but for simplicity, we apply the master scale. 
            // The crossfade routine will still do the Lerp.
            if (musicSourceA.isPlaying) musicSourceA.volume = vol;
        }
        if (musicSourceB != null)
        {
            if (musicSourceB.isPlaying) musicSourceB.volume = vol;
        }
    }
}
