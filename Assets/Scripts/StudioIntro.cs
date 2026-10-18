using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

// Oyun açılışındaki "LD Games" stüdyo intro'su (~5 sn):
// renkli bloklar sekerek minik bir kule dizer, "L" ve "D" harfleri tek tek fırlar,
// altın çizgi açılır, "GAMES" harfleri sırayla yükselir, logo hafifçe süzülür,
// sonra her şey yavaşça kaybolup başlangıç ekranını açar.
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
    public float blockDropStart = 0.35f;
    public float blockDropInterval = 0.25f;
    public float blockDropDuration = 0.55f;
    public float ldStart = 1.35f;
    public float ldLetterDelay = 0.15f;
    public float lineStart = 1.85f;
    public float gamesStart = 2.05f;
    public float gamesLetterDelay = 0.07f;
    public float exitStart = 3.9f;
    public float exitDuration = 1.1f;

    [Header("Motion")]
    public float blockDropHeight = 420f;
    public float accentLineWidth = 440f;
    public float floatAmount = 8f;

    private static bool hasPlayed;
    private float elapsed;
    private Vector2[] blockTargets;
    private Vector2 logoStart;
    private TextAnimator ldAnimator;
    private TextAnimator gamesAnimator;

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
        logoStart = logoGroup.anchoredPosition;

        ldAnimator = new TextAnimator(ldText);
        gamesAnimator = new TextAnimator(gamesText);

        Evaluate(0f);
    }

    void OnDisable()
    {
        ldAnimator?.Dispose();
        gamesAnimator?.Dispose();
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
        // Bloklar sırayla yukarıdan düşer, sekerek oturur ve yere değince hafifçe ezilir
        for (int i = 0; i < blocks.Length; i++)
        {
            float start = blockDropStart + i * blockDropInterval;
            float p = Progress(t, start, blockDropDuration);
            float impact = Mathf.Sin(Progress(t, start + blockDropDuration, 0.22f) * Mathf.PI);

            blocks[i].localScale = p > 0f
                ? new Vector3(1f + 0.12f * impact, 1f - 0.22f * impact, 1f)
                : Vector3.zero;
            blocks[i].anchoredPosition = blockTargets[i] + Vector2.up * blockDropHeight * (1f - EaseOutBounce(p));
        }

        // "L" ve "D" sırayla aşağıdan fırlayıp taşarak yerine oturur
        ldAnimator.Apply(i =>
        {
            float p = Progress(t, ldStart + i * ldLetterDelay, 0.5f);
            return (Mathf.LerpUnclamped(0.3f, 1f, EaseOutBack(p)), new Vector2(0f, -60f * (1f - EaseOutCubic(p))), p);
        });

        // Altın çizgi ortadan iki yana açılır
        float line = EaseOutCubic(Progress(t, lineStart, 0.5f));
        accentLine.sizeDelta = new Vector2(accentLineWidth * line, accentLine.sizeDelta.y);

        // "GAMES" harfleri soldan sağa, birer birer yükselerek belirir
        gamesAnimator.Apply(i =>
        {
            float p = Progress(t, gamesStart + i * gamesLetterDelay, 0.4f);
            return (1f, new Vector2(0f, -30f * (1f - EaseOutCubic(p))), p);
        });

        // Bekleme sırasında logo yavaşça süzülür
        float floatIn = Progress(t, ldStart, 1f);
        float drift = Mathf.Sin((t - ldStart) * 2.2f) * floatAmount * floatIn;

        // Çıkış: logo yukarı kayıp büyürken tüm ekran yavaşça kaybolur
        float exit = EaseInOutCubic(Progress(t, exitStart, exitDuration));
        canvasGroup.alpha = 1f - exit;
        canvasGroup.blocksRaycasts = exit <= 0f;
        logoGroup.anchoredPosition = logoStart + Vector2.up * (drift + 50f * exit);
        logoGroup.localScale = Vector3.one * (1f + 0.06f * exit);
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

    // TextMeshPro harflerini tek tek ölçekler, kaydırır ve saydamlaştırır.
    private class TextAnimator
    {
        private readonly TMP_Text text;
        private TMP_MeshInfo[] source;

        public TextAnimator(TMP_Text text)
        {
            this.text = text;
            text.ForceMeshUpdate();
            source = text.textInfo.CopyMeshInfoVertexData();
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        }

        public void Dispose()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
        }

        // TMP metni yeniden ürettiğinde (ör. ekran boyutu değişince) kaynak köşeleri tazele
        private void OnTextChanged(Object obj)
        {
            if (obj == text)
                source = text.textInfo.CopyMeshInfoVertexData();
        }

        public void Apply(System.Func<int, (float scale, Vector2 offset, float alpha)> letter)
        {
            TMP_TextInfo info = text.textInfo;
            for (int i = 0; i < info.characterCount; i++)
            {
                TMP_CharacterInfo ch = info.characterInfo[i];
                if (!ch.isVisible) continue;

                int m = ch.materialReferenceIndex;
                int v = ch.vertexIndex;
                if (m >= source.Length || v + 3 >= source[m].vertices.Length) continue;

                Vector3[] srcVerts = source[m].vertices;
                Color32[] srcColors = source[m].colors32;
                Vector3[] verts = info.meshInfo[m].vertices;
                Color32[] colors = info.meshInfo[m].colors32;

                var (scale, offset, alpha) = letter(i);
                Vector3 center = (srcVerts[v] + srcVerts[v + 2]) * 0.5f;
                for (int j = 0; j < 4; j++)
                {
                    verts[v + j] = (srcVerts[v + j] - center) * scale + center + (Vector3)offset;
                    Color32 c = srcColors[v + j];
                    c.a = (byte)(c.a * Mathf.Clamp01(alpha));
                    colors[v + j] = c;
                }
            }
            text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
        }
    }
}
