using UnityEngine;
using Mirror;

/// <summary>
/// Diwata launches magical flower petals in a spiral projectile pattern.
/// Phase scaling increases the number of petals spawned.
/// Triggers vulnerability counter on completion.
/// </summary>
public class PetalBarrageAttack : BaseAttack
{
    public const string Event_SpawnPetals = "SpawnPetals";
    public const string Event_FirePetals = "FirePetals";

    [Header("Petal Settings")]
    public DiwataPetal petalPrefab;
    public Transform spawnOrigin;
    public LayerMask playerLayer;

    [Header("Pattern")]
    public float petalSpeed = 8f;
    public float petalDamage = 12f;
    public float petalLifetime = 5f;
    public float spiralSpread = 30f; // degrees between each petal in spiral

    [Header("Phase Scaling")]
    public int phase1Petals = 6;
    public int phase2Petals = 10;
    public int phase3Petals = 16;

    private BossPhaseManager phaseManager;
    private DiwataVulnerabilityManager vulnerabilityManager;

    public override void Initialize(BossController bossController)
    {
        base.Initialize(bossController);
        phaseManager = bossController != null ? bossController.GetComponent<BossPhaseManager>() : null;
        vulnerabilityManager = bossController != null ? bossController.GetComponent<DiwataVulnerabilityManager>() : null;
    }

    public override void Server_Execute()
    {
        // Animation-driven
    }

    public override void Server_OnAnimationEvent(string eventName)
    {
        if (eventName == Event_FirePetals)
        {
            int count = Server_GetPetalCount();
            Server_SpawnSpiralPetals(count);
            Rpc_OnPetalsFired();

            // Count toward vulnerability
            if (vulnerabilityManager != null)
                vulnerabilityManager.Server_OnAttackCompleted();
        }
    }

    int Server_GetPetalCount()
    {
        int idx = phaseManager != null ? phaseManager.GetCurrentPhaseIndex() : 0;
        return idx switch
        {
            0 => phase1Petals,
            1 => phase2Petals,
            _ => phase3Petals,
        };
    }

    [Server]
    void Server_SpawnSpiralPetals(int count)
    {
        if (petalPrefab == null) return;

        Transform origin = spawnOrigin != null ? spawnOrigin : transform;
        float angleStep = spiralSpread;
        float startAngle = Random.Range(0f, 360f);

        for (int i = 0; i < count; i++)
        {
            float angle = (startAngle + angleStep * i) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

            DiwataPetal petal = Instantiate(petalPrefab, origin.position, Quaternion.identity);
            petal.Server_Initialize(
                owner: boss != null ? boss.netIdentity : null,
                direction: dir,
                speed: petalSpeed,
                damage: petalDamage,
                lifetime: petalLifetime,
                playerLayer: playerLayer
            );

            NetworkServer.Spawn(petal.gameObject);
        }
    }

    [ClientRpc]
    void Rpc_OnPetalsFired()
    {
        Debug.Log("[PetalBarrage] Petals fired!");
    }

    public override void Server_Stop()
    {
        // Nothing persistent to stop
    }
}
