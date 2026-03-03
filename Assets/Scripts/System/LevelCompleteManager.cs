using UnityEngine;
using Mirror;
using System.Collections;

/// <summary>
/// Server-authoritative manager placed in each level scene.
/// When the boss dies it snapshots player data, shows a victory screen,
/// waits a configurable delay, marks the level complete, and returns to the overworld.
/// </summary>
public class LevelCompleteManager : NetworkBehaviour
{
    [Header("Level Config")]
    [Tooltip("Must match the levelId in LevelProgressionManager.")]
    public string levelId;

    [Header("Transition")]
    [Tooltip("Seconds to wait after boss death before returning to overworld.")]
    public float returnDelay = 8f;

    [Tooltip("Overworld scene name in build settings.")]
    public string overworldSceneName = "Mirror Networking";

    private bool levelCompleted = false;

    public override void OnStartServer()
    {
        base.OnStartServer();
        BossDeathHandler.OnBossDefeated += HandleBossDefeated;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        BossDeathHandler.OnBossDefeated -= HandleBossDefeated;
    }

    [Server]
    void HandleBossDefeated()
    {
        if (levelCompleted) return;
        levelCompleted = true;

        Debug.Log($"[LevelCompleteManager] Boss defeated in level '{levelId}'. Starting return countdown ({returnDelay}s).");

        // Snapshot player health & coins into session data now
        SnapshotAllPlayerData();

        // Queue level completion — LevelProgressionManager doesn't exist in level scenes,
        // so we store it in CustomNetworkManager and apply when overworld reloads.
        CustomNetworkManager manager = CustomNetworkManager.Instance;
        if (manager != null)
        {
            manager.ServerMarkLevelCompleted(levelId);
        }

        // Tell all clients to show the victory UI
        RpcShowLevelComplete(returnDelay);

        // Start the countdown to return to overworld
        StartCoroutine(ReturnToOverworldAfterDelay());
    }

    [Server]
    void SnapshotAllPlayerData()
    {
        CustomNetworkManager manager = CustomNetworkManager.Instance;
        if (manager == null) return;

        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn.identity == null) continue;

            if (!manager.TryGetSessionData(conn, out CustomNetworkManager.PlayerSessionData data))
                continue;

            // Save health
            PlayerStatsManager stats = conn.identity.GetComponent<PlayerStatsManager>();
            if (stats != null)
            {
                data.currentHealth = stats.health.currentValue;
                data.maxHealth = stats.health.maxValue;
            }

            // Save coins
            PlayerCurrencyManager currencyManager = conn.identity.GetComponent<PlayerCurrencyManager>();
            if (currencyManager != null)
            {
                data.coins = currencyManager.GetCoins();
            }
        }
    }

    IEnumerator ReturnToOverworldAfterDelay()
    {
        yield return new WaitForSeconds(returnDelay);

        Debug.Log("[LevelCompleteManager] Returning to overworld.");

        CustomNetworkManager manager = CustomNetworkManager.Instance;
        if (manager != null)
        {
            manager.ServerChangeScene(overworldSceneName);
        }
    }

    [ClientRpc]
    void RpcShowLevelComplete(float countdown)
    {
        LevelCompleteUI ui = FindFirstObjectByType<LevelCompleteUI>();
        if (ui != null)
        {
            ui.Show(countdown);
        }
        else
        {
            Debug.LogWarning("[LevelCompleteManager] No LevelCompleteUI found in scene.");
        }
    }
}
