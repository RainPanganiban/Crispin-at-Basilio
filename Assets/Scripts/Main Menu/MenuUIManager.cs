using UnityEngine;
using Mirror;
using System.Net;
using System.Net.Sockets;
using TMPro;

public class MenuUIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject multiplayerPanel;

    [Header("Multiplayer UI")]
    public TMP_InputField ipInputField;
    public TMP_Text localIpText;

    private Mirror.NetworkManager networkManager;
    
    void Start()
    {
        networkManager = NetworkManager.singleton;

        ShowMainMenu();
        DisplayLocalIP();
    }

    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        multiplayerPanel.SetActive(false);
    }

    public void ShowMultiplayerMenu()
    {
        mainMenuPanel.SetActive(false);
        multiplayerPanel.SetActive(true);
    }

    public void HostGame()
    {
        networkManager.StartHost();
    }

    public void JoinGame()
    {
        if (string.IsNullOrEmpty(ipInputField.text))
        {
            Debug.LogWarning("IP Address is empty!");
            return;
        }

        networkManager.networkAddress = ipInputField.text;
        networkManager.StartClient();
    }

    void DisplayLocalIP()
    {
        string localIP = "Unavailable";

        foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                localIP = ip.ToString();
                break;
            }
        }

        localIpText.text = $"Your IP: {localIP}";
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
