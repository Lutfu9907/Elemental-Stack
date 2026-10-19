using UnityEngine;

// Kameranın arkasına dikey renk geçişli bir arka plan çizer.
// Kule yükseldikçe renkler sıradaki palete yumuşakça geçer (gündüz → gün batımı → gece...).
[RequireComponent(typeof(Camera))]
public class BackgroundGradient : MonoBehaviour
{
    [System.Serializable]
    public struct Palette
    {
        public Color top;
        public Color bottom;
    }

    public Renderer backgroundRenderer;
    public TowerController tower;
    public int floorsPerPalette = 12;
    public float blendSpeed = 1.5f;
    [Tooltip("Arka plan düzleminin kameraya uzaklığı; kameranın far clip değerinden küçük olmalı")]
    public float distance = 150f;

    public Palette[] palettes =
    {
        new Palette { top = new Color(0.33f, 0.62f, 0.95f), bottom = new Color(0.82f, 0.93f, 1f) },   // gündüz
        new Palette { top = new Color(0.98f, 0.62f, 0.45f), bottom = new Color(1f, 0.88f, 0.62f) },   // ikindi
        new Palette { top = new Color(0.55f, 0.27f, 0.55f), bottom = new Color(1f, 0.55f, 0.42f) },   // gün batımı
        new Palette { top = new Color(0.16f, 0.15f, 0.38f), bottom = new Color(0.52f, 0.32f, 0.58f) }, // alacakaranlık
        new Palette { top = new Color(0.03f, 0.04f, 0.12f), bottom = new Color(0.12f, 0.16f, 0.36f) }, // gece
    };

    private const int TextureHeight = 64;
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

    private Camera cam;
    private Texture2D texture;
    private readonly Color[] pixels = new Color[TextureHeight];
    private MaterialPropertyBlock props;
    private Color currentTop;
    private Color currentBottom;
    private Palette[] defaultPalettes;   // "Gündüz" teması: Inspector'daki varsayılan döngü

    void Awake()
    {
        cam = GetComponent<Camera>();
        texture = new Texture2D(1, TextureHeight, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };
        props = new MaterialPropertyBlock();

        defaultPalettes = palettes;
        ApplySkyTheme();
    }

    void OnEnable()
    {
        SkinCatalog.SkinChanged += ApplySkyTheme;
    }

    void OnDisable()
    {
        SkinCatalog.SkinChanged -= ApplySkyTheme;
    }

    // Seçili gökyüzü temasının paletlerine geçer ve renkleri yumuşatmadan hemen uygular
    private void ApplySkyTheme()
    {
        BackgroundGradient.Palette[] themed = SkinCatalog.CurrentSky.palettes;
        palettes = themed != null && themed.Length > 0 ? themed : defaultPalettes;
        if (palettes.Length == 0) return;

        GetTargetColors(out currentTop, out currentBottom);
        UpdateTexture();
    }

    void OnDestroy()
    {
        if (texture != null)
            Destroy(texture);
    }

    void LateUpdate()
    {
        if (palettes.Length == 0) return;

        GetTargetColors(out Color targetTop, out Color targetBottom);
        float t = 1f - Mathf.Exp(-blendSpeed * Time.deltaTime);
        Color newTop = Color.Lerp(currentTop, targetTop, t);
        Color newBottom = Color.Lerp(currentBottom, targetBottom, t);

        // Hedefe yeterince yaklaşınca dokuyu her kare yeniden yüklemeyi bırak
        if (ColorDistance(newTop, currentTop) + ColorDistance(newBottom, currentBottom) > 0.0005f)
        {
            currentTop = newTop;
            currentBottom = newBottom;
            UpdateTexture();
        }

        FitToView();
    }

    private void GetTargetColors(out Color top, out Color bottom)
    {
        int floors = tower != null ? tower.floors : 0;
        float p = floors / (float)Mathf.Max(1, floorsPerPalette);
        int i = Mathf.FloorToInt(p) % palettes.Length;
        int j = (i + 1) % palettes.Length;
        float f = p - Mathf.Floor(p);

        top = Color.Lerp(palettes[i].top, palettes[j].top, f);
        bottom = Color.Lerp(palettes[i].bottom, palettes[j].bottom, f);
    }

    private static float ColorDistance(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
    }

    private void UpdateTexture()
    {
        for (int y = 0; y < TextureHeight; y++)
            pixels[y] = Color.Lerp(currentBottom, currentTop, y / (TextureHeight - 1f));
        texture.SetPixels(pixels);
        texture.Apply(false);

        cam.backgroundColor = currentBottom;

        if (backgroundRenderer != null)
        {
            backgroundRenderer.GetPropertyBlock(props);
            props.SetTexture(BaseMapId, texture);
            backgroundRenderer.SetPropertyBlock(props);
        }
    }

    private void FitToView()
    {
        if (backgroundRenderer == null || !cam.orthographic) return;

        // Düzlem kameranın alt objesi; görüş alanını her zaman tamamen kaplasın
        Transform bg = backgroundRenderer.transform;
        float height = cam.orthographicSize * 2f * 1.05f;
        bg.localPosition = new Vector3(0f, 0f, distance);
        bg.localRotation = Quaternion.identity;
        bg.localScale = new Vector3(height * cam.aspect, height, 1f);
    }
}
