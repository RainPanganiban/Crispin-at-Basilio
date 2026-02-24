using UnityEngine;
using Mirror;

public class OverworldShopUI : MonoBehaviour
{
    [SerializeField] private GameObject root;

    public void Open()
    {
        if (root != null)
            root.SetActive(true);
        
        // Unlock cursor for shop interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Hide the player HUD so it doesn't overlap the shop
        SetPlayerUIVisible(false);
    }

    public void Close()
    {
        if (root != null)
            root.SetActive(false);
        
        // Lock cursor again for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Show the player HUD again
        SetPlayerUIVisible(true);
    }

    void SetPlayerUIVisible(bool visible)
    {
        if (NetworkClient.localPlayer == null) return;

        PlayerUI playerUI = NetworkClient.localPlayer.GetComponent<PlayerUI>();
        if (playerUI != null)
        {
            Canvas canvas = playerUI.GetComponentInChildren<Canvas>();
            if (canvas != null)
                canvas.enabled = visible;
        }
    }
}
