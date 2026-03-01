using UnityEngine;
using UnityEngine.AI;
using Mirror;

public class EnemyAnimationController : NetworkBehaviour
{
    public Animator animator;
    public NavMeshAgent agent;

    void Update()
    {
        if (animator != null && agent != null)
        {
            // Syncs the animation based on the actual physical speed of the agent
            animator.SetFloat("Speed", agent.velocity.magnitude);
        }
    }
}
