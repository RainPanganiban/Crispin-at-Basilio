using UnityEngine;
using Mirror;
using UnityEngine.AI;

public class ShokoyBubbleBurstAttack : EnemyAttack
{
    [Header("Bubble Burst Settings")]
    public GameObject bubblePrefab;
    public int bubbleCount = 4;
    public float spawnRadius = 5f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip burstCastClip; // Tunog ng Shokoy habang nag-a-attack
    [SerializeField] private AudioClip bubbleSpawnClip; // Tunog ng bawat bubble na sumisulpot
    private EnemySoundManager soundManager;

    [Header("Animation")]
    public NetworkAnimator networkAnimator;
    public string attackTrigger = "BubbleBurst";

    private EnemyAggro aggroSystem;

    void Awake()
    {
        aggroSystem = GetComponent<EnemyAggro>();
        soundManager = GetComponent<EnemySoundManager>();

        if (networkAnimator == null)
        {
            networkAnimator = GetComponent<NetworkAnimator>();
            if (networkAnimator == null) networkAnimator = GetComponentInParent<NetworkAnimator>();
        }
    }

    protected override void OnExecute()
    {
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (networkAnimator != null)
        {
            networkAnimator.SetTrigger(attackTrigger);

            // Tawagin ang corrected RPC
            RpcPlayCastSound();
        }
    }

    [ClientRpc]
    private void RpcPlayCastSound()
    {
        // Gagamit na lang ng direct reference sa burstCastClip variable
        if (soundManager != null && burstCastClip != null)
        {
            soundManager.PlaySpecificAttack(burstCastClip);
        }
    }

    [ServerCallback]
    public void SpawnBubblesEvent()
    {
        Transform target = aggroSystem != null ? aggroSystem.GetCurrentTarget() : null;
        if (target == null) return;

        for (int i = 0; i < bubbleCount; i++)
        {
            Vector3 randomOffset = Random.insideUnitSphere * spawnRadius;
            randomOffset.y = 0f;

            Vector3 spawnPos = target.position + randomOffset;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(spawnPos, out hit, 2f, NavMesh.AllAreas))
            {
                spawnPos = hit.position;
            }

            GameObject bubble = Instantiate(bubblePrefab, spawnPos, Quaternion.identity);
            NetworkServer.Spawn(bubble);

            // Tawagin ang corrected RPC (Vector3 ay okay i-pass sa Mirror)
            RpcPlayBubbleSpawnSound(spawnPos);

            ShokoyBubble script = bubble.GetComponent<ShokoyBubble>();
            if (script != null)
            {
                script.Initialize(GetComponent<Collider>());
            }
        }
    }

    [ClientRpc]
    private void RpcPlayBubbleSpawnSound(Vector3 position)
    {
        // Gagamit na lang ng direct reference sa bubbleSpawnClip variable
        if (bubbleSpawnClip != null)
        {
            float vol = GetVolume();
            AudioSource.PlayClipAtPoint(bubbleSpawnClip, position, vol);
        }
    }

    private float GetVolume()
    {
        if (SoundManager.Instance != null)
            return SoundManager.Instance.EffectiveSFXVolume;
        return 1f;
    }
}