using UnityEngine;

public class Block : MonoBehaviour
{
    public BlockElementType elementType;

    [Header("Renderer")]
    public SpriteRenderer sr;

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
                sr.color = new Color(1f, 0.3f, 0.2f); // Kýrmýzýmsý
                break;

            case BlockElementType.Ice:
                sr.color = new Color(0.3f, 0.7f, 1f); // Mavi
                break;

            case BlockElementType.Wind:
                sr.color = new Color(0.7f, 1f, 0.7f); // Yeþil
                break;

            case BlockElementType.Electric:
                sr.color = new Color(1f, 1f, 0.3f); // Sarý
                break;

            case BlockElementType.None:
                sr.color = Color.white; // Ýlk blok veya boþ tip için
                break;
        }
    }
}
