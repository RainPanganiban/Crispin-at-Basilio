using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Collections;
using System.Collections.Generic;

public class LoadingScreenManager : MonoBehaviour
{
    public static LoadingScreenManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("The main container panel for the loading screen.")]
    public GameObject loadingPanel;
    
    [Tooltip("CanvasGroup for fading transitions.")]
    public CanvasGroup canvasGroup;
    
    [Tooltip("The slider that shows load progress (0 to 1).")]
    public Slider progressBar;
    
    [Tooltip("Text to display the percentage value.")]
    public TMP_Text progressText;

    [Tooltip("Text to display random gameplay tips.")]
    public TMP_Text tipText;

    [Header("Settings")]
    public float fadeDuration = 0.5f;
    public float minLoadingTime = 2.0f;
    public float smoothSpeed = 5.0f;

    [Header("Content")]
    public string[] tips = new string[] 
    {
        "Crispin's quick attacks can interrupt enemy Leap behaviors.",
        "Basilio's heavy strikes deal massive damage to Tyanak armor.",
        "Watch for the glowing red eyes—it means an attack is coming!",
        "Stay mobile to avoid being surrounded by the swarm.",
        "Coins can be spent at the Overworld shop for upgrades."
    };

    private Coroutine fadeCoroutine;
    private float targetProgress = 0f;
    private float currentProgress = 0f;
    private float startTime;
    private bool isLoading = false;

    private void Awake()
    {
        Debug.Log($"[LoadingScreenManager] Awake on object: {gameObject.name}");
        if (Instance != null && Instance != this)
        {
            Debug.Log("[LoadingScreenManager] Duplicate instance found, destroying.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Ensure loading screen is hidden by default
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }
    }

    private void Start()
    {
        Debug.Log("[LoadingScreenManager] Start");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Debug.Log("[LoadingScreenManager] Singleton Instance is being destroyed!");
            Instance = null;
        }
    }

    private void Update()
    {
        // Only update if the loading screen is active
        if (loadingPanel == null || !loadingPanel.activeSelf) 
            return;

        // Mirror's NetworkManager exposes the current transition's AsyncOperation
        if (NetworkManager.loadingSceneAsync != null)
        {
            // Unity's SceneManager.LoadSceneAsync stops at 0.9f before activation
            targetProgress = Mathf.Clamp01(NetworkManager.loadingSceneAsync.progress / 0.9f);
        }
        else if (!isLoading)
        {
            // If we are fading out or not in a network load, move towards 1
            targetProgress = 1f;
        }

        // Smoothly interpolate the progress bar
        currentProgress = Mathf.MoveTowards(currentProgress, targetProgress, Time.deltaTime * (1f / minLoadingTime));
        // Also allow it to move faster if the real progress is ahead
        currentProgress = Mathf.Max(currentProgress, Mathf.MoveTowards(currentProgress, targetProgress, Time.deltaTime * smoothSpeed));

        if (progressBar != null)
            progressBar.value = currentProgress;

        if (progressText != null)
            progressText.text = $"{(currentProgress * 100):0}%";
    }

    public void ShowLoadingScreen()
    {
        Debug.Log("[LoadingScreenManager] Showing Loading Screen");
        if (loadingPanel != null)
        {
            isLoading = true;
            startTime = Time.time;
            currentProgress = 0f;
            targetProgress = 0f;

            loadingPanel.SetActive(true);
            
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(Fade(1f));

            // Pick a random tip
            if (tipText != null && tips.Length > 0)
            {
                tipText.text = tips[Random.Range(0, tips.Length)];
            }

            if (progressBar != null) progressBar.value = 0f;
            if (progressText != null) progressText.text = "0%";
        }
        else
        {
            Debug.LogWarning("[LoadingScreenManager] ShowLoadingScreen called but loadingPanel is null!");
        }
    }

    public void HideLoadingScreen()
    {
        Debug.Log("[LoadingScreenManager] Requesting Hide Loading Screen");
        isLoading = false;
        StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        // Wait until both:
        // 1. The minimum time has passed
        // 2. The scene is actually loaded (handled by Update progress bar reaching 1 eventually)
        
        float timeElapsed = Time.time - startTime;
        float remainingTime = minLoadingTime - timeElapsed;

        if (remainingTime > 0)
        {
            yield return new WaitForSeconds(remainingTime);
        }

        // Ensure progress bar hit 100% visually
        while (currentProgress < 0.99f)
        {
            yield return null;
        }

        Debug.Log("[LoadingScreenManager] Fading out Loading Screen");
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(Fade(0f, () => loadingPanel.SetActive(false)));
    }

    private IEnumerator Fade(float targetAlpha, System.Action onComplete = null)
    {
        if (canvasGroup == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        onComplete?.Invoke();
    }
}
