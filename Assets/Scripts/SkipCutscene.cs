using UnityEngine;
using Mirror;

public class SkipCutscene : NetworkBehaviour
{
    [Tooltip("Pangalan ng scene (hal. Overworld)")]
    public string nextSceneName = "Overworld";

    // Naka-sync ito sa lahat ng players. 
    // Kapag nagbago ito sa server, mag-uupdate din sa clients.
    [SyncVar]
    private int skipVotes = 0;

    // Listahan para masiguradong isang beses lang makaka-vote ang bawat player
    private bool hasVoted = false;

    public void Skip()
    {
        if (hasVoted) return; // Bawal na ulit mag-click kung nakaboto na

        hasVoted = true;
        CmdSubmitSkipVote();
        Debug.Log("Vote submitted. Waiting for others...");
    }

    [Command(requiresAuthority = false)]
    void CmdSubmitSkipVote()
    {
        skipVotes++;
        Debug.Log($"Votes: {skipVotes} / {NetworkServer.connections.Count}");

        // I-check kung ang bilang ng boto ay kapantay o sobra sa bilang ng players
        if (skipVotes >= NetworkServer.connections.Count)
        {
            ProceedToNextScene();
        }
    }

    [Server]
    void ProceedToNextScene()
    {
        Debug.Log("All players voted. Changing scene...");
        NetworkManager.singleton.ServerChangeScene(nextSceneName);
    }

    // Optional: I-display sa UI kung ilan na ang nag-vote
    void OnGUI()
    {
        if (skipVotes > 0)
        {
            GUI.Label(new Rect(10, 10, 300, 30), $"Waiting for others to skip: {skipVotes}/{NetworkManager.singleton.numPlayers}");
        }
    }
}