using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using UnityEngine.InputSystem; // Para sa New Input System support

public class PauseMenuManager : NetworkBehaviour
{
    public GameObject pauseMenuPanel;
    private bool isPaused = false;

    void Start()
    {
        // Sinisiguro na tago ang menu sa simula
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
            isPaused = false;
        }
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        // Suportado nito ang luma at bagong Input System para sa Escape key
        bool escPressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) escPressed = true;
#endif

        if (Input.GetKeyDown(KeyCode.Escape)) escPressed = true;

        if (escPressed)
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Resume()
    {
        pauseMenuPanel.SetActive(false);
        isPaused = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Pause()
    {
        pauseMenuPanel.SetActive(true);
        isPaused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void BackToMainMenu()
    {
        // Linisin ang connection bago lumipat
        if (NetworkManager.singleton != null)
        {
            if (isServer && isClient) NetworkManager.singleton.StopHost();
            else if (isClient) NetworkManager.singleton.StopClient();
        }
        SceneManager.LoadScene("Main Menu");
    }
}