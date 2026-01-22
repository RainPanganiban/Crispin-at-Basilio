using UnityEngine;
using Mirror;
using TMPro;

public class LobbyPlayer : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnNameChanged))]
    public string playerName;


    [SyncVar(hook = nameof(OnClassChanged))]
    public string playerClass; // "Crispin" or "Basilio"

    [SyncVar(hook = nameof(OnReadyChanged))]
    public bool isReady;

    [Header("UI References")]
    public TMP_Text nameText;
    public TMP_Text classText;
    public TMP_Text readyText;

    public override void OnStartClient()
    {
        base.OnStartClient();

        LobbyUIManager ui = FindFirstObjectByType<LobbyUIManager>();
        if (ui != null)
            ui.AssignPlayerSlots();
    }

    //para sa buttons
    void OnNameChanged(string oldName, string newName)
    {
        if (nameText != null)
            nameText.text = newName;
    }

    void OnClassChanged(string oldClass, string newClass)
    {
        if (classText != null)
            classText.text = newClass;
    }

    void OnReadyChanged(bool oldReady, bool newReady)
    {
        if (readyText != null)
            readyText.text = newReady ? "READY" : "NOT READY";
    }

    // commands galing sa mirror
    [Command]
    public void CmdSetName(string newName)
    {
        playerName = newName;
    }

    [Command]
    public void CmdSelectClass(string classChoice)
    {
        foreach (LobbyPlayer p in FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None))
        {
            if (p != this && p.playerClass == classChoice)
                return;
        }

        playerClass = classChoice;
        isReady = false; // force re-ready
    }

    [Command]
    public void CmdToggleReady()
    {
        if (string.IsNullOrEmpty(playerClass))
            return; // cannot ready without class

        isReady = !isReady;
    }

    public void AssignCard(TMP_Text nameT, TMP_Text classT, TMP_Text readyT)
    {
        nameText = nameT;
        classText = classT;
        readyText = readyT;

        RefreshUI();
    }

    public void RefreshUI()
    {
        if(nameText) nameText.text = playerName;
        if(classText) classText.text = playerClass;
        if(readyText) readyText.text = isReady ? "READY" : "NOT READY";
    }
}
