using UnityEngine;

public class SwarmTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Tinitignan kung ang pumasok ay may Tag na "Player"
        if (other.CompareTag("Player"))
        {
            if (SoundManager.Instance != null)
            {
                // Tatawagin ang function sa SoundManager mo
                SoundManager.Instance.PlaySwarmMusic();
                Debug.Log("Swarm Music Triggered!");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Kapag lumabas ang player sa zone, babalik sa normal music
        if (other.CompareTag("Player"))
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayExplorationMusic();
                Debug.Log("Exited Swarm Zone: Back to Exploration Music.");
            }
        }
    }
}