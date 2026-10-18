using UnityEngine;

// RectTransform'u telefonun güvenli alanına (çentik, kamera deliği, alt çubuk dışı) sığdırır.
// İçine konan UI öğeleri ekranın kesik kısımlarının altında kalmaz.
[RequireComponent(typeof(RectTransform))]
public class SafeArea : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;

    void Awake()
    {
        rectTransform = (RectTransform)transform;
        Apply();
    }

    void Update()
    {
        // Ekran döndüğünde ya da çözünürlük değiştiğinde yeniden hesapla
        if (Screen.safeArea != lastSafeArea ||
            Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
            Apply();
    }

    private void Apply()
    {
        lastSafeArea = Screen.safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        if (Screen.width <= 0 || Screen.height <= 0) return;

        Vector2 min = lastSafeArea.min;
        Vector2 max = lastSafeArea.max;
        min.x /= Screen.width;
        min.y /= Screen.height;
        max.x /= Screen.width;
        max.y /= Screen.height;

        rectTransform.anchorMin = min;
        rectTransform.anchorMax = max;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
