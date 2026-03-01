using UnityEngine;
using UnityEngine.AI;
using Mirror;

public class EnemyAnimationController : NetworkBehaviour
{
    public Animator animator;
    
    private Vector3 lastPosition;
    private float currentSpeed;

    void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
        if (animator == null) return;

        // Calculate velocity based on actual movement in world space
        // This works on both Server (Agent) and Clients (NetworkTransform)
        Vector3 displacement = transform.position - lastPosition;
        displacement.y = 0; // Ignore vertical movement for walking speed
        
        float velocity = displacement.magnitude / Time.deltaTime;
        lastPosition = transform.position;

        // Smooth the speed value slightly to prevent animation jitter
        currentSpeed = Mathf.Lerp(currentSpeed, velocity, Time.deltaTime * 10f);
        
        animator.SetFloat("Speed", currentSpeed);
    }
}
