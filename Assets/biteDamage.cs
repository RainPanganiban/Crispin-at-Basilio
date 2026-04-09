using UnityEngine;
using Mirror;

public class biteDamage : NetworkBehaviour
{
    public float damage = 30f;

    // Optional: Visual feedback
    public bool showDebugGizmo = true;
    public Color gizmoColor = Color.red;

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        if (other.CompareTag("Player"))
        {
            PlayerStatsManager playerStats = other.GetComponent<PlayerStatsManager>();
            if (playerStats != null)
            {
                playerStats.TakeDamage(damage, transform.root); // Send root as attacker
                Debug.Log($"Boss mouth hit player! Damage: {damage}");
            }
        }
    }

#if UNITY_EDITOR
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