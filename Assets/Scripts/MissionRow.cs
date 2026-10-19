using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Görevler ekranındaki tek satır: açıklama, ilerleme çubuğu (3/5) ve ödül ya da tik.
public class MissionRow : MonoBehaviour
{
    public Image background;
    public TextMeshProUGUI description;
    public RectTransform progressFill;   // çubuğun dolu kısmı; genişliği ilerlemeye göre
    public TextMeshProUGUI progressText;
    public GameObject rewardRow;
    public TextMeshProUGUI rewardText;
    public GameObject doneMark;

    private static readonly Color NormalColor = new Color(0.95f, 0.96f, 0.98f);
    private static readonly Color DoneColor = new Color(0.86f, 0.97f, 0.88f);

    public void Show(Mission mission, int progress, bool done)
    {
        description.text = mission.Describe();
        float ratio = mission.target > 0 ? Mathf.Clamp01(progress / (float)mission.target) : 0f;
        progressFill.anchorMax = new Vector2(Mathf.Max(0.04f, ratio), 1f);
        progressText.text = Mathf.Min(progress, mission.target) + "/" + mission.target;
        rewardText.text = "+" + mission.reward;
        rewardRow.SetActive(!done);
        doneMark.SetActive(done);
        background.color = done ? DoneColor : NormalColor;
    }
}
