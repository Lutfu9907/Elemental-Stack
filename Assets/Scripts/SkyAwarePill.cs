using UnityEngine;
using UnityEngine.UI;

// Başlangıç ekranındaki kutu ve butonlar gökyüzüne uyar: gündüz beyaz, koyu gökyüzünde (uzay)
// yarı saydam lacivert cam olur ve yazılarla simgeler beyaza döner. Böylece arka plan kutuların içinden görünür.
public class SkyAwarePill : MonoBehaviour
{
    public Image background;
    public Graphic[] foregrounds;   // koyu gökyüzünde beyaza dönen yazılar ve simgeler

    private static readonly Color GlassColor = new Color(0.07f, 0.09f, 0.22f, 0.5f);

    private Color dayBackground;
    private Color[] dayForegrounds;

    void Awake()
    {
        dayBackground = background.color;
        dayForegrounds = new Color[foregrounds.Length];
        for (int i = 0; i < foregrounds.Length; i++)
            dayForegrounds[i] = foregrounds[i].color;
    }

    void OnEnable()
    {
        SkinCatalog.SkinChanged += Apply;
        Apply();
    }

    void OnDisable()
    {
        SkinCatalog.SkinChanged -= Apply;
    }

    private void Apply()
    {
        bool dark = SkinCatalog.CurrentSky.darkSky;
        background.color = dark ? GlassColor : dayBackground;
        for (int i = 0; i < foregrounds.Length; i++)
            foregrounds[i].color = dark ? Color.white : dayForegrounds[i];
    }
}
