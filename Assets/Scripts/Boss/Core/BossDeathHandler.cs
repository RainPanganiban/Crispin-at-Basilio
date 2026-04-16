using UnityEngine;
using Mirror;

public class BossDeathHandler : NetworkBehaviour
{
    /// <summary>
    /// Fired on the server when any boss dies. LevelCompleteManager subscribes to this.
    /// </summary>
    public static event System.Action OnBossDefeated;

    [Header("Death Settings")]
    public float delayBeforeDestroy = 5f;
    public GameObject deathVfxPrefab;
    public Transform vfxSpawnPoint;

    private BossController controller;
    private Collider[] bossColliders;

    void Awake()
    {
        controller = GetComponent<BossController>();
        bossColliders = GetComponentsInChildren<Collider>();
    }

    [Server]
    public void Server_OnBossDeath()
    {
        // 1. Drop Coins (Idinagdag na logic)
        if (TryGetComponent<EnemyCoinDrop>(out var coinDrop))
        {
            coinDrop.DropCoins();
        }

        // Disable movement
        if (TryGetComponent<BossMovementBase>(out var movement))
        {
            movement.Server_SetMovementEnabled(false);
        }

        // Disable colliders so players can walk through
        foreach (var col in bossColliders)
        {
            col.enabled = false;
        }

        // Notify clients to play death effects
        Rpc_OnDeath();

        // Spawn death VFX on server
        if (deathVfxPrefab != null)
        {
            GameObject vfx = Instantiate(deathVfxPrefab, vfxSpawnPoint ? vfxSpawnPoint.position : transform.position, Quaternion.identity);
            NetworkServer.Spawn(vfx);
        }

        // Notify level-complete system
        OnBossDefeated?.Invoke();

        // Cleanup after delay
        Invoke(nameof(Server_Cleanup), delayBeforeDestroy);
    }

    [ClientRpc]
    void Rpc_OnDeath()
    {
        // Trigger client-side only death effects (screen fade, music change, etc)
        Debug.Log("[BossDeathHandler] Boss has died. Triggering client-side effects.");
    }

    [Server]
    void Server_Cleanup()
    {
        NetworkServer.Destroy(gameObject);
    }
}