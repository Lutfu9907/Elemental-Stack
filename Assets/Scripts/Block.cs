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

    // Element renkleri canlı, elementsiz bloklar soluk pastel; böylece element blokları hemen fark edilir
    public static Color ElementColor(BlockElementType type)
    {
        switch (type)
        {
            case BlockElementType.Fire: return new Color(1f, 0.42f, 0.33f);      // Mercan kırmızısı
            case BlockElementType.Ice: return new Color(0.36f, 0.72f, 0.98f);    // Buz mavisi
            case BlockElementType.Wind: return new Color(0.45f, 0.85f, 0.6f);    // Nane yeşili
            case BlockElementType.Electric: return new Color(1f, 0.82f, 0.3f);   // Altın sarısı
            default: return new Color(0.84f, 0.89f, 0.98f);                      // Taban: açık mavi
        }
    }

    public void ApplyElementColor()
    {
        if (elementType == BlockElementType.None && floor > 0)
        {
            // Elementsiz katlar kat kat değişen soluk pastel tonlar alır
            float hue = Mathf.Repeat(0.58f + floor * 0.035f, 1f);
            blockColor = Color.HSVToRGB(hue, 0.22f, 0.97f);
        }
        else
        {
            blockColor = ElementColor(elementType);
        }

        if (rend == null) return;

        // Tek materyal paylaşılır; renk blok başına property block ile verilir
        if (props == null)
            props = new MaterialPropertyBlock();
        rend.GetPropertyBlock(props);
        props.SetColor(BaseColorId, blockColor);
        rend.SetPropertyBlock(props);
    }
}
