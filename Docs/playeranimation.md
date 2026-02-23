# Player Animation Documentation: Crispin at Basilio

This document details the setup, code implementation, and troubleshooting steps for player animations for both Basilio (combo attack) and Crispin (wind-up attack), building upon the `project-context.md`.

---

## 1. Core Player Animation System Overview

Based on `project-context.md`:

*   **Architecture:**
    *   Shared base controller `CharacterAnimationController` (implemented within `BasilioAnimation.cs` file) updates locomotion parameters and uses `NetworkAnimator`.
    *   Character-specific controllers (`BasilioAnimation`, `CrispinAnimation`) inherit from `CharacterAnimationController`.
    *   Networking via Mirror `NetworkAnimator` with client authority for player-owned characters.
*   **Animator Parameters:**
    *   Floats: `Speed` (0–1, normalized), `AttackWindUp` (0-1, for Crispin)
    *   Bools: `IsGrounded`, `IsWindingUp` (for Crispin)
    *   Triggers: `Jump`, `Roll`, `Attack` (Basilio: starts combo, Crispin: starts wind-up), `ReleaseAttack` (for Crispin), `ContinueCombo` (for Basilio)
*   **Setup (per player prefab):**
    *   Ensure an `Animator` component exists and is assigned a controller.
    *   Add `NetworkAnimator` (clientAuthority = true), point its `animator` to the same `Animator`.
    *   Ensure the prefab has the correct `CharacterAnimationController` derivative attached (`BasilioAnimation` or `CrispinAnimation`).
    *   Player scripts now call `GetComponent<CharacterAnimationController>()` (or the specific derivative) for action triggers.
*   **Multiplayer safety:** Locomotion params are set on the owning client and synced by `NetworkAnimator`. Discrete actions use triggers through `NetworkAnimator.SetTrigger(...)`.

---

## 2. Basilio Animation Setup (3-Hit Combo)

Basilio utilizes a 3-hit attack combo system where subsequent attacks can only be chained if the player inputs within a short "combo window."

### 2.1. Basilio Animator Controller Setup

**(Requires manual setup in Unity Editor)**

1.  **Parameters Tab:**
    *   **Floats:** `Speed` (0-1)
    *   **Bools:** `IsGrounded`
    *   **Triggers:** `Jump`, `Roll`, `Attack`, `ContinueCombo`
    *   *(Basilio does NOT need `Direction` or `AttackWindUp`, `IsWindingUp`, `ReleaseAttack`)*

2.  **Base Layer States & Transitions:**

    *   **`Locomotion` (Blend Tree):**
        *   Right-click in an empty area, `Create State > From New Blend Tree`. Name it `Locomotion`.
        *   **Blend Type:** `1D`
        *   **Parameter:** `Speed`
        *   **Motion Fields:**
            *   `Idle Animation Clip`: `Threshold 0`
            *   `Walk_Forward Animation Clip`: `Threshold 0.5`
            *   `Run_Forward Animation Clip`: `Threshold 1.0`

    *   **`Jump` State:**
        *   Create state, assign `Jump Animation Clip`.
        *   **Tag:** None
        *   **Transition `Any State` -> `Jump`:** `Has Exit Time`: Unchecked, `Condition`: `Jump` (Trigger), `Transition Duration`: `0.1`
        *   **Transition `Jump` -> `Locomotion`:** `Has Exit Time`: Checked, `Transition Duration`: `0.1-0.2`, `Conditions`: None

    *   **`Roll` State:**
        *   Create state, assign `Roll Animation Clip`.
        *   **Tag:** `Action`
        *   **Transition `Any State` -> `Roll`:** `Has Exit Time`: Unchecked, `Condition`: `Roll` (Trigger), `Transition Duration`: `0.1`
        *   **Transition `Roll` -> `Locomotion`:** `Has Exit Time`: Checked, `Transition Duration`: `0.1-0.2`, `Conditions`: None

    *   **`Attack1`, `Attack2`, `Attack3` States (for Combo):**
        *   Create three separate states, assign `Attack1`, `Attack2`, `Attack3` animation clips respectively.
        *   **Tag (for all three):** `Action` (Crucial for `isLocked` logic)

    *   **Attack Combo Transitions:**
        *   **Transition `Any State` -> `Attack1`:**
            *   `Has Exit Time`: Unchecked
            *   `Condition`: `Attack` (Trigger)
            *   `Transition Duration`: `0.05` or `0`
        *   **Transition `Attack1` -> `Attack2`:**
            *   `Has Exit Time`: Unchecked
            *   `Condition`: `ContinueCombo` (Trigger)
            *   `Transition Duration`: `0.05` or `0`
        *   **Transition `Attack2` -> `Attack3`:**
            *   `Has Exit Time`: Unchecked
            *   `Condition`: `ContinueCombo` (Trigger)
            *   `Transition Duration`: `0.05` or `0`
        *   **Transition `Attack3` -> `Locomotion` (End of Combo):**
            *   `Has Exit Time`: Checked
            *   `Transition Duration`: `0.1-0.2`, `Conditions`: None
        *   **Transition `Attack1` -> `Locomotion` (Combo Failed/Not Continued):**
            *   `Has Exit Time`: Checked
            *   `Transition Duration`: `0.1-0.2`, `Conditions`: None
        *   **Transition `Attack2` -> `Locomotion` (Combo Failed/Not Continued):**
            *   `Has Exit Time`: Checked
            *   `Transition Duration`: `0.1-0.2`, `Conditions`: None

