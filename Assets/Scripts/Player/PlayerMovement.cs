using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float runSpeed = 8f;
    public float rotationSpeed = 10f;
    public float rollDistance = 5f;
    public float rollDuration = 0.3f;
    public float rollCooldown = 0.8f;

    [Header("Jump & Physics (Bunny Hop)")]
    public float jumpHeight = 2.5f;
    public float gravity = -9.81f;
    public float fallMultiplier = 2.5f;
    public float bhopSpeedMultiplier = 1.05f;
    public float maxBhopSpeed = 12f;

    [Header("Jump Polish (The Fix)")]
    public float jumpBufferTime = 0.2f; // Window para sa maagang pindot
    private float jumpBufferCounter;
    public float coyoteTime = 0.15f;    // Window para sa huling hirit na talon
    private float coyoteTimeCounter;

    [Header("Air Control & Momentum")]
    [Range(0, 1)] public float airControl = 0.4f;
    private Vector3 currentHorizontalVelocity;

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
    private float nextRollTime = 0f;

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

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        statsManager = GetComponent<PlayerStatsManager>();
        soundManager = GetComponent<PlayerSoundManager>();
    }

    public override void OnStartLocalPlayer()
    {
        cam = cameraTransform;
        if (cam != null) cam.gameObject.SetActive(true);
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
        if (statsManager != null && statsManager.stamina.currentValue > 0f)
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
    void CmdSetRunning(bool state) => isRunning = state;

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer) return;

        if (context.performed)
        {
            jumpBufferCounter = jumpBufferTime; // Itatala na pinindot ang jump
        }
    }

    public void OnRoll(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer) return;

        CharacterAnimationController animCtrl = GetComponent<CharacterAnimationController>();
        bool isLocked = animCtrl != null && animCtrl.IsActionLocked;

        if (context.performed && !isRolling && !isLocked && Time.time >= nextRollTime && statsManager.stamina.currentValue >= rollStaminaCost)
        {
            nextRollTime = Time.time + rollCooldown;
            statsManager.CmdUseStamina(rollStaminaCost);
            animCtrl?.PlayRoll();
            if (soundManager != null) soundManager.PlayRoll();
            StartCoroutine(Roll());
        }
    }

    public void OnLightAttack(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer) return;
        if (context.performed && isRolling) isRolling = false;
        combatHandler?.OnLightAttack(context);
    }

    #endregion

    void Update()
    {
        if (!isLocalPlayer) return;

        // 1. Physics Timers
        HandleTimers();
        HandleExternalForces();

        // 2. Vertical Movement & Jump Check
        ApplyGravityAndFinalMove();
        CheckForJump();

        // 3. Movement Lock
        if (isRolling) return;

        // 4. WASD Movement
        CalculateHorizontalMovement();
    }

    private void HandleTimers()
    {
        // Jump Buffer Timer
        if (jumpBufferCounter > 0) jumpBufferCounter -= Time.deltaTime;

        // Coyote Time Timer
        if (controller.isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
    }

    private void CheckForJump()
    {
        // Kung may buffer (pinindot) at may coyote (nasa lupa or kaka-alis lang)
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f && !isRolling)
        {
            ExecuteJump();
        }
    }

    private void ExecuteJump()
    {
        // Bunny Hop logic
        if (currentHorizontalVelocity.magnitude > moveSpeed)
        {
            currentHorizontalVelocity = Vector3.ClampMagnitude(currentHorizontalVelocity * bhopSpeedMultiplier, maxBhopSpeed);
        }

        velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        GetComponent<CharacterAnimationController>()?.PlayJump();

        // I-clear ang timers para hindi mag-double jump
        jumpBufferCounter = 0;
        coyoteTimeCounter = 0;
    }

    private void CalculateHorizontalMovement()
    {
        CharacterAnimationController animCtrl = GetComponent<CharacterAnimationController>();
        bool isLocked = animCtrl != null && animCtrl.IsActionLocked;

        Vector3 targetMoveDir = Vector3.zero;

        if (moveInput.sqrMagnitude > 0.01f && !isLocked)
        {
            Vector3 camForward = cam.forward;
            Vector3 camRight = cam.right;
            camForward.y = 0;
            camRight.y = 0;
            targetMoveDir = (camForward.normalized * moveInput.y + camRight.normalized * moveInput.x).normalized;
        }

        float targetSpeed = GetCurrentSpeed();

        if (controller.isGrounded)
        {
            Vector3 desiredVelocity = targetMoveDir * targetSpeed;
            currentHorizontalVelocity = Vector3.Lerp(currentHorizontalVelocity, desiredVelocity, 15f * Time.deltaTime);

            if (targetMoveDir != Vector3.zero && !isAiming)
            {
                Quaternion targetRot = Quaternion.LookRotation(targetMoveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }
        }
        else
        {
            Vector3 airDir = targetMoveDir * targetSpeed;
            currentHorizontalVelocity = Vector3.Lerp(currentHorizontalVelocity, airDir, airControl * Time.deltaTime);
        }

        controller.Move(currentHorizontalVelocity * Time.deltaTime);
    }

    private float GetCurrentSpeed()
    {
        if (isAiming) return moveSpeed * 0.3f;
        if (isRunning && statsManager.stamina.currentValue > 0f)
        {
            statsManager.UseStamina(staminaCostPerSecondRunning * Time.deltaTime);
            return runSpeed;
        }
        return moveSpeed;
    }

    private void ApplyGravityAndFinalMove()
    {
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float currentGravity = gravity;
        if (velocity.y < 0) currentGravity *= fallMultiplier;

        velocity.y += currentGravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private IEnumerator Roll()
    {
        isRolling = true;
        Vector3 rollDir = transform.forward;
        float elapsed = 0f;

        while (elapsed < rollDuration)
        {
            if (!isRolling) yield break;
            controller.Move(rollDir * (rollDistance / rollDuration) * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        isRolling = false;
    }

    private void HandleExternalForces()
    {
        if (knockbackTimer > 0)
        {
            knockbackTimer -= Time.deltaTime;
            controller.Move(knockbackVelocity * Time.deltaTime);
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, Time.deltaTime * 5f);
        }

        if (attackStepTimer > 0)
        {
            attackStepTimer -= Time.deltaTime;
            controller.Move(attackStepVelocity * Time.deltaTime);
            attackStepVelocity = Vector3.Lerp(attackStepVelocity, Vector3.zero, Time.deltaTime * 5f);
        }
    }

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
}