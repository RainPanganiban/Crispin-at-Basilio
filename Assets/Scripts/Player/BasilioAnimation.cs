using UnityEngine;
using Mirror;

[RequireComponent(typeof(Animator))]
public class BasilioAnimation : NetworkBehaviour
{
    private Animator animator;
    private PlayerMovement movement;

    private bool isLocked; // movement lock (attack / roll)

    void Awake()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

        bool inAction =
            state.IsTag("Action") ||
            state.IsName("Jump");

        isLocked = inAction;

        UpdateLocomotion();
    }

    void UpdateLocomotion()
    {
        if (isLocked) return;

        Vector2 input = movement.MoveInput;

        float speed = input.magnitude;

        // Normalize speed for blend tree
        if (movement.IsRunning)
            speed = Mathf.Clamp01(speed);
        else
            speed *= 0.5f; // walk range

        animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);
        animator.SetFloat("Direction", 0f); // forward-only
        animator.SetBool("IsGrounded", movement.IsGrounded);

        Debug.Log($"Input: {movement.MoveInput} SpeedParam: {animator.GetFloat("Speed")}");
    }

    float movementVelocityMagnitude()
    {
        Vector3 v = movementVelocity();
        return Mathf.Clamp01(v.magnitude / movement.runSpeed);
    }

    Vector3 movementVelocity()
    {
        return movement.transform.InverseTransformDirection(
            movement.GetComponent<CharacterController>().velocity
        );
    }

    bool movementIsGrounded()
    {
        return movement.GetComponent<CharacterController>().isGrounded;
    }

    // -------------------------
    // Public animation triggers
    // -------------------------

    public void PlayAttack()
    {
        animator.SetTrigger("Attack");
    }

    public void PlayRoll()
    {
        animator.SetTrigger("Roll");
    }

    public void PlayJump()
    {
        animator.SetTrigger("Jump");
    }

    // -------------------------
    // Lock control
    // -------------------------

    public void LockMovement()
    {
        isLocked = true;
    }

    public void UnlockMovement()
    {
        isLocked = false;
    }
}
