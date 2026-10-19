using System.Collections;
using TMPro;
using UnityEngine;

// Element geldiğinde ekranın üstünde kısa süre görünen bilgi balonu.
// İlk birkaç karşılaşmada büyük: "Yeni element: Buz" ve altında ne yaptığı;
// oyuncu elementi tanıdıktan sonra küçük bir etiket: "Buz  Yavaş kayar".
public class ElementBanner : MonoBehaviour
{
    // Bir elementle bu kadar oyunda karşılaşana kadar büyük tanıtım gösterilir
    public const int FullIntroductions = 3;

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

        ShowText(Localization.Get("new_element") + " " + ColoredName(element, key), Localization.Get("element_" + key + "_desc"));
    }

    // Oyuncu elementi zaten tanıyor: daha küçük ve kısa süreli hatırlatma
    public void ShowTag(Block.BlockElementType element)
    {
        string key = ElementKey(element);
        if (key == null) return;

        label.text = ColoredName(element, key) + "  <size=70%>" + Localization.Get("element_" + key + "_desc") + "</size>";
        Play(0.7f, 1.1f);
    }

    private static string ColoredName(Block.BlockElementType element, string key)
    {
        string color = ColorUtility.ToHtmlStringRGB(Block.ElementColor(element) * 0.8f);
        return "<color=#" + color + ">" + Localization.Get("element_" + key) + "</color>";
    }

    // Genel kullanım: başlık ve altında küçük bir satır (yükseklik hedefi, yeni rekor...)
    public void ShowText(string title, string subtitle)
    {
        label.text = string.IsNullOrEmpty(subtitle) ? title : title + "\n<size=70%>" + subtitle + "</size>";
        Play(1f, holdSeconds);
    }

    private void Play(float size, float hold)
    {
        if (routine != null)
            StopCoroutine(routine);
        routine = StartCoroutine(ShowRoutine(size, hold));
    }

    private IEnumerator ShowRoutine(float size, float hold)
    {
        RectTransform rt = (RectTransform)transform;

        // Hafif taşarak büyüyerek gelir
        for (float t = 0f; t < 0.25f; t += Time.unscaledDeltaTime)
        {
            float n = t / 0.25f;
            canvasGroup.alpha = n;
            rt.localScale = Vector3.one * size * Mathf.Lerp(0.7f, 1f, 1f - Mathf.Pow(1f - n, 3f));
            yield return null;
        }
        canvasGroup.alpha = 1f;
        rt.localScale = Vector3.one * size;

        yield return new WaitForSecondsRealtime(hold);

        for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime)
        {
            canvasGroup.alpha = 1f - t / 0.4f;
            yield return null;
        }
        canvasGroup.alpha = 0f;
        routine = null;
    }

    public static string ElementKey(Block.BlockElementType element)
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
