using Mirror;
using UnityEngine;

public class OverworldManager : NetworkBehaviour
{
    public static OverworldManager Instance { get; private set; }

    [Header("Scenes")]
    [Tooltip("Name of the overworld scene in build settings (currently 'Mirror Networking').")]
    public string overworldSceneName = "Mirror Networking";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    [Server]
    public void ServerEnterLevel(string levelId, string sceneName)
    {
        if (!NetworkServer.active)
            return;

        LevelProgressionManager progression = LevelProgressionManager.Instance;
        if (progression != null && !progression.IsLevelUnlocked(levelId))
            return;

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[OverworldManager] Scene name not set for level " + levelId);
            return;
        }

        CustomNetworkManager manager = CustomNetworkManager.Instance;
        if (manager != null)
        {
            manager.ServerChangeScene(sceneName);
        }
    }
}

