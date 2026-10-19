using UnityEngine;

// İlk oyunun rehberi: "Bloğu bırakmak için dokun" yazısı ve nabız gibi atan bir dokunma halkası.
// İlk üç blok daha yavaş kayar; üç kat konunca rehber kaybolur ve bir daha gösterilmez.
public class TutorialGuide : MonoBehaviour
{
    public TowerController tower;
    public CanvasGroup hint;
    public RectTransform tapRing;
    public int floorsToLearn = 3;
    public float slowSpeed = 0.6f;

    private bool active;
    private float fade = 1f;

    void Start()
    {
        active = !PlayerData.TutorialDone && tower != null;
        hint.alpha = 0f;
        if (!active)
        {
            gameObject.SetActive(false);
            return;
        }
        tower.speedMultiplier = slowSpeed;
        tower.FloorPlaced += OnFloorPlaced;
    }

    void OnDestroy()
    {
        if (tower != null)
            tower.FloorPlaced -= OnFloorPlaced;
    }

    void Update()
    {
        if (tower == null) return;

        // Oyun başlayınca görünür, bitince kaybolur
        float target = active && tower.gameStarted ? 1f : 0f;
        fade = Mathf.MoveTowards(fade, target, Time.unscaledDeltaTime * 3f);
        hint.alpha = tower.gameStarted ? fade : 0f;

        // Dokunma halkası büyüyüp sönerek "buraya dokun" der
        float t = Mathf.Repeat(Time.unscaledTime * 0.9f, 1f);
        tapRing.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.3f, t);
        var ringImage = tapRing.GetComponent<UnityEngine.UI.Image>();
        if (ringImage != null)
        {
            Color c = ringImage.color;
            c.a = 1f - t;
            ringImage.color = c;
        }

        if (!active && fade <= 0f)
            gameObject.SetActive(false);
    }

    private void OnFloorPlaced(int floor, Transform block)
    {
        if (!active || floor < floorsToLearn) return;
        active = false;
        tower.speedMultiplier = 1f;
        PlayerData.MarkTutorialDone();
    }
}
