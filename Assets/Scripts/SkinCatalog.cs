using System;
using UnityEngine;

public enum SkinType { Block, Sky }

// Mağazadaki bir tema. Blok temaları elementsiz blokların ve tabanın görünümünü,
// gökyüzü temaları arka planı değiştirir. Element renkleri hiçbir temada değişmez.
public class Skin
{
    public string id;
    public string nameKey;           // Localization anahtarı
    public SkinType type;
    public int price;

    // Blok teması
    public Color[] plainColors;      // elementsiz katlar bu renkler arasında sırayla döner
    public Color baseColor;
    public float smoothness = 0.15f;
    public float metallic;

    // Gökyüzü teması
    public BackgroundGradient.Palette[] palettes;   // null: varsayılan gündüz → gece döngüsü
    public bool darkSky;             // koyu gökyüzünde başlık beyaz yazılır

    // Mağaza kartındaki küçük önizleme (üstten alta)
    public Color[] preview;
}

public static class SkinCatalog
{
    public const string DefaultBlockSkin = "classic";
    public const string DefaultSkySkin = "day";

    public static readonly Skin[] All =
    {
        // --- Bloklar
        new Skin
        {
            id = "classic", nameKey = "skin_classic", type = SkinType.Block, price = 0,
            plainColors = new[] { C(0.97f, 0.95f, 0.9f), C(0.86f, 0.88f, 0.9f), C(0.95f, 0.93f, 0.95f), C(0.88f, 0.9f, 0.93f) },
            baseColor = C(0.84f, 0.89f, 0.98f),
        },
        new Skin
        {
            id = "candy", nameKey = "skin_candy", type = SkinType.Block, price = 300,
            plainColors = new[] { C(1f, 0.86f, 0.9f), C(0.86f, 0.97f, 0.92f), C(0.9f, 0.88f, 1f), C(1f, 0.92f, 0.85f) },
            baseColor = C(1f, 0.8f, 0.88f), smoothness = 0.35f,
        },
        new Skin
        {
            id = "wood", nameKey = "skin_wood", type = SkinType.Block, price = 500,
            plainColors = new[] { C(0.78f, 0.6f, 0.42f), C(0.7f, 0.52f, 0.36f), C(0.84f, 0.68f, 0.5f), C(0.66f, 0.48f, 0.32f) },
            baseColor = C(0.5f, 0.36f, 0.25f), smoothness = 0.05f,
        },
        new Skin
        {
            id = "neon", nameKey = "skin_neon", type = SkinType.Block, price = 800,
            plainColors = new[] { C(0.13f, 0.15f, 0.28f), C(0.16f, 0.18f, 0.33f), C(0.12f, 0.13f, 0.25f), C(0.18f, 0.16f, 0.32f) },
            baseColor = C(0.1f, 0.11f, 0.2f), smoothness = 0.6f,
        },
        new Skin
        {
            id = "gold", nameKey = "skin_gold", type = SkinType.Block, price = 1500,
            plainColors = new[] { C(0.95f, 0.76f, 0.32f), C(0.86f, 0.64f, 0.22f), C(0.98f, 0.82f, 0.42f), C(0.82f, 0.6f, 0.2f) },
            baseColor = C(0.7f, 0.5f, 0.16f), smoothness = 0.65f, metallic = 0.3f,
        },

        // --- Gökyüzü
        new Skin
        {
            id = "day", nameKey = "sky_day", type = SkinType.Sky, price = 0,
            palettes = null,
            preview = new[] { C(0.33f, 0.62f, 0.95f), C(0.58f, 0.78f, 0.98f), C(0.82f, 0.93f, 1f) },
        },
        new Skin
        {
            id = "sunset", nameKey = "sky_sunset", type = SkinType.Sky, price = 400,
            palettes = new[]
            {
                P(C(0.98f, 0.55f, 0.45f), C(1f, 0.85f, 0.6f)),
                P(C(0.85f, 0.4f, 0.55f), C(1f, 0.7f, 0.5f)),
                P(C(0.5f, 0.25f, 0.5f), C(0.95f, 0.5f, 0.45f)),
                P(C(0.25f, 0.15f, 0.4f), C(0.7f, 0.35f, 0.5f)),
            },
            preview = new[] { C(0.85f, 0.4f, 0.55f), C(0.95f, 0.6f, 0.5f), C(1f, 0.85f, 0.6f) },
        },
        new Skin
        {
            id = "space", nameKey = "sky_space", type = SkinType.Sky, price = 1000, darkSky = true,
            palettes = new[]
            {
                P(C(0.05f, 0.05f, 0.15f), C(0.2f, 0.12f, 0.35f)),
                P(C(0.02f, 0.03f, 0.1f), C(0.12f, 0.2f, 0.4f)),
                P(C(0.1f, 0.02f, 0.15f), C(0.3f, 0.1f, 0.35f)),
                P(C(0.01f, 0.01f, 0.05f), C(0.1f, 0.1f, 0.25f)),
            },
            preview = new[] { C(0.02f, 0.03f, 0.1f), C(0.1f, 0.08f, 0.25f), C(0.2f, 0.12f, 0.35f) },
        },
    };

    public static event Action SkinChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        SkinChanged = null;
    }

    public static Skin Get(string id)
    {
        foreach (Skin skin in All)
            if (skin.id == id)
                return skin;
        return null;
    }

    public static Skin CurrentBlock => Get(PlayerData.SelectedBlockSkin) ?? Get(DefaultBlockSkin);
    public static Skin CurrentSky => Get(PlayerData.SelectedSkySkin) ?? Get(DefaultSkySkin);

    // Seçim değişince bloklar, arka plan ve başlık hemen yeni temaya geçsin
    public static void NotifyChanged()
    {
        SkinChanged?.Invoke();
    }

    // Önizleme renkleri; blok temasında üstten alta iki kat rengi ve taban
    public static Color[] PreviewColors(Skin skin)
    {
        if (skin.preview != null) return skin.preview;
        return new[] { skin.plainColors[0], skin.plainColors[1], skin.baseColor };
    }

    private static Color C(float r, float g, float b) => new Color(r, g, b);

    private static BackgroundGradient.Palette P(Color top, Color bottom) =>
        new BackgroundGradient.Palette { top = top, bottom = bottom };
}
