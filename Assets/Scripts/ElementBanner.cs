using System.Collections;
using TMPro;
using UnityEngine;

// Bir element ilk kez geldiğinde ekranın üstünde kısa süre görünen bilgi balonu:
// "Yeni element: Buz" ve altında ne yaptığı.
public class ElementBanner : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI label;
    public float holdSeconds = 2.2f;

    private Coroutine routine;

    void Awake()
    {
        canvasGroup.alpha = 0f;
    }

    public void Show(Block.BlockElementType element)
    {
        string key = ElementKey(element);
        if (key == null) return;

        string color = ColorUtility.ToHtmlStringRGB(Block.ElementColor(element) * 0.8f);
        ShowText(Localization.Get("new_element") + " <color=#" + color + ">" + Localization.Get("element_" + key) + "</color>",
                 Localization.Get("element_" + key + "_desc"));
    }

    // Genel kullanım: başlık ve altında küçük bir satır (yükseklik hedefi, yeni rekor...)
    public void ShowText(string title, string subtitle)
    {
        label.text = string.IsNullOrEmpty(subtitle) ? title : title + "\n<size=70%>" + subtitle + "</size>";

        if (routine != null)
            StopCoroutine(routine);
        routine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        RectTransform rt = (RectTransform)transform;

        // Hafif taşarak büyüyerek gelir
        for (float t = 0f; t < 0.25f; t += Time.unscaledDeltaTime)
        {
            float n = t / 0.25f;
            canvasGroup.alpha = n;
            rt.localScale = Vector3.one * Mathf.Lerp(0.7f, 1f, 1f - Mathf.Pow(1f - n, 3f));
            yield return null;
        }
        canvasGroup.alpha = 1f;
        rt.localScale = Vector3.one;

        yield return new WaitForSecondsRealtime(holdSeconds);

        for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime)
        {
            canvasGroup.alpha = 1f - t / 0.4f;
            yield return null;
        }
        canvasGroup.alpha = 0f;
        routine = null;
    }

    private static string ElementKey(Block.BlockElementType element)
    {
        switch (element)
        {
            case Block.BlockElementType.Ice: return "ice";
            case Block.BlockElementType.Fire: return "fire";
            case Block.BlockElementType.Wind: return "wind";
            case Block.BlockElementType.Electric: return "electric";
            default: return null;
        }
    }
}
