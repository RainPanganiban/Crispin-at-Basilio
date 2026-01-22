using UnityEngine;
using Mirror;

public class EnemySync : NetworkBehaviour
{
    [SyncVar] private Vector3 syncPosition;
    [SyncVar] private Quaternion syncRotation;

    private Transform t;
    private float lerpSpeed = 10f; // tweak for smoothness

    void Awake()
    {
        t = transform;
    }

    void Update()
    {
        if (isServer)
        {
            // Server writes its position/rotation
            syncPosition = t.position;
            syncRotation = t.rotation;
        }
        else
        {
            // Clients smoothly follow the server
            t.position = Vector3.Lerp(t.position, syncPosition, Time.deltaTime * lerpSpeed);
            t.rotation = Quaternion.Slerp(t.rotation, syncRotation, Time.deltaTime * lerpSpeed);
        }
    }
}
