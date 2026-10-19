using UnityEngine;
using Element = Block.BlockElementType;

// Kayan element bloğunu göze sokar: kendi renginde nabız gibi parlar, etrafında parlak bir
// çerçeve ve üstünde elementin simgesi süzülür. Ateş titreşir, elektrik çakar, rüzgâr bloğu salınır.
// Blok yerleşince (Placed) hepsi kaldırılır ve blok hafif bir kalıcı parıltıyla kalır.
public class ElementVisual : MonoBehaviour
{
    private const float RestingGlow = 0.12f;   // yerleşmiş element blokları kulede hafifçe parlamaya devam eder
    private const float FrameGap = 0.07f;      // çerçevenin bloktan uzaklığı (dünya birimi)
    private const float FrameWidth = 0.07f;
    private const float IconHeight = 0.95f;    // simgenin bloğun üstündeki yüksekliği
    private const float IconSize = 0.85f;

    private Block block;
    private Element element;
    private LineRenderer topFrame;
    private LineRenderer bottomFrame;
    private Transform icon;
    private Camera cam;
    private float age;
    private float flash;   // elektrik çakması için anlık parlama

    public static ElementVisual Attach(Block block, Sprite iconSprite, Sprite badgeSprite, Material iconMaterial, Material frameMaterial)
    {
        var visual = block.gameObject.AddComponent<ElementVisual>();
        visual.block = block;
        visual.element = block.elementType;
        visual.cam = Camera.main;

        Color color = Block.ElementColor(visual.element);
        visual.topFrame = CreateFrame(frameMaterial, color, "TopFrame");
        visual.bottomFrame = CreateFrame(frameMaterial, color, "BottomFrame");
        visual.icon = CreateIcon(iconSprite, badgeSprite, iconMaterial, color);
        visual.Update();
        return visual;
    }

    void Update()
    {
        age += Time.deltaTime;
        float pulse = 0.5f + 0.5f * Mathf.Sin(age * 6f);

        // Parlama: her elementin kendi ritmi
        float glow;
        switch (element)
        {
            case Element.Fire:
                glow = 0.6f + 0.6f * Mathf.PerlinNoise(age * 8f, 0f);   // ateş gibi titrek
                break;
            case Element.Electric:
                if (Random.value < Time.deltaTime * 3f) flash = 1f;     // ara ara çakar
                flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * 6f);
                glow = 0.4f + 0.3f * pulse + 1.2f * flash;
                break;
            default:
                glow = 0.4f + 0.5f * pulse;
                break;
        }
        block.SetGlow(glow);

        // Rüzgâr bloğu kayarken hafifçe salınır
        if (element == Element.Wind)
            transform.localRotation = Quaternion.Euler(0f, Mathf.Sin(age * 4.5f) * 4f, 0f);

        UpdateFrame(topFrame, 0.5f, 0.55f + 0.45f * pulse);
        UpdateFrame(bottomFrame, -0.5f, 0.35f + 0.35f * pulse);

        if (icon != null)
        {
            icon.position = transform.position + Vector3.up * (IconHeight + Mathf.Sin(age * 3f) * 0.08f);
            if (cam != null)
                icon.rotation = cam.transform.rotation;
            icon.localScale = Vector3.one * IconSize * (1f + 0.06f * pulse);
        }
    }

    public void Placed()
    {
        transform.localRotation = Quaternion.identity;
        block.SetGlow(RestingGlow);
        if (topFrame != null) Destroy(topFrame.gameObject);
        if (bottomFrame != null) Destroy(bottomFrame.gameObject);
        if (icon != null) Destroy(icon.gameObject);
        Destroy(this);
    }

    // Blok yok olursa (ör. ıskalayıp düşerse) ekstra objeler de temizlensin
    void OnDestroy()
    {
        if (icon != null) Destroy(icon.gameObject);
        if (topFrame != null) Destroy(topFrame.gameObject);
        if (bottomFrame != null) Destroy(bottomFrame.gameObject);
    }

    // Bloğun üst (y = 0.5) ya da alt (y = -0.5) kenarını biraz dışarıdan çevreleyen çerçeve.
    // Dünya koordinatında çizilir ki çizgi kalınlığı bloğun ölçeğinden etkilenmesin.
    private void UpdateFrame(LineRenderer frame, float localY, float alpha)
    {
        if (frame == null) return;

        Vector3 scale = transform.localScale;
        float hx = 0.5f + FrameGap / Mathf.Max(0.01f, scale.x);
        float hz = 0.5f + FrameGap / Mathf.Max(0.01f, scale.z);
        frame.SetPosition(0, transform.TransformPoint(new Vector3(-hx, localY, -hz)));
        frame.SetPosition(1, transform.TransformPoint(new Vector3(-hx, localY, hz)));
        frame.SetPosition(2, transform.TransformPoint(new Vector3(hx, localY, hz)));
        frame.SetPosition(3, transform.TransformPoint(new Vector3(hx, localY, -hz)));

        Color c = frame.startColor;
        c.a = alpha;
        frame.startColor = c;
        frame.endColor = c;
    }

    private static LineRenderer CreateFrame(Material material, Color color, string name)
    {
        var go = new GameObject(name);
        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = true;
        line.positionCount = 4;
        line.numCornerVertices = 2;
        line.widthMultiplier = FrameWidth;
        line.sharedMaterial = material;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        Color bright = Color.Lerp(color, Color.white, 0.35f);
        line.startColor = bright;
        line.endColor = bright;
        return line;
    }

    // Beyaz yuvarlak rozet üstünde element renginde simge; kameraya dönük durur
    private static Transform CreateIcon(Sprite iconSprite, Sprite badgeSprite, Material material, Color color)
    {
        if (iconSprite == null) return null;

        var root = new GameObject("ElementIcon").transform;

        if (badgeSprite != null)
        {
            var badge = new GameObject("Badge").AddComponent<SpriteRenderer>();
            badge.transform.SetParent(root, false);
            badge.sprite = badgeSprite;
            badge.sharedMaterial = material;
            badge.color = Color.white;
            badge.sortingOrder = 10;
            float badgeScale = 1f / badgeSprite.bounds.size.x;   // 1 dünya birimi çap
            badge.transform.localScale = Vector3.one * badgeScale;
        }

        var symbol = new GameObject("Symbol").AddComponent<SpriteRenderer>();
        symbol.transform.SetParent(root, false);
        symbol.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        symbol.sprite = iconSprite;
        symbol.sharedMaterial = material;
        symbol.color = color * 0.9f + new Color(0f, 0f, 0f, 0.1f);
        symbol.sortingOrder = 11;
        float symbolScale = 0.68f / iconSprite.bounds.size.x;
        symbol.transform.localScale = Vector3.one * symbolScale;

        return root;
    }
}
