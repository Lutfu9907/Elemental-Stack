using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Başlangıç ekranındaki "Elementler" sayfası: dört elementin ne yaptığı.
// Oyuncunun henüz karşılaşmadığı element kilitli görünür ve hangi katta geleceği yazar.
public class ElementsPanel : MonoBehaviour
{
    [System.Serializable]
    public class Row
    {
        public Block.BlockElementType element;
        public Image badge;
        public Image icon;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI infoText;
    }

    public Row[] rows;

    private static readonly Color LockedBadge = new Color(0.85f, 0.88f, 0.93f);
    private static readonly Color LockedText = new Color(0.42f, 0.5f, 0.62f);
    private static readonly Color Navy = new Color(0.137f, 0.224f, 0.357f);

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
            int floor = ElementRules.UnlockFloor(row.element);
            bool known = PlayerData.ElementSeenCount(row.element) > 0;

            row.badge.color = known ? Block.ElementColor(row.element) : LockedBadge;
            row.icon.color = known ? Color.white : LockedText;
            row.nameText.color = known ? Navy : LockedText;
            row.nameText.text = known
                ? Localization.Get("element_" + key) + "  <size=70%><color=#6B7A90>" + Localization.Format("element_floor", floor) + "</color></size>"
                : "? ? ?";
            row.infoText.text = known ? Localization.Get("element_" + key + "_info") : Localization.Format("element_locked", floor);
        }
    }
}
