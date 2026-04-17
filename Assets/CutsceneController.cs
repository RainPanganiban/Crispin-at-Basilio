using UnityEngine;
using Mirror;
using UnityEngine.Video;

public class CutsceneController : NetworkBehaviour
{
    public VideoPlayer videoPlayer;
    public string nextScene = "Overworld";

    void Start()
    {
        // 1. Utusan ang SoundManager na itigil ang music gamit ang Crossfade
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopMusic();
            Debug.Log("SoundManager: Crossfading music to silence for cutscene.");
        }

        if (isServer && videoPlayer != null)
        {
            videoPlayer.loopPointReached += EndReached;
        }
    }

    void EndReached(VideoPlayer vp)
    {
        if (isServer)
        {
            NetworkManager.singleton.ServerChangeScene(nextScene);
        }
    }
}