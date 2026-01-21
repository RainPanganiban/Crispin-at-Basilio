using UnityEngine;
using Mirror;

public class PlayerAim : NetworkBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float aimDistance = 100f;

    public Ray GetAimRay()
    {
        return playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        Ray ray = GetAimRay();
        Debug.DrawRay(ray.origin, ray.direction * aimDistance, Color.red);
    }
}
