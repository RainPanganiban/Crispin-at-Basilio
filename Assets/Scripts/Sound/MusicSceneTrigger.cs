using UnityEngine;

/// <summary>
/// A helper component that automatically triggers sound/music changes 
/// when the scene it is in starts. Use this to ensure each scene
/// (Overworld, Levels, Shops) has the correct background track.
/// </summary>
public class MusicSceneTrigger : MonoBehaviour
{
    public enum MusicTriggerMode
    {
        MainMenu,
        Overworld,
        NormalLevel,
        OptionalLevel,
        StopMusic
    }

    [Header("Trigger Settings")]
    [Tooltip("What type of music should start playing when this scene loads?")]
    public MusicTriggerMode mode = MusicTriggerMode.NormalLevel;

    [Tooltip("The name of the level or optional area (must match the Level Name in SoundManager).")]
    [SerializeField] private string levelName;

    private void Start()
    {
        // Give the SoundManager a frame to initialize if needed
        Invoke(nameof(TriggerMusic), 0.1f);
    }

    private void TriggerMusic()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("[MusicSceneTrigger] SoundManager.Instance is null! Music cannot be triggered.");
            return;
        }

        switch (mode)
        {
            case MusicTriggerMode.MainMenu:
                SoundManager.Instance.PlayMainMenuMusic();
                break;
            case MusicTriggerMode.Overworld:
                SoundManager.Instance.PlayOverworldMusic();
                break;
            case MusicTriggerMode.NormalLevel:
                if (!string.IsNullOrEmpty(levelName))
                {
                    SoundManager.Instance.LoadNormalLevelProfile(levelName);
                    SoundManager.Instance.PlayExplorationMusic();
                }
                break;
            case MusicTriggerMode.OptionalLevel:
                if (!string.IsNullOrEmpty(levelName))
                {
                    SoundManager.Instance.PlayOptionalLevelMusic(levelName);
                }
                break;
            case MusicTriggerMode.StopMusic:
                SoundManager.Instance.StopMusic();
                break;
        }
    }
}
