using UnityEngine;
using Mirror;

public class LobbyAutoAddPlayers : MonoBehaviour
{
    void Update()
    {
        if (!NetworkClient.isConnected)
            return;

        if (!NetworkClient.ready)
        {
            Debug.Log("[CLIENT] Calling NetworkClient.Ready()");
            NetworkClient.Ready();
            return;
        }

        if (NetworkClient.localPlayer == null)
        {
            Debug.Log("[CLIENT] Calling NetworkClient.AddPlayer()");
            NetworkClient.AddPlayer();
            enabled = false;
        }
    }
}
