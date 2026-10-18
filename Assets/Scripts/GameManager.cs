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

    [Header("Game Over")]
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI finalBestText;
    public GameObject newBestBadge;

    [Header("Other")]
    public FadeController fade;
    public TowerController tower;

    private const string HighScoreKey = "HighScore";

    private int score = 0;
    private int highScore = 0;
    private int highScoreAtStart = 0;
    private bool isGameOver = false;
    private bool isGameStarted = false;

    void Awake()
    {
        highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        highScoreAtStart = highScore;
    }

    void OnEnable()
    {
        Localization.LanguageChanged += RefreshTexts;
    }

    void OnDisable()
    {
        Localization.LanguageChanged -= RefreshTexts;
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

        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
        }

        RefreshTexts();
    }

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        PlayerPrefs.Save();
        RefreshTexts();
        if (newBestBadge != null)
            newBestBadge.SetActive(score > highScoreAtStart);

        gameUIPanel.SetActive(false);
        gameOverPanel.Show();

        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
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

    // Skor ve "en iyi" yazılarını o anki değer ve dile göre yeniler
    private void RefreshTexts()
    {
        string best = Localization.Format("best", highScore);

        if (scoreText != null) scoreText.text = score.ToString();
        if (highScoreText != null) highScoreText.text = best;
        if (startBestText != null) startBestText.text = best;
        if (finalScoreText != null) finalScoreText.text = score.ToString();
        if (finalBestText != null) finalBestText.text = best;
    }
}
