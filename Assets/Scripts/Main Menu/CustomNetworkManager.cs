using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class CustomNetworkManager : NetworkManager
{
    [System.Serializable]
    public class PlayerSessionData
    {
        public string playerClass;
        public string playerName;

        // Session-long progression values
        public int coins;
        public List<string> purchasedUpgrades = new List<string>();
    }

    [Header("Prefabs")]
    public GameObject lobbyPlayerPrefab;      // Lobby player prefab
    
    [Header("Gameplay Prefabs")]
    public GameObject crispinGameplayPrefab;
    public GameObject basilioGameplayPrefab;

    // Session data keyed by connectionId so it survives scene changes
    private readonly Dictionary<int, PlayerSessionData> sessionDataByConnection =
        new Dictionary<int, PlayerSessionData>();

    public static CustomNetworkManager Instance => (CustomNetworkManager)singleton;
    
    public override void Awake()
    {
        base.Awake();
        autoCreatePlayer = false;
    }
    
    // Called when client requests a player (Lobby only)
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        // In this project, we only add players explicitly in the Lobby.
        if (SceneManager.GetActiveScene().name != "Lobby")
            return;

        if (lobbyPlayerPrefab == null)
        {
            Debug.LogWarning("[CustomNetworkManager] Lobby player prefab is not assigned.");
            return;
        }

        GameObject lobbyPlayer = Instantiate(lobbyPlayerPrefab);
        NetworkServer.AddPlayerForConnection(conn, lobbyPlayer);
    }

    public override void OnServerChangeScene(string newSceneName)
    {
        // When leaving the Lobby, cache player selection into session data
        string currentSceneName = SceneManager.GetActiveScene().name;

        if (currentSceneName == "Lobby")
        {
            foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
            {
                if (conn.identity == null)
                    continue;

                LobbyPlayer lobbyPlayer = conn.identity.GetComponent<LobbyPlayer>();
                if (lobbyPlayer == null)
                    continue;

                if (!sessionDataByConnection.TryGetValue(conn.connectionId, out PlayerSessionData data))
                {
                    data = new PlayerSessionData();
                    sessionDataByConnection[conn.connectionId] = data;
                }

                data.playerClass = lobbyPlayer.playerClass;
                data.playerName = lobbyPlayer.playerName;
            }
        }

        base.OnServerChangeScene(newSceneName);
    }

    // Called AFTER scene has fully loaded
    public override void OnServerSceneChanged(string sceneName)
    {
        // Handle all gameplay scenes (not just overworld)
        // Skip Lobby scene (players are already spawned there)
        if (sceneName == "Lobby")
            return;

        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (!sessionDataByConnection.TryGetValue(conn.connectionId, out PlayerSessionData data))
            {
                continue;
            }

            GameObject prefabToSpawn = GetGameplayPrefabForClass(data.playerClass);
            if (prefabToSpawn == null)
            {
                continue;
            }

            Transform startPos = GetStartPosition();
            Vector3 spawnPos = startPos ? startPos.position : Vector3.zero;
            Quaternion spawnRot = startPos ? startPos.rotation : Quaternion.identity;

            GameObject gameplayPlayer = Instantiate(prefabToSpawn, spawnPos, spawnRot);

            PlayerIdentity identity = gameplayPlayer.GetComponent<PlayerIdentity>();
            if (identity != null)
            {
                identity.playerName = data.playerName;
            }

            NetworkServer.ReplacePlayerForConnection(conn, gameplayPlayer, true);

            ApplySessionDataToPlayer(gameplayPlayer, conn);
        }
    }

    GameObject GetGameplayPrefabForClass(string playerClass)
    {
        if (playerClass == "Crispin")
            return crispinGameplayPrefab;
        if (playerClass == "Basilio")
            return basilioGameplayPrefab;

        return null;
    }

    void ApplySessionDataToPlayer(GameObject player, NetworkConnectionToClient conn)
    {
        if (!sessionDataByConnection.TryGetValue(conn.connectionId, out PlayerSessionData data))
            return;

        PlayerStatsManager stats = player.GetComponent<PlayerStatsManager>();
        if (stats != null)
        {
            stats.ServerApplyPersistentData(data.coins, data.purchasedUpgrades);
        }

        // Sync coins to PlayerCurrency component if it exists
        PlayerCurrency currency = player.GetComponent<PlayerCurrency>();
        if (currency != null)
        {
            currency.SetCoins(data.coins);
        }
    }

    // Example hook to record rewards after a completed level.
    [Server]
    public void ServerRecordLevelRewards(NetworkConnectionToClient conn, int coinsEarned)
    {
        if (!sessionDataByConnection.TryGetValue(conn.connectionId, out PlayerSessionData data))
        {
            data = new PlayerSessionData();
            sessionDataByConnection[conn.connectionId] = data;
        }

        data.coins += coinsEarned;
    }

    [Server]
    public bool TryGetSessionData(NetworkConnectionToClient conn, out PlayerSessionData data)
    {
        return sessionDataByConnection.TryGetValue(conn.connectionId, out data);
    }
}