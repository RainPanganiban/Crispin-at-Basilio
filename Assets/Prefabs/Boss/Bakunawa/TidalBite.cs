using UnityEngine;
using Mirror;
using System.Collections;

public class TidalBite : BaseAttack
{
    [Header("Bite Settings")]
    public float damage = 30f;
    public float lungeForce = 7f;
    public float lungeDuration = 0.3f;
    public Transform mouthPoint; // I-drag dito yung Head bone sa Inspector

    [Header("Shockwave Settings")]
    public bool enableShockwave = true;
    public float shockwaveRadius = 5f;
    public float shockwaveDamage = 15f;
    public float shockwaveKnockback = 10f;
    public GameObject shockwaveVFX;

    private Animator animator;
    private NetworkAnimator networkAnimator;
    private BakunawaMovement movement;
    private bool isExecuting = false;

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

        if (movement != null) movement.Server_SetMovementEnabled(false);

        if (networkAnimator != null)
            networkAnimator.SetTrigger(AttackTrigger);
        else if (animator != null)
            animator.SetTrigger(AttackTrigger);

        StartCoroutine(LungeRoutine());
    }

    [Server]
    public override void Server_Stop()
    {
        isExecuting = false;
        StopAllCoroutines();
        if (movement != null) movement.Server_SetMovementEnabled(true);
    }

    // --- ITO ANG FIX PARA SA ERROR MO ---
    [Server]
    public override void Server_OnAnimationEvent(string eventName)
    {
        // Tinatawag nito yung functions na hinahanap ng animation mo sa Console kanina
        if (eventName == "DealDamage" || eventName == "Event_DealBiteDamage")
        {
            Event_DealBiteDamage();
        }
        else if (eventName == "AttackFinished" || eventName == "Event_OnAttackFinished")
        {
            Event_OnAttackFinished();
        }
    }

    // Para sa direct calls mula sa animation events mo
    [Server]
    public void Event_DealBiteDamage()
    {
        DealBiteDamage();
        if (enableShockwave) TriggerShockwave();
    }

    [Server]
    public void Event_OnAttackFinished()
    {
        Server_Stop();
    }

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

    private void DealBiteDamage()
    {
        Debug.Log("DealBiteDamage is being called!"); // Pag lumabas ito sa Console, working ang Animation Event.
    Vector3 origin = mouthPoint != null ? mouthPoint.position : transform.position + (transform.forward * 2.5f);
        Collider[] hits = Physics.OverlapSphere(origin, 2.5f);

        foreach (Collider h in hits)
        {
            if (h.CompareTag("Player") && h.transform != transform)
            {
                var health = h.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.TakeDamage(damage, transform);
                }
            }
        }
    }

    private void TriggerShockwave()
    {
        Vector3 origin = mouthPoint != null ? mouthPoint.position : transform.position;

        // SAFE SPAWNING
        if (shockwaveVFX != null)
        {
            // Siguraduhin na may NetworkIdentity ang prefab na 'to!
            if (shockwaveVFX.GetComponent<NetworkIdentity>() != null)
            {
                GameObject vfx = Instantiate(shockwaveVFX, origin, Quaternion.identity);
                NetworkServer.Spawn(vfx);
            }
            else
            {
                Debug.LogWarning("Yung Shockwave VFX mo walang Network Identity! Hindi ko muna i-spawn para walang error.");
            }
        }

        // DAMAGE LOGIC (Dapat tumuloy ito kahit walang VFX)
        Collider[] aoeHits = Physics.OverlapSphere(origin, shockwaveRadius);
        foreach (Collider h in aoeHits)
        {
            if (h.CompareTag("Player") && h.transform != transform)
            {
                var health = h.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.TakeDamage(shockwaveDamage, transform);
                    Debug.Log($"<color=green>SUCCESS:</color> Damaged {h.name}");
                }
            }
        }
    }
    private void OnDrawGizmosSelected()
    {
        Vector3 origin = mouthPoint != null ? mouthPoint.position : transform.position + (transform.forward * 2.5f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin, 2.5f);

        float lungeDistance = lungeForce * lungeDuration;
        Vector3 maxRangePos = transform.position + (transform.forward * (lungeDistance + 2.5f));
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(maxRangePos, 2.5f);

        if (enableShockwave)
        {
            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.5f);
            Gizmos.DrawWireSphere(origin, shockwaveRadius);
        }
    }
}