using UnityEngine;

public class CreditsController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject creditsPanel;

    private void Start()
    {
        // Siguraduhin na tago ang credits sa simula
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    public void OpenCredits()
    {
        if (creditsPanel != null)
        {
            creditsPanel.SetActive(true);
            // HUWAG mong i-disable ang Main Menu object para hindi mawala yung character sa background
        }
    }

    public void CloseCredits()
    {
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }
}