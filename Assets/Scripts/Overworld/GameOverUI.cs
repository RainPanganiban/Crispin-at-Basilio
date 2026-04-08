using UnityEngine;
using Mirror;
using TMPro;
using System.Collections;

public class GameOverUI : NetworkBehaviour
{
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Transition Settings")]
    [SerializeField] private float fadeDuration = 1.0f; // Bilis ng fade-in
    private CanvasGroup canvasGroup;

    [SyncVar(hook = nameof(OnReadyCountChanged))]
    private int readyPlayers = 0;

    void Start()
    {
        // Siguraduhin na may Canvas Group ang panel para sa fade effect
        canvasGroup = gameOverPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameOverPanel.AddComponent<CanvasGroup>();
        }

        // Simula na invisible
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        gameOverPanel.SetActive(false);
    }

    [ClientRpc]
    public void RpcShowGameOver()
    {
        gameOverPanel.SetActive(true);

        // Simulan ang Fade Transition
        StopAllCoroutines();
        StartCoroutine(FadeInPanel());

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (isServer) readyPlayers = 0;
    }

    IEnumerator FadeInPanel()
    {
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            yield return null;
        }

        // Gawing clickable ang mga buttons pagkatapos ng fade
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public void ReturnToOverworld()
    {
        CmdPlayerReadyToRestart();
    }

    [Command(requiresAuthority = false)]
    void CmdPlayerReadyToRestart()
    {
        readyPlayers++;

        int totalPlayers = NetworkServer.connections.Count;

        if (readyPlayers >= totalPlayers)
        {
            // Reset Currency
            PlayerCurrencyManager[] allCurrencies = Object.FindObjectsByType<PlayerCurrencyManager>(FindObjectsSortMode.None);
            foreach (PlayerCurrencyManager pcm in allCurrencies)
            {
                if (pcm != null) pcm.ResetCoins();
            }

            // Revive Players
            PlayerStatsManager[] allPlayers = Object.FindObjectsByType<PlayerStatsManager>(FindObjectsSortMode.None);
            foreach (var player in allPlayers)
            {
                if (player != null) player.ServerRevive();
            }

            Debug.Log("Lahat ready na. Moving to Overworld...");

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
            statusText.text = $"Waiting for others... ({newVal}/2)";
        }
    }
}