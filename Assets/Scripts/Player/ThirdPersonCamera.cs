using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("References")]
    public Transform playerBody;

    [Header("Settings")]
    public float mouseSensitivity = 120f;
    public float minY = -30f;
    public float maxY = 60f;

    private float xRotation = 0f;

    private PlayerInput playerInput;
    private InputAction lookAction;

    void Awake()
    {
        playerInput = GetComponentInParent<PlayerInput>();
        lookAction = playerInput.actions["Look"];
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        Vector2 look = lookAction.ReadValue<Vector2>() * mouseSensitivity * Time.deltaTime;

        // Vertical look (camera up/down)
        xRotation -= look.y;
        xRotation = Mathf.Clamp(xRotation, minY, maxY);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Horizontal look (player rotation)
        playerBody.Rotate(Vector3.up * look.x);
    }
}
