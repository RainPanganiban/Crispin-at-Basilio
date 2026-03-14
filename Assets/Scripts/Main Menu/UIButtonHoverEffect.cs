using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class UIButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Target Components")]
    [SerializeField] private Image targetImage;
    
    [Header("Alpha Settings")]
    [SerializeField] [Range(0f, 1f)] private float normalAlpha = 0f;
    [SerializeField] [Range(0f, 1f)] private float hoverAlpha = 1f;
    [SerializeField] private float fadeDuration = 0.15f;

    private Coroutine fadeCoroutine;

    private void OnEnable()
    {
        if (targetImage != null)
        {
            Color color = targetImage.color;
            color.a = normalAlpha;
            targetImage.color = color;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StopFade();
        fadeCoroutine = StartCoroutine(FadeTo(hoverAlpha));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopFade();
        fadeCoroutine = StartCoroutine(FadeTo(normalAlpha));
    }

    private void StopFade()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        if (targetImage == null) yield break;

        float startAlpha = targetImage.color.a;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            
            Color color = targetImage.color;
            color.a = newAlpha;
            targetImage.color = color;
            
            yield return null;
        }

        Color finalColor = targetImage.color;
        finalColor.a = targetAlpha;
        targetImage.color = finalColor;
        
        fadeCoroutine = null;
    }
}
