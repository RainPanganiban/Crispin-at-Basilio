using UnityEngine;
using UnityEngine.UI;
using TMPro; // Mahalaga para sa TextMeshPro

public class SettingsManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuButtons;
    public GameObject settingsPanel;

    [Header("Sliders")]
    public Slider sensitivitySlider;
    public Slider bgmSlider;
    public Slider sfxSlider;

    [Header("Value Labels (Optional)")]
    public TextMeshProUGUI sensitivityText;
    public TextMeshProUGUI bgmText;
    public TextMeshProUGUI sfxText;

    private void Start()
    {
        // 1. Initial UI State (Hindi na natatago ang Main Menu)
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // 2. Load settings (Sens default is 150 para mas ramdam)
        float savedSens = PlayerPrefs.GetFloat("MouseSensitivity", 150f);
        float savedBGM = PlayerPrefs.GetFloat("BGMVolume", 0.7f);
        float savedSFX = PlayerPrefs.GetFloat("SFXVolume", 1f);

        // 3. Set slider values
        if (sensitivitySlider != null) sensitivitySlider.value = savedSens;
        if (bgmSlider != null) bgmSlider.value = savedBGM;
        if (sfxSlider != null) sfxSlider.value = savedSFX;

        // 4. Apply agad ang values sa simula
        ApplySensitivity(savedSens);
        ApplyBGM(savedBGM);
        ApplySFX(savedSFX);

        // 5. Listeners para sa real-time updates
        if (sensitivitySlider != null) sensitivitySlider.onValueChanged.AddListener(ApplySensitivity);
        if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(ApplyBGM);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(ApplySFX);
    }

    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        PlayerPrefs.Save();
    }

    public void ApplyBGM(float val)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.MusicVolume = val;

        // I-update lang ang text kung may naka-assign sa slot
        if (bgmText != null) bgmText.text = Mathf.RoundToInt(val * 100).ToString() + "%";

        PlayerPrefs.SetFloat("BGMVolume", val);
    }

    public void ApplySFX(float val)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.SFXVolume = val;

        if (sfxText != null) sfxText.text = Mathf.RoundToInt(val * 100).ToString() + "%";

        PlayerPrefs.SetFloat("SFXVolume", val);
    }

    public void ApplySensitivity(float val)
    {
        // Dito nagkaka-error kanina (Line 71). Ngayon safe na dahil sa if check.
        if (sensitivityText != null) sensitivityText.text = Mathf.RoundToInt(val).ToString();

        PlayerPrefs.SetFloat("MouseSensitivity", val);

        // Hanapin ang camera para i-apply ang bagong speed
        ThirdPersonCamera cam = FindFirstObjectByType<ThirdPersonCamera>();
        if (cam != null)
        {
            cam.sensitivity = val;
        }
    }
}