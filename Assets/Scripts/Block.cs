using UnityEngine;

public class Block : MonoBehaviour
{
    public enum BlockElementType { Fire, Ice, Wind, Electric, None }
    public BlockElementType elementType;

    [Header("Renderer")]
    public SpriteRenderer sr;

    [HideInInspector]
    public Color blockColor; // Her bloğun renk bilgisi burada tutulacak

    private void Awake()
    {
        if (sr == null)
            sr = GetComponentInChildren<SpriteRenderer>();
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
                blockColor = new Color(1f, 0.3f, 0.2f); // Kırmızımsı
                break;
            case BlockElementType.Ice:
                blockColor = new Color(0.3f, 0.7f, 1f); // Mavi
                break;
            case BlockElementType.Wind:
                blockColor = new Color(0.7f, 1f, 0.7f); // Yeşil
                break;
            case BlockElementType.Electric:
                blockColor = new Color(1f, 1f, 0.3f); // Sarı
                break;
            case BlockElementType.None:
                blockColor = Color.white;
                break;
        }

        if (sr != null)
            sr.color = blockColor;
    }
}
