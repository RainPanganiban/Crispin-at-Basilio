using UnityEngine;
using Mirror;

[RequireComponent(typeof(Animator))]
public abstract class CharacterAnimationController : NetworkBehaviour
{
    protected Animator animator;
    protected NetworkAnimator networkAnimator;
    protected PlayerMovement movement;
    protected PlayerStatsManager statsManager;

    // Combo System Fields
    [Header("Combo Settings")]
    [SerializeField]
    private float comboWindowDuration = 0.5f; // Time player has to input next attack      
    protected int comboCount = 0; // Current attack in combo (0: none, 1: Attack1, 2: Attack2, 3: Attack3)
    protected float nextAttackInputTime = 0f; // Time when the combo input window closes     
    protected bool attackInputQueued = false; // True if attack button pressed during combo window

    protected bool isLocked;

    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
        networkAnimator = GetComponent<NetworkAnimator>();
        movement = GetComponent<PlayerMovement>();
        statsManager = GetComponent<PlayerStatsManager>();
    }

    protected virtual void Update()
    {
        // This log will show even if the object is not the local player.
        Debug.Log($"Update() called on GameObject '{this.gameObject.name}'. isLocalPlayer = {isLocalPlayer}");

        if (!isLocalPlayer) return;

        // Log the state *before* any logic runs
        Debug.Log($"Frame Start -- isLocked: {isLocked}, MoveInput Magnitude: {movement.MoveInput.magnitude}");

        UpdateLockState();
        UpdateLocomotion();

        // Combo Timeout Logic
        if (comboCount > 0 && Time.time > nextAttackInputTime)
        {
            // Combo window expired, reset combo
            comboCount = 0;
            attackInputQueued = false;
            // Optionally, if you have a specific "return to idle" transition from attack  
            // states, you might want to set another trigger here, but usually Has Exit Time handles this.
        }
    }

    void UpdateLockState()
    {
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        bool inAction =
            state.IsTag("Action") || // Check for states tagged as "Action"
            state.IsName("Jump");

        isLocked = inAction;
    }

    protected virtual void UpdateLocomotion()
    {
        if (isLocked)
        {
            Debug.Log("UpdateLocomotion SKIPPED because isLocked is true.");
            return;
        }

        Vector2 input = movement.MoveInput;
        float animationSpeed = input.magnitude;

        if (movement.IsRunning)
        {
            animationSpeed = 0.5f + (animationSpeed * 0.5f);
        }
        else
        {
            animationSpeed *= 0.5f;
        }
        animationSpeed = Mathf.Clamp01(animationSpeed);

        Debug.Log($"UpdateLocomotion EXECUTED. Setting Speed to: {animationSpeed}");
        animator.SetFloat("Speed", animationSpeed, 0.1f, Time.deltaTime);
        animator.SetBool("IsGrounded", movement.IsGrounded);
    }

    // Generic Trigger method remains the same
    protected void Trigger(string name)
    {
        if (networkAnimator != null)
            networkAnimator.SetTrigger(name);
        else
            animator.SetTrigger(name);
    }

    public virtual void PlayAttack()
    {
        if (comboCount == 0) // Only check isLocked when starting a NEW combo
        {
            if (isLocked) return; // Prevent starting a new combo if locked

            comboCount = 1;
            Trigger("Attack"); // Trigger the main Attack trigger for Attack1
            nextAttackInputTime = Time.time + comboWindowDuration; // Start combo window
            attackInputQueued = false; // Reset queued input
        }
        else if (comboCount < 3) // If already in a combo, allow queuing regardless of locked state
        {
            attackInputQueued = true;
        }
        // If comboCount is 3, player is at the end of combo, cannot queue more.
    }

    public virtual void PlayRoll()
    {
        if (isLocked) return;
        Trigger("Roll");
        // Reset combo if rolling, as it interrupts attack flow
        comboCount = 0;
        attackInputQueued = false;
    }

    public virtual void PlayJump()
    {
        if (isLocked) return;
        Trigger("Jump");
        // Reset combo if jumping, as it interrupts attack flow
        comboCount = 0;
        attackInputQueued = false;
    }

    public void LockMovement()
    {
        isLocked = true;
    }

    public void UnlockMovement()
    {
        isLocked = false;
    }

    // --- Animation Event Methods ---
    // These methods are called via Animation Events on the attack animation clips.        

    // Called at the start of the combo input window for the next attack
    public void AnimationEvent_SetComboWindow(int state)
    {
        Debug.Log($"AnimationEvent_SetComboWindow: state={state}, comboCount={comboCount}");
        if (state == 1) // Start of window
        {
            nextAttackInputTime = Time.time + comboWindowDuration;
            // Optionally, you might want to reset attackInputQueued here
            // if the window only starts after an anim frame.
        }
        // If state == 0, it means end of window. But we manage this with nextAttackInputTime.
    }

    // Called towards the end of each attack animation to check for combo continuation     
    public void AnimationEvent_CheckCombo()
    {
        Debug.Log($"AnimationEvent_CheckCombo: attackInputQueued={attackInputQueued}, comboCount={comboCount}");
        if (attackInputQueued && comboCount < 3)
        {
            // Player pressed attack during the window, proceed to next combo step
            comboCount++;
            attackInputQueued = false; // Consume the queued input
            Trigger("ContinueCombo"); // Trigger Animator to transition to next attack     
            nextAttackInputTime = Time.time + comboWindowDuration; // Start new window for 
            // next attack
        }
        else
        {
            // No input, or max combo reached, reset combo
            comboCount = 0;
            attackInputQueued = false;
            // The Animator should transition back to Locomotion via Has Exit Time from    
            // Attack3, or if no ContinueCombo trigger is received from Attack1/Attack2.
        }
    }
}

public class BasilioAnimation : CharacterAnimationController
{
}
