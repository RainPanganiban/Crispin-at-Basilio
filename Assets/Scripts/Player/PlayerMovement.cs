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

    [Header("Combat Rotation")]
    public bool isAiming;
    
    [SerializeField] private Transform cameraTransform;

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector3 velocity;
    private Transform cam;
    public Transform PlayerCamera => cam;

    [SyncVar] private bool isRunning = false;
    private bool isRolling = false;

    [Header("Stamina Settings")]
    public float staminaCostPerSecondRunning = 15f;
    public float rollStaminaCost = 25f;

    private PlayerStatsManager statsManager;
    private PlayerSoundManager soundManager;
    private ICombatHandler combatHandler;

    [Header("Animation Parameters")]
    public Vector2 MoveInput => moveInput;
    public bool IsRunning => isRunning;
    public bool IsGrounded => controller.isGrounded;
    public bool IsRolling => isRolling;

    private Vector3 knockbackVelocity;
    private float knockbackTimer;

    private Vector3 attackStepVelocity;
    private float attackStepTimer;

    public void ApplyKnockback(Vector3 force, float duration)
    {
        knockbackVelocity = force;
        knockbackTimer = duration;
    }

    public void ApplyAttackStep(Vector3 velocity, float duration)
    {
        attackStepVelocity = velocity;
        attackStepTimer = duration;
    }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        statsManager = GetComponent<PlayerStatsManager>();
        soundManager = GetComponent<PlayerSoundManager>();
    }

    public override void OnStartLocalPlayer()
    {
        cam = cameraTransform;
        cam.gameObject.SetActive(true);

        combatHandler = GetComponent<ICombatHandler>();
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
        
        bool runInput = context.ReadValueAsButton();
        if (statsManager.stamina.currentValue > 0f)
        {
            isRunning = runInput;
            CmdSetRunning(runInput);
        }
        else
        {
            isRunning = false;
            CmdSetRunning(false);
        }
    }

    [Command]
    void CmdSetRunning(bool state)
    {
        isRunning = state;
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer) return;
        if (context.performed && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            GetComponent<CharacterAnimationController>()?.PlayJump();
        }
    }

    public void OnRoll(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer) return;
        
        if (context.performed && !isRolling && statsManager.stamina.currentValue >= rollStaminaCost)
        {
            statsManager.CmdUseStamina(rollStaminaCost);
            GetComponent<CharacterAnimationController>()?.PlayRoll();
            if (soundManager != null) soundManager.PlayRoll();
            StartCoroutine(Roll());
        }
    }

    public void OnLightAttack(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer) return;

        combatHandler?.OnLightAttack(context);
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

        // Knockback Handling
        if (knockbackTimer > 0)
        {
            knockbackTimer -= Time.deltaTime;
            controller.Move(knockbackVelocity * Time.deltaTime);
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, Time.deltaTime * 5f);
            
            // Apply gravity even during knockback
            if (controller.isGrounded && velocity.y < 0) velocity.y = -2f;
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
            return; // Skip normal movement while knocked back
        }

        // Lock movement if attacking
        CharacterAnimationController animCtrl = GetComponent<CharacterAnimationController>();
        bool isLocked = animCtrl != null && animCtrl.IsActionLocked;

        // Attack Step Handling
        if (attackStepTimer > 0)
        {
            attackStepTimer -= Time.deltaTime;
            controller.Move(attackStepVelocity * Time.deltaTime);
            attackStepVelocity = Vector3.Lerp(attackStepVelocity, Vector3.zero, Time.deltaTime * 5f);
        }

        // Movement input
        if (moveInput.sqrMagnitude > 0.01f && !isLocked)
        {
            Vector3 camForward = cam.forward;
            Vector3 camRight = cam.right;
            camForward.y = 0;
            camRight.y = 0;
            Vector3 moveDir = camForward.normalized * moveInput.y + camRight.normalized * moveInput.x;

            float speed = moveSpeed;

            // Slow Walk Approach: Cut speed dramatically while aiming
            if (isAiming)
            {
                speed = moveSpeed * 0.3f; // 70% reduction in speed
                if (isRunning) 
                {
                    isRunning = false;
                    CmdSetRunning(false);
                }
            }
            else if (isRunning && statsManager.stamina.currentValue > 0f)
            {
                speed = runSpeed;
                // Drain stamina while running (only if server to update SyncVar,
                // client also calls it for smooth local UI update)
                statsManager.UseStamina(staminaCostPerSecondRunning * Time.deltaTime);

                // Stop running if out of stamina
                if (statsManager.stamina.currentValue <= 0f)
                {
                    isRunning = false;
                    CmdSetRunning(false);
                }
            }

            controller.Move(moveDir * speed * Time.deltaTime);

            // Smooth rotation
            if (!isAiming)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRot,
                    rotationSpeed * Time.deltaTime
                );
            }
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
