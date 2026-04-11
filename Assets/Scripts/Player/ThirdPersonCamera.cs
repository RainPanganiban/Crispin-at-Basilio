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

    [Header("Valorant Feel Settings")]
    [Tooltip("Bawasan ito para mas mabilis ang response (0.01 - 0.05). Gawing 0 para sa Pure Raw Input.")]
    public float rotationSmoothTime = 0.03f;
    [Tooltip("Taasan ito para mas sumunod agad ang camera sa galaw ng mouse.")]
    public float inputSmoothSpeed = 25f;

    private float yawVelocity;
    private float pitchVelocity;

    [Header("Aiming System")]
    public float aimSensitivityMultiplier = 0.5f;
    public float zoomDistance = 1.5f;
    public float zoomSpeed = 10f;

    private Vector2 lookInput;
    private Vector2 smoothedLookInput;
    private float yaw;
    private float pitch;
    private float targetYaw;
    private float targetPitch;

    private bool isAiming;
    public float currentSensitivity;
    private float currentDistance;

    NetworkIdentity ownerIdentity;
    private PlayerStatsManager localStats;

    void Start()
    {
        ownerIdentity = GetComponentInParent<NetworkIdentity>();

        if (ownerIdentity == null || !ownerIdentity.isLocalPlayer)
        {
            Camera cam = GetComponent<Camera>();
            if (cam != null) cam.enabled = false;
            AudioListener listener = GetComponent<AudioListener>();
            if (listener != null) listener.enabled = false;
            return;
        }

        // Load saved sensitivity
        sensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 120f);
        currentSensitivity = sensitivity;

        localStats = ownerIdentity.GetComponent<PlayerStatsManager>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentDistance = distance;

        // Initialize angles correctly
        Vector3 angles = transform.eulerAngles;
        targetYaw = angles.y;
        targetPitch = angles.x;
        yaw = targetYaw;
        pitch = targetPitch;
    }

    public void SetAiming(bool aiming)
    {
        isAiming = aiming;
        float baseSens = PlayerPrefs.GetFloat("MouseSensitivity", sensitivity);
        currentSensitivity = aiming ? baseSens * aimSensitivityMultiplier : baseSens;
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (ownerIdentity != null && !ownerIdentity.isLocalPlayer) return;
        // Gamit ang Delta para sa New Input System para sa raw movement
        lookInput = context.ReadValue<Vector2>();
    }

    void LateUpdate()
    {
        if (ownerIdentity == null || !ownerIdentity.isLocalPlayer) return;
        CheckSpectatorTarget();
        if (target == null) return;

        // Real-time Sensitivity update from settings
        if (!isAiming)
        {
            currentSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", sensitivity);
        }

        // VALORANT FEEL: Mas mabilis na Lerp para sa input
        smoothedLookInput = Vector2.Lerp(smoothedLookInput, lookInput, Time.deltaTime * inputSmoothSpeed);

        // Compute rotation without clamping steps too much (para sa flick shots)
        targetYaw += smoothedLookInput.x * currentSensitivity * 0.01f; // Ginamitan ng 0.01f multiplier para mas madaling i-tune ang slider
        targetPitch -= smoothedLookInput.y * currentSensitivity * 0.01f;
        targetPitch = Mathf.Clamp(targetPitch, minY, maxY);

        // VALORANT FEEL: Sobrang liit na SmoothDamp o kaya direct Apply
        yaw = Mathf.SmoothDampAngle(yaw, targetYaw, ref yawVelocity, rotationSmoothTime);
        pitch = Mathf.SmoothDampAngle(pitch, targetPitch, ref pitchVelocity, rotationSmoothTime);

        // Distance smoothing (Zoom)
        float targetDistance = isAiming ? zoomDistance : distance;
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, zoomSpeed * Time.deltaTime);

        // Apply
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.position = target.position + transform.rotation * new Vector3(0, 0, -currentDistance);
    }

    void CheckSpectatorTarget()
    {
        if (localStats != null && !localStats.IsDead)
        {
            target = ownerIdentity.transform;
            return;
        }

        if (localStats != null && localStats.IsDead)
        {
            PlayerStatsManager[] allPlayers = Object.FindObjectsByType<PlayerStatsManager>(FindObjectsSortMode.None);
            foreach (var p in allPlayers)
            {
                if (!p.IsDead)
                {
                    target = p.transform;
                    break;
                }
            }
        }
    }
}