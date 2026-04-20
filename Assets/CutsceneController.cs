using UnityEngine;
using Mirror;
using UnityEngine.Video;

public class CutsceneController : NetworkBehaviour
{
    public VideoPlayer videoPlayer;
    public GameObject videoCanvas; // I-drag ang Canvas dito

    void Start()
    {
        // Force Enable: Minsan pinapatay ng Mirror ang Canvas pagka-join
        if (videoCanvas != null) videoCanvas.SetActive(true);
        if (videoPlayer != null) videoPlayer.gameObject.SetActive(true);

        SetupLocalVideo();
    }

    void SetupLocalVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.targetCamera = Camera.main;
            videoPlayer.Play();

            if (isServer)
            {
                videoPlayer.loopPointReached += (vp) => {
                    NetworkManager.singleton.ServerChangeScene("Overworld");
                };
            }
        }
    }

    // Force check sa Update kung biglang namatay ang UI (Common Mirror Bug)
    void Update()
    {
        if (videoCanvas != null && !videoCanvas.activeSelf)
        {
            videoCanvas.SetActive(true);
        }
    }

    public void SkipCutscene()
    {
        if (isServer) NetworkManager.singleton.ServerChangeScene("Overworld");
        else CmdRequestSkip();
    }

    [Command(requiresAuthority = false)]
    void CmdRequestSkip() { NetworkManager.singleton.ServerChangeScene("Overworld"); }
}