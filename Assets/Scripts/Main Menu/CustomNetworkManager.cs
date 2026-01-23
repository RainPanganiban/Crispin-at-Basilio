using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class CustomNetworkManager : NetworkManager
{
    [Header("Prefabs")]
    public GameObject lobbyPlayerPrefab;      // Lobby player prefab
    public GameObject gameplayPlayerPrefab;   // Actual controllable player prefab

    // Called when a client connects to the server
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        // Decide which prefab to spawn based on the current scene
        GameObject prefabToSpawn = null;

        if (SceneManager.GetActiveScene().name == "Lobby")
        {
            prefabToSpawn = lobbyPlayerPrefab;
        }
        else
        {
            prefabToSpawn = gameplayPlayerPrefab;
        }

        // Instantiate and add player
        GameObject player = Instantiate(prefabToSpawn, Vector3.zero, Quaternion.identity);
        NetworkServer.AddPlayerForConnection(conn, player);
    }

    // Called when the server changes scenes
    public override void OnServerSceneChanged(string sceneName)
    {
        if (sceneName == "Mirror Networking") // Replace with your gameplay scene name
        {
            foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
            {
                if (conn.identity != null)
                {
                    // Store position of lobby player
                    GameObject lobbyPlayer = conn.identity.gameObject;
                    Vector3 spawnPos = lobbyPlayer.transform.position;

                    // Destroy the lobby player
                    NetworkServer.Destroy(lobbyPlayer);

                    // Spawn the gameplay player prefab
                    GameObject gameplayPlayer = Instantiate(gameplayPlayerPrefab, spawnPos, Quaternion.identity);
                    NetworkServer.AddPlayerForConnection(conn, gameplayPlayer);
                }
            }
        }
    }
}