using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

public class RangedAttack : NetworkBehaviour, ICombatHandler
{
    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;

    [Header("Charge Zoom")]
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float chargedFOV = 45f;
    [SerializeField] private float zoomSpeed = 8f;

    [Header("Charge Settings")]
    [SerializeField] private float maxChargeTime = 2f;
    [SerializeField] private float minDamage = 10f;
    [SerializeField] private float maxDamage = 40f;
    [SerializeField] private float minSpeed = 10f;
    [SerializeField] private float maxSpeed = 30f;
    [SerializeField] private float maxLifetime = 5f;

    private float currentCharge;
    private bool isCharging;

    private PlayerMovement movement;
    private Camera playerCamera;

    public override void OnStartLocalPlayer()
    {
        movement = GetComponent<PlayerMovement>();
        playerCamera = GetComponentInChildren<Camera>();
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        // Camera zoom
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null)
                return;
        }

        float targetFOV = isCharging ? chargedFOV : normalFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, zoomSpeed * Time.deltaTime);

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

        if (context.started)
            StartCharge();
        else if (context.canceled)
            ReleaseCharge();
    }

    void StartCharge()
    {
        isCharging = true;
        currentCharge = 0f;

        if (movement == null)
            movement = GetComponent<PlayerMovement>();
        if (movement != null)
            movement.isAiming = true;
    }

    void ReleaseCharge()
    {
        isCharging = false;
        if (movement == null)
            movement = GetComponent<PlayerMovement>();
        if (movement != null)
            movement.isAiming = false;

        float chargePercent = Mathf.Clamp01(currentCharge / maxChargeTime);

        float damage = Mathf.Lerp(minDamage, maxDamage, chargePercent);
        float speed = Mathf.Lerp(minSpeed, maxSpeed, chargePercent);
        float lifetime = Mathf.Lerp(1.5f, maxLifetime, chargePercent);

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 direction = ray.direction.normalized;

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
