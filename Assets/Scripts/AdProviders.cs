using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Reklam ağıyla konuşan katman. AdsManager hangisinin kullanılacağını seçer:
//   Editör                       → SimulatedAdProvider ("Test reklamı" kartı, akışları denemek için)
//   GOOGLE_MOBILE_ADS tanımlıysa → AdMobProvider (Google Mobile Ads eklentisi kurulunca)
//   Diğer                        → NoAdProvider (reklam yok; ödüllü butonlar gizlenir)
public interface IAdProvider
{
    void Initialize(MonoBehaviour host);
    bool RewardedReady { get; }
    bool InterstitialReady { get; }
    void ShowRewarded(Action<bool> done);   // true: oyuncu ödülü hak etti
    void ShowInterstitial(Action done);
}

public class NoAdProvider : IAdProvider
{
    public void Initialize(MonoBehaviour host) { }
    public bool RewardedReady => false;
    public bool InterstitialReady => false;
    public void ShowRewarded(Action<bool> done) => done?.Invoke(false);
    public void ShowInterstitial(Action done) => done?.Invoke();
}

// Editörde reklam akışını denemek için: ekranı kaplayan "Test reklamı" kartı birkaç saniye görünür
public class SimulatedAdProvider : IAdProvider
{
    private MonoBehaviour host;

    public void Initialize(MonoBehaviour host) { this.host = host; }
    public bool RewardedReady => true;
    public bool InterstitialReady => true;

    public void ShowRewarded(Action<bool> done) =>
        host.StartCoroutine(ShowFake(Localization.Get("test_ad") + "\n<size=60%>(rewarded)</size>", 2f, () => done?.Invoke(true)));

    public void ShowInterstitial(Action done) =>
        host.StartCoroutine(ShowFake(Localization.Get("test_ad") + "\n<size=60%>(interstitial)</size>", 1.5f, done));

    private static IEnumerator ShowFake(string label, float seconds, Action done)
    {
        var canvasGo = new GameObject("TestAd", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(canvasGo.transform, false);
        var bgRt = (RectTransform)bg.transform;
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.95f);

        var text = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        text.transform.SetParent(bg.transform, false);
        var textRt = text.rectTransform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = textRt.offsetMax = Vector2.zero;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 64;
        text.color = Color.white;
        text.text = label;

        yield return new WaitForSecondsRealtime(seconds);
        UnityEngine.Object.Destroy(canvasGo);
        done?.Invoke();
    }
}

#if GOOGLE_MOBILE_ADS
// Google AdMob. Kurulum: docs/RELEASE.md → "Reklamlar". Önce Google'ın test kimlikleri kullanılır;
// yayından önce AdMob panelindeki gerçek reklam birimi kimlikleriyle değiştirilmeli.
public class AdMobProvider : IAdProvider
{
    private const string RewardedId = "ca-app-pub-3940256099942544/5224354917";      // Google test kimliği
    private const string InterstitialId = "ca-app-pub-3940256099942544/1033173712";  // Google test kimliği

    private GoogleMobileAds.Api.RewardedAd rewarded;
    private GoogleMobileAds.Api.InterstitialAd interstitial;

    public void Initialize(MonoBehaviour host)
    {
        GoogleMobileAds.Api.MobileAds.RaiseAdEventsOnUnityMainThread = true;

        // Avrupa (GDPR) için önce izin formu; izin varsa reklamlar başlar
        var request = new GoogleMobileAds.Ump.Api.ConsentRequestParameters();
        GoogleMobileAds.Ump.Api.ConsentInformation.Update(request, updateError =>
        {
            GoogleMobileAds.Ump.Api.ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
            {
                if (GoogleMobileAds.Ump.Api.ConsentInformation.CanRequestAds())
                    GoogleMobileAds.Api.MobileAds.Initialize(status =>
                    {
                        LoadRewarded();
                        LoadInterstitial();
                    });
            });
        });
    }

    public bool RewardedReady => rewarded != null && rewarded.CanShowAd();
    public bool InterstitialReady => interstitial != null && interstitial.CanShowAd();

    public void ShowRewarded(Action<bool> done)
    {
        if (!RewardedReady) { done?.Invoke(false); return; }
        bool earned = false, finished = false;
        void Finish(bool result)
        {
            if (finished) return;
            finished = true;
            done?.Invoke(result);
            LoadRewarded();
        }
        rewarded.OnAdFullScreenContentClosed += () => Finish(earned);
        rewarded.OnAdFullScreenContentFailed += error => Finish(false);
        rewarded.Show(reward => earned = true);
    }

    public void ShowInterstitial(Action done)
    {
        if (!InterstitialReady) { done?.Invoke(); return; }
        bool finished = false;
        void Finish()
        {
            if (finished) return;
            finished = true;
            done?.Invoke();
            LoadInterstitial();
        }
        interstitial.OnAdFullScreenContentClosed += Finish;
        interstitial.OnAdFullScreenContentFailed += error => Finish();
        interstitial.Show();
    }

    private void LoadRewarded()
    {
        rewarded?.Destroy();
        rewarded = null;
        GoogleMobileAds.Api.RewardedAd.Load(RewardedId, new GoogleMobileAds.Api.AdRequest(), (ad, error) =>
        {
            if (error == null) rewarded = ad;
        });
    }

    private void LoadInterstitial()
    {
        interstitial?.Destroy();
        interstitial = null;
        GoogleMobileAds.Api.InterstitialAd.Load(InterstitialId, new GoogleMobileAds.Api.AdRequest(), (ad, error) =>
        {
            if (error == null) interstitial = ad;
        });
    }
}
#endif
