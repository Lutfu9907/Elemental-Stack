using UnityEngine;
using TMPro;

public class ScoreUIManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highScoreText;

    private void Start()
    {
        ApplyScorePositions();
    }

    private void ApplyScorePositions()
    {
        // === SCORE TEXT ===
        RectTransform srt = scoreText.rectTransform;

        // Anchor → Top Left
        srt.anchorMin = new Vector2(0f, 1f);
        srt.anchorMax = new Vector2(0f, 1f);
        srt.pivot = new Vector2(0f, 1f);

        // Position → (30, -30)
        srt.anchoredPosition = new Vector2(30f, -30f);


        // === HIGH SCORE TEXT ===
        RectTransform hrt = highScoreText.rectTransform;

        // Anchor → Top Left
        hrt.anchorMin = new Vector2(0f, 1f);
        hrt.anchorMax = new Vector2(0f, 1f);
        hrt.pivot = new Vector2(0f, 1f);

        // Position → (30, -80)
        hrt.anchoredPosition = new Vector2(30f, -80f);
    }
}
