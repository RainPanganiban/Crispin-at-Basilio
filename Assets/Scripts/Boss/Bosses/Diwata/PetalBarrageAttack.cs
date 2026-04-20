using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class PetalBarrageAttack : BaseAttack
{
    public const string Event_SpawnPetals = "SpawnPetals";
    public const string Event_FirePetals = "FirePetals";

    [Header("Petal Settings")]
    public DiwataPetal petalPrefab;
    [Tooltip("Dito ilalagay ang mga Transforms kung saan pwedeng lumabas ang petals.")]
    public List<Transform> spawnOrigins = new List<Transform>();
    public LayerMask playerLayer;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip spawnPetalsClip;
    [SerializeField] private AudioClip firePetalsClip;

    [Header("Multi-Spawn Logic")]
    [Tooltip("Ilan sa mga spawn points ang gagamitim nang sabay-sabay?")]
    [Range(1, 5)]
    public int spawnPointCountToUse = 1;

    [Header("Pattern")]
    public float petalSpeed = 8f;
    public float petalDamage = 12f;
    public float petalLifetime = 5f;
    public float spiralSpread = 30f;

    [Header("Phase Scaling")]
    public int phase1Petals = 6;
    public int phase2Petals = 10;
    public int phase3Petals = 16;

    private BossPhaseManager phaseManager;
    private DiwataVulnerabilityManager vulnerabilityManager;
    private EnemySoundManager soundManager;

    public override void Initialize(BossController bossController)
    {
        base.Initialize(bossController);
        phaseManager = bossController != null ? bossController.GetComponent<BossPhaseManager>() : null;
        vulnerabilityManager = bossController != null ? bossController.GetComponent<DiwataVulnerabilityManager>() : null;
        soundManager = bossController != null ? bossController.GetComponent<EnemySoundManager>() : null;
    }

    public override bool Server_CanExecute()
    {
        if (!isServer || boss == null || !NetworkServer.active) return false;

        Transform targetPlayer = Server_FindClosestPlayer();
        if (targetPlayer != null)
        {
            float dist = Vector3.Distance(transform.position, targetPlayer.position);
            return dist <= maxRange;
        }
        return false;
    }

    public override void Server_Execute()
    {
        if (boss != null && !string.IsNullOrEmpty(animationTriggerName))
        {
            boss.Server_PlayTrigger(animationTriggerName);
        }
    }

    public override void Server_OnAnimationEvent(string eventName)
    {
        // Play sound kapag nag-start na ang preparation
        if (eventName == Event_SpawnPetals)
        {
            Rpc_PlaySpawnSound();
        }

        if (eventName == Event_FirePetals)
        {
            int totalPetalCount = Server_GetPetalCount();
            List<Transform> selectedOrigins = Server_GetRandomSpawnPoints();

            foreach (Transform origin in selectedOrigins)
            {
                int petalsPerPoint = totalPetalCount / selectedOrigins.Count;
                Server_SpawnSpiralPetals(origin, petalsPerPoint);
            }

            if (NetworkServer.active)
            {
                Rpc_OnPetalsFired();
            }

            if (vulnerabilityManager != null)
                vulnerabilityManager.Server_OnAttackCompleted();
        }
    }

    // --- HELPER METHODS (Dito galing ang errors mo kanina) ---

    List<Transform> Server_GetRandomSpawnPoints()
    {
        List<Transform> picked = new List<Transform>();

        if (spawnOrigins == null || spawnOrigins.Count == 0)
        {
            picked.Add(transform);
            return picked;
        }

        List<Transform> pool = new List<Transform>(spawnOrigins);
        int countToPick = Mathf.Min(spawnPointCountToUse, pool.Count);

        for (int i = 0; i < countToPick; i++)
        {
            int randomIndex = Random.Range(0, pool.Count);
            picked.Add(pool[randomIndex]);
            pool.RemoveAt(randomIndex);
        }

        return picked;
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
    void Server_SpawnSpiralPetals(Transform origin, int count)
    {
        if (petalPrefab == null) return;

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

    Transform Server_FindClosestPlayer()
    {
        Transform best = null;
        float bestSqr = float.PositiveInfinity;

        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn == null || conn.identity == null) continue;
            float d = (conn.identity.transform.position - transform.position).sqrMagnitude;
            if (d < bestSqr)
            {
                bestSqr = d;
                best = conn.identity.transform;
            }
        }
        return best;
    }

    // --- NETWORKING / RPCs ---

    [ClientRpc]
    void Rpc_PlaySpawnSound()
    {
        if (soundManager != null && spawnPetalsClip != null)
            soundManager.PlaySpecificAttack(spawnPetalsClip);
    }

    [ClientRpc]
    void Rpc_OnPetalsFired()
    {
        if (soundManager != null && firePetalsClip != null)
            soundManager.PlaySpecificAttack(firePetalsClip);

        Debug.Log("[PetalBarrage] Petals fired from multiple points!");
    }

    public override void Server_Stop() { } // Para mawala ang CS0534 error
}