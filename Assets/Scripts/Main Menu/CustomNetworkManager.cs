using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class CustomNetworkManager : NetworkManager
{
    [Header("Prefabs")]
    public GameObject lobbyPlayerPrefab;      // Lobby player prefab
    
    [Header("Gameplay Prefabs")]
    public GameObject crispinGameplayPrefab;
    public GameObject basilioGameplayPrefab;

    // Cache lobby players before scene change
    private readonly Dictionary<NetworkConnectionToClient, (string playerClass, string playerName)>
    playerData = new Dictionary<NetworkConnectionToClient, (string, string)>();
    
    public override void Awake()
    {
        base.Awake();
        autoCreatePlayer = false;

        foreach (var p in spawnPrefabs)
        {
            Debug.Log(" - " + p.name);
        }
    }
    
    // Called when client requests a player (Lobby only)
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        if (SceneManager.GetActiveScene().name != "Lobby")
        return;

        if (lobbyPlayerPrefab == null)
        {
            return;
        }

        GameObject lobbyPlayer = Instantiate(lobbyPlayerPrefab);
        NetworkServer.AddPlayerForConnection(conn, lobbyPlayer);


        NetworkIdentity ni = lobbyPlayer.GetComponent<NetworkIdentity>();
    }

    public override void OnServerChangeScene(string newSceneName)
    {
        if (newSceneName == "Mirror Networking")
        {
            playerData.Clear();

            foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
            {
                if (conn.identity == null) continue;

                LobbyPlayer lobbyPlayer = conn.identity.GetComponent<LobbyPlayer>();
                if (lobbyPlayer == null) continue;

                playerData[conn] = (
                    lobbyPlayer.playerClass,
                    lobbyPlayer.playerName
                );
            }
        }

        base.OnServerChangeScene(newSceneName);
    }

    // Called AFTER scene has fully loaded
    public override void OnServerSceneChanged(string sceneName)
    {
        if (sceneName != "Mirror Networking")
            return;

        foreach (var entry in playerData)
        {
            NetworkConnectionToClient conn = entry.Key;
            string playerClass = entry.Value.playerClass;
            string playerName = entry.Value.playerName;

            GameObject prefabToSpawn = null;

            if (playerClass == "Crispin")
                prefabToSpawn = crispinGameplayPrefab;
            else if (playerClass == "Basilio")
                prefabToSpawn = basilioGameplayPrefab;

            if (prefabToSpawn == null) continue;

            GameObject gameplayPlayer = Instantiate(prefabToSpawn);

            PlayerIdentity identity = gameplayPlayer.GetComponent<PlayerIdentity>();
            identity.playerName = playerName;

            NetworkServer.ReplacePlayerForConnection(conn, gameplayPlayer, true);
        }

        playerData.Clear();
    }
}