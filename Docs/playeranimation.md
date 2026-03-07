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
    *   **Floats:** `Speed` (0-1), `InputX` (for aiming)
    *   **Bools:** `IsGrounded`
    *   **Triggers:** `Jump`, `Roll`, `Attack`, `ContinueCombo`

2.  **Base Layer States & Transitions:**

    *   **`Locomotion` (Blend Tree):**
        *   **Blend Type:** `1D`
        *   **Parameter:** `Speed`
        *   **Motion Fields:** `Idle` (0), `Walk` (0.5), `Run` (1.0).

    *   **Combat Orientation:** Basilio automatically re-orients his root transform towards the camera's forward direction at the start of any attack and between every combo hit. This ensures he always strikes where the player is looking, even if they course-correct mid-combo.

    *   **`Attack1`, `Attack2`, `Attack3` States:** Tagged as `Action`. Transitions are driven by `Attack` and `ContinueCombo`.

### 2.2. Basilio Code (CharacterAnimationController.cs)

The base class handles the "True Input Buffer" system, which remembers inputs during animations to trigger the next combo hit seamlessly.

```csharp
    protected virtual void Update()
    {
        if (!isLocalPlayer) return;

        UpdateLockState();
        UpdateLocomotion();

        // Tightly responsive True Input Buffer
        if (Time.time - lastAttackInputTime <= inputBufferTime)
        {
            if (!isLocked) // Free to start fresh combo
            {
                comboCount = 1;
                Trigger("Attack"); 
                lastAttackInputTime = -10f; 
                canCombo = false;
                
                // Attack Step momentum
                GetComponent<PlayerMovement>()?.ApplyAttackStep(transform.forward * 12f, 0.15f);
            }
            else if (canCombo && comboCount < 3) // Mid-attack, but combo window is OPEN
            {
                comboCount++;
                Trigger("ContinueCombo"); 
                lastAttackInputTime = -10f; 
                canCombo = false;
                
                // Mid-Combo Rotation: Re-align to crosshair between hits
                Transform camTransform = movement != null ? movement.PlayerCamera : null;
                if (camTransform != null)
                {
                    Vector3 camForward = camTransform.forward;
                    camForward.y = 0;
                    if (camForward.sqrMagnitude > 0.01f)
                    {
                        transform.rotation = Quaternion.LookRotation(camForward);
                    }
                }

                GetComponent<PlayerMovement>()?.ApplyAttackStep(transform.forward * 12f, 0.15f);
            }
        }
    }
```

---

## 3. Crispin Animation Setup (Wind-up Attack)

Crispin uses a single wind-up attack that allows movement and directional aiming while charging.

### 3.1. Crispin Animator Setup

1.  **Parameters Tab:**
    *   **Floats:** `Speed`, `AttackWindUp` (0-1), `InputX` (Left -1, Right 1)
    *   **Bools:** `IsWindingUp` (True while charging)
    *   **Triggers:** `ReleaseAttack`

2.  **State Machine:**
    *   **`WindUp` State (1D Blend Tree):** Driven by `InputX`. Blends between `WindUp_Left`, `WindUp_Still`, and `WindUp_Right`.
    *   **Slow Walk Mechanic:** While `IsWindingUp` is true, the `PlayerMovement` script reduces speed to 30% of normal, ensuring Crispin can strafe without "moonwalking."

### 3.2. Crispin Code (CrispinAnimation.cs)

Crispin overrides the locomotion logic to feed `InputX` into the 1D Blend Tree and handles the wind-up logic in `Update`.

```csharp
    protected override void Update()
    {
        base.Update(); 

        if (!isLocalPlayer) return;

        // Handle the wind-up animation logic
        if (isHoldingAttack)
        {
            float maxChargeTime = (rangedAttack != null && rangedAttack.maxChargeTime > 0) ? rangedAttack.maxChargeTime : 1.0f;
            attackWindUpValue += Time.deltaTime / maxChargeTime;
            attackWindUpValue = Mathf.Clamp01(attackWindUpValue);
            animator.SetFloat("AttackWindUp", attackWindUpValue);
        }
        else
        {
            if (attackWindUpValue > 0)
            {
                float maxChargeTime = (rangedAttack != null && rangedAttack.maxChargeTime > 0) ? rangedAttack.maxChargeTime : 1.0f;
                attackWindUpValue -= (Time.deltaTime / maxChargeTime) * 2; 
                attackWindUpValue = Mathf.Clamp01(attackWindUpValue);
                animator.SetFloat("AttackWindUp", attackWindUpValue);
            }
        }
    }
```

### 3.3. Performance & Networking
* **Network Authoritative:** All triggers and parameters are synced via `NetworkAnimator`.
* **Crosshair Integration:** Projectiles are calculation-driven based on the camera crosshair hitpoint, ensuring perfect accuracy regardless of animation pose.
