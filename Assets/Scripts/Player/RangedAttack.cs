using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

public class RangedAttack : NetworkBehaviour, ICombatHandler
{
    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;

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
        isCharging = false;

        float chargePercent = currentCharge / maxChargeTime;

        float damage = Mathf.Lerp(minDamage, maxDamage, chargePercent);
        float speed = Mathf.Lerp(minSpeed, maxSpeed, chargePercent);
        float lifetime = Mathf.Lerp(1.5f, maxLifetime, chargePercent);

        CmdFireProjectile(damage, speed, lifetime);
    }

    [Command]
    void CmdFireProjectile(float damage, float speed, float lifetime)
    {
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        Projectile projectile = proj.GetComponent<Projectile>();

        // Use int damage for the Projectile class
        projectile.Initialize((int)damage, speed, lifetime, netIdentity);

        NetworkServer.Spawn(proj);
    }
}
