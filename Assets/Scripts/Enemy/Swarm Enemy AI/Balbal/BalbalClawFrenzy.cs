using UnityEngine;
using Mirror;

public class BalbalClawFrenzy : EnemyAttack
{
    [Header("Claw Frenzy Settings")]
    public GameObject waveProjectilePrefab;
    public float projectileDamage = 10f;
    public float projectileSpeed = 12f;
    public float projectileLifetime = 3f;
    public Transform waveSpawnPoint;
    
    [Header("Animation")]
    public NetworkAnimator networkAnimator; 
    public string attackTrigger = "ClawFrenzy";

    void Awake()
    {
        if (networkAnimator == null) 
        {
            networkAnimator = GetComponent<NetworkAnimator>();
            if (networkAnimator == null) networkAnimator = GetComponentInParent<NetworkAnimator>();
        }
    }

    protected override void OnExecute()
    {
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
    }

    [ServerCallback]
    public void FireWave1Event()
    {
        SpawnWave();
    }

    [ServerCallback]
    public void FireWave2Event()
    {
        SpawnWave();
    }

    [Server]
    private void SpawnWave()
    {
        if (waveProjectilePrefab == null) return;

        Vector3 spawnPos = waveSpawnPoint != null ? waveSpawnPoint.position : transform.position + transform.forward + Vector3.up;

        GameObject wave = Instantiate(waveProjectilePrefab, spawnPos, transform.rotation);
        
        EnemyProjectile proj = wave.GetComponent<EnemyProjectile>();
        if (proj != null)
        {
            proj.Initialize(
                Mathf.RoundToInt(projectileDamage), 
                projectileSpeed, 
                projectileLifetime, 
                transform.forward, 
                GetComponent<Collider>(), 
                GetComponent<NetworkIdentity>()
            );
        }

        NetworkServer.Spawn(wave);
    }
}
