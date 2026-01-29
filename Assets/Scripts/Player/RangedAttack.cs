using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

public class RangedAttack : NetworkBehaviour, ICombatHandler
{
    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;

    [SerializeField] private Camera playerCamera;
    [SerializeField] private float aimMaxDistance = 100f;

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

    void Update()
    {
        if (!isLocalPlayer) return;

        if (isCharging)
        {
            currentCharge += Time.deltaTime;
            currentCharge = Mathf.Clamp(currentCharge, 0f, maxChargeTime);
        }

        float targetFOV = isCharging ? chargedFOV : normalFOV;
        playerCamera.fieldOfView = Mathf.Lerp(
            playerCamera.fieldOfView,
            targetFOV,
            Time.deltaTime * zoomSpeed
        );
    }

    Vector3 GetAimDirection()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, aimMaxDistance))
        {
            return (hit.point - firePoint.position).normalized;
        }

        // Fallback: shoot straight
        return playerCamera.transform.forward;
    }

    public void OnLightAttack(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer) return;

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
    }

    void ReleaseCharge()
    {
        if (!isLocalPlayer) return;

        isCharging = false;

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
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));
        Projectile projectile = proj.GetComponent<Projectile>();

        // Pass the direction to Initialize
        projectile.Initialize(damage, speed, lifetime, direction, netIdentity);

        NetworkServer.Spawn(proj);
    }
}
