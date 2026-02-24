using UnityEngine;
using Mirror;

/// <summary>
/// Spawned on the server when an enemy dies.
/// Floats in place, waits for a player to walk into its pickup radius,
/// then awards coins to that player and destroys itself.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(NetworkIdentity))]
public class CoinPickup : NetworkBehaviour
{
    [Header("Coin Settings")]
    public int coinValue = 1;
    public float pickupRadius = 1.2f;
    public float pickupDelay = 0.5f;

    [Header("Movement & Physics")]
    public float fallSpeed = 4f;
    public float popForce = 2f;
    public LayerMask groundLayer = ~0; // Default to everything

    [Header("Bob Animation")]
    public float bobAmplitude = 0.15f;
    public float bobFrequency  = 2f;

    [SyncVar]
    private bool collected = false;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private SphereCollider pickupCollider;
    private float spawnTime;
    private bool isFalling = true;
    private float verticalVelocity;

    public bool IsCollected => collected;

    void Awake()
    {
        pickupCollider = GetComponent<SphereCollider>();
        pickupCollider.isTrigger = true;
        pickupCollider.radius    = pickupRadius;
    }

    void Start()
    {
        spawnTime = Time.time;
        verticalVelocity = popForce; // Initial upward pop

        // Find the ground below
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, 20f, groundLayer))
        {
            targetPosition = hit.point + Vector3.up * 0.5f; // Offset slightly above ground
        }
        else
        {
            // If No ground found (e.g. over a void), just stay at current level or fall a bit
            targetPosition = transform.position;
            isFalling = false; 
        }
    }

    void Update()
    {
        if (collected) return;

        if (isFalling)
        {
            // Simple physics: upward pop then fall
            verticalVelocity += -9.81f * Time.deltaTime;
            transform.position += Vector3.up * verticalVelocity * Time.deltaTime;

            // Check if reached target ground height
            if (verticalVelocity < 0 && transform.position.y <= targetPosition.y)
            {
                transform.position = targetPosition;
                startPosition = targetPosition;
                isFalling = false;
            }
        }
        else
        {
            // Gentle bobbing animation
            float newY = startPosition.y + Mathf.Sin((Time.time - spawnTime) * bobFrequency) * bobAmplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected || Time.time < spawnTime + pickupDelay) return;

        PlayerCurrencyManager currency = other.GetComponent<PlayerCurrencyManager>();
        if (currency != null && currency.isLocalPlayer)
        {
            Debug.Log($"[CoinPickup] Local player {other.name} entered trigger. Requesting pickup.");
            currency.CmdRequestPickup(GetComponent<NetworkIdentity>());
        }
    }

    [Server]
    public void ServerCollect(PlayerCurrencyManager collector)
    {
        if (collected) return;
        collected = true;

        Debug.Log($"[CoinPickup] Server confirming collection by {collector.name}. Awarding {coinValue} coins.");
        collector.AddCoins(coinValue);
        NetworkServer.Destroy(gameObject);
    }
}
