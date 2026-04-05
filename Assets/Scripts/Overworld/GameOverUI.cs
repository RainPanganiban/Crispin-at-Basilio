using UnityEngine;
using Mirror;
using TMPro;

public class GameOverUI : NetworkBehaviour
{
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI statusText; // Opsyonal: Para ipakita "1/2 Ready"

    [SyncVar(hook = nameof(OnReadyCountChanged))]
    private int readyPlayers = 0;

    [ClientRpc]
    public void RpcShowGameOver()
    {
        gameOverPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        readyPlayers = 0; // Reset count
    }

    public void ReturnToOverworld() // Ikabit ito sa Button
    {
        CmdPlayerReadyToRestart();
    }

    [Command(requiresAuthority = false)]
    void CmdPlayerReadyToRestart()
    {
        readyPlayers++;

        // I-check kung lahat ng players ay ready na
        int totalPlayers = NetworkServer.connections.Count;
        if (readyPlayers >= totalPlayers)
        {
            NetworkManager.singleton.ServerChangeScene("Overworld");
        }
    }

    void OnReadyCountChanged(int oldVal, int newVal)
    {
        if (statusText != null)
            statusText.text = $"Waiting for others... ({newVal}/{NetworkServer.connections.Count})";
    }
}