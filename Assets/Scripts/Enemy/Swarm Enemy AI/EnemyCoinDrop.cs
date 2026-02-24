using UnityEngine;
using Mirror;

/// <summary>
/// Attach to any enemy prefab. When DropCoins() is called on the server,
/// it spawns between minCoins and maxCoins coin pickups near the enemy's position.
/// </summary>
public class EnemyCoinDrop : NetworkBehaviour
{
    [Header("Drop Settings")]
    [Tooltip("Prefab that has CoinPickup + NetworkIdentity.")]
    public GameObject coinPrefab;

    [Tooltip("Minimum coins dropped on death.")]
    public int minCoins = 1;

    [Tooltip("Maximum coins dropped on death.")]
    public int maxCoins = 5;

    [Tooltip("How far from the death position coins are scattered.")]
    public float scatterRadius = 0.6f;

    // =========================================================
    // Public API
    // =========================================================

    /// <summary>Called on the server when this enemy dies.</summary>
    [Server]
    public void DropCoins()
    {
        if (coinPrefab == null)
        {
            Debug.LogWarning($"[EnemyCoinDrop] No coin prefab assigned on {gameObject.name}!");
            return;
        }

        int amount = Random.Range(minCoins, maxCoins + 1);

        for (int i = 0; i < amount; i++)
        {
            // Random scatter around the death position
            Vector2 scatter = Random.insideUnitCircle * scatterRadius;
            Vector3 spawnPos = transform.position + new Vector3(scatter.x, 0.3f, scatter.y);

            GameObject coin = Instantiate(coinPrefab, spawnPos, Quaternion.identity);
            NetworkServer.Spawn(coin);
        }
    }
}
