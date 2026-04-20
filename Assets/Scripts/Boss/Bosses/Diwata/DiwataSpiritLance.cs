using UnityEngine;
using Mirror;

/// <summary>
/// A fast directional light spear projectile.
/// Fires from the arena edge toward the center, 
/// damages players on contact, destroys after maxDistance.
/// </summary>
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(AudioSource))] // --- DAGDAG: Para automatic may AudioSource ang prefab ---
public class DiwataSpiritLance : NetworkBehaviour
{
    [Header("Visuals")]
    public ParticleSystem psTrail;
    public ParticleSystem psImpact;

    [Header("Audio Settings")] // --- DAGDAG: Sound Setup ---
    [SerializeField] private AudioClip spawnClip; // Tunog paglipad (e.g., Heavy Whoosh / Magic Beam)
    [SerializeField] private AudioClip hitClip;   // Tunog pagtama (e.g., Heavy Impact / Piercing Sound)
    [Range(0f, 1f)] public float volume = 0.8f;   // Medyo mas malakas sa petal (0.8f)

    [Header("Runtime (server initialized)")]
    [SyncVar] private float damage;
    [SyncVar] private float speed;
    [SyncVar] private float maxDistance;

    private Vector3 direction;
    private Vector3 startPos;
    private NetworkIdentity owner;
    private LayerMask playerLayer;
    private CapsuleCollider trigger;
    private AudioSource audioSource; // --- DAGDAG: Reference ---
    private bool hit;

    void Awake()
    {
        trigger = GetComponent<CapsuleCollider>();
        trigger.isTrigger = true;

        // I-setup ang audio source pagka-spawn
        audioSource = GetComponent<AudioSource>();
        ConfigureAudioSource();
    }

    // --- DAGDAG: Tutunog sa lahat ng player (Client) paglabas ng Lance ---
    public override void OnStartClient()
    {
        base.OnStartClient();
        PlaySpawnSound();
    }

    [Server]
    public void Server_Initialize(
        NetworkIdentity owner,
        Vector3 direction,
        float speed,
        float damage,
        float maxDistance,
        LayerMask playerLayer
    )
    {
        this.owner = owner;
        this.direction = direction.normalized;
        this.speed = speed;
        this.damage = damage;
        this.maxDistance = maxDistance;
        this.playerLayer = playerLayer;
        this.startPos = transform.position;
        this.hit = false;

        // Orient the lance along direction
        if (direction.sqrMagnitude > 0.001f)
            transform.forward = direction;
    }

    [ServerCallback]
    void Update()
    {
        if (hit) return;

        transform.position += direction * speed * Time.deltaTime;

        float traveled = Vector3.Distance(startPos, transform.position);
        if (traveled >= maxDistance)
            NetworkServer.Destroy(gameObject);
    }

    [ServerCallback]
    void OnTriggerEnter(Collider other)
    {
        if (hit) return;
        if (other.isTrigger) return;
        if (((1 << other.gameObject.layer) & playerLayer.value) == 0) return;

        if (other.TryGetComponent<IDamageable>(out var dmg))
        {
            dmg.TakeDamage(damage, owner != null ? owner.transform : null);
        }

        hit = true;
        Rpc_OnHit();
        trigger.enabled = false;
        Invoke(nameof(DestroyLance), 1.5f); // Same delay para matapos yung particles at sound
    }

    [ClientRpc]
    void Rpc_OnHit()
    {
        if (psTrail != null) psTrail.Stop();
        if (psImpact != null) psImpact.Play();

        // --- DAGDAG: Play Hit Sound ---
        if (hitClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitClip, volume);
        }

        MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
        if (renderer != null) renderer.enabled = false;
    }

    // --- DAGDAG: Helper Methods para sa Audio ---
    private void PlaySpawnSound()
    {
        if (spawnClip != null && audioSource != null)
        {
            // Random pitch para hindi robotic pakinggan lalo na kung maraming lances
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(spawnClip, volume);
        }
    }

    private void ConfigureAudioSource()
    {
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // Full 3D Sound
        audioSource.minDistance = 3f;  // Mas malaki nang konti sa petal para mas rinig ang danger
        audioSource.maxDistance = 25f; // Mas malayong rinig
        audioSource.rolloffMode = AudioRolloffMode.Linear;
    }

    void DestroyLance()
    {
        NetworkServer.Destroy(gameObject);
    }
}