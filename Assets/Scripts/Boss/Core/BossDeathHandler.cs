using UnityEngine;
using Mirror;

public class BossDeathHandler : NetworkBehaviour
{
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
        // Disable AI think loop via controller state (already handled by controller.state = Dead)
        
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
