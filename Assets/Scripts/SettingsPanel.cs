using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ayarlar ekranı: müzik, ses efektleri, titreşim, grafik kalitesi, dil ve gizlilik politikası.
// Her satırın sağında değeri gösteren bir buton var; dokununca değer değişir.
public class SettingsPanel : MonoBehaviour
{
    public TextMeshProUGUI musicValue;
    public TextMeshProUGUI sfxValue;
    public TextMeshProUGUI hapticsValue;
    public TextMeshProUGUI qualityValue;
    public TextMeshProUGUI languageValue;
    public Image musicPill;
    public Image sfxPill;
    public Image hapticsPill;
    public TextMeshProUGUI versionText;
    public TextMeshProUGUI restoreText;

    private static readonly Color OnColor = new Color(1f, 0.824f, 0.302f);
    private static readonly Color OffColor = new Color(0.85f, 0.88f, 0.93f);

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
        Click();
        gameObject.SetActive(true);
    }

    public void Close()
    {
        Click();
        gameObject.SetActive(false);
    }

    public void ToggleMusic()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.MusicEnabled = !AudioManager.Instance.MusicEnabled;
        Click();
        Refresh();
    }

    public void ToggleSfx()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SfxEnabled = !AudioManager.Instance.SfxEnabled;
        Click();
        Refresh();
    }

    public void ToggleHaptics()
    {
        Haptics.Enabled = !Haptics.Enabled;
        PlayerPrefs.Save();
        if (Haptics.Enabled) Haptics.Light();   // açınca hissettir
        Click();
        Refresh();
    }

    public void ToggleQuality()
    {
        GraphicsQuality.High = !GraphicsQuality.High;
        Click();
        Refresh();
    }

    public void ToggleLanguage()
    {
        Click();
        Localization.NextLanguage();   // LanguageChanged → Refresh
    }

    public void OpenPrivacy()
    {
        Click();
        Application.OpenURL(GameLinks.PrivacyPolicy);
    }

    // Reklamsız sürümü yeniden yüklenen telefona geri getirir
    public void RestorePurchases()
    {
        Click();
        StoreService.Restore(ok =>
        {
            if (restoreText != null)
                restoreText.text = Localization.Get(ok ? "ads_removed" : "restore_none");
        });
    }

    private void Refresh()
    {
        AudioManager audio = AudioManager.Instance;
        SetToggle(musicValue, musicPill, audio == null || audio.MusicEnabled);
        SetToggle(sfxValue, sfxPill, audio == null || audio.SfxEnabled);
        SetToggle(hapticsValue, hapticsPill, Haptics.Enabled);
        qualityValue.text = Localization.Get(GraphicsQuality.High ? "quality_high" : "quality_low");
        languageValue.text = Localization.Get("language_name");
        if (restoreText != null)
            restoreText.text = Localization.Get("restore_purchases");
        if (versionText != null)
            versionText.text = "v" + Application.version;
    }

    private static void SetToggle(TextMeshProUGUI label, Image pill, bool on)
    {
        label.text = Localization.Get(on ? "on" : "off");
        if (pill != null)
            pill.color = on ? OnColor : OffColor;
    }

    private static void Click()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
    }
}
