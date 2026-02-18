using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

/// <summary>
/// Client-side UI component for LevelNode countdown and unlock visuals.
/// Attach this to a child GameObject of LevelNode.
/// </summary>
public class LevelNodeUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LevelNode levelNode;
    
    [Header("Locked/Unlocked Visuals")]
    [SerializeField] private GameObject lockedVisual;
    [SerializeField] private GameObject unlockedVisual;
    
    [Header("Countdown UI")]
    [SerializeField] private GameObject countdownRoot;
    [SerializeField] private Slider countdownSlider;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private float countdownDisplayHeight = 3f; // Height above node

    void Start()
    {
        if (levelNode == null)
        {
            levelNode = GetComponentInParent<LevelNode>();
        }

        if (levelNode == null)
        {
            Debug.LogError("[LevelNodeUI] LevelNode not found! Make sure this is a child of LevelNode.");
            enabled = false;
            return;
        }

        // Subscribe to countdown changes via reflection (since SyncVar hooks are private)
        // We'll use Update polling instead for simplicity
        UpdateVisuals();
    }

    void Update()
    {
        UpdateCountdownUI();
    }

    void UpdateVisuals()
    {
        if (levelNode == null) return;

        // Check unlock state by reading the SyncVar via reflection or polling
        // For now, we'll use a public getter we'll add to LevelNode
        bool isUnlocked = levelNode.IsUnlocked();

        if (lockedVisual != null)
            lockedVisual.SetActive(!isUnlocked);

        if (unlockedVisual != null)
            unlockedVisual.SetActive(isUnlocked);
    }

    void UpdateCountdownUI()
    {
        if (levelNode == null) return;

        float countdown = levelNode.GetCountdownRemaining();
        float requiredTime = levelNode.GetRequiredHoldTime();

        bool showCountdown = countdown > 0f;

        if (countdownRoot != null)
            countdownRoot.SetActive(showCountdown);

        if (showCountdown)
        {
            float progress = countdown / requiredTime;

            if (countdownSlider != null)
            {
                countdownSlider.value = progress;
            }

            if (countdownText != null)
            {
                countdownText.text = countdown.ToString("F1") + "s";
            }

            // Position countdown UI above the node
            if (countdownRoot != null)
            {
                Vector3 worldPos = levelNode.transform.position + Vector3.up * countdownDisplayHeight;
                countdownRoot.transform.position = worldPos;
                
                // Find the local player's camera for billboarding
                Camera targetCamera = null;
                
                // Try Camera.main first
                if (Camera.main != null)
                {
                    targetCamera = Camera.main;
                }
                else if (NetworkClient.localPlayer != null)
                {
                    // Try to find camera in local player hierarchy
                    targetCamera = NetworkClient.localPlayer.GetComponentInChildren<Camera>();
                }
                
                if (targetCamera != null)
                {
                    // Billboard: make UI face camera
                    Vector3 directionToCamera = targetCamera.transform.position - countdownRoot.transform.position;
                    if (directionToCamera != Vector3.zero)
                    {
                        countdownRoot.transform.rotation = Quaternion.LookRotation(-directionToCamera);
                    }
                }
            }
        }
    }

    // Called when unlock state changes (you can call this from LevelNode's hook)
    public void OnUnlockStateChanged(bool isUnlocked)
    {
        UpdateVisuals();
    }
}
