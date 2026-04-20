using UnityEngine;
using Mirror;

public class SkipCutscene : NetworkBehaviour
{
    public string nextSceneName = "Overworld";

    [SyncVar]
    private int skipVotes = 0;
    private bool hasVoted = false;

    void Start()
    {
        // 1. Locally mute lahat ng sound (BGM at SFX) pagpasok sa scene na ito.
        AudioListener.volume = 0f;
        Debug.Log("Audio Muted locally for Cutscene.");
    }

    // 2. ITO ANG PINAKA-IMPORTANTE:
    // Kapag ang scene na ito ay na-unload (skip o tapos na),
    // automatic na ibabalik ang volume sa normal (1.0).
    void OnDestroy()
    {
        AudioListener.volume = 1f;
        Debug.Log("Audio Restored locally as Cutscene scene was unloaded.");
    }

    public void Skip()
    {
        if (hasVoted) return;
        hasVoted = true;
        CmdSubmitSkipVote();
    }

    [Command(requiresAuthority = false)]
    void CmdSubmitSkipVote()
    {
        skipVotes++;
        if (skipVotes >= NetworkServer.connections.Count)
        {
            ProceedToNextScene();
        }
    }

    [Server]
    void ProceedToNextScene()
    {
        // Gagamit ang NetworkManager ng ServerChangeScene para lumipat
        NetworkManager.singleton.ServerChangeScene(nextSceneName);
    }

    void OnGUI()
    {
        if (skipVotes > 0)
        {
            GUI.Label(new Rect(10, 10, 300, 30), $"Waiting for others to skip: {skipVotes}/{NetworkManager.singleton.numPlayers}");
        }
    }
}