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
            NetworkClient.Ready();
            return;
        }

        if (NetworkClient.localPlayer == null)
        {
            NetworkClient.AddPlayer();
            enabled = false;
        }
    }
}
