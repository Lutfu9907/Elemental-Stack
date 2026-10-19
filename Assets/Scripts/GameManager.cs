using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject startPanel;
    public GameObject gameUIPanel;
    public GameOverPanelController gameOverPanel;

    [Header("HUD")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highScoreText;

    [Header("Start Screen")]
    public TextMeshProUGUI startBestText;
    public TextMeshProUGUI startCoinsText;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI nextGoalText;   // "Sıradaki hedef: Bulutlar · 25. kat"

    [Header("Game Over")]
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI finalBestText;
    public GameObject newBestBadge;
    public TextMeshProUGUI coinsEarnedText;

    [Header("Coins")]
    [Tooltip("Her kat için kazanılan coin")]
    public int coinsPerFloor = 1;
    [Tooltip("Her perfect için ek coin")]
    public int coinsPerPerfect = 1;
    public float coinCountDelay = 0.5f;
    public float coinCountDuration = 0.9f;

    [Header("Other")]
    public FadeController fade;
    public TowerController tower;
    public AltitudeTracker altitude;
    public MissionTracker missions;
    public DailyRewardPanel dailyReward;
    public TextMeshProUGUI missionsButtonText;   // "Görevler 1/3"
    public GameObject pausePanel;

    private int score = 0;
    private int highScore = 0;
    private int highScoreAtStart = 0;
    private bool isGameOver = false;
    private bool isGameStarted = false;
    private Color titleColor;
    private Texture2D shareImage;
    private bool isPaused;

    void Awake()
    {
        highScore = PlayerData.BestScore;
        highScoreAtStart = highScore;
        if (titleText != null)
            titleColor = titleText.color;
    }

    void OnEnable()
    {
        Localization.LanguageChanged += RefreshTexts;
        PlayerData.CoinsChanged += RefreshTexts;
        SkinCatalog.SkinChanged += RefreshTexts;
    }

    void OnDisable()
    {
        Localization.LanguageChanged -= RefreshTexts;
        PlayerData.CoinsChanged -= RefreshTexts;
        SkinCatalog.SkinChanged -= RefreshTexts;
    }

    void Start()
    {
        startPanel.SetActive(true);
        gameUIPanel.SetActive(false);
        gameOverPanel.gameObject.SetActive(false);

        Time.timeScale = 0f;
        PlayerData.RefreshDay();   // yeni günse giriş serisi ve görevler yenilenir
        if (dailyReward != null)
            dailyReward.ShowIfAvailable();
        RefreshTexts();
    }

    public void StartGame()
    {
        if (isGameStarted) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
        isGameStarted = true;

        startPanel.SetActive(false);
        gameUIPanel.SetActive(true);
        gameOverPanel.gameObject.SetActive(false);

        Time.timeScale = 1f;

        if (tower != null)
            tower.StartTower();

        RefreshTexts();
    }

    public void UpdateScore(int newScore)
    {
        score = newScore;
        highScore = Mathf.Max(highScore, score);
        RefreshTexts();
    }

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        // Kazanılan coin'ler: her kat + her perfect
        int floors = tower != null ? tower.floors : 0;
        int perfects = tower != null ? tower.perfectCount : 0;
        int coinsEarned = floors * coinsPerFloor + perfects * coinsPerPerfect;
        if (altitude != null)
            coinsEarned += altitude.BonusCoins;   // ulaşılan bölgelerin ödülü
        PlayerData.RecordGame(score, floors, perfects);
        PlayerData.AddCoins(coinsEarned);
        if (missions != null)
            missions.OnGameOver(coinsEarned);

        RefreshTexts();
        if (newBestBadge != null)
            newBestBadge.SetActive(score > highScoreAtStart);

        Time.timeScale = 0f;
        StartCoroutine(ShowGameOver(coinsEarned));
    }

    // Paylaşmak için önce kulenin (skor görünürken) ekran görüntüsü alınır, sonra oyun sonu kartı açılır
    private IEnumerator ShowGameOver(int coinsEarned)
    {
        yield return new WaitForEndOfFrame();
        if (shareImage != null)
            Destroy(shareImage);
        shareImage = ScreenCapture.CaptureScreenshotAsTexture();

        gameUIPanel.SetActive(false);
        gameOverPanel.Show();
        StartCoroutine(CountCoins(coinsEarned));
    }

    public void Share()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
        string link = "https://play.google.com/store/apps/details?id=" + Application.identifier;
        ShareService.Share(Localization.Format("share_text", tower != null ? tower.floors : score, link), shareImage);
    }

    void OnDestroy()
    {
        if (shareImage != null)
            Destroy(shareImage);
    }

    // Duraklat: oyun durur, üstte "Devam / Ayarlar / Ana ekran" kartı çıkar
    public void PauseGame()
    {
        if (!isGameStarted || isGameOver || isPaused) return;
        isPaused = true;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
        Time.timeScale = 0f;
        if (pausePanel != null)
            pausePanel.SetActive(true);
    }

    public void ResumeGame()
    {
        if (!isPaused) return;
        isPaused = false;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
        if (pausePanel != null)
            pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    // Telefon arka plana alınınca (arama, bildirim) oyun kendiliğinden duraklar
    void OnApplicationPause(bool paused)
    {
        if (paused)
            PauseGame();
    }

    public void RestartGame()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
        Time.timeScale = 1f;

        fade.FadeIn(() =>
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        });
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    // Oyun sonu kartında kazanılan coin 0'dan yukarı sayılır; oyun durmuş olduğu için ölçeksiz zaman
    private IEnumerator CountCoins(int earned)
    {
        if (coinsEarnedText == null) yield break;

        coinsEarnedText.text = "+0";
        yield return new WaitForSecondsRealtime(coinCountDelay);
        if (earned <= 0) yield break;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayCoin();
        for (float t = 0f; t < coinCountDuration; t += Time.unscaledDeltaTime)
        {
            float eased = 1f - Mathf.Pow(1f - t / coinCountDuration, 2f);
            coinsEarnedText.text = "+" + Mathf.RoundToInt(earned * eased);
            yield return null;
        }
        coinsEarnedText.text = "+" + earned;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayCoin();
    }

    // Skor, "en yüksek skor" ve coin yazılarını o anki değer ve dile göre yeniler
    private void RefreshTexts()
    {
        string best = Localization.Format("best", highScore);

        if (scoreText != null) scoreText.text = score.ToString();
        if (highScoreText != null) highScoreText.text = best;
        if (startBestText != null) startBestText.text = best;
        if (startCoinsText != null) startCoinsText.text = PlayerData.Coins.ToString("N0");
        // Koyu gökyüzü temasında lacivert başlık okunmaz; beyaz yazılır
        if (titleText != null) titleText.color = SkinCatalog.CurrentSky.darkSky ? Color.white : titleColor;

        if (missionsButtonText != null)
            missionsButtonText.text = Localization.Format("missions_button", MissionTracker.DoneCount(), PlayerData.MissionCount);

        // Sıradaki yükseklik hedefi; hepsine ulaşıldıysa zirve
        if (nextGoalText != null)
        {
            int next = AltitudeZones.NextIndex(PlayerData.BestFloor);
            nextGoalText.text = next >= 0
                ? Localization.Format("next_zone", AltitudeZones.Name(next), AltitudeZones.All[next].floor)
                : Localization.Format("peak_zone", AltitudeZones.Name(AltitudeZones.All.Length - 1));
        }
        if (finalScoreText != null) finalScoreText.text = score.ToString();
        if (finalBestText != null) finalBestText.text = best;
    }
}
