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

    [Header("Aiming System")]
    public float aimSensitivity = 60f;
    public float zoomDistance = 1.5f; // How close to the player the camera gets when aiming
    public float zoomSpeed = 8f;

    private Vector2 lookInput;
    private float yaw;
    private float pitch;

    private bool isAiming;
    private float currentSensitivity;
    private float currentDistance;

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

        currentSensitivity = sensitivity;
        currentDistance = distance;
    }

    public void SetAiming(bool aiming)
    {
        isAiming = aiming;
        currentSensitivity = aiming ? aimSensitivity : sensitivity;
        Debug.Log($"[ThirdPersonCamera] SetAiming({aiming}) called. Target Distance: {(aiming ? zoomDistance : distance)}.");
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    void LateUpdate()
    {
        if (ownerIdentity == null || !ownerIdentity.isLocalPlayer)
            return;

        yaw += lookInput.x * currentSensitivity * Time.deltaTime;
        pitch -= lookInput.y * currentSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minY, maxY);

        // Calculate physical distance zoom instead of FOV
        float targetDistance = isAiming ? zoomDistance : distance;
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, zoomSpeed * Time.deltaTime);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.position =
            target.position + transform.rotation * new Vector3(0, 0, -currentDistance);
    }
}
