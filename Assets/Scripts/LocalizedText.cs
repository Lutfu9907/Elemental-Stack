using TMPro;
using UnityEngine;

// Bir TextMeshPro yazısını Localization anahtarına bağlar; dil değişince yazı kendiliğinden güncellenir.
[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    public string key;

    private TMP_Text text;

    void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        Localization.LanguageChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        Localization.LanguageChanged -= Refresh;
    }

    public void Refresh()
    {
        if (!string.IsNullOrEmpty(key))
            text.text = Localization.Get(key);
    }
}
