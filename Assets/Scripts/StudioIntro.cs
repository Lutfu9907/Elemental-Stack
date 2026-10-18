using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

// Oyun açılışındaki "LD Games" stüdyo intro'su: minik bir kule dizilir, logo belirir,
// sonra her şey yumuşakça kaybolup başlangıç ekranını açar.
// Sadece uygulama ilk açıldığında oynar (restart'ta tekrar gelmez). Dokunmak intro'yu geçer.
public class StudioIntro : MonoBehaviour, IPointerDownHandler
{
    [Header("References")]
    public CanvasGroup canvasGroup;
    public RectTransform logoGroup;
    public RectTransform[] blocks;      // logonun üstündeki minik kule, alttan üste
    public TextMeshProUGUI ldText;
    public RectTransform accentLine;
    public TextMeshProUGUI gamesText;

    [Header("Timing (saniye)")]
    public float blockDropStart = 0.25f;
    public float blockDropInterval = 0.18f;
    public float blockDropDuration = 0.45f;
    public float ldStart = 0.95f;
    public float lineStart = 1.25f;
    public float gamesStart = 1.45f;
    public float exitStart = 2.9f;
    public float exitDuration = 0.6f;

    [Header("Motion")]
    public float blockDropHeight = 360f;
    public float accentLineWidth = 440f;
    public float gamesSpacingFrom = 90f;
    public float gamesSpacingTo = 28f;

    private static bool hasPlayed;
    private float elapsed;
    private Vector2[] blockTargets;

    // "Enter Play Mode" ayarında domain reload kapalı olsa bile intro her Play'de oynasın
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        hasPlayed = false;
    }

    void Awake()
    {
        if (hasPlayed)
        {
            gameObject.SetActive(false);
            return;
        }
        hasPlayed = true;

        blockTargets = new Vector2[blocks.Length];
        for (int i = 0; i < blocks.Length; i++)
            blockTargets[i] = blocks[i].anchoredPosition;

        Evaluate(0f);
    }

    void Update()
    {
        // GameManager oyunu Time.timeScale = 0 ile beklettiği için ölçeksiz zaman kullanılır
        elapsed += Time.unscaledDeltaTime;
        Evaluate(elapsed);

        if (elapsed >= exitStart + exitDuration)
            gameObject.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (elapsed < exitStart)
            elapsed = exitStart;
    }

    private void Evaluate(float t)
    {
        // Bloklar sırayla yukarıdan düşüp sekerek yerine oturur
        for (int i = 0; i < blocks.Length; i++)
        {
            float p = Progress(t, blockDropStart + i * blockDropInterval, blockDropDuration);
            blocks[i].localScale = p > 0f ? Vector3.one : Vector3.zero;
            blocks[i].anchoredPosition = blockTargets[i] + Vector2.up * blockDropHeight * (1f - EaseOutBounce(p));
        }

        // "LD" hafif taşarak büyür
        float ld = Progress(t, ldStart, 0.5f);
        ldText.alpha = ld;
        ldText.rectTransform.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, EaseOutBack(ld));

        // Altın çizgi ortadan iki yana açılır
        float line = EaseOutCubic(Progress(t, lineStart, 0.45f));
        accentLine.sizeDelta = new Vector2(accentLineWidth * line, accentLine.sizeDelta.y);

        // "GAMES" geniş harf aralığından toparlanarak belirir
        float games = Progress(t, gamesStart, 0.7f);
        gamesText.alpha = games;
        gamesText.characterSpacing = Mathf.Lerp(gamesSpacingFrom, gamesSpacingTo, EaseOutCubic(games));

        // Çıkış: logo hafifçe büyürken tüm ekran kaybolur
        float exit = Progress(t, exitStart, exitDuration);
        canvasGroup.alpha = 1f - EaseInOutCubic(exit);
        canvasGroup.blocksRaycasts = exit <= 0f;
        logoGroup.localScale = Vector3.one * (1f + 0.08f * EaseOutCubic(exit));
    }

    private static float Progress(float t, float start, float duration)
    {
        return Mathf.Clamp01((t - start) / duration);
    }

    private static float EaseOutCubic(float x)
    {
        return 1f - Mathf.Pow(1f - x, 3f);
    }

    private static float EaseInOutCubic(float x)
    {
        return x < 0.5f ? 4f * x * x * x : 1f - Mathf.Pow(-2f * x + 2f, 3f) / 2f;
    }

    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    private static float EaseOutBounce(float x)
    {
        const float n1 = 7.5625f;
        const float d1 = 2.75f;
        if (x < 1f / d1) return n1 * x * x;
        if (x < 2f / d1) { x -= 1.5f / d1; return n1 * x * x + 0.75f; }
        if (x < 2.5f / d1) { x -= 2.25f / d1; return n1 * x * x + 0.9375f; }
        x -= 2.625f / d1;
        return n1 * x * x + 0.984375f;
    }
}
