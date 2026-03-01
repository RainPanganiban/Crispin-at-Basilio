using UnityEngine;
using Mirror;
using System.Collections;

public class TiktikBrain : EnemyBrain
{
    public enum TiktikState
    {
        HoverPatrol,
        RangedAttack,
        Reposition,
        SwoopAttack,
        Recover,
        Dead
    }

    [Header("Tiktik References")]
    public ProjectileAttack projectileAttack;

    [Header("Hover Patrol")]
    public float orbitRadius = 7f;
    public float verticalWaveAmplitude = 0.4f;
    public float verticalWaveFreq = 2f;
    public float hoverMinDuration = 2f;
    public float hoverMaxDuration = 4f;
    public float maxTimeAbovePlayer = 2f;
    [Range(0f, 1f)]
    public float swoopRandomChance = 0.15f;
    public float swoopCheckInterval = 3f;

    [Header("Ranged Attack")]
    public float shotDelayMin = 1.2f;
    public float shotDelayMax = 1.8f;

    [Header("Reposition")]
    public float repositionDurationMin = 0.5f;
    public float repositionDurationMax = 1.2f;
    public float repositionDashSpeed = 10f;
    public float minDistanceFromPlayer = 4f;

    [Header("Swoop Attack")]
    public float swoopTelegraphDuration = 0.4f;
    public float swoopSpeed = 18f;
    public float swoopDamage = 6f;
    public float swoopCooldown = 5f;
    public LayerMask playerLayer;

    [Header("Recover")]
    public float recoverDuration = 0.8f;
    public float recoverUpwardSpeed = 8f;
    public float safeAltitudeOffset = 4f;

    private TiktikState currentTiktikState;
    private Transform target;
    private float stateTimer;
    private float orbitAngle;
    private float timeDirectlyAbove;
    private float lastSwoopCheckTime;
    private float lastSwoopTime = -999f;
    private Vector3 swoopTargetPos;
    private Vector3 repositionDirection;
    private bool swoopDamageApplied;
    private bool rangedShotFired;

    void Start()
    {
        if (!isServer) return;
        if (aggroSystem == null) aggroSystem = GetComponent<EnemyAggro>();
        if (projectileAttack == null) projectileAttack = GetComponent<ProjectileAttack>();
        currentTiktikState = TiktikState.HoverPatrol;
        stateTimer = Random.Range(hoverMinDuration, hoverMaxDuration);
        orbitAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
    }

    void Update()
    {
        if (!isServer || currentTiktikState == TiktikState.Dead) return;

        target = aggroSystem != null ? aggroSystem.GetCurrentTarget() : null;
        if (target == null)
        {
            return;
        }

        switch (currentTiktikState)
        {
            case TiktikState.HoverPatrol:
                HandleHoverPatrol();
                break;
            case TiktikState.RangedAttack:
                HandleRangedAttack();
                break;
            case TiktikState.Reposition:
                HandleReposition();
                break;
            case TiktikState.SwoopAttack:
                HandleSwoopAttack();
                break;
            case TiktikState.Recover:
                HandleRecover();
                break;
        }
    }

    void HandleHoverPatrol()
    {
        stateTimer -= Time.deltaTime;

        float playerY = target.position.y;
        float baseAltitude = playerY + safeAltitudeOffset;
        float waveY = Mathf.Sin(Time.time * verticalWaveFreq) * verticalWaveAmplitude;

        Vector3 orbitCenter = new Vector3(target.position.x, baseAltitude, target.position.z);
        orbitAngle += (2f * Mathf.PI / 8f) * Time.deltaTime;

        Vector3 offset = new Vector3(Mathf.Cos(orbitAngle), 0f, Mathf.Sin(orbitAngle)) * orbitRadius;
        Vector3 idealPos = orbitCenter + offset + Vector3.up * waveY;

        Vector3 toPlayer = (target.position - transform.position).normalized;
        toPlayer.y = 0;
        bool directlyAbove = toPlayer.sqrMagnitude < 0.01f || Vector3.Dot((transform.position - target.position).normalized, Vector3.down) > 0.9f;

        if (directlyAbove)
        {
            timeDirectlyAbove += Time.deltaTime;
            if (timeDirectlyAbove > maxTimeAbovePlayer)
            {
                orbitAngle += 90f * Mathf.Deg2Rad;
                timeDirectlyAbove = 0f;
            }
        }
        else
        {
            timeDirectlyAbove = 0f;
        }

        transform.position = Vector3.MoveTowards(transform.position, idealPos, 6f * Time.deltaTime);
        FaceTarget();

        if (stateTimer <= 0f)
        {
            if (Time.time >= lastSwoopCheckTime + swoopCheckInterval && Time.time >= lastSwoopTime + swoopCooldown && Random.value < swoopRandomChance)
            {
                currentTiktikState = TiktikState.SwoopAttack;
                stateTimer = swoopTelegraphDuration;
                swoopTargetPos = target.position;
                swoopDamageApplied = false;

                // Trigger Swoop Animation
                NetworkAnimator netAnim = GetComponent<NetworkAnimator>();
                if (netAnim != null)
                {
                    netAnim.SetTrigger("SwoopAttack");
                }
            }
            else
            {
                currentTiktikState = TiktikState.RangedAttack;
                stateTimer = Random.Range(shotDelayMin, shotDelayMax);
                rangedShotFired = false;
            }
            lastSwoopCheckTime = Time.time;
        }
    }

