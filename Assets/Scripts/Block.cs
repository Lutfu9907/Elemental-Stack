using UnityEngine;

public class Block : MonoBehaviour
{
    public enum BlockElementType { Fire, Ice, Wind, Electric, None }
    public BlockElementType elementType;

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

    public void ApplyElementColor()
    {
        switch (elementType)
        {
            case BlockElementType.Fire:
                blockColor = new Color(1f, 0.42f, 0.33f); // Mercan kırmızısı
                break;
            case BlockElementType.Ice:
                blockColor = new Color(0.36f, 0.72f, 0.98f); // Buz mavisi
                break;
            case BlockElementType.Wind:
                blockColor = new Color(0.45f, 0.85f, 0.6f); // Nane yeşili
                break;
            case BlockElementType.Electric:
                blockColor = new Color(1f, 0.82f, 0.3f); // Altın sarısı
                break;
            case BlockElementType.None:
                blockColor = new Color(0.92f, 0.92f, 0.95f);
                break;
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
