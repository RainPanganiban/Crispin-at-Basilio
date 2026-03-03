using UnityEngine;
using Mirror;
using System.Collections;

/// <summary>
/// Server-authoritative manager placed in arena scenes.
/// When the arena is completed it snapshots player data, shows a results screen,
/// waits a configurable delay, and returns everyone to the overworld.
///
/// Unlike LevelCompleteManager, this does NOT mark any level as completed
/// (arenas are optional and repeatable).
/// </summary>
public class ArenaCompleteManager : NetworkBehaviour
{
    [Header("Transition")]
    [Tooltip("Seconds to wait after arena completion before returning to overworld.")]
    public float returnDelay = 6f;

    [Tooltip("Overworld scene name in build settings.")]
    public string overworldSceneName = "Mirror Networking";

    private bool arenaCompleted = false;

    public override void OnStartServer()
    {
        base.OnStartServer();
        ArenaManager.OnArenaCompleted += HandleArenaCompleted;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        ArenaManager.OnArenaCompleted -= HandleArenaCompleted;
    }

    [Server]
    void HandleArenaCompleted()
    {
        if (arenaCompleted) return;
        arenaCompleted = true;

        Debug.Log($"[ArenaCompleteManager] Arena cleared! Starting return countdown ({returnDelay}s).");

        // Snapshot player health & coins into session data
        SnapshotAllPlayerData();

        // Tell all clients to show the arena complete UI
        RpcShowArenaComplete(returnDelay);

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

        Debug.Log("[ArenaCompleteManager] Returning to overworld.");

        CustomNetworkManager manager = CustomNetworkManager.Instance;
        if (manager != null)
        {
            manager.ServerChangeScene(overworldSceneName);
        }
    }

    [ClientRpc]
    void RpcShowArenaComplete(float countdown)
    {
        ArenaUI ui = FindFirstObjectByType<ArenaUI>();
        if (ui != null)
        {
            ui.ShowArenaComplete(countdown);
        }
        else
        {
            Debug.LogWarning("[ArenaCompleteManager] No ArenaUI found in scene.");
        }
    }
}
