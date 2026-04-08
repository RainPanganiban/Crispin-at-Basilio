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



        // Local client update para sa UI

        if (isServer) readyPlayers = 0;

    }



    /// <summary>

    /// Ikabit ito sa "BACK TO MENU" o "TRY AGAIN" button sa Inspector

    /// </summary>

    public void ReturnToOverworld()

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

            // --- 1. RESET CURRENCY PARA SA LAHAT ---

            // Hinahanap ang lahat ng PlayerCurrencyManager at ginagawang 0 ang coins

            PlayerCurrencyManager[] allCurrencies = Object.FindObjectsByType<PlayerCurrencyManager>(FindObjectsSortMode.None);

            foreach (PlayerCurrencyManager pcm in allCurrencies)

            {

                if (pcm != null) pcm.ResetCoins();

            }



            // --- 2. REVIVE PLAYERS ---

            PlayerStatsManager[] allPlayers = Object.FindObjectsByType<PlayerStatsManager>(FindObjectsSortMode.None);

            foreach (var player in allPlayers)

            {

                if (player != null) player.ServerRevive();

            }



            Debug.Log("Lahat ready na. Coins Reset at Players Revived. Moving to Overworld...");



            // --- 3. CHANGE SCENE ---

            // Siguraduhin na "Overworld" ang tamang pangalan ng scene sa Build Settings

            if (OverworldManager.Instance != null)

            {

                NetworkManager.singleton.ServerChangeScene(OverworldManager.Instance.overworldSceneName);

            }

            else

            {

                NetworkManager.singleton.ServerChangeScene("Overworld");

            }

        }

    }



    void OnReadyCountChanged(int oldVal, int newVal)

    {

        if (statusText != null)

        {

            // Note: Ang NetworkServer.connections.Count ay server-side lang. 

            // Sa client, pwedeng i-approximate o i-hardcode kung ilan kayo (hal. 2)

            statusText.text = $"Waiting for others... ({newVal}/2)";

        }

    }

}