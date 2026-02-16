using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class EnemyHealthUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private Canvas canvas;

    [Header("Visibility Settings")]
    [SerializeField] private bool alwaysVisible = false;
    [SerializeField] private float visibleDuration = 3f;

    private EnemyHealth enemyHealth;
    private Camera localCamera;
    private float timer;

    void Awake()
    {
        canvas.enabled = alwaysVisible;
    }

    void Start()
    {
        enemyHealth = GetComponentInParent<EnemyHealth>();

        if (enemyHealth == null)
        {
            Debug.LogError("[EnemyHealthUI] EnemyHealth not found!");
            return;
        }

        enemyHealth.OnHealthChangedUI += UpdateHealth;
        enemyHealth.OnDamaged += Show;

        TryFindLocalCamera();

        // Initialize slider value
        UpdateHealth(enemyHealth.GetCurrentHealth(), enemyHealth.GetMaxHealth());
    }

    void Update()
    {
        TryFindLocalCamera();
        FaceCamera();

        if (alwaysVisible) return;

        if (!canvas.enabled) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
            canvas.enabled = false;
    }

    void TryFindLocalCamera()
    {
        if (localCamera != null) return;

        if (NetworkClient.localPlayer != null)
        {
            localCamera = NetworkClient.localPlayer.GetComponentInChildren<Camera>();
        }
    }

    void FaceCamera()
    {
        if (localCamera == null) return;

        Vector3 direction = transform.position - localCamera.transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    void UpdateHealth(float current, float max)
    {
        if (healthBar != null)
            healthBar.value = current / max;
    }

    void Show()
    {
        if (alwaysVisible) return;

        canvas.enabled = true;
        timer = visibleDuration;
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
