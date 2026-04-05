using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

public class RangedAttack : NetworkBehaviour, ICombatHandler
{
    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;

    [Header("Aiming")]
    [SerializeField] private LayerMask aimLayerMask = ~0;

    [Header("Charge Settings")]
    [SerializeField] public float maxChargeTime = 2f;
    [SerializeField] private float minDamage = 10f;
    [SerializeField] private float maxDamage = 40f;
    [SerializeField] private float minSpeed = 10f;
    [SerializeField] private float maxSpeed = 30f;
    [SerializeField] private float maxLifetime = 5f;

    [Header("Anti-Spam Settings")]
    [SerializeField] private float attackCooldown = 0.5f; // Oras bago makatira ulit
    private float nextAttackTime = 0f;

    private float currentCharge;
    private bool isCharging;

    private PlayerMovement movement;
    private Camera playerCamera;
    private ThirdPersonCamera tpCamera;
    private CrispinAnimation crispinAnimation;
    private PlayerStatsManager statsManager;

    public override void OnStartLocalPlayer()
    {
        movement = GetComponent<PlayerMovement>();
        tpCamera = GetComponent<ThirdPersonCamera>() ?? GetComponentInChildren<ThirdPersonCamera>();

        if (tpCamera == null)
            Debug.LogError("[RangedAttack] ThirdPersonCamera NOT FOUND!");

        playerCamera = GetComponentInChildren<Camera>() ?? Camera.main;
        if (playerCamera == null) playerCamera = FindFirstObjectByType<Camera>();

        crispinAnimation = GetComponent<CrispinAnimation>();
        statsManager = GetComponent<PlayerStatsManager>();
    }

    void Update()
    {
        if (!isLocalPlayer || playerCamera == null || !isCharging) return;

        currentCharge = Mathf.Min(currentCharge + Time.deltaTime, maxChargeTime);

        // Rotate player to camera forward
        Vector3 aimDir = playerCamera.transform.forward;
        aimDir.y = 0f;

        if (aimDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(aimDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 12f * Time.deltaTime);
        }
    }

    public void OnLightAttack(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer) return;

        if (context.started)
        {
            // Eto yung pang-harang sa spam
            if (Time.time >= nextAttackTime)
            {
                StartCharge();
            }
        }
        else if (context.canceled)
        {
            ReleaseCharge();
        }
    }

    void StartCharge()
    {
        isCharging = true;
        currentCharge = 0f;

        if (tpCamera != null) tpCamera.SetAiming(true);
        if (movement != null) movement.isAiming = true;
        if (crispinAnimation != null) crispinAnimation.OnAttackStarted();
    }

    void ReleaseCharge()
    {
        if (!isCharging) return;

        isCharging = false;

        // I-set ang susunod na pwedeng attack time
        nextAttackTime = Time.time + attackCooldown;

        if (tpCamera != null) tpCamera.SetAiming(false);
        if (movement != null) movement.isAiming = false;
        if (crispinAnimation != null) crispinAnimation.OnAttackReleased();

        ExecuteFire();
    }

    private void ExecuteFire()
    {
        float chargePercent = Mathf.Clamp01(currentCharge / maxChargeTime);
        float bonus = statsManager != null ? statsManager.BonusAttackDamage : 0f;

        float damage = Mathf.Lerp(minDamage, maxDamage, chargePercent) + bonus;
        float speed = Mathf.Lerp(minSpeed, maxSpeed, chargePercent);
        float lifetime = Mathf.Lerp(1.5f, maxLifetime, chargePercent);

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint = Physics.Raycast(ray, out RaycastHit hit, 100f, aimLayerMask) ? hit.point : ray.GetPoint(100f);
        Vector3 direction = (targetPoint - firePoint.position).normalized;

        CmdFireProjectile(Mathf.RoundToInt(damage), speed, lifetime, direction);
    }

    [Command]
    void CmdFireProjectile(int damage, float speed, float lifetime, Vector3 direction)
    {
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));

        if (proj.TryGetComponent(out Projectile projectile))
        {
            projectile.Initialize(
                damage,
                speed,
                lifetime,
                direction,
                GetComponent<Collider>(),
                netIdentity
            );
        }

        NetworkServer.Spawn(proj);
    }
}