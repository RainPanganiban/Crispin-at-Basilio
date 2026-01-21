using UnityEngine;
using Mirror;

public class PlayerUI : NetworkBehaviour
{
    [SerializeField] private GameObject crosshair;

    public override void OnStartLocalPlayer()
    {
        crosshair.SetActive(true);
    }

    public override void OnStopLocalPlayer()
    {
        crosshair.SetActive(false);
    }
}