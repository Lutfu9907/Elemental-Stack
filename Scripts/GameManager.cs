using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject startPanel;
    public GameObject gameUIPanel;
    public GameOverPanelController gameOverPanel;

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highScoreText;
    public FadeController fade;

    [Header("Gameplay")]
    public TowerController tower;

    private int highScore = 0;
    private bool isGameOver = false;
    private bool isGameStarted = false;

    void Start()
    {
        startPanel.SetActive(true);
        gameUIPanel.SetActive(false);
        gameOverPanel.gameObject.SetActive(false);

        Time.timeScale = 0f;
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

        highScore = PlayerPrefs.GetInt("HighScore", 0);
        highScoreText.text = "BEST: " + highScore;
        scoreText.text = "SCORE: 0";
    }

    public void UpdateScore(int score)
    {
        scoreText.text = "SCORE: " + score;

        if (score > highScore)
        {
            highScore = score;
            highScoreText.text = "BEST: " + highScore;
            PlayerPrefs.SetInt("HighScore", highScore);
        }
    }

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

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
        Debug.Log("Oyun kapatıldı!");
    }
}
