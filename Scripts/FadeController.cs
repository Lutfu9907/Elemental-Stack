using UnityEngine;
using System;
using System.Collections;
using UnityEngine.UI;

public class FadeController : MonoBehaviour
{
    public Image fadeImage;
    public float fadeDuration = 0.6f;

    private void Awake()
    {
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
    }

    public void FadeIn(Action onComplete = null)
    {
        StartCoroutine(FadeInRoutine(onComplete));
    }

    private IEnumerator FadeInRoutine(Action onComplete)
    {
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = t / fadeDuration;
            fadeImage.color = new Color(0f, 0f, 0f, a);
            yield return null;
        }

        fadeImage.color = new Color(0f, 0f, 0f, 1f);
        onComplete?.Invoke();
    }
}
