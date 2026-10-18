using UnityEngine;

// Perfect yerleşimde bloğun etrafında genişleyip kaybolan beyaz çerçeve.
public class PerfectRipple : MonoBehaviour
{
    private const float Duration = 0.5f;
    private const float Spread = 0.7f;   // çerçevenin bloktan ne kadar uzağa açılacağı (dünya birimi)
    private const float Width = 0.08f;

    private LineRenderer line;
    private Vector2 halfSize;
    private Color color;
    private float elapsed;

    public static void Spawn(Vector3 center, Vector2 size, Material material, Color color, float delay = 0f)
    {
        if (material == null) return;

        var go = new GameObject("PerfectRipple");
        go.transform.position = center;
        PerfectRipple ripple = go.AddComponent<PerfectRipple>();
        ripple.halfSize = size * 0.5f;
        ripple.color = color;
        ripple.elapsed = -delay;

        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 4;
        line.numCornerVertices = 2;
        line.sharedMaterial = material;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        ripple.line = line;
        ripple.Apply(0f);
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float p = Mathf.Clamp01(elapsed / Duration);
        Apply(p);

        if (p >= 1f)
            Destroy(gameObject);
    }

    private void Apply(float p)
    {
        line.enabled = elapsed >= 0f;

        float eased = 1f - Mathf.Pow(1f - p, 3f);
        float grow = Mathf.Lerp(0.05f, Spread, eased);
        Vector2 h = halfSize + Vector2.one * grow;

        line.SetPosition(0, new Vector3(-h.x, 0f, -h.y));
        line.SetPosition(1, new Vector3(-h.x, 0f, h.y));
        line.SetPosition(2, new Vector3(h.x, 0f, h.y));
        line.SetPosition(3, new Vector3(h.x, 0f, -h.y));

        Color c = new Color(color.r, color.g, color.b, color.a * (1f - p));
        line.startColor = c;
        line.endColor = c;
        line.widthMultiplier = Width * (1f - 0.5f * p);
    }
}
