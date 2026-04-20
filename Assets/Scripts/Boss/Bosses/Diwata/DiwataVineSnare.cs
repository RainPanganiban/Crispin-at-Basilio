using UnityEngine;
using Mirror;

/// <summary>
/// Stationary ground hazard. Appears at a position, telegraphs with a delay,
/// then erupts dealing AoE damage to players in radius.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(AudioSource))] // --- DAGDAG: Para sa local sound effects ---
public class DiwataVineSnare : NetworkBehaviour
{
    [Header("Visuals")]
    public ParticleSystem psTelegraph;
    public ParticleSystem psErupt;

    [Header("Audio Settings")] // --- DAGDAG: Sound Setup ---
    [SerializeField] private AudioClip telegraphClip; // Tunog habang nag-te-telegraph (e.g., Low Rumble)
    [SerializeField] private AudioClip eruptClip;     // Tunog pagputok (e.g., Earth Shatter / Vine Whip)
    [Range(0f, 1f)] public float volume = 0.7f;

    [Header("Runtime (server initialized)")]
    [SyncVar] private float damage;
    [SyncVar] private float eruptRadius;
    [SyncVar] private float telegraphDuration;

    private NetworkIdentity owner;
    private LayerMask playerLayer;
    private float eruptAt;
    private bool erupted;
    private SphereCollider trigger;
    private AudioSource audioSource; // --- DAGDAG ---

    void Awake()
    {
        trigger = GetComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.enabled = false;

        audioSource = GetComponent<AudioSource>();
        ConfigureAudioSource();
    }

    [Server]
    public void Server_Initialize(
        NetworkIdentity owner,
        float damage,
        float eruptRadius,
        float telegraphDuration,
        LayerMask playerLayer
    )
    {
        this.owner = owner;
        this.damage = damage;
        this.eruptRadius = eruptRadius;
        this.telegraphDuration = telegraphDuration;
        this.playerLayer = playerLayer;
        this.eruptAt = Time.time + telegraphDuration;
        this.erupted = false;

        trigger.radius = eruptRadius;

        Rpc_ShowTelegraph();
    }

    [ClientRpc]
    void Rpc_ShowTelegraph()
    {
        if (psTelegraph != null) psTelegraph.Play();

        // --- DAGDAG: Play Telegraph Sound ---
        if (telegraphClip != null && audioSource != null)
        {
            audioSource.clip = telegraphClip;
            audioSource.loop = true; // Naka-loop habang naghihintay pumutok
            audioSource.Play();
        }
    }

    [ServerCallback]
    void Update()
    {
        if (erupted) return;

        if (Time.time >= eruptAt)
        {
            Server_Erupt();
        }
    }

    [Server]
    void Server_Erupt()
    {
        erupted = true;

        Collider[] hits = Physics.OverlapSphere(transform.position, eruptRadius, playerLayer);
        foreach (Collider hit in hits)
        {
            if (!hit.TryGetComponent<IDamageable>(out var dmg))
                continue;

            dmg.TakeDamage(damage, owner != null ? owner.transform : null);
        }

        Rpc_OnErupt();
        Invoke(nameof(DestroySnare), 2.0f);
    }

    [ClientRpc]
    void Rpc_OnErupt()
    {
        if (psTelegraph != null) psTelegraph.Stop();
        if (psErupt != null) psErupt.Play();

        // --- DAGDAG: Play Erupt Sound ---
        if (audioSource != null)
        {
            audioSource.Stop(); // Itigil ang telegraph rumble
            if (eruptClip != null)
            {
                audioSource.pitch = Random.Range(0.85f, 1.15f);
                audioSource.PlayOneShot(eruptClip, volume);
            }
        }
    }

    private void ConfigureAudioSource()
    {
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D Sound
        audioSource.minDistance = 3f;
        audioSource.maxDistance = 20f;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
    }

    void DestroySnare()
    {
        NetworkServer.Destroy(gameObject);
    }
}