using UnityEngine;
using Mirror;

public class PlayerIdentity : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnNameChanged))]
    public string playerName;

    [SerializeField] private NameTag nameTag;

    public override void OnStartClient()
    {
        base.OnStartClient();

        nameTag.SetName(playerName);

        // Hide own name
        if (isLocalPlayer)
            nameTag.gameObject.SetActive(false);
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        // This is guaranteed to have the camera
        Camera cam = GetComponentInChildren<Camera>();

        // Tell ALL NameTags which camera to face
        NameTag.SetLocalCamera(cam);
    }

    void OnNameChanged(string oldName, string newName)
    {
        nameTag.SetName(newName);
    }
}
