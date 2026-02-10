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

        UpdateLocomotion();
    }

    void UpdateLocomotion()
    {
        if (isLocked) return;

        float speed = movementVelocityMagnitude();
        animator.SetFloat("Speed", speed);
        animator.SetFloat("Direction", 0f); // forward only (for now)
        animator.SetBool("IsGrounded", movementIsGrounded());
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

    public void PlayJump()
    {
        animator.SetTrigger("Jump");
    }

    public void PlayRoll()
    {
        animator.SetTrigger("Roll");
        LockMovement();
    }

    public void PlayAttack()
    {
        animator.SetTrigger("Attack");
        LockMovement();
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
