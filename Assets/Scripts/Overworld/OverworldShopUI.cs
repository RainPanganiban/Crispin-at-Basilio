using UnityEngine;

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
    }

    public void Close()
    {
        if (root != null)
            root.SetActive(false);
        
        // Lock cursor again for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}

