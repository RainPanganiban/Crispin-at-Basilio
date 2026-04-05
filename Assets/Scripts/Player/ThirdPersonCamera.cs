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
    public float zoomDistance = 1.5f;
    public float zoomSpeed = 8f;

    private Vector2 lookInput;
    private float yaw;
    private float pitch;

    private bool isAiming;
    private float currentSensitivity;
    private float currentDistance;

    NetworkIdentity ownerIdentity;
    private PlayerStatsManager localStats; // Para malaman kung patay ang player

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

        // Kunin ang stats ng local player
        localStats = ownerIdentity.GetComponent<PlayerStatsManager>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentSensitivity = sensitivity;
        currentDistance = distance;
    }

    public void SetAiming(bool aiming)
    {
        isAiming = aiming;
        currentSensitivity = aiming ? aimSensitivity : sensitivity;
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    void LateUpdate()
    {
        if (ownerIdentity == null || !ownerIdentity.isLocalPlayer)
            return;

        // --- SPECTATOR LOGIC ---
        // Kung patay ang local player, lumipat ang target sa teammate
        CheckSpectatorTarget();

        // Siguraduhin na may target bago mag-calculate (iwas error kung wala pang teammate)
        if (target == null) return;

        yaw += lookInput.x * currentSensitivity * Time.deltaTime;
        pitch -= lookInput.y * currentSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minY, maxY);

        float targetDistance = isAiming ? zoomDistance : distance;
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, zoomSpeed * Time.deltaTime);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.position = target.position + transform.rotation * new Vector3(0, 0, -currentDistance);
    }

    void CheckSpectatorTarget()
    {
        // Kung buhay ang player, dapat sarili niya ang target
        if (localStats != null && !localStats.IsDead)
        {
            target = ownerIdentity.transform;
            return;
        }

        // Kung patay ang player (Spectating mode), hanapin ang buhay na teammate
        if (localStats != null && localStats.IsDead)
        {
            PlayerStatsManager[] allPlayers = FindObjectsByType<PlayerStatsManager>(FindObjectsSortMode.None);
            foreach (var p in allPlayers)
            {
                // Kung itong player na ito ay BUHAY, siya ang magiging bagong target
                if (!p.IsDead)
                {
                    target = p.transform;
                    break;
                }
            }
        }
    }
}