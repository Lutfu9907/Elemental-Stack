using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Başlangıç ekranından açılan "Görevler" ekranı: günün üç görevi, ilerleme çubukları,
// ödülleri ve üçü bitince verilecek bonus; ayrıca giriş serisi bilgisi.
public class MissionsPanel : MonoBehaviour
{
    public RectTransform list;
    public MissionRow rowTemplate;
    public TextMeshProUGUI bonusText;
    public TextMeshProUGUI streakText;

    private readonly List<MissionRow> rows = new List<MissionRow>();

    void Awake()
    {
        rowTemplate.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        Localization.LanguageChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        Localization.LanguageChanged -= Refresh;
    }

    public void Open()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
        gameObject.SetActive(true);
    }

    public void Close()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
        gameObject.SetActive(false);
    }

    private void Refresh()
    {
        PlayerData.RefreshDay();

        while (rows.Count < PlayerData.MissionCount)
        {
            MissionRow row = Instantiate(rowTemplate, list, false);
            row.gameObject.SetActive(true);
            rows.Add(row);
        }
        for (int i = 0; i < rows.Count; i++)
        {
            bool used = i < PlayerData.MissionCount;
            rows[i].gameObject.SetActive(used);
            if (used)
                rows[i].Show(PlayerData.GetMission(i), PlayerData.GetMissionProgress(i), PlayerData.IsMissionDone(i));
        }

        bonusText.text = PlayerData.MissionBonusClaimed
            ? Localization.Get("missions_all_done")
            : Localization.Format("missions_bonus", MissionCatalog.AllDoneBonus);
        streakText.text = Localization.Format("streak_days", PlayerData.LoginStreak);
    }
}
