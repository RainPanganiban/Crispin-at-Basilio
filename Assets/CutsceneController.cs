using UnityEngine;
using Mirror;
using UnityEngine.Video;

public class CutsceneController : NetworkBehaviour
{
    public VideoPlayer videoPlayer;
    public string nextScene = "Overworld";

    void Start()
    {
        // 1. Patayin ang music locally para sa lahat ng papasok sa scene
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopMusic();
        }

        if (videoPlayer != null)
        {
            // 2. CRITICAL FIX: I-assign ang Main Camera locally. 
            // Ito ang dahilan kung bakit "Blue Screen" ang Client dahil hindi niya alam kung saan i-re-render ang video.
            if (videoPlayer.targetCamera == null)
            {
                videoPlayer.targetCamera = Camera.main;
            }

            // 3. I-play ang video locally
            videoPlayer.Play();

            // 4. SERVER-ONLY: Ang server lang ang mag-aabang kung tapos na ang video para mag-change scene
            if (isServer)
            {
                videoPlayer.loopPointReached += EndReached;
            }
        }
        else
        {
            Debug.LogError("CutsceneController: Walang VideoPlayer na naka-assign sa Inspector!");
        }
    }

    // I-assign mo itong 'SkipCutscene' function sa OnClick event ng iyong Skip Button sa Inspector
    public void SkipCutscene()
    {
        if (isServer)
        {
            // Kung Host/Server ang nag-click, diretso agad sa EndReached
            EndReached(videoPlayer);
        }
        else
        {
            // Kung Client ang nag-click, magpapadala ng "Command" sa Server para laktawan ang scene
            CmdRequestSkip();
        }
    }

    [Command(requiresAuthority = false)]
    void CmdRequestSkip()
    {
        // Tatakbo ito sa Server side kahit Client ang nag-click ng button
        EndReached(videoPlayer);
    }

    void EndReached(VideoPlayer vp)
    {
        // Siguraduhin na Server lang ang magpapalit ng scene para sa lahat (Networked Scene Change)
        if (isServer)
        {
            Debug.Log("Cutscene tapos na o na-skip. Nililipat na ang lahat sa: " + nextScene);

            // Tanggalin ang event subscription para iwas double trigger
            if (videoPlayer != null) videoPlayer.loopPointReached -= EndReached;

            NetworkManager.singleton.ServerChangeScene(nextScene);
        }
    }
}