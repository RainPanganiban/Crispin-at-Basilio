using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Client-side UI that shows a "Level Complete!" banner and countdown timer.
/// Attach to a Canvas in the level scene with the panel disabled by default.
/// </summary>
public class LevelCompleteUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Root panel that gets enabled/disabled.")]
    public GameObject panel;

    [Tooltip("Text displaying 'LEVEL COMPLETE!'")]
    public TMP_Text titleText;

    [Tooltip("Text displaying the countdown timer.")]
    public TMP_Text countdownText;

    void Awake()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    /// <summary>
    /// Called by LevelCompleteManager's ClientRpc to display the victory screen.
    /// </summary>
    public void Show(float countdown)
    {
        if (panel != null)
            panel.SetActive(true);

        if (titleText != null)
            titleText.text = "LEVEL COMPLETE!";

        StartCoroutine(CountdownRoutine(countdown));
    }

    IEnumerator CountdownRoutine(float duration)
    {
        float remaining = duration;

        while (remaining > 0f)
        {
            if (countdownText != null)
            {
                countdownText.text = $"Returning to overworld in {Mathf.CeilToInt(remaining)}...";
            }

            yield return new WaitForSeconds(1f);
            remaining -= 1f;
        }

        if (countdownText != null)
        {
            countdownText.text = "Loading...";
        }
    }
}
