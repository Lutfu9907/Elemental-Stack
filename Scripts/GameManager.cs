using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public GameObject gameOverPanel; // Inspector’dan baðlayacaðýz
    private bool isGameOver = false;

    void Start()
    {
        // Baþlangýçta panel kapalý
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        // Oyun durumu baþta açýk
        isGameOver = false;
        Time.timeScale = 1f;
    }
    public void GameOver()
    {
        if (isGameOver) return; // Zaten game over ise tekrar çalýþtýrma
        isGameOver = true;

        Debug.Log("Oyun bitti!");

        // Paneli göster
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        // Oyunu durdur
        Time.timeScale = 0f;
    }
    public void RestartGame()
    {
        // Zamaný tekrar aç
        Time.timeScale = 1f;

        // Sahneyi yeniden yükle
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