    void HandleRangedAttack()
    {
        stateTimer -= Time.deltaTime;
        FaceTarget();

        if (stateTimer <= 0f && !rangedShotFired)
        {
            rangedShotFired = true;
            if (projectileAttack != null && projectileAttack.CanUse())
            {
                projectileAttack.Execute();
            }
            else
            {
                OnAttackFinished();
            }
        }
    }

    public override void OnAttackFinished()
    {
        currentTiktikState = TiktikState.Reposition;
        stateTimer = Random.Range(repositionDurationMin, repositionDurationMax);
        Vector3 toPlayer = (target.position - transform.position).normalized;
        toPlayer.y = 0;
        repositionDirection = Vector3.Cross(Vector3.up, toPlayer).normalized;
        if (Random.value > 0.5f) repositionDirection *= -1f;
    }

    void HandleReposition()
    {
        stateTimer -= Time.deltaTime;
        transform.position += repositionDirection * repositionDashSpeed * Time.deltaTime;

        float distToPlayer = Vector3.Distance(transform.position, target.position);
        if (distToPlayer < minDistanceFromPlayer)
        {
            Vector3 away = (transform.position - target.position).normalized;
            away.y = 0;
            if (away.sqrMagnitude > 0.001f)
                transform.position += away * (minDistanceFromPlayer - distToPlayer) * 0.5f * Time.deltaTime;
        }

        float baseAltitude = target.position.y + safeAltitudeOffset;
        if (transform.position.y < baseAltitude - 1f)
        {
            transform.position += Vector3.up * recoverUpwardSpeed * 0.5f * Time.deltaTime;
        }

        FaceTarget();

        if (stateTimer <= 0f)
        {
            currentTiktikState = TiktikState.HoverPatrol;
            stateTimer = Random.Range(hoverMinDuration, hoverMaxDuration);
        }
    }

    void HandleSwoopAttack()
    {
        if (stateTimer > 0f)
        {
            stateTimer -= Time.deltaTime;
            FaceTarget();
            return;
        }

        Vector3 toTarget = (swoopTargetPos - transform.position);
        float dist = toTarget.magnitude;

        if (dist > 0.5f)
        {
            Vector3 dir = toTarget.normalized;
            transform.position += dir * swoopSpeed * Time.deltaTime;

            if (!swoopDamageApplied)
            {
                Collider[] hits = Physics.OverlapSphere(transform.position, 1.5f, playerLayer);
                foreach (Collider hit in hits)
                {
                    IDamageable d = hit.GetComponent<IDamageable>();
                    if (d != null)
                    {
                        d.TakeDamage(swoopDamage, transform);
                        swoopDamageApplied = true;
                        break;
                    }
                }
            }
        }
        else
        {
            lastSwoopTime = Time.time;
            currentTiktikState = TiktikState.Recover;
            stateTimer = recoverDuration;
        }
    }

    void HandleRecover()
    {
        stateTimer -= Time.deltaTime;
        transform.position += Vector3.up * recoverUpwardSpeed * Time.deltaTime;

        float targetAltitude = target.position.y + safeAltitudeOffset;
        if (transform.position.y >= targetAltitude)
        {
            stateTimer = 0f;
        }

        if (stateTimer <= 0f)
        {
            currentTiktikState = TiktikState.HoverPatrol;
            stateTimer = Random.Range(hoverMinDuration, hoverMaxDuration);
        }
    }

    void FaceTarget()
    {
        if (target == null) return;
        Vector3 dir = (target.position - transform.position).normalized;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);
        }
    }

    public override void Die()
    {
        currentTiktikState = TiktikState.Dead;
    }
}
