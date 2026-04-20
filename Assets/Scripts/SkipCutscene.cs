using UnityEngine;
using Mirror;

public class SkipCutscene : NetworkBehaviour
{
    public string nextSceneName = "Overworld";

    [SyncVar]
    private int skipVotes = 0;
    private bool hasVoted = false;

    // Tinatawag ito sa lahat (Host at Client) kapag handa na ang network object
    public override void OnStartClient()
    {
        base.OnStartClient();
        MuteSystem();
    }

    // Para sigurado, sa Start din para sa local logic
    void Start()
    {
        MuteSystem();
    }

    void MuteSystem()
    {
        // Gagamit tayo ng pause para mas "forceful" kaysa sa volume
        AudioListener.pause = true;
        Debug.Log("Audio Paused for " + (isServer ? "Host" : "Client"));
    }

    // Kapag lumipat na ng scene at na-destroy itong script, ibalik ang tunog
    void OnDestroy()
    {
        AudioListener.pause = false;
        Debug.Log("Audio Restored.");
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
        // Bilangin ang players base sa actual connections sa server
        if (skipVotes >= NetworkServer.connections.Count)
        {
            ProceedToNextScene();
        }
    }

    [Server]
    void ProceedToNextScene()
    {
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