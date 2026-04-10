using UnityEngine;
using Mirror;
using System.Collections;

public class TidalBite : BaseAttack
{
    [Header("Bite Settings")]
    public float damage = 50f;
    public float lungeForce = 7f;
    public float lungeDuration = 0.3f;
    public float attackFullDuration = 1.2f;
    public Transform mouthPoint;

    [Header("AOE Settings")]
    public float aoeRadius = 8.0f;
    public float aoeOffsetForward = 3.0f;
    public LayerMask playerLayer;

    private Animator animator;
    private NetworkAnimator networkAnimator;
    private BakunawaMovement movement;
    private bool isExecuting = false;
    private float attackStartTime;
    private bool damageDealt = false;

    private static readonly int AttackTrigger = Animator.StringToHash("TidalBite");

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        networkAnimator = GetComponent<NetworkAnimator>();
        movement = GetComponent<BakunawaMovement>();
    }

    [Server]
    public override void Server_Execute()
    {
        if (isExecuting) return;

        isExecuting = true;
        damageDealt = false;
        attackStartTime = Time.time;

        if (movement != null) movement.Server_SetMovementEnabled(false);

        if (networkAnimator != null)
            networkAnimator.SetTrigger(AttackTrigger);
        else if (animator != null)
            animator.SetTrigger(AttackTrigger);

        StartCoroutine(LungeRoutine());
    }

    void Update()
    {
        if (!isServer || !isExecuting) return;

        float elapsed = Time.time - attackStartTime;

        if (!damageDealt && elapsed >= 0.6f)
        {
            DealBiteDamage();
        }

        if (elapsed >= attackFullDuration)
        {
            Server_Stop();
        }
    }

    [Server]
    public override void Server_Stop()
    {
        if (!isExecuting) return;
        isExecuting = false;
        StopAllCoroutines();
        if (movement != null) movement.Server_SetMovementEnabled(true);
    }

    public override void Server_OnAnimationEvent(string eventName)
    {
        if (eventName.Contains("Damage") || eventName.Contains("Hit") || eventName.Contains("Deal"))
        {
            DealBiteDamage();
        }
    }

    // ETO YUNG NAWAWALA KAYA MAY ERROR:
    private IEnumerator LungeRoutine()
    {
        float timer = 0;
        while (timer < lungeDuration)
        {
            transform.position += transform.forward * lungeForce * Time.deltaTime;
            timer += Time.deltaTime;
            yield return null;
        }
    }

    [Server]
    private void DealBiteDamage()
    {
        if (damageDealt) return;
        damageDealt = true;

        Vector3 basePos = mouthPoint != null ? mouthPoint.position : transform.position + (transform.forward * 4.0f);
        Vector3 aoeCenter = basePos + (transform.forward * aoeOffsetForward);

        Collider[] hits = Physics.OverlapSphere(aoeCenter, aoeRadius, playerLayer);

        foreach (Collider h in hits)
        {
            if (h.transform == transform) continue;

            var stats = h.GetComponent<PlayerStatsManager>();
            if (stats != null)
            {
                stats.TakeDamage(damage, transform);
                Debug.Log($"<color=green>[TidalBite]</color> Hit {h.name}!");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 basePos = mouthPoint != null ? mouthPoint.position : transform.position + (transform.forward * 4.0f);
        Vector3 aoeCenter = basePos + (transform.forward * aoeOffsetForward);
        Gizmos.color = new Color(1, 0, 0, 0.4f);
        Gizmos.DrawSphere(aoeCenter, aoeRadius);
    }
}