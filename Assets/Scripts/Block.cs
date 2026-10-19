using UnityEngine;

public class Block : MonoBehaviour
{
    public enum BlockElementType { Fire, Ice, Wind, Electric, None }
    public BlockElementType elementType;

    [Tooltip("Kulede kaçıncı kat (1'den başlar). Elementsiz bloklar rengini buna göre alır; taban için 0.")]
    public int floor = 0;

    [Header("Renderer")]
    public Renderer rend;

    [HideInInspector]
    public Color blockColor; // Her bloğun renk bilgisi burada tutulacak

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
    private static readonly int MetallicId = Shader.PropertyToID("_Metallic");
    private MaterialPropertyBlock props;

    private void Awake()
    {
        if (rend == null)
            rend = GetComponentInChildren<Renderer>();
    }

    private void Start()
    {
        ApplyElementColor();
    }

    // Mağazada tema değişince kuledeki bloklar da hemen yeni temaya geçer
    private void OnEnable()
    {
        SkinCatalog.SkinChanged += ApplyElementColor;
    }

    private void OnDisable()
    {
        SkinCatalog.SkinChanged -= ApplyElementColor;
    }

    // Element renkleri doygun ve canlı; elementsiz bloklar neredeyse renksiz.
    // Böylece kulede renkli olan her blok bir elementtir ve hemen göze çarpar.
    public static Color ElementColor(BlockElementType type)
    {
        switch (type)
        {
            case BlockElementType.Fire: return new Color(1f, 0.36f, 0.24f);      // Ateş kırmızısı
            case BlockElementType.Ice: return new Color(0.25f, 0.66f, 1f);       // Buz mavisi
            case BlockElementType.Wind: return new Color(0.3f, 0.85f, 0.52f);    // Rüzgâr yeşili
            case BlockElementType.Electric: return new Color(1f, 0.8f, 0.15f);   // Şimşek sarısı
            default: return new Color(0.84f, 0.89f, 0.98f);                      // Taban: açık mavi
        }
    }

    public void ApplyElementColor()
    {
        // Elementsiz katlar ve taban seçili blok temasının renklerini alır; element renkleri sabit
        Skin skin = SkinCatalog.CurrentBlock;
        bool themed = elementType == BlockElementType.None;
        if (!themed)
            blockColor = ElementColor(elementType);
        else if (floor > 0)
            blockColor = skin.plainColors[floor % skin.plainColors.Length];
        else
            blockColor = skin.baseColor;

        if (rend == null) return;

        // Tek materyal paylaşılır; renk ve yüzey blok başına property block ile verilir
        if (props == null)
            props = new MaterialPropertyBlock();
        rend.GetPropertyBlock(props);
        props.SetColor(BaseColorId, blockColor);
        props.SetFloat(SmoothnessId, themed ? skin.smoothness : 0.15f);
        props.SetFloat(MetallicId, themed ? skin.metallic : 0f);
        rend.SetPropertyBlock(props);
    }

    // Bloğu kendi renginde parlatır (1'in üstü bloom ile ışık saçar). 0 = parlama yok.
    public void SetGlow(float intensity)
    {
        if (rend == null) return;
        if (props == null)
            props = new MaterialPropertyBlock();
        rend.GetPropertyBlock(props);
        props.SetColor(EmissionColorId, blockColor * intensity);
        rend.SetPropertyBlock(props);
    }
}
