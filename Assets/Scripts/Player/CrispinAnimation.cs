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
    }

    protected override void Update()
    {
        base.Update(); // Call base Update for locomotion, lock state, etc.

        if (!isLocalPlayer) return;

        // Handle the wind-up animation logic
        if (isHoldingAttack)
        {
            // Read maxChargeTime from RangedAttack script to sync animation perfectly.
            // Fallback to 1 second if the script isn't found.
            float maxChargeTime = (rangedAttack != null && rangedAttack.maxChargeTime > 0) ? rangedAttack.maxChargeTime : 1.0f;

            // While holding the button, increase the wind-up value over time
            attackWindUpValue += Time.deltaTime / maxChargeTime;
            attackWindUpValue = Mathf.Clamp01(attackWindUpValue);
            animator.SetFloat("AttackWindUp", attackWindUpValue);
        }
        else
        {
            // If not holding, and the wind-up value is not yet 0, reset it smoothly.
            if (attackWindUpValue > 0)
            {
                float maxChargeTime = (rangedAttack != null && rangedAttack.maxChargeTime > 0) ? rangedAttack.maxChargeTime : 1.0f;
                // Reset faster than winding up
                attackWindUpValue -= (Time.deltaTime / maxChargeTime) * 2; 
                attackWindUpValue = Mathf.Clamp01(attackWindUpValue);
                animator.SetFloat("AttackWindUp", attackWindUpValue);
            }
        }
    }
    
    // The old PlayAttack() override is no longer needed for this new mechanic.
}
