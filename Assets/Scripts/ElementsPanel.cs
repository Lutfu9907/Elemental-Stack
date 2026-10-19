using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Başlangıç ekranındaki "Elementler" sayfası: dört elementin ne yaptığı ve kaçıncı katta geldiği.
// Her element kendi renginde görünür; oyuncunun henüz karşılaşmadığı elementin kat etiketinde kilit olur.
public class ElementsPanel : MonoBehaviour
{
    [System.Serializable]
    public class Row
    {
        public Block.BlockElementType element;
        public Image background;
        public Image badge;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI infoText;
        public Image floorPill;
        public TextMeshProUGUI floorText;
        public GameObject lockIcon;
    }

    public Row[] rows;

    void OnEnable()
    {
        Localization.LanguageChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        Localization.LanguageChanged -= Refresh;
    }

    public void Open()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
        gameObject.SetActive(true);
    }

    public void Close()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
        gameObject.SetActive(false);
    }

    private void Refresh()
    {
        foreach (Row row in rows)
        {
            string key = ElementBanner.ElementKey(row.element);
            Color color = Block.ElementColor(row.element);
            bool known = PlayerData.ElementSeenCount(row.element) > 0;

            row.background.color = new Color(color.r, color.g, color.b, 0.13f);
            row.badge.color = color;
            row.nameText.text = Localization.Get("element_" + key);
            row.nameText.color = Darker(color);
            row.infoText.text = Localization.Get("element_" + key + "_info");

            // Kat etiketi: tanınan elementte sade, henüz görülmeyende kilitli ve elementin renginde
            row.floorText.text = Localization.Format("element_floor_short", ElementRules.UnlockFloor(row.element));
            row.lockIcon.SetActive(!known);
            row.floorText.margin = known ? new Vector4(16f, 0f, 16f, 0f) : new Vector4(46f, 0f, 14f, 0f);
            row.floorPill.color = known ? Color.white : color;
            row.floorText.color = known ? Darker(color) : Color.white;
        }
    }

    private static Color Darker(Color c) => new Color(c.r * 0.72f, c.g * 0.72f, c.b * 0.72f, 1f);
}
