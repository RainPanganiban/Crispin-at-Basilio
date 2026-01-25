using UnityEngine;
using Mirror;

public class NetworkDebugClient : MonoBehaviour
{
    void Start()
    {
        Debug.Log($"[CLIENT] isConnected={NetworkClient.isConnected}");
        Debug.Log($"[CLIENT] localPlayer={NetworkClient.localPlayer}");
    }

    void Update()
    {
        if (NetworkClient.isConnected && NetworkClient.localPlayer == null)
        {
            Debug.Log("[CLIENT] Calling AddPlayer()");
            NetworkClient.AddPlayer();
            enabled = false;
        }
    }
}
