using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject startPanel;
    public GameObject gameUIPanel;
    public GameObject gameOverPanel;

    private bool isGameOver = false;
    private bool isGameStarted = false;
    public TowerController tower;

    void Start()
    {
        // Baþlangýçta sadece Start Panel açýk
        if (startPanel != null) startPanel.SetActive(true);
        if (gameUIPanel != null) gameUIPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        Time.timeScale = 0f; // Oyun durdurulmuþ þekilde baþlasýn
    }

    public void StartGame()
    {
        if (isGameStarted) return;
        isGameStarted = true;

        // Paneller
        startPanel.SetActive(false);
        gameUIPanel.SetActive(true);
        gameOverPanel.SetActive(false);

        // Oyun zamaný baþlasýn
        Time.timeScale = 1f;

        // TowerController’ý baþlat
        if (tower != null)
        {
            tower.StartTower(); // TowerController’da public StartTower() metodu olmalý
        }
    }
    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (gameUIPanel != null) gameUIPanel.SetActive(false);

        Time.timeScale = 0f; // Oyun dursun
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Oyun kapatýldý!");
    }
}
