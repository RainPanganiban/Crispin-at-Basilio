using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class EnemyHealthUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private Canvas canvas;

    [Header("Visibility")]
    public float visibleDuration = 3f;

    [Header("DEBUG")]
    [SerializeField] private bool alwaysVisibleForDebug = true;

    private EnemyHealth enemyHealth;
    private Camera localCamera;
    private float timer;

    void Awake()
    {
        // Initially enable canvas for debug
        canvas.enabled = alwaysVisibleForDebug;
    }

    void Start()
    {
        // Find the EnemyHealth on parent
        enemyHealth = GetComponentInParent<EnemyHealth>();
        if (enemyHealth == null)
        {
            Debug.LogError("[EnemyHealthUI] EnemyHealth not found!");
            return;
        }

        // Subscribe to EnemyHealth public events
        enemyHealth.OnHealthChangedUI += UpdateHealth;
        enemyHealth.OnDamaged += Show;

        // Find the local player's camera
        if (NetworkClient.localPlayer != null)
        {
            localCamera = NetworkClient.localPlayer.GetComponentInChildren<Camera>();
            if (localCamera == null)
                Debug.LogWarning("[EnemyHealthUI] Local player's camera not found!");
        }
        else
        {
            Debug.LogWarning("[EnemyHealthUI] Local player not yet spawned, will try in Update.");
        }

        // Always visible for debug
        canvas.enabled = true;
    }

    void Update()
    {
        // Try to assign camera if not yet set
        if (localCamera == null && NetworkClient.localPlayer != null)
        {
            localCamera = NetworkClient.localPlayer.GetComponentInChildren<Camera>();
        }

        // Billboard towards local camera
        if (localCamera != null)
        {
            Vector3 direction = transform.position - localCamera.transform.position;
            direction.y = 0; // Keep upright
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(direction);
        }

        // Skip auto-hide in debug mode
        if (alwaysVisibleForDebug) return;

        if (!canvas.enabled) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
            canvas.enabled = false;
    }

    void UpdateHealth(float current, float max)
    {
        if (healthBar != null)
            healthBar.value = current / max;
    }

    void Show()
    {
        if (alwaysVisibleForDebug) return;

        if (canvas != null)
        {
            canvas.enabled = true;
            timer = visibleDuration;
        }
    }

    void OnDestroy()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnHealthChangedUI -= UpdateHealth;
            enemyHealth.OnDamaged -= Show;
        }
    }
}