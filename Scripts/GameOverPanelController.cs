using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class GameOverPanelController : MonoBehaviour
{
    [Header("References")]
    public CanvasGroup canvasGroup;
    public RectTransform restartButton;
    public RectTransform gameOverText;

    [Header("Timings")]
    public float fadeDuration = 0.35f;
    public float buttonDelay = 0.15f;
    public float buttonAnimDuration = 0.25f;
    
    [Header("Text Animation")]
    public float textAnimDuration = 0.3f;
    public float textSlideDistance = 40f;

    [Header("Fade Out")]
    public Image fadeImage;
    public float fadeOutDuration = 0.3f;

    private void Awake()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (restartButton != null)
            restartButton.localScale = Vector3.one * 0.8f;

        if (gameOverText != null)
        {
            gameOverText.localScale = Vector3.one * 1.2f;
            gameOverText.anchoredPosition += Vector2.up * textSlideDistance;
        }

    }
    public void RestartWithFade()
    {
        StartCoroutine(RestartRoutine());
    }
    private IEnumerator RestartRoutine()
    {
        float t = 0f;

        fadeImage.gameObject.SetActive(true);

        while (t < fadeOutDuration)
        {
            t += Time.unscaledDeltaTime;
            float n = t / fadeOutDuration;

            fadeImage.color = new Color(0, 0, 0, n);
            yield return null;
        }

        fadeImage.color = Color.black;

        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    public void Show()
    {
        gameObject.SetActive(true);
        StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        yield return new WaitForSecondsRealtime(buttonDelay);
        StartCoroutine(ScalePunch(restartButton));
        StartCoroutine(GameOverTextAnim());
    }
    private IEnumerator GameOverTextAnim()
    {
        float t = 0f;

        Vector3 startScale = Vector3.one * 1.2f;
        Vector3 endScale = Vector3.one;

        Vector2 endPos = gameOverText.anchoredPosition - Vector2.up * textSlideDistance;
        Vector2 startPos = gameOverText.anchoredPosition;

        while (t < textAnimDuration)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.SmoothStep(0f, 1f, t / textAnimDuration);

            gameOverText.localScale = Vector3.Lerp(startScale, endScale, n);
            gameOverText.anchoredPosition = Vector2.Lerp(startPos, endPos, n);

            yield return null;
        }

        gameOverText.localScale = endScale;
        gameOverText.anchoredPosition = endPos;
    }

    private IEnumerator ScalePunch(RectTransform target)
    {
        float t = 0f;
        Vector3 start = Vector3.one * 0.8f;
        Vector3 end = Vector3.one;

        while (t < buttonAnimDuration)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.SmoothStep(0f, 1f, t / buttonAnimDuration);
            target.localScale = Vector3.Lerp(start, end, n);
            yield return null;
        }

        target.localScale = end;
    }
}
