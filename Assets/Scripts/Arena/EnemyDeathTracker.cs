using Mirror;
using UnityEngine;

/// <summary>
/// Lightweight component added at runtime by ArenaManager to each spawned enemy.
/// When the enemy GameObject is destroyed (via NetworkServer.Destroy in EnemyHealth.Die()),
/// this notifies the ArenaManager to decrement the alive count.
///
/// This avoids modifying existing enemy scripts.
/// </summary>
public class EnemyDeathTracker : MonoBehaviour
{
    private ArenaManager arenaManager;

    /// <summary>
    /// Called by ArenaManager immediately after adding this component.
    /// </summary>
    public void Initialize(ArenaManager manager)
    {
        arenaManager = manager;
    }

    void OnDestroy()
    {
        // Only the server tracks enemy deaths via this component
        if (arenaManager != null)
        {
            arenaManager.NotifyEnemyDead(gameObject);
        }
    }
}
