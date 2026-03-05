using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

public class RangedAttack : NetworkBehaviour, ICombatHandler
{
    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;

    [Header("Aiming")]
    [SerializeField] private LayerMask aimLayerMask = ~0; // Everything by default

    [Header("Charge Settings")]
    [SerializeField] public float maxChargeTime = 2f;
    [SerializeField] private float minDamage = 10f;
    [SerializeField] private float maxDamage = 40f;
    [SerializeField] private float minSpeed = 10f;
    [SerializeField] private float maxSpeed = 30f;
    [SerializeField] private float maxLifetime = 5f;

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
        

        // Locate ThirdPersonCamera
        tpCamera = GetComponent<ThirdPersonCamera>();
        if (tpCamera == null)
        {
            tpCamera = GetComponentInChildren<ThirdPersonCamera>();
        }
        
        if (tpCamera == null)
        {
            Debug.LogError("[RangedAttack] ThirdPersonCamera NOT FOUND on Player root or children!");
        }
        else
        {
            Debug.Log("[RangedAttack] ThirdPersonCamera successfully found!");
        }

        // Locate Camera
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null) playerCamera = FindFirstObjectByType<Camera>();
        }

        crispinAnimation = GetComponent<CrispinAnimation>();
        statsManager = GetComponent<PlayerStatsManager>();
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null) playerCamera = Camera.main;
            if (playerCamera == null)
                return;
        }

        if (tpCamera == null)
        {
            tpCamera = GetComponent<ThirdPersonCamera>();
            if (tpCamera == null) tpCamera = GetComponentInChildren<ThirdPersonCamera>();
        }

        if (!isCharging) return;

        currentCharge += Time.deltaTime;
        currentCharge = Mathf.Clamp(currentCharge, 0f, maxChargeTime);

        // Rotate player to camera forward
        Vector3 aimDir = playerCamera.transform.forward;
        aimDir.y = 0f;

        if (aimDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(aimDir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                12f * Time.deltaTime
            );
        }
    }

    public void OnLightAttack(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer) return;

        // The PlayerControls asset should have the "Attack" action's Interactions set to "Hold"
        if (context.started)
            StartCharge();
        else if (context.canceled)
            ReleaseCharge();
    }

    void StartCharge()
    {
        isCharging = true;
        currentCharge = 0f;

        if (tpCamera != null)
        {
            Debug.Log("[RangedAttack] Telling tpCamera to SetAiming(true)");
            tpCamera.SetAiming(true);
        }
        else
        {
            Debug.LogWarning("[RangedAttack] Cannot aim: tpCamera is NULL!");
        }

        movement.isAiming = true;

        // Tell the animation controller to start the wind-up animation
        if (crispinAnimation != null)
        {
            crispinAnimation.OnAttackStarted();
        }
        if (movement == null)
            movement = GetComponent<PlayerMovement>();
        if (movement != null)
            movement.isAiming = true;
    }

    void ReleaseCharge()
    {
        // Don't release if we weren't charging in the first place
        if (!isCharging) return;

        isCharging = false;
        
        if (tpCamera != null)
        {
            Debug.Log("[RangedAttack] Telling tpCamera to SetAiming(false)");
            tpCamera.SetAiming(false);
        }

        if (movement == null)
            movement = GetComponent<PlayerMovement>();
        if (movement != null)
            movement.isAiming = false;

        // Tell the animation controller to play the release animation
        if (crispinAnimation != null)
        {
            crispinAnimation.OnAttackReleased();
        }

        float chargePercent = Mathf.Clamp01(currentCharge / maxChargeTime);

        float bonus = statsManager != null ? statsManager.BonusAttackDamage : 0f;
        float damage = Mathf.Lerp(minDamage, maxDamage, chargePercent) + bonus;
        float speed = Mathf.Lerp(minSpeed, maxSpeed, chargePercent);
        float lifetime = Mathf.Lerp(1.5f, maxLifetime, chargePercent);

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint;

        // Raycast from camera center to find exactly what the crosshair is looking at
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, aimLayerMask))
        {
            targetPoint = hit.point;
        }
        else
        {
            // If we didn't hit anything, pick a point far away in the air
            targetPoint = ray.GetPoint(100f);
        }

        // Projectile direction is exactly from the hand to the hit point
        Vector3 direction = (targetPoint - firePoint.position).normalized;

        CmdFireProjectile(
            Mathf.RoundToInt(damage),
            speed,
            lifetime,
            direction
        );
    }

    [Command]
    void CmdFireProjectile(int damage, float speed, float lifetime, Vector3 direction)
    {
        GameObject proj = Instantiate(
            projectilePrefab,
            firePoint.position,
            Quaternion.LookRotation(direction)
        );

        Projectile projectile = proj.GetComponent<Projectile>();
        Collider ownerCollider = GetComponent<Collider>();
        NetworkIdentity ownerId = netIdentity; // add this

        projectile.Initialize(
            damage,
            speed,
            lifetime,
            direction,
            ownerCollider,
            ownerId // pass the owner
        );

        NetworkServer.Spawn(proj);
    }
}
