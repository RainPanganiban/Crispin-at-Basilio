using UnityEngine;

public class BossMusicTrigger : MonoBehaviour
{
    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        // Siguraduhin na 'Player' ang pumasok
        if (other.CompareTag("Player") && !hasTriggered)
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayBossMusic();
                Debug.Log("Boss Music Triggered!");
                hasTriggered = true; // Para hindi paulit-ulit mag-trigger
            }
        }
    }
}