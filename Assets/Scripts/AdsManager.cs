using System;
using UnityEngine;

// Reklamların tek giriş noktası ve kuralları. Oyun açılınca kendiliğinden oluşur, sahneler arasında yaşar.
// Kurallar (oyuncuyu bıktırmamak için):
//  - Ara reklam: ilk 3 oyunda hiç yok; sonra en fazla 3 oyunda bir ve en az 90 saniye arayla,
//    "Tekrar oyna"ya basınca. Reklamsız sürüm alındıysa hiç yok.
//  - Ödüllü reklam: sadece oyuncu isterse (devam et, 2 kat coin). Reklamsız sürümde de seçenek olarak kalır.
public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance { get; private set; }

    private const int FreeGames = 3;
    private const int GamesBetweenInterstitials = 3;
    private const float MinSecondsBetweenInterstitials = 90f;

    private IAdProvider provider;
    private int gamesSinceInterstitial;
    private float lastInterstitialTime = -999f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (Instance != null) return;
        var go = new GameObject("AdsManager");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<AdsManager>();
    }

    void Awake()
    {
        provider = CreateProvider();
        provider.Initialize(this);
    }

    private static IAdProvider CreateProvider()
    {
#if UNITY_EDITOR
        return new SimulatedAdProvider();
#elif GOOGLE_MOBILE_ADS
        return new AdMobProvider();
#else
        return new NoAdProvider();
#endif
    }

    public bool RewardedReady => provider != null && provider.RewardedReady;

    public void ShowRewarded(Action<bool> done)
    {
        if (!RewardedReady)
        {
            done?.Invoke(false);
            return;
        }
        provider.ShowRewarded(done);
    }

    // "Tekrar oyna"ya basılınca çağrılır: sırası geldiyse önce ara reklam, sonra devam
    public void OnGameFinished(Action then)
    {
        gamesSinceInterstitial++;
        bool due = !PlayerData.AdsRemoved
                   && PlayerData.GamesPlayed > FreeGames
                   && gamesSinceInterstitial >= GamesBetweenInterstitials
                   && Time.realtimeSinceStartup - lastInterstitialTime >= MinSecondsBetweenInterstitials
                   && provider.InterstitialReady;
        if (!due)
        {
            then?.Invoke();
            return;
        }

        gamesSinceInterstitial = 0;
        lastInterstitialTime = Time.realtimeSinceStartup;
        provider.ShowInterstitial(then);
    }
}