### 2.2. `CharacterAnimationController.cs` (Basilio's Code)

The `CharacterAnimationController` (found in `Assets/Scripts/Player/BasilioAnimation.cs`) was modified to include combo logic.

```csharp
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
        if (!isLocalPlayer) return;

        UpdateLockState();
        UpdateLocomotion();

        // Combo Timeout Logic
        if (comboCount > 0 && Time.time > nextAttackInputTime)
        {
            // Combo window expired, reset combo
            comboCount = 0;
            attackInputQueued = false;
            // The Animator should transition back to Locomotion via Has Exit Time from attack states.
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
        if (isLocked) return;

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
        if (state == 1) // Start of window
        {
            nextAttackInputTime = Time.time + comboWindowDuration;
        }
    }

    // Called towards the end of each attack animation to check for combo continuation     
    public void AnimationEvent_CheckCombo()
    {
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
            // The Animator should transition back to Locomotion via Has Exit Time from attack states.
        }
    }
}

public class BasilioAnimation : CharacterAnimationController
{
}
```

### 2.3. Basilio - Problems Encountered & Solutions

*   **Problem:** Manual `Speed` parameter adjustment in Animator overridden by script.
    *   **Solution:** Explain that this is expected behavior when `isLocalPlayer` is true. To debug, disable the script component on the player in Play Mode.
*   **Problem:** Initial combo logic didn't work, required "spam clicking" for second hit, and didn't proceed to third. Debug logs showed `attackInputQueued=false` at `AnimationEvent_CheckCombo` for `Attack1`. `isLocked` was too restrictive.
    *   **Solution:**
        *   Modified `PlayAttack()` logic to only check `isLocked` when initiating a *new* combo (when `comboCount == 0`), allowing subsequent combo inputs to be queued even if `isLocked` is `true` during an attack animation.
        *   **Crucial Manual Steps:** Ensure `AnimationEvent_CheckCombo` is placed at the *very end* of `Attack1` and `Attack2` animation clips. Fine-tune `comboWindowDuration` (e.g., increase to `0.7s-1.0s`).
*   **Problem:** Syntax errors due to line breaks in comments and string literals after copy-pasting code.
    *   **Solution:** Corrected the specific line breaks in the `CharacterAnimationController.cs` file.
*   **Problem:** Locomotion (Walk/Run) not playing, `Speed` parameter stuck at 0. Jump/Roll/Attack not returning to Locomotion.
    *   **Solution:** Confirmed all transitions from `Jump`, `Roll`, and `Attack` states *to* `Locomotion` **must have `Has Exit Time` checked** and no additional conditions. Also confirmed the `Idle` state is NOT tagged `Action`, and other action states ARE tagged `Action`. This resolved the `isLocked` issue for locomotion.

---

## 3. Crispin Animation Setup (Single Wind-up Attack)

Crispin uses a single wind-up attack where the attack's "power" or visual is determined by how long the button is held. She does not have a multi-hit combo.

### 3.1. Crispin Animator Controller Setup

**(Requires manual setup in Unity Editor)**

