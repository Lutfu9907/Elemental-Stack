using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class IntroFadeController : MonoBehaviour
{
    public Image fadeImage;
    public Image logoImage;

    public float fadeInDuration = 0.6f;
    public float logoHoldTime = 1.5f;
    public float fadeOutDuration = 0.6f;

    private void Start()
    {
        StartCoroutine(IntroRoutine());
    }

    IEnumerator IntroRoutine()
    {
        // BAÞTA TAM SÝYAH
        fadeImage.color = new Color(0, 0, 0, 1);
        logoImage.color = new Color(1, 1, 1, 0);

        // FADE IN (LOGO GELÝR)
        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            float a = t / fadeInDuration;

            fadeImage.color = new Color(0, 0, 0, 1 - a);
            logoImage.color = new Color(1, 1, 1, a);
            yield return null;
        }

        yield return new WaitForSeconds(logoHoldTime);

        // FADE OUT
        t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            float a = t / fadeOutDuration;

            fadeImage.color = new Color(0, 0, 0, a);
            logoImage.color = new Color(1, 1, 1, 1 - a);
            yield return null;
        }

        SceneManager.LoadScene("SampleScene");
    }
}
