using UnityEngine;
using Mirror;

public class ProjectileAttack : EnemyAttack
{
    [Header("Projectile Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private int damage = 10;
    [SerializeField] private float projectileSpeed = 15f;
    [SerializeField] private float projectileLifetime = 5f;

    private Collider ownerCollider;
    private NetworkIdentity ownerIdentity;
    private EnemyAggro aggroSystem;

    void Awake()
    {
        ownerCollider = GetComponent<Collider>();
        ownerIdentity = netIdentity;
        aggroSystem = GetComponent<EnemyAggro>();
    }

    protected override void OnExecute()
    {
        if (projectilePrefab == null || firePoint == null)
        {
            Debug.LogWarning("ProjectileAttack missing projectilePrefab or firePoint");
            return;
        }

        // Default direction is forward from firePoint
        Vector3 direction = firePoint.forward;

        // If we have an aggro target, aim directly at it (including vertical offset)
        if (aggroSystem != null)
        {
            Transform target = aggroSystem.GetCurrentTarget();
            if (target != null)
            {
                Vector3 toTarget = (target.position - firePoint.position);
                if (toTarget.sqrMagnitude > 0.001f)
                {
                    direction = toTarget.normalized;
                }
            }
        }

        direction = direction.normalized;

        GameObject proj = Instantiate(
            projectilePrefab,
            firePoint.position,
            Quaternion.LookRotation(direction)
        );

        EnemyProjectile projectile = proj.GetComponent<EnemyProjectile>();

        if (projectile != null)
        {
            projectile.Initialize(
                damage,
                projectileSpeed,
                projectileLifetime,
                direction,
                ownerCollider,
                ownerIdentity
            );
        }

        NetworkServer.Spawn(proj);
    }
}

