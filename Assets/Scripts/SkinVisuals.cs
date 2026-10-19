using UnityEngine;

// Blok temalarının görsel zenginliği: doku, ışıyan kenarlar ve yerleşme efektleri.
//   Şeker : pastel şeritler; konunca renkli şeker serpintisi
//   Ahşap : tahta damarları; konunca talaş
//   Neon  : koyu bloklar, kenarları kat kat değişen renklerde ışır; konunca neon halka
//   Altın : fırçalanmış metal; kulenin tepesinde ara ara parıltılar, konunca altın tozu
// Ayrıca mağaza kartlarındaki küçük resimleri tutar. Sahnede tek tanedir.
public class SkinVisuals : MonoBehaviour
{
    public static SkinVisuals Instance { get; private set; }

    [Header("Textures")]
    public Texture2D candyMap;
    public Texture2D woodMap;
    public Texture2D goldMap;
    public Texture2D neonEdgeMap;   // ışıma haritası: kenarlar beyaz, yüzey neredeyse siyah

    [Header("Placement effects")]
    public GameObject candyBurst;
    public GameObject woodBurst;
    public GameObject goldBurst;
    public GameObject goldSparkle;  // kulenin tepesini izleyen sürekli parıltı
    public Material neonRingMaterial;
    [Tooltip("Oyun başında parıltının duracağı blok (taban)")]
    public Transform initialTop;

    [Header("Shop previews")]
    public string[] previewIds;
    public Sprite[] previewSprites;

    private ParticleSystem sparkle;
    private Transform lastPlaced;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void OnEnable()
    {
        SkinCatalog.SkinChanged += UpdateSparkle;
    }

    void OnDisable()
    {
        SkinCatalog.SkinChanged -= UpdateSparkle;
    }

    void Start()
    {
        // Parıltı bu objenin çocuğudur, kulenin değil: kule en üst bloğunu çocuklarından bulur
        if (goldSparkle != null)
            sparkle = Instantiate(goldSparkle, transform).GetComponent<ParticleSystem>();
        lastPlaced = initialTop;
        UpdateSparkle();
    }

    void LateUpdate()
    {
        // Parıltı, kulenin en üstteki birkaç katını kaplar ve kuleyle birlikte yükselir
        if (sparkle != null && lastPlaced != null)
            sparkle.transform.position = lastPlaced.position + Vector3.down * 1.2f;
    }

    public Texture BaseMap(string skinId)
    {
        switch (skinId)
        {
            case "candy": return candyMap;
            case "wood": return woodMap;
            case "gold": return goldMap;
            default: return null;
        }
    }

    public Texture EmissionMap(string skinId)
    {
        return skinId == "neon" ? neonEdgeMap : null;
    }

    public Sprite Preview(string skinId)
    {
        if (previewIds == null) return null;
        int index = System.Array.IndexOf(previewIds, skinId);
        return index >= 0 && index < previewSprites.Length ? previewSprites[index] : null;
    }

    // TowerController her yerleştirmeden sonra çağırır; elementsiz bloklarda temanın efekti oynar
    public void OnBlockPlaced(Block block, float blockHeight)
    {
        lastPlaced = block.transform;
        if (block.elementType != Block.BlockElementType.None) return;

        Skin skin = SkinCatalog.CurrentBlock;
        Transform t = block.transform;
        switch (skin.id)
        {
            case "candy": Burst(candyBurst, t); break;
            case "wood": Burst(woodBurst, t); break;
            case "gold": Burst(goldBurst, t); break;
            case "neon":
                if (skin.emissionColors != null && skin.emissionColors.Length > 0)
                {
                    Color neon = skin.emissionColors[block.floor % skin.emissionColors.Length];
                    Vector3 ringCenter = t.position + Vector3.down * (blockHeight * 0.5f - 0.01f);
                    PerfectRipple.Spawn(ringCenter, new Vector2(t.localScale.x, t.localScale.z), neonRingMaterial, neon);
                }
                break;
        }
    }

    // Efekt bloğun hacminden çıkar (prefab'ın kutu şekli bloğun ölçüsünü alır) ve kendini yok eder
    private static void Burst(GameObject prefab, Transform block)
    {
        if (prefab == null) return;
        GameObject fx = Instantiate(prefab, block.position, Quaternion.identity);
        fx.transform.localScale = block.localScale;
    }

    private void UpdateSparkle()
    {
        if (sparkle == null) return;
        bool gold = SkinCatalog.CurrentBlock.id == "gold";
        if (gold && !sparkle.isPlaying) sparkle.Play();
        if (!gold && sparkle.isPlaying) sparkle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }
}
