using UnityEngine;
using Mirror;

[RequireComponent(typeof(Animator))]
public abstract class CharacterAnimationController : NetworkBehaviour
{
    protected Animator animator;
    protected NetworkAnimator networkAnimator;
    protected PlayerMovement movement;
    protected PlayerStatsManager statsManager;

    // True Input Buffer & Combo System
    [Header("Combo Settings")]
    [SerializeField]
    private float inputBufferTime = 0.5f; // How long to remember a button press
    protected float lastAttackInputTime = -10f;
    protected int comboCount = 0; // Current attack in combo (0: none, 1: Attack1, 2: Attack2, 3: Attack3)
    protected bool canCombo = false; // Set via Animation Event to allow next attack
    protected bool isLocked;
    public bool IsActionLocked => isLocked;

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
        // Debug.Log($"Update() called on GameObject '{this.gameObject.name}'. isLocalPlayer = {isLocalPlayer}");

        if (!isLocalPlayer) return;

        UpdateLockState();
        UpdateLocomotion();

        // Tightly responsive True Input Buffer
        if (Time.time - lastAttackInputTime <= inputBufferTime)
        {
            if (!isLocked) // Free to start fresh combo
            {
                Debug.Log($"[Combo] Starting New Combo. Step: 1");
                comboCount = 1;
                Trigger("Attack"); // Trigger Animator from Idle to Attack1
                lastAttackInputTime = -10f; // Consume input
                canCombo = false;
                
                // Strong punchy forward momentum
                GetComponent<PlayerMovement>()?.ApplyAttackStep(transform.forward * 12f, 0.15f);
            }
            else if (canCombo && comboCount < 3) // Mid-attack, but combo window is OPEN
            {
                Debug.Log($"[Combo] Buffering Next Hit. Current Step: {comboCount} -> Target Step: {comboCount + 1}");
                comboCount++;
                Trigger("ContinueCombo"); // Trigger Animator from Attack N to Attack N+1
                lastAttackInputTime = -10f; // Consume input
                canCombo = false;
                
                // Strong punchy forward momentum
                GetComponent<PlayerMovement>()?.ApplyAttackStep(transform.forward * 12f, 0.15f);
            }
        }
    }

    private int lastResetCheckFrame = 0;

    void UpdateLockState()
    {
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        bool transitioning = animator.IsInTransition(0);
        
        bool inAction =
            state.IsTag("Action") || // Check for states tagged as "Action"
            state.IsName("Jump");

        // Cleanly reset combo state when exiting an Action state back back to Locomotion
        // We add a tiny frame delay (2 frames) before resetting. This handles the case where 
        // Unity might report !inAction for a single frame during a transition.
        if (isLocked && !inAction && !transitioning)
        {
            if (Time.frameCount > lastResetCheckFrame + 2)
            {
                Debug.Log($"[Combo] Action Finished. Resetting Combo.");
                comboCount = 0;
                canCombo = false;
                animator.speed = 1f; // Reset speed multiplier if we exit Action
            }
        }
        else
        {
            lastResetCheckFrame = Time.frameCount;
        }

        isLocked = inAction || transitioning;
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
        lastAttackInputTime = Time.time; // Simply store the input time
    }

    public virtual void PlayRoll()
    {
        if (isLocked) return;
        Trigger("Roll");
        // Reset combo if rolling, as it interrupts attack flow
        comboCount = 0;
        canCombo = false;
        lastAttackInputTime = -10f; // Clear input buffer
    }

    public virtual void PlayJump()
    {
        if (isLocked) return;
        Trigger("Jump");
        // Reset combo if jumping, as it interrupts attack flow
        comboCount = 0;
        canCombo = false;
        lastAttackInputTime = -10f; // Clear input buffer
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

    // Called precisely when the weapon finishes striking and starts returning.
    // This allows the player to chain the next attack instantly.
    public void AnimationEvent_OpenComboWindow()
    {
        canCombo = true;
    }

    // (Optional) Closes the combo buffer early if you want a strict ending window.
    // However, going back to Idle automatically resets comboCount anyway.
    public void AnimationEvent_CloseComboWindow()
    {
        canCombo = false;
    }

    // --- Legacy / Compatibility Stubs ---
    // These methods are no longer used by the new Input Buffer system.
    // They are left as empty stubs to prevent "No Receiver" errors in Unity until 
    // you remove/replace referencing events in the Animation Clips.
    public void AnimationEvent_SetComboWindow(int state) { }
    public void AnimationEvent_CheckCombo() { }

    // --- Combat / VFX Animation Events ---
    // (Trail methods removed at user request)

    // Called at exact frame of impact
    public void AnimationEvent_Hit()
    {
        if (!isServer) // Usually we want server to apply damage, but for responsiveness we can signal local MeleeCombat to Cmd Apply Damage.
        {
            // We tell MeleeCombat on local client to tell server to apply damage now
            if (isLocalPlayer)
            {
                MeleeCombat combat = GetComponent<MeleeCombat>();
                if (combat != null)
                {
                    combat.ApplyDamageLocalClient(comboCount);
                }
            }
        }
        else if (isLocalPlayer && isServer) // Host case
        {
            MeleeCombat combat = GetComponent<MeleeCombat>();
            if (combat != null)
            {
                combat.ApplyDamageLocalClient(comboCount);
            }
        }
    }
}

public class BasilioAnimation : CharacterAnimationController
{
}
