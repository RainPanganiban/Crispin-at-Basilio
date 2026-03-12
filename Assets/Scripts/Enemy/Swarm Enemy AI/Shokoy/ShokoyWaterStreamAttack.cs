using UnityEngine;
using Mirror;
using System.Collections;

public class ShokoyWaterStreamAttack : EnemyAttack
{
    [Header("Water Stream Settings")]
    public GameObject waterProjectilePrefab;
    public float directDamage = 5f;
    public float projectileSpeed = 10f;
    public float arcHeight = 2f; 
    public int projectilesToFire = 3;
    public float delayBetweenShots = 0.3f;
    public Transform firePoint;

    [Header("Animation")]
    public NetworkAnimator networkAnimator;
    public string attackTrigger = "WaterStream";

    private EnemyAggro aggroSystem;

    void Awake()
    {
        aggroSystem = GetComponent<EnemyAggro>();
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
    public void FireStreamEvent()
    {
        StartCoroutine(FireStreamRoutine());
    }

    private IEnumerator FireStreamRoutine()
    {
        for (int i = 0; i < projectilesToFire; i++)
        {
            Transform target = aggroSystem != null ? aggroSystem.GetCurrentTarget() : null;
            if (target != null)
            {
                SpawnWaterProjectile(target.position);
            }
            yield return new WaitForSeconds(delayBetweenShots);
        }
    }

    [Server]
    private void SpawnWaterProjectile(Vector3 targetPosition)
    {
        if (waterProjectilePrefab == null) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + transform.forward + Vector3.up;

        GameObject projectile = Instantiate(waterProjectilePrefab, spawnPos, Quaternion.identity);
        
        ShokoyWaterProjectile script = projectile.GetComponent<ShokoyWaterProjectile>();
        if(script != null)
        {
            script.InitializeArc(targetPosition, arcHeight, projectileSpeed, directDamage, GetComponent<Collider>(), GetComponent<NetworkIdentity>());
        }

        NetworkServer.Spawn(projectile);
    }
}
