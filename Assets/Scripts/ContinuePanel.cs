using TMPro;
using UnityEngine;

// Blok ıskalanınca çıkan "Devam etmek ister misin?" kartı: reklam izleyerek ya da coin ile devam,
// ya da "Hayır, teşekkürler". 5 saniye içinde seçim yapılmazsa oyun biter.
public class ContinuePanel : MonoBehaviour
{
    public GameObject watchAdButton;
    public GameObject coinsButton;
    public TextMeshProUGUI coinsCostText;
    public TextMeshProUGUI countdownText;
    public float seconds = 5f;

    private GameManager gm;
    private float remaining;
    private bool waiting;   // reklam açıkken geri sayım durur

    public void Show(GameManager manager, int cost, bool adReady, bool canPay)
    {
        gm = manager;
        watchAdButton.SetActive(adReady);
        coinsButton.SetActive(canPay);
        coinsCostText.text = cost.ToString();
        remaining = seconds;
        waiting = false;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (waiting || gm == null) return;
        remaining -= Time.unscaledDeltaTime;
        countdownText.text = Mathf.CeilToInt(Mathf.Max(0f, remaining)).ToString();
        if (remaining <= 0f)
            Decline();
    }

    public void WatchAd()
    {
        waiting = true;
        gm.ContinueWithAd();
    }

    public void UseCoins()
    {
        gm.ContinueWithCoins();
    }

    public void Decline()
    {
        waiting = true;
        gm.DeclineContinue();
    }
}
