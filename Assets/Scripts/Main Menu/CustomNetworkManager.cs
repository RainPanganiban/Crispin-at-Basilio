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
        Debug.Log("[NM] Awake");

        Debug.Log("[NM] Registered spawnable prefabs:");
        foreach (var p in spawnPrefabs)
        {
            Debug.Log(" - " + p.name);
        }
    }

    public override void OnStartServer()
    {
        Debug.Log("[NM] OnStartServer");
    }

    public override void OnStartClient()
    {
        Debug.Log("[NM] OnStartClient");
    }
    
    // Called when client requests a player (Lobby only)
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Debug.Log($"[NM] OnServerAddPlayer | Scene={SceneManager.GetActiveScene().name}");
        if (SceneManager.GetActiveScene().name != "Lobby")
        return;

        if (lobbyPlayerPrefab == null)
        {
            Debug.LogError("[NM] lobbyPlayerPrefab is NULL in inspector!");
            return;
        }

        GameObject lobbyPlayer = Instantiate(lobbyPlayerPrefab);
        Debug.Log("[NM] Instantiated LobbyPlayer prefab");
        NetworkServer.AddPlayerForConnection(conn, lobbyPlayer);
        Debug.Log("[NM] Added LobbyPlayer to connection");

        NetworkIdentity ni = lobbyPlayer.GetComponent<NetworkIdentity>();
        Debug.Log($"[NM] NetworkIdentity present = {ni != null}");
    }

    public override void OnServerChangeScene(string newSceneName)
    {
        if (newSceneName == "Mirror Networking")
        {
            Debug.Log("[NM] Caching lobby player classes");

            playerClasses.Clear();

            foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
            {
                if (conn.identity == null)
                {
                    Debug.Log("[NM] conn.identity NULL while caching");
                    continue;
                }

                LobbyPlayer lobbyPlayer = conn.identity.GetComponent<LobbyPlayer>();
                if (lobbyPlayer == null)
                {
                    Debug.Log("[NM] LobbyPlayer component missing");
                    continue;
                }

                Debug.Log($"[NM] Cached class {lobbyPlayer.playerClass}");
                playerClasses[conn] = lobbyPlayer.playerClass;
            }
        }

        base.OnServerChangeScene(newSceneName);
    }

    // Called AFTER scene has fully loaded
    public override void OnServerSceneChanged(string sceneName)
    {
        if (sceneName != "Mirror Networking")
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
                Debug.LogError("Missing gameplay prefab for class: " + playerClass);
                continue;
            }

            GameObject gameplayPlayer = Instantiate(prefabToSpawn);
            NetworkServer.ReplacePlayerForConnection(conn, gameplayPlayer, true);
        }

        playerClasses.Clear();
    }
}