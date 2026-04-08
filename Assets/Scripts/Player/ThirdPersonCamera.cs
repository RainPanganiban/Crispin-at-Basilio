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

    [Header("Smoothness Settings")]
    [Tooltip("Mas mataas, mas smooth ang camera pero mas 'mabigat' ang feeling.")]
    public float rotationSmoothTime = 0.12f;
    [Tooltip("Pang-filter sa mabilis na mouse flick.")]
    public float inputSmoothSpeed = 15f;

    private float yawVelocity;
    private float pitchVelocity;

    [Header("Aiming System")]
    public float aimSensitivity = 60f;
    public float zoomDistance = 1.5f;
    public float zoomSpeed = 8f;

    private Vector2 lookInput;
    private Vector2 smoothedLookInput;
    private float yaw;
    private float pitch;
    private float targetYaw;
    private float targetPitch;

    private bool isAiming;
    private float currentSensitivity;
    private float currentDistance;

    NetworkIdentity ownerIdentity;
    private PlayerStatsManager localStats;

    void Start()
    {
        ownerIdentity = GetComponentInParent<NetworkIdentity>();

        // Setup para sa Local Player lang ang camera
        if (ownerIdentity == null || !ownerIdentity.isLocalPlayer)
        {
            Camera cam = GetComponent<Camera>();
            if (cam != null) cam.enabled = false;
            AudioListener listener = GetComponent<AudioListener>();
            if (listener != null) listener.enabled = false;
            return;
        }

        localStats = ownerIdentity.GetComponent<PlayerStatsManager>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentSensitivity = sensitivity;
        currentDistance = distance;

        // Kunin ang initial rotation para hindi mag-snap ang camera sa simula
        targetYaw = transform.eulerAngles.y;
        targetPitch = transform.eulerAngles.x;
        yaw = targetYaw;
        pitch = targetPitch;
    }

    // PUBLIC ito para ma-access ng RangedAttack.cs (Fix sa error mo)
    public void SetAiming(bool aiming)
    {
        isAiming = aiming;
        currentSensitivity = aiming ? aimSensitivity : sensitivity;
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        // Check kung local player bago tanggapin ang input
        if (ownerIdentity != null && !ownerIdentity.isLocalPlayer) return;
        lookInput = context.ReadValue<Vector2>();
    }

    void LateUpdate()
    {
        if (ownerIdentity == null || !ownerIdentity.isLocalPlayer) return;

        CheckSpectatorTarget();
        if (target == null) return;

        // 1. INPUT SMOOTHING: Pinapakinis ang raw mouse input
        smoothedLookInput = Vector2.Lerp(smoothedLookInput, lookInput, Time.deltaTime * inputSmoothSpeed);

        // 2. FLICK PROTECTION: Nililimitahan ang bilis ng pag-ikot per frame para iwas jitter
        float maxRotationStep = 15f;
        float deltaYaw = Mathf.Clamp(smoothedLookInput.x * currentSensitivity * Time.deltaTime, -maxRotationStep, maxRotationStep);
        float deltaPitch = Mathf.Clamp(smoothedLookInput.y * currentSensitivity * Time.deltaTime, -maxRotationStep, maxRotationStep);

        targetYaw += deltaYaw;
        targetPitch -= deltaPitch;
        targetPitch = Mathf.Clamp(targetPitch, minY, maxY);

        // 3. ROTATION DAMPING: SmoothDamp para sa professional camera feel
        yaw = Mathf.SmoothDampAngle(yaw, targetYaw, ref yawVelocity, rotationSmoothTime);
        pitch = Mathf.SmoothDampAngle(pitch, targetPitch, ref pitchVelocity, rotationSmoothTime);

        // 4. ZOOM LERP: Para sa smooth na pag-zoom kapag nag-a-aim
        float targetDistance = isAiming ? zoomDistance : distance;
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, zoomSpeed * Time.deltaTime);

        // 5. FINAL POSITIONING
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