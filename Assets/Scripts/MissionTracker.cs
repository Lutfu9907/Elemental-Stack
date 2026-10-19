using UnityEngine;

// Oyun sırasında günlük görevlerin ilerlemesini sayar. Bir görev bitince coin'i hemen verir
// ve ekranda "Görev tamam! +40" yazar; üçü de bitince bonus verir.
public class MissionTracker : MonoBehaviour
{
    public TowerController tower;
    public ElementBanner banner;

    void Start()
    {
        if (tower == null) return;
        tower.FloorPlaced += OnFloorPlaced;
        tower.PerfectPlaced += OnPerfectPlaced;
    }

    void OnDestroy()
    {
        if (tower == null) return;
        tower.FloorPlaced -= OnFloorPlaced;
        tower.PerfectPlaced -= OnPerfectPlaced;
    }

    private void OnFloorPlaced(int floor, Transform block)
    {
        Block placed = block != null ? block.GetComponent<Block>() : null;
        for (int i = 0; i < PlayerData.MissionCount; i++)
        {
            Mission m = PlayerData.GetMission(i);
            if (m.type == MissionType.ReachFloor)
                Advance(i, floor, true);
            else if (m.type == MissionType.PlaceElement && placed != null && placed.elementType == m.element)
                Advance(i, PlayerData.GetMissionProgress(i) + 1, true);
        }
    }

    private void OnPerfectPlaced(Block.BlockElementType element)
    {
        for (int i = 0; i < PlayerData.MissionCount; i++)
        {
            Mission m = PlayerData.GetMission(i);
            if (m.type == MissionType.PerfectsTotal)
                Advance(i, PlayerData.GetMissionProgress(i) + 1, true);
            else if (m.type == MissionType.PerfectStreak)
                Advance(i, tower.Combo, true);
        }
    }

    // GameManager oyun bitince çağırır: oynanan oyun ve kazanılan coin görevleri
    public void OnGameOver(int coinsEarned)
    {
        for (int i = 0; i < PlayerData.MissionCount; i++)
        {
            Mission m = PlayerData.GetMission(i);
            if (m.type == MissionType.PlayGames)
                Advance(i, PlayerData.GetMissionProgress(i) + 1, false);
            else if (m.type == MissionType.EarnCoins)
                Advance(i, PlayerData.GetMissionProgress(i) + coinsEarned, false);
        }
    }

    // İlerleme yalnızca artar; hedefe ulaşınca görev tamamlanır
    private void Advance(int index, int value, bool announce)
    {
        if (PlayerData.IsMissionDone(index)) return;

        Mission m = PlayerData.GetMission(index);
        int progress = Mathf.Max(PlayerData.GetMissionProgress(index), Mathf.Min(value, m.target));
        PlayerData.SetMissionProgress(index, progress);
        if (progress < m.target) return;

        PlayerData.CompleteMission(index);
        if (announce && banner != null)
            banner.ShowText(Localization.Get("mission_complete"), m.Describe() + "  +" + m.reward);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayCoin();

        if (AllDone() && !PlayerData.MissionBonusClaimed)
        {
            PlayerData.ClaimMissionBonus();
            if (announce && banner != null)
                banner.ShowText(Localization.Get("missions_all_done"), "+" + MissionCatalog.AllDoneBonus + " " + Localization.Get("coins"));
        }
    }

    public static bool AllDone()
    {
        for (int i = 0; i < PlayerData.MissionCount; i++)
            if (!PlayerData.IsMissionDone(i))
                return false;
        return PlayerData.MissionCount > 0;
    }

    public static int DoneCount()
    {
        int done = 0;
        for (int i = 0; i < PlayerData.MissionCount; i++)
            if (PlayerData.IsMissionDone(i))
                done++;
        return done;
    }
}