1.  **Parameters Tab:**
    *   **Floats:** `Speed` (0-1), `AttackWindUp` (0-1, represents charge progress)
    *   **Bools:** `IsGrounded`, `IsWindingUp` (true when holding attack button)
    *   **Triggers:** `Jump`, `Roll`, `ReleaseAttack` (triggers the attack release animation)
    *   *(Crispin does NOT need `Direction`, `Attack` (Trigger), or `ContinueCombo`)*

2.  **Base Layer States & Transitions:**

    *   **`Locomotion` (Blend Tree):** (Same as Basilio's, `1D` blend with `Speed` parameter, `Idle` 0, `Walk_Forward` 0.5, `Run_Forward` 1.0 thresholds).

    *   **`Jump` State:** (Same as Basilio's setup).

    *   **`Roll` State:** (Same as Basilio's setup).

    *   **`WindUp` State:**
        *   Create state, assign Crispin's `WindUp Animation Clip` (this might be a single frame, a looped animation, or a blend tree itself based on `AttackWindUp` Float).
        *   **Tag:** `Action`
        *   **Transition `Any State` -> `WindUp`:** `Has Exit Time`: Unchecked, `Condition`: `IsWindingUp` (Bool) = `true`, `Transition Duration`: `0.1`
        *   *(Optional: If the wind-up animation is driven by `AttackWindUp` float, you might use a Blend Tree here)*

    *   **`AttackRelease` State:**
        *   Create state, assign Crispin's `AttackRelease Animation Clip`.
        *   **Tag:** `Action`
        *   **Transition `WindUp` -> `AttackRelease`:** `Has Exit Time`: Unchecked, `Condition`: `ReleaseAttack` (Trigger), `Transition Duration`: `0.05` or `0`
        *   *(Optional: Add a transition from `Locomotion` to `AttackRelease` with `ReleaseAttack` trigger if you want to allow instant release without wind-up)*

    *   **Transitions out of Attack:**
        *   **Transition `WindUp` -> `Locomotion`:** `Has Exit Time`: Unchecked, `Condition`: `IsWindingUp` (Bool) = `false`, `Transition Duration`: `0.1-0.2` (for cancelling wind-up)
        *   **Transition `AttackRelease` -> `Locomotion`:** `Has Exit Time`: Checked, `Transition Duration`: `0.1-0.2`, `Conditions`: None

### 3.2. `CrispinAnimation.cs` Code

`CrispinAnimation.cs` overrides the `PlayAttack()` method from the base class and introduces new methods for managing the wind-up.

```csharp
using UnityEngine;
using Mirror;

[RequireComponent(typeof(Animator))]
public class CrispinAnimation : CharacterAnimationController
{
    private bool isHoldingAttack = false;
    private float attackWindUpValue = 0f;
    
    // Reference to the RangedAttack script to sync timing
    private RangedAttack rangedAttack;

    protected override void Awake()
    {
        base.Awake();
        // Get the reference to the RangedAttack script on the same GameObject
        rangedAttack = GetComponent<RangedAttack>();
    }

    // --- Public methods to be called by your Input System ---

    /// <summary>
    /// Call this when the attack button is first PRESSED DOWN.
    /// </summary>
    public void OnAttackStarted()
    {
        // Prevent starting a wind-up if already in a locked action (like jump or roll)
        if (isLocked) return;

        isHoldingAttack = true;
        animator.SetBool("IsWindingUp", true);
        attackWindUpValue = 0f; // Reset wind-up value on new attack

        // Also reset combo from base class, just in case (Crispin doesn't use it)
        comboCount = 0;
        attackInputQueued = false;
    }

    /// <summary>
    /// Call this when the attack button is RELEASED.
    /// </summary>
    public void OnAttackReleased()
    {
        // Only do something on release if we were actually holding the attack
        if (!isHoldingAttack) return; 

        isHoldingAttack = false;
        
        // We trigger the release animation. The current wind-up value on the animator
        // will determine the "power" or look of the attack if you use it in the release animation.
        animator.SetTrigger("ReleaseAttack");

        // After triggering the release, we set IsWindingUp to false.
        // This will allow the animator to transition from AttackRelease back to Locomotion,
        // or from WindUp back to Locomotion if the attack was cancelled.
        animator.SetBool("IsWindingUp", false);

        // Also reset combo from base class, just in case
        comboCount = 0;
        attackInputQueued = false;
    }

    protected override void Update()
    {
        base.Update(); // Call base Update for locomotion, lock state, etc.

        if (!isLocalPlayer) return;

        // Handle the wind-up animation logic
        if (isHoldingAttack)
        {
            // Read maxChargeTime from RangedAttack script to sync animation perfectly.
            // Fallback to 1 second if the script isn't found or maxChargeTime is invalid.
            float maxChargeTime = (rangedAttack != null && rangedAttack.maxChargeTime > 0) ? rangedAttack.maxChargeTime : 1.0f;

            // While holding the button, increase the wind-up value over time
            attackWindUpValue += Time.deltaTime / maxChargeTime;
            attackWindUpValue = Mathf.Clamp01(attackWindUpValue);
            animator.SetFloat("AttackWindUp", attackWindUpValue);
        }
        else
        {
            // If not holding, and the wind-up value is not yet 0, reset it smoothly.
            // This handles cases where the attack was cancelled or the release animation finished.
            if (attackWindUpValue > 0)
            {
                float maxChargeTime = (rangedAttack != null && rangedAttack.maxChargeTime > 0) ? rangedAttack.maxChargeTime : 1.0f;
                // Reset faster than winding up for a snappier feel
                attackWindUpValue -= (Time.deltaTime / maxChargeTime) * 2; 
                attackWindUpValue = Mathf.Clamp01(attackWindUpValue);
                animator.SetFloat("AttackWindUp", attackWindUpValue);
            }
        }
    }
    
    // The old PlayAttack() override is no longer needed for this new mechanic.
    // Overriding it with an empty body to prevent accidental calls if desired.
    public override void PlayAttack() { /* No longer used for Crispin's attack logic */ }
}
```

### 3.3. Crispin - Problems Encountered & Current Status

*   **Problem:** `isLocked`, `comboCount`, `attackInputQueued` were inaccessible in `CrispinAnimation` due to `private` protection level in `CharacterAnimationController`.
    *   **Solution:** Changed access modifiers to `protected` in `CharacterAnimationController.cs`.
*   **Current Problem:** Crispin's wind-up attack is "not working". Locomotion, roll, and jump animations are functional.

---

## 4. Current Outstanding Issues

The primary outstanding issue is Crispin's **wind-up attack not playing/functioning correctly.**

**Troubleshooting Steps for Crispin's Wind-up Attack:**

1.  **Input System Verification:**
    *   Ensure your input handler is calling `crispinAnimation.OnAttackStarted()` precisely when the attack button is pressed down.
    *   Ensure it's calling `crispinAnimation.OnAttackReleased()` precisely when the attack button is released.
    *   Add `Debug.Log` statements to the start of both `OnAttackStarted()` and `OnAttackReleased()` to confirm they are firing.

2.  **Animator Parameters Check:**
    *   In Play Mode, open the Animator window, go to the "Parameters" tab, and select Crispin's GameObject in the Hierarchy.
    *   **Hold the attack button:**
        *   Does `IsWindingUp` (Bool) become `true`?
        *   Does `AttackWindUp` (Float) increase from `0` to `1`?
    *   **Release the attack button:**
        *   Does `ReleaseAttack` (Trigger) briefly flash?
        *   Does `IsWindingUp` (Bool) become `false`?

3.  **Animator State Machine (Crispin's specific issue):**
    *   **While holding the attack button:** Does the Animator's current state correctly transition into your `WindUp` state?
    *   **After releasing the attack button:** Does it correctly transition from `WindUp` to `AttackRelease`?
    *   **Check all transitions:** Ensure they have the correct `IsWindingUp` (Bool) and `ReleaseAttack` (Trigger) conditions, and that `Has Exit Time` is used appropriately for transitions out of states like `AttackRelease` back to `Locomotion`.
    *   **State Tags:** Confirm `WindUp` and `AttackRelease` states are tagged as `Action`.

4.  **`RangedAttack` Component:**
    *   Ensure Crispin's GameObject has the `RangedAttack` script attached, and its `maxChargeTime` is set to a reasonable value (> 0).

---

This document summarizes our work and current issues. Please use the troubleshooting steps provided for Crispin's wind-up attack, and provide the `Debug.Log` outputs and Animator parameter/state observations.
