using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float runSpeed = 8f;      // speed when running
    public float rotationSpeed = 10f;
    public float gravity = -9.81f;
    public float jumpHeight = 2f;
    public float rollDistance = 5f;
    public float rollDuration = 0.3f;
    
    [SerializeField] private Transform cameraTransform;

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector3 velocity;
    private Transform cam;

    private bool isRunning = false;
    private bool isRolling = false;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    public override void OnStartLocalPlayer()
    {
        cam = cameraTransform;
        cam.gameObject.SetActive(true);
    }

    #region Input Callbacks

    public void OnMove(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer) return;
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnRun(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer) return;
        isRunning = context.ReadValueAsButton();
        // Later: check stamina before allowing run
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer) return;
        if (context.performed && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    public void OnRoll(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer) return;
        if (context.performed && !isRolling)
        {
            StartCoroutine(Roll());
        }
    }

    public void OnLightAttack(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer) return;
        if (context.performed)
        {
            // TODO: Call server RPC for light attack
        }
    }

    public void OnHeavyAttack(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer) return;
        if (context.performed)
        {
            // TODO: Call server RPC for heavy attack
        }
    }

    #endregion

    void Update()
    {
        if (!isLocalPlayer) return;
        if (isRolling) return; // disable normal movement during roll

        // Movement input
        if (moveInput.sqrMagnitude > 0.01f)
        {
            Vector3 camForward = cam.forward;
            Vector3 camRight = cam.right;
            camForward.y = 0;
            camRight.y = 0;
            Vector3 moveDir = camForward.normalized * moveInput.y + camRight.normalized * moveInput.x;

            float speed = isRunning ? runSpeed : moveSpeed;
            controller.Move(moveDir * speed * Time.deltaTime);

            // Smooth rotation
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        // Gravity
        if (controller.isGrounded && velocity.y < 0) velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private System.Collections.IEnumerator Roll()
    {
        isRolling = true;
        Vector3 rollDir = transform.forward; // roll in movement direction
        float elapsed = 0f;
        while (elapsed < rollDuration)
        {
            controller.Move(rollDir * rollDistance / rollDuration * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        isRolling = false;
    }
}
