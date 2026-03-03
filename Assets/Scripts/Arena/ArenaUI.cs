using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Client-side UI controller for the arena HUD.
/// Reads SyncVar values from ArenaManager to display wave info,
/// inter-wave countdowns, and the completion screen.
///
/// Wire up the UI elements in the Inspector. All fields are optional —
/// the script gracefully handles null references.
/// </summary>
public class ArenaUI : MonoBehaviour
{
    [Header("Wave Banner")]
    [Tooltip("Root object for the wave announcement banner.")]
    [SerializeField] private GameObject waveBannerRoot;

    [Tooltip("Text displaying 'Wave X / Y'.")]
    [SerializeField] private TextMeshProUGUI waveText;

    [Header("Inter-Wave Countdown")]
    [Tooltip("Root object for the inter-wave countdown display.")]
    [SerializeField] private GameObject countdownRoot;

    [Tooltip("Text showing seconds until next wave.")]
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("Arena Complete")]
    [Tooltip("Root object for the arena complete panel.")]
    [SerializeField] private GameObject completeRoot;

    [Tooltip("Text showing 'Arena Cleared!' or similar.")]
    [SerializeField] private TextMeshProUGUI completeText;

    [Tooltip("Text showing the return countdown.")]
    [SerializeField] private TextMeshProUGUI returnCountdownText;

    // ── Internal state ───────────────────────────────────────────────
    private ArenaManager arenaManager;
    private bool isShowingComplete;
    private float returnTimer;
    private int lastDisplayedWave = -1;
    private float bannerDisplayTimer;
    private const float BANNER_DISPLAY_DURATION = 2.5f;

    void Start()
    {
        // Hide everything initially
        SetActive(waveBannerRoot, false);
        SetActive(countdownRoot, false);
        SetActive(completeRoot, false);
    }

    void Update()
    {
        // Find ArenaManager lazily (it may not exist on the first frame)
        if (arenaManager == null)
        {
            arenaManager = ArenaManager.Instance;
            if (arenaManager == null) return;
        }

        if (isShowingComplete)
        {
            UpdateReturnCountdown();
            return;
        }

        UpdateWaveBanner();
        UpdateInterWaveCountdown();
    }

    // ══════════════════════════════════════════════════════════════════
    // Wave Banner
    // ══════════════════════════════════════════════════════════════════

    void UpdateWaveBanner()
    {
        int wave = arenaManager.CurrentWave;
        int total = arenaManager.TotalWaves;

        // Show banner when a new wave starts
        if (wave != lastDisplayedWave && arenaManager.InterWaveCountdown <= 0f && arenaManager.IsArenaActive)
        {
            lastDisplayedWave = wave;
            bannerDisplayTimer = BANNER_DISPLAY_DURATION;

            if (waveText != null)
                waveText.text = $"WAVE {wave} / {total}";

            SetActive(waveBannerRoot, true);
        }

        // Auto-hide banner after duration
        if (bannerDisplayTimer > 0f)
        {
            bannerDisplayTimer -= Time.deltaTime;
            if (bannerDisplayTimer <= 0f)
            {
                SetActive(waveBannerRoot, false);
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // Inter-Wave Countdown
    // ══════════════════════════════════════════════════════════════════

    void UpdateInterWaveCountdown()
    {
        float countdown = arenaManager.InterWaveCountdown;
        bool show = countdown > 0f;

        SetActive(countdownRoot, show);

        if (show && countdownText != null)
        {
            countdownText.text = $"NEXT WAVE IN {Mathf.CeilToInt(countdown)}...";
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // Arena Complete
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by ArenaCompleteManager via RPC.
    /// </summary>
    public void ShowArenaComplete(float returnDelay)
    {
        isShowingComplete = true;
        returnTimer = returnDelay;

        SetActive(waveBannerRoot, false);
        SetActive(countdownRoot, false);
        SetActive(completeRoot, true);

        if (completeText != null)
            completeText.text = "ARENA CLEARED!";
    }

    void UpdateReturnCountdown()
    {
        returnTimer -= Time.deltaTime;
        if (returnTimer < 0f) returnTimer = 0f;

        if (returnCountdownText != null)
            returnCountdownText.text = $"Returning to overworld in {Mathf.CeilToInt(returnTimer)}...";
    }

    // ══════════════════════════════════════════════════════════════════
    // Utilities
    // ══════════════════════════════════════════════════════════════════

    void SetActive(GameObject obj, bool active)
    {
        if (obj != null && obj.activeSelf != active)
            obj.SetActive(active);
    }
}
