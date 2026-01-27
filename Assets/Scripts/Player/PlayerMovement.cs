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

    [Header("Stamina Settings")]
    public float staminaCostPerSecondRunning = 15f;
    public float rollStaminaCost = 25f;

    private PlayerStatsManager statsManager;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        statsManager = GetComponent<PlayerStatsManager>();
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
        
        if (statsManager.stamina.currentValue > 0f)
        {
            isRunning = context.ReadValueAsButton();
        }
        else
        {
            isRunning = false;
        }
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
        
        if (context.performed && !isRolling && statsManager.stamina.currentValue >= rollStaminaCost)
        {
            statsManager.UseStamina(rollStaminaCost);
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
        if (isRolling) return;

        // Movement input
        if (moveInput.sqrMagnitude > 0.01f)
        {
            Vector3 camForward = cam.forward;
            Vector3 camRight = cam.right;
            camForward.y = 0;
            camRight.y = 0;
            Vector3 moveDir = camForward.normalized * moveInput.y + camRight.normalized * moveInput.x;

            float speed = moveSpeed;

            if (isRunning && statsManager.stamina.currentValue > 0f)
            {
                speed = runSpeed;
                // Drain stamina while running
                statsManager.UseStamina(staminaCostPerSecondRunning * Time.deltaTime);

                // Stop running if out of stamina
                if (statsManager.stamina.currentValue <= 0f)
                    isRunning = false;
            }

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
        Vector3 rollDir = transform.forward;
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
