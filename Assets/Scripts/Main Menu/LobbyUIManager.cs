using Mirror;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class LobbyUIManager : MonoBehaviour
{
    public LobbyPlayer localPlayer;

    public TMP_InputField nameInput;
    public Button crispinButton;
    public Button basilioButton;
    public Button readyButton;
    public Button startGameButton;

    public TMP_Text leftNameText;
    public TMP_Text leftClassText;
    public TMP_Text leftReadyText;

    public TMP_Text rightNameText;
    public TMP_Text rightClassText;
    public TMP_Text rightReadyText;

    public GameObject gameplayPlayerPrefab;
    public GameObject lobbyPlayerPrefab;

    void Start()
    {
        StartCoroutine(WaitForLocalPlayer());

        // Disable buttons initially
        crispinButton.interactable = false;
        basilioButton.interactable = false;
        readyButton.interactable = false;
    }

    public void StartGame()
    {
        if (!NetworkServer.active) return; // only host can start
        NetworkManager.singleton.ServerChangeScene("Level 1");
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
