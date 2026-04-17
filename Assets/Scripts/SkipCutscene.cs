using UnityEngine;
using Mirror; // Kailangan ito para sa NetworkManager

public class SkipCutscene : MonoBehaviour
{
    [Tooltip("Pangalan ng scene (hal. Overworld)")]
    public string nextSceneName = "Overworld";

    public void Skip()
    {
        // 1. I-check kung ang nag-click ay ang Server/Host
        // Sa Mirror, ang Server lang ang may kapangyarihan magpalit ng scene para sa lahat
        if (NetworkServer.active)
        {
            Debug.Log("Server is changing scene to: " + nextSceneName);
            NetworkManager.singleton.ServerChangeScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("Only the Host/Server can skip the cutscene for everyone.");
        }
    }
}