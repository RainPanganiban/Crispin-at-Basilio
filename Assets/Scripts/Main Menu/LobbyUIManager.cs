using Mirror;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement; // Importante para sa paglipat ng Scene

public class LobbyUIManager : MonoBehaviour
{
    public LobbyPlayer localPlayer;

    [Header("UI Elements")]
    public TMP_InputField nameInput;
    public Button crispinButton;
    public Button basilioButton;
    public Button readyButton;
    public Button startGameButton;
    public Button backButton; // I-drag ang iyong bagong Back Button dito sa Inspector

    [Header("Player Slots")]
    public TMP_Text leftNameText;
    public TMP_Text leftClassText;
    public TMP_Text leftReadyText;

    public TMP_Text rightNameText;
    public TMP_Text rightClassText;
    public TMP_Text rightReadyText;

    [Header("Prefabs")]
    public GameObject gameplayPlayerPrefab;
    public GameObject lobbyPlayerPrefab;

    void Start()
    {
        StartCoroutine(WaitForLocalPlayer());

        // Disable buttons initially
        crispinButton.interactable = false;
        basilioButton.interactable = false;
        readyButton.interactable = false;

        // Setup Back Button click listener
        if (backButton != null)
        {
            backButton.onClick.AddListener(BackToMainMenu);
        }
    }

    // Hanapin ang function na ito sa loob ng LobbyUIManager.cs
    public void StartGame()
    {
        if (!NetworkServer.active) return; // Host lang ang pwedeng mag-trigger

        // Mula "Overworld", palitan natin papuntang "IntroCutscene"
        NetworkManager.singleton.ServerChangeScene("IntroCutscene");
    }
    // FUNCTION PARA SA BACK BUTTON
    public void BackToMainMenu()
    {
        if (NetworkManager.singleton != null)
        {
            // Tinitigil ang connection base kung host o client ang player
            if (NetworkServer.active && NetworkClient.isConnected)
            {
                NetworkManager.singleton.StopHost();
            }
            else
            {
                NetworkManager.singleton.StopClient();
            }
        }

        // Siguraduhin na "Main Menu" ang eksaktong pangalan ng scene mo sa Build Settings
        SceneManager.LoadScene("Main Menu");
    }

    IEnumerator WaitForLocalPlayer()
    {
        LobbyPlayer lobbyPlayer = null;

        while (NetworkClient.localPlayer == null ||
            (lobbyPlayer = NetworkClient.localPlayer.GetComponent<LobbyPlayer>()) == null ||
            !lobbyPlayer.isOwned)
        {
            yield return null;
        }

        localPlayer = lobbyPlayer;

        if (NetworkServer.active)
        {
            startGameButton.gameObject.SetActive(true);
            startGameButton.onClick.AddListener(StartGame);
        }
        else
        {
            startGameButton.gameObject.SetActive(false);
        }

        AssignPlayerSlots();

        crispinButton.interactable = true;
        basilioButton.interactable = true;
        readyButton.interactable = true;

        crispinButton.onClick.AddListener(() => localPlayer.CmdSelectClass("Crispin"));
        basilioButton.onClick.AddListener(() => localPlayer.CmdSelectClass("Basilio"));
        readyButton.onClick.AddListener(() => localPlayer.CmdToggleReady());
        nameInput.onValueChanged.AddListener((value) => localPlayer.CmdSetName(value));
    }

    public void AssignPlayerSlots()
    {
        LobbyPlayer[] players =
            FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None);

        // Sort by netId so order is consistent
        System.Array.Sort(players, (a, b) => a.netId.CompareTo(b.netId));

        if (players.Length > 0)
            players[0].AssignCard(leftNameText, leftClassText, leftReadyText);

        if (players.Length > 1)
            players[1].AssignCard(rightNameText, rightClassText, rightReadyText);
    }

    public void UpdateStartButton()
    {
        if (!NetworkServer.active)
            return;

        LobbyPlayer[] players =
            FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None);

        if (players.Length < 2)
        {
            startGameButton.interactable = false;
            return;
        }

        foreach (LobbyPlayer p in players)
        {
            if (!p.isReady || string.IsNullOrEmpty(p.playerClass))
            {
                startGameButton.interactable = false;
                return;
            }
        }

        startGameButton.interactable = true;
    }

    void OnDestroy()
    {
        // This ensures when scene changes, the UI is cleaned up
        if (localPlayer != null)
        {
            localPlayer.AssignCard(null, null, null);
        }
    }
}