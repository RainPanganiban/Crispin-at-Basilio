using UnityEngine;
using Mirror;

public class TyanakClawSwipe : EnemyAttack
{
    [Header("Claw Settings")]
    public float damage = 5f;
    public float hitRadius = 1.2f;
    public LayerMask playerLayer;
    public float targetVerticalOffset = 1.0f;
    public float hitForwardOffset = 1.0f;

    [Header("Audio Settings")]
    [Tooltip("I-assign dito ang swipe o slash sound.")]
    public AudioClip swipeSound;
    private EnemySoundManager soundManager;

    [Header("Animation")]
    public NetworkAnimator networkAnimator;
    public string attackTrigger = "ClawSwipe";

    void Awake()
    {
        soundManager = GetComponent<EnemySoundManager>();
        if (networkAnimator == null)
        {
            networkAnimator = GetComponent<NetworkAnimator>();
            if (networkAnimator == null) networkAnimator = GetComponentInParent<NetworkAnimator>();
        }
    }

    protected override void OnExecute()
    {
        if (!isServer) return;

        Debug.Log($"[TyanakClawSwipe] Executing Claw Swipe on {name}");

        // Patigilin ang movement
        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (networkAnimator != null)
        {
            networkAnimator.SetTrigger(attackTrigger);
        }

        // I-sync ang sound sa lahat ng clients
        RpcPlaySwipeSound();
    }

    [ClientRpc]
    private void RpcPlaySwipeSound()
    {
        if (soundManager == null) soundManager = GetComponent<EnemySoundManager>();

        if (soundManager != null && swipeSound != null)
        {
            // PlayOneShot style attack sound
            soundManager.PlaySpecificAttack(swipeSound);
        }
    }

    [ServerCallback]
    public void ClawHitEvent()
    {
        Vector3 checkPos = transform.position + transform.forward * hitForwardOffset + Vector3.up * targetVerticalOffset;

        Collider[] hits = Physics.OverlapSphere(checkPos, hitRadius, playerLayer);

        foreach (Collider hit in hits)
        {
            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage, transform);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.5f);
        Vector3 checkPos = transform.position + transform.forward * hitForwardOffset + Vector3.up * targetVerticalOffset;
        Gizmos.DrawSphere(checkPos, hitRadius);
    }
}