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

    [Header("Feel Settings (Snappy + Smooth)")]
    [Tooltip("0 = Pure Raw (Roblox). 0.02 to 0.05 = Smooth pero Snappy. 0.1 = Cinematic/Mabigat.")]
    public float rotationSmoothTime = 0.03f; // Ito ang "shock absorber" mo

    [Header("Aiming System")]
    public float aimSensitivityMultiplier = 0.5f;
    public float zoomDistance = 1.5f;
    public float zoomSpeed = 10f;

    private Vector2 lookInput;
    private float yaw;
    private float pitch;
    private float targetYaw;   // Dinagdag ulit natin 'to para may target ang SmoothDamp
    private float targetPitch;

    private float yawVelocity;
    private float pitchVelocity;

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

        sensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 120f);
        currentSensitivity = sensitivity;

        localStats = ownerIdentity.GetComponent<PlayerStatsManager>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentDistance = distance;

        // Initialize angles
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
        lookInput = context.ReadValue<Vector2>();
    }

    void LateUpdate()
    {
        if (ownerIdentity == null || !ownerIdentity.isLocalPlayer) return;
        CheckSpectatorTarget();
        if (target == null) return;

        if (!isAiming)
        {
            currentSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", sensitivity);
        }

        // 1. I-apply ang mouse movement sa TARGET variables
        targetYaw += lookInput.x * currentSensitivity * 0.02f;
        targetPitch -= lookInput.y * currentSensitivity * 0.02f;
        targetPitch = Mathf.Clamp(targetPitch, minY, maxY);

        lookInput = Vector2.zero; // Reset input

        // 2. I-SmoothDamp papunta sa target (Ito nagbibigay ng "butter" feel)
        yaw = Mathf.SmoothDampAngle(yaw, targetYaw, ref yawVelocity, rotationSmoothTime);
        pitch = Mathf.SmoothDampAngle(pitch, targetPitch, ref pitchVelocity, rotationSmoothTime);

        // Zoom logic
        float targetDistance = isAiming ? zoomDistance : distance;
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, zoomSpeed * Time.deltaTime);

        // 3. Apply rotation at position
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