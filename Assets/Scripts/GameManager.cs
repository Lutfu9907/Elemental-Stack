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

    private int score = 0;
    private int highScore = 0;
    private int highScoreAtStart = 0;
    private bool isGameOver = false;
    private bool isGameStarted = false;

    void Awake()
    {
        highScore = PlayerData.BestScore;
        highScoreAtStart = highScore;
    }

    void OnEnable()
    {
        Localization.LanguageChanged += RefreshTexts;
        PlayerData.CoinsChanged += RefreshTexts;
    }

    void OnDisable()
    {
        Localization.LanguageChanged -= RefreshTexts;
        PlayerData.CoinsChanged -= RefreshTexts;
    }

    void Start()
    {
        startPanel.SetActive(true);
        gameUIPanel.SetActive(false);
        gameOverPanel.gameObject.SetActive(false);

        Time.timeScale = 0f;
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
        PlayerData.RecordGame(score, floors, perfects);
        PlayerData.AddCoins(coinsEarned);

        RefreshTexts();
        if (newBestBadge != null)
            newBestBadge.SetActive(score > highScoreAtStart);

        gameUIPanel.SetActive(false);
        gameOverPanel.Show();

        Time.timeScale = 0f;
        StartCoroutine(CountCoins(coinsEarned));
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
        if (finalScoreText != null) finalScoreText.text = score.ToString();
        if (finalBestText != null) finalBestText.text = best;
    }
}
