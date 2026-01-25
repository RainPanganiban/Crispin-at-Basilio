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
    private readonly Dictionary<NetworkConnectionToClient, string> playerClasses
        = new Dictionary<NetworkConnectionToClient, string>();
    
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
        if (newSceneName == "Level 1")
        {

            playerClasses.Clear();

            foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
            {
                if (conn.identity == null)
                {
                    continue;
                }

                LobbyPlayer lobbyPlayer = conn.identity.GetComponent<LobbyPlayer>();
                if (lobbyPlayer == null)
                {
                    continue;
                }

                playerClasses[conn] = lobbyPlayer.playerClass;
            }
        }

        base.OnServerChangeScene(newSceneName);
    }

    // Called AFTER scene has fully loaded
    public override void OnServerSceneChanged(string sceneName)
    {
        if (sceneName != "Level 1")
            return;

        foreach (var entry in playerClasses)
        {
            NetworkConnectionToClient conn = entry.Key;
            string playerClass = entry.Value;

            GameObject prefabToSpawn = null;

            if (playerClass == "Crispin")
                prefabToSpawn = crispinGameplayPrefab;
            else if (playerClass == "Basilio")
                prefabToSpawn = basilioGameplayPrefab;

            if (prefabToSpawn == null)
            {
                continue;
            }

            GameObject gameplayPlayer = Instantiate(prefabToSpawn);
            NetworkServer.ReplacePlayerForConnection(conn, gameplayPlayer, true);
        }

        playerClasses.Clear();
    }
}