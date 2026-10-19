using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Günün ilk açılışında çıkan "Günlük ödül" kartı: kaçıncı gün olduğu, 7 günlük seri noktaları,
// ödül miktarı ve "Al" butonu. Ödül her gün büyür, 7. günden sonra başa döner; bir gün atlanırsa seri sıfırlanır.
public class DailyRewardPanel : MonoBehaviour
{
    public TextMeshProUGUI dayText;
    public TextMeshProUGUI amountText;
    public Image[] dayDots;              // 7 nokta
    public Color dotOn = new Color(1f, 0.824f, 0.302f);
    public Color dotOff = new Color(0.85f, 0.88f, 0.93f);

    // Ödül alınabiliyorsa gösterir (GameManager başlangıçta çağırır)
    public void ShowIfAvailable()
    {
        PlayerData.RefreshDay();
        if (!PlayerData.CanClaimDailyReward)
        {
            gameObject.SetActive(false);
            return;
        }

        int dayIndex = PlayerData.DailyRewardDayIndex;
        dayText.text = Localization.Format("day_n", dayIndex + 1);
        amountText.text = "+" + PlayerData.DailyRewardAmount;
        for (int i = 0; i < dayDots.Length; i++)
            dayDots[i].color = i <= dayIndex ? dotOn : dotOff;
        gameObject.SetActive(true);
    }

    public void Collect()
    {
        PlayerData.ClaimDailyReward();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayCoin();
        Haptics.Light();
        gameObject.SetActive(false);
    }
}
