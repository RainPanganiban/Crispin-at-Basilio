using UnityEngine;
using Mirror; // Required for NetworkBehaviour and isServer

public class BiteAttack : NetworkBehaviour
{
    public float damage = 30f;

    [Header("Visual Debugging")]
    public bool showDebugGizmo = true;
    public Color gizmoColor = Color.red;

    // This handles the actual damage logic
    private void OnTriggerEnter(Collider other)
    {
        // Only the server should calculate and apply damage
        if (!isServer) return;

        if (other.CompareTag("Player"))
        {
            PlayerStatsManager playerStats = other.GetComponent<PlayerStatsManager>();
            if (playerStats != null)
            {
                // Applying damage and passing the boss's root transform as the attacker
                playerStats.TakeDamage(damage, transform.root);
                Debug.Log($"Bite connected! Damage dealt: {damage}");
            }
        }
    }

#if UNITY_EDITOR
    // Visualizes the mouth's hitbox in the Scene view
    private void OnDrawGizmos()
    {
        if (!showDebugGizmo) return;

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = gizmoColor;
            if (col is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(transform.position, sphere.radius);
            }
            else if (col is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.center, box.size);
                Gizmos.matrix = Matrix4x4.identity;
            }
        }
    }
#endif
}