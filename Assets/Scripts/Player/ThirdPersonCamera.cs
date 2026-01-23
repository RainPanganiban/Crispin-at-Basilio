using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    public float sensitivity = 120f;
    public float minY = -30f;
    public float maxY = 60f;
    public float distance = 3f;

    private Vector2 lookInput;
    private float yaw;
    private float pitch;

    NetworkIdentity ownerIdentity;

    void Start()
    {
        ownerIdentity = GetComponentInParent<NetworkIdentity>();

        if (ownerIdentity == null || !ownerIdentity.isLocalPlayer)
        {
            Camera cam = GetComponent<Camera>();
            if (cam != null)
                cam.enabled = false;

            AudioListener listener = GetComponent<AudioListener>();
            if (listener != null)
                listener.enabled = false;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    void LateUpdate()
    {
        if (ownerIdentity == null || !ownerIdentity.isLocalPlayer)
            return;

        yaw += lookInput.x * sensitivity * Time.deltaTime;
        pitch -= lookInput.y * sensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minY, maxY);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.position =
            target.position + transform.rotation * new Vector3(0, 0, -distance);
    }
}
