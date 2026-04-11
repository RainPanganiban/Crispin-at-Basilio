using UnityEngine;
using UnityEngine.UI;
using TMPro; // Dagdag ito para sa TextMeshPro

public class SettingsManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuButtons;
    public GameObject settingsPanel;

    [Header("Sliders")]
    public Slider sensitivitySlider;
    public Slider bgmSlider;
    public Slider sfxSlider;

    [Header("Value Labels")]
    public TextMeshProUGUI sensitivityText; // Slot para sa text ng Sens
    public TextMeshProUGUI bgmText;         // Slot para sa text ng BGM
    public TextMeshProUGUI sfxText;         // Slot para sa text ng SFX

    private void Start()
    {
        // Load settings
        float savedSens = PlayerPrefs.GetFloat("MouseSensitivity", 120f);
        float savedBGM = PlayerPrefs.GetFloat("BGMVolume", 0.7f);
        float savedSFX = PlayerPrefs.GetFloat("SFXVolume", 1f);

        // Set slider values
        sensitivitySlider.value = savedSens;
        bgmSlider.value = savedBGM;
        sfxSlider.value = savedSFX;

        // I-apply at i-update ang text labels agad sa simula
        ApplySensitivity(savedSens);
        ApplyBGM(savedBGM);
        ApplySFX(savedSFX);

        // Listeners
        sensitivitySlider.onValueChanged.AddListener(ApplySensitivity);
        bgmSlider.onValueChanged.AddListener(ApplyBGM);
        sfxSlider.onValueChanged.AddListener(ApplySFX);
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        PlayerPrefs.Save();
    }

    public void ApplyBGM(float val)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.MusicVolume = val;
        bgmText.text = Mathf.RoundToInt(val * 100).ToString() + "%"; // Ginagawang percentage (e.g. 80%)
        PlayerPrefs.SetFloat("BGMVolume", val);
    }

    public void ApplySFX(float val)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.SFXVolume = val;
        sfxText.text = Mathf.RoundToInt(val * 100).ToString() + "%"; // Ginagawang percentage
        PlayerPrefs.SetFloat("SFXVolume", val);
    }

    public void ApplySensitivity(float val)
    {
        sensitivityText.text = Mathf.RoundToInt(val).ToString(); // Pinapakita yung actual number (e.g. 120)
        PlayerPrefs.SetFloat("MouseSensitivity", val);
        ThirdPersonCamera cam = FindFirstObjectByType<ThirdPersonCamera>();
        if (cam != null) cam.sensitivity = val;
    }
}