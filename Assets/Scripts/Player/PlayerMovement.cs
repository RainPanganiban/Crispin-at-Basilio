using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float runSpeed = 8f;
    public float rotationSpeed = 10f;
    public float rollDistance = 5f;
    public float rollDuration = 0.3f;

    [Header("Jump & Physics (Bunny Hop)")]
    public float jumpHeight = 2.5f;
    public float gravity = -9.81f;
    public float fallMultiplier = 2.5f; // Eto ang magpapabilis sa bagsak (para hindi lutang)
    public float bhopSpeedMultiplier = 1.05f; // Dagdag bilis kada talon
    public float maxBhopSpeed = 12f;

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
            // BUNNY HOP LOGIC: Kung mabilis na ang takbo, dagdagan pa natin pag tumalon
            if (currentHorizontalVelocity.magnitude > moveSpeed)
            {
                currentHorizontalVelocity = Vector3.ClampMagnitude(currentHorizontalVelocity * bhopSpeedMultiplier, maxBhopSpeed);
            }

            // Vertical force for jump
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

    #endregion

    void Update()
    {
        if (!isLocalPlayer) return;
        if (isRolling) return;

        // 1. External Forces (Knockback/Attack Steps)
        HandleExternalForces();

        // 2. Horizontal Movement (Momentum & Air Control)
        CalculateHorizontalMovement();

        // 3. Vertical Movement (Gravity & Final Move)
        ApplyGravityAndFinalMove();
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
            // BHOP FIX: Gamit ang Lerp para hindi biglang hinto ang momentum paglapag sa lupa
            Vector3 desiredVelocity = targetMoveDir * targetSpeed;
            currentHorizontalVelocity = Vector3.Lerp(currentHorizontalVelocity, desiredVelocity, 15f * Time.deltaTime);

            // Rotation
            if (targetMoveDir != Vector3.zero && !isAiming)
            {
                Quaternion targetRot = Quaternion.LookRotation(targetMoveDir);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRot,
                    rotationSpeed * Time.deltaTime
                );
            }
        }
        else
        {
            // Air logic: Kontrol sa ere pero naitatabi ang momentum
            Vector3 airDir = targetMoveDir * targetSpeed;
            currentHorizontalVelocity = Vector3.Lerp(currentHorizontalVelocity, airDir, airControl * Time.deltaTime);
        }

        // Apply Move
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

        // BAGSAK FIX: Mas mabilis ang gravity kapag pababa na ang character (Fall Multiplier)
        float currentGravity = gravity;
        if (velocity.y < 0)
        {
            currentGravity *= fallMultiplier;
        }

        velocity.y += currentGravity * Time.deltaTime;

        // Final move call for vertical velocity
        controller.Move(velocity * Time.deltaTime);
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