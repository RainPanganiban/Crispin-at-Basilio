using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    public float speed = 5f;
    public float gravity = -9.81f;

    private CharacterController controller;
    private Vector2 moveInput;
    private float verticalVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void OnMove(InputValue value)
    {
        if (!IsOwner) return;
        moveInput = value.Get<Vector2>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (controller.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f; // keeps player grounded
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 moveDirection =
            transform.right * moveInput.x +
            transform.forward * moveInput.y;

        if (moveDirection.magnitude > 1f)
            moveDirection.Normalize();

        Vector3 velocity = moveDirection * speed;
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);
    }
}
