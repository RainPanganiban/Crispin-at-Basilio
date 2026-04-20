using UnityEngine;
using Mirror;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(AudioSource))] // Siguraduhin na may AudioSource ang Prefab
public class DiwataPetal : NetworkBehaviour
{
    [Header("Visuals")]
    public ParticleSystem psTrail;
    public ParticleSystem psImpact;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip spawnClip; // Sound paglabas ng petal
    [SerializeField] private AudioClip hitClip;   // Sound pagtama sa player
    [Range(0f, 1f)] public float volume = 0.6f;

    [Header("Runtime (server initialized)")]
    [SyncVar] private float damage;
    [SyncVar] private float speed;
    [SyncVar] private float lifetime;

    private Vector3 direction;
    private float dieAt;
    private NetworkIdentity owner;
    private LayerMask playerLayer;
    private SphereCollider trigger;
    private AudioSource audioSource;

    void Awake()
    {
        trigger = GetComponent<SphereCollider>();
        trigger.isTrigger = true;

        audioSource = GetComponent<AudioSource>();
        ConfigureAudioSource();
    }

    // Tinatawag sa lahat ng Clients kapag nag-spawn ang object
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
        float lifetime,
        LayerMask playerLayer
    )
    {
        this.owner = owner;
        this.direction = direction.normalized;
        this.speed = speed;
        this.damage = damage;
        this.lifetime = lifetime;
        this.playerLayer = playerLayer;
        dieAt = Time.time + lifetime;
    }

    [ServerCallback]
    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;

        if (Time.time >= dieAt)
            NetworkServer.Destroy(gameObject);
    }

    [ServerCallback]
    void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger) return;
        if (((1 << other.gameObject.layer) & playerLayer.value) == 0) return;

        if (other.TryGetComponent<IDamageable>(out var dmg))
        {
            dmg.TakeDamage(damage, owner != null ? owner.transform : null);
        }

        Rpc_OnHit();
        trigger.enabled = false;
        // Binagalan ng konti ang destroy para matapos ang sound at particles
        Invoke(nameof(DestroyPetal), 1.5f);
    }

    [ClientRpc]
    void Rpc_OnHit()
    {
        if (psTrail != null) psTrail.Stop();
        if (psImpact != null) psImpact.Play();

        // Play Hit Sound
        if (hitClip != null)
        {
            audioSource.PlayOneShot(hitClip, volume);
        }

        MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
        if (renderer != null) renderer.enabled = false;
    }

    private void PlaySpawnSound()
    {
        if (spawnClip != null && audioSource != null)
        {
            // Nagdagdag ng konting random pitch para hindi "robotic" pakinggan
            // lalo na kung sabay-sabay silang lumalabas
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(spawnClip, volume);
        }
    }

    private void ConfigureAudioSource()
    {
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // Full 3D Sound
        audioSource.minDistance = 2f;
        audioSource.maxDistance = 20f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
    }

    void DestroyPetal()
    {
        NetworkServer.Destroy(gameObject);
    }
}