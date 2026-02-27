using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class BossUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BossHealth bossHealth;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI bossNameText;

    [Header("Settings")]
    [SerializeField] private float activationRange = 15f;
    [SerializeField] private float fadeSpeed = 2f;
    [SerializeField] private string bossName = "Onglo, Guardian of the Basilio";

    private bool isActive = false;
    private Camera localCamera;

    void Start()
    {
        if (bossHealth == null)
        {
            Debug.LogWarning("[BossUIController] BossHealth not assigned. UI will not update.");
            return;
        }

        bossHealth.OnHealthChangedUI += UpdateHealthBar;
        
        if (bossNameText != null)
            bossNameText.text = bossName;

        // Initialize slider
        UpdateHealthBar(bossHealth.GetCurrentHealth(), bossHealth.GetMaxHealth());

        // Hide initially
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    void OnDestroy()
    {
        if (bossHealth != null)
            bossHealth.OnHealthChangedUI -= UpdateHealthBar;
    }

    void Update()
    {
        CheckProximity();
        HandleVisibility();
    }

    private void CheckProximity()
    {
        if (NetworkClient.localPlayer == null || bossHealth == null) return;

        float distance = Vector3.Distance(NetworkClient.localPlayer.transform.position, bossHealth.transform.position);
        
        // Activate if player gets close
        if (!isActive && distance <= activationRange)
        {
            isActive = true;
        }
        // Optional: Deactivate if they run very far away (optional engagement rule)
        else if (isActive && distance > activationRange * 1.5f)
        {
            // We usually keep the bar once fight starts until boss dies, 
            // but for "Encounter UI" we can hide it if player flees.
            isActive = false;
        }
        
        // Hide if boss is dead
        if (bossHealth.GetCurrentHealth() <= 0)
        {
            isActive = false;
        }
    }

    private void HandleVisibility()
    {
        if (canvasGroup == null) return;

        float targetAlpha = isActive ? 1f : 0f;
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
        
        // Turn off blocksRaycasts if hidden to avoid UI issues
        canvasGroup.blocksRaycasts = canvasGroup.alpha > 0.1f;
    }

    private void UpdateHealthBar(float current, float max)
    {
        if (healthSlider != null && max > 0)
        {
            float fill = current / max;
            healthSlider.value = fill;
        }
    }
}
