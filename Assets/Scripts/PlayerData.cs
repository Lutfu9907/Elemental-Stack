using System;
using System.Collections.Generic;
using UnityEngine;

// Oyuncunun kalıcı verileri: coin, en yüksek skor ve istatistikler (ileride skin'ler ve görevler de buraya).
// Hepsi tek bir JSON olarak PlayerPrefs'te saklanır; ileride bulut kayda taşımak kolay olsun diye.
public static class PlayerData
{
    [Serializable]
    private class SaveData
    {
        public int version = 2;
        public int coins;
        public int bestScore;
        public int bestFloor;
        public int gamesPlayed;
        public int totalPerfects;

        // Mağaza
        public List<string> ownedSkins = new List<string>();
        public string selectedBlockSkin;
        public string selectedSkySkin;

        // Günlük görevler
        public string missionDay;
        public List<string> missionIds = new List<string>();
        public List<int> missionProgress = new List<int>();
        public List<bool> missionDone = new List<bool>();
        public bool missionBonusClaimed;

        // Giriş serisi
        public string lastLoginDay;
        public int loginStreak;
        public bool dailyRewardClaimed;
    }

    // Seri günlerine göre günlük ödül (7. günden sonra döngü başa sarar)
    private static readonly int[] DailyRewards = { 20, 30, 40, 50, 60, 80, 100 };

    private const string SaveKey = "SaveData";
    private const string LegacyHighScoreKey = "HighScore";   // coin sisteminden önceki en yüksek skor

    private static SaveData data;

    public static event Action CoinsChanged;

    // "Enter Play Mode" ayarında domain reload kapalı olsa bile kayıt baştan okunsun
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        data = null;
        CoinsChanged = null;
    }

    private static SaveData Data
    {
        get
        {
            if (data == null)
                Load();
            return data;
        }
    }

    public static int Coins => Data.coins;
    public static int BestScore => Data.bestScore;
    public static int BestFloor => Data.bestFloor;
    public static int GamesPlayed => Data.gamesPlayed;

    public static void AddCoins(int amount)
    {
        if (amount == 0) return;
        Data.coins = Mathf.Max(0, Data.coins + amount);
        Save();
        CoinsChanged?.Invoke();
    }

    // Yeterli coin varsa harcar ve true döner (mağaza için)
    public static bool SpendCoins(int amount)
    {
        if (amount < 0 || Data.coins < amount) return false;
        Data.coins -= amount;
        Save();
        CoinsChanged?.Invoke();
        return true;
    }

    public static string SelectedBlockSkin => Data.selectedBlockSkin;
    public static string SelectedSkySkin => Data.selectedSkySkin;

    public static bool OwnsSkin(string id) => Data.ownedSkins.Contains(id);

    public static void UnlockSkin(string id)
    {
        if (OwnsSkin(id)) return;
        Data.ownedSkins.Add(id);
        Save();
    }

    public static void SelectSkin(Skin skin)
    {
        if (skin.type == SkinType.Block) Data.selectedBlockSkin = skin.id;
        else Data.selectedSkySkin = skin.id;
        Save();
    }

    // ----------------------------------------------------------- günlük

    private static string DayKey(DateTime day) => day.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

    // Gün değiştiyse giriş serisini ilerletir ve yeni günün görevlerini seçer.
    // Her sahne açılışında (GameManager) çağrılır.
    public static void RefreshDay()
    {
        SaveData d = Data;
        DateTime now = DateTime.Now;
        string today = DayKey(now);
        bool changed = false;

        if (d.lastLoginDay != today)
        {
            d.loginStreak = d.lastLoginDay == DayKey(now.AddDays(-1)) ? d.loginStreak + 1 : 1;
            d.lastLoginDay = today;
            d.dailyRewardClaimed = false;
            changed = true;
        }

        if (d.missionDay != today || d.missionIds == null || d.missionIds.Count == 0)
        {
            d.missionDay = today;
            d.missionIds = new List<string>();
            d.missionProgress = new List<int>();
            d.missionDone = new List<bool>();
            foreach (Mission m in MissionCatalog.PickForDay(now.Date, d.bestFloor))
            {
                d.missionIds.Add(m.Id);
                d.missionProgress.Add(0);
                d.missionDone.Add(false);
            }
            d.missionBonusClaimed = false;
            changed = true;
        }

        if (changed)
            Save();
    }

    public static int MissionCount => Data.missionIds.Count;
    public static Mission GetMission(int i) => Mission.FromId(Data.missionIds[i]);
    public static int GetMissionProgress(int i) => Data.missionProgress[i];
    public static bool IsMissionDone(int i) => Data.missionDone[i];
    public static bool MissionBonusClaimed => Data.missionBonusClaimed;

    // Görev ilerlemesi sık değişir; diske yazma oyun sonunda ya da bir görev bitince olur
    public static void SetMissionProgress(int i, int value)
    {
        Data.missionProgress[i] = value;
        SaveQuiet();
    }

    public static void CompleteMission(int i)
    {
        if (Data.missionDone[i]) return;
        Data.missionDone[i] = true;
        AddCoins(GetMission(i).reward);
    }

    public static void ClaimMissionBonus()
    {
        if (Data.missionBonusClaimed) return;
        Data.missionBonusClaimed = true;
        AddCoins(MissionCatalog.AllDoneBonus);
    }

    public static int LoginStreak => Mathf.Max(1, Data.loginStreak);
    public static bool CanClaimDailyReward => !Data.dailyRewardClaimed;
    public static int DailyRewardAmount => DailyRewards[(LoginStreak - 1) % DailyRewards.Length];
    public static int DailyRewardDayIndex => (LoginStreak - 1) % DailyRewards.Length;   // 0..6

    public static void ClaimDailyReward()
    {
        if (Data.dailyRewardClaimed) return;
        Data.dailyRewardClaimed = true;
        AddCoins(DailyRewardAmount);
    }

    // Sadece test için: tüm kaydı (coin, skor, temalar) siler
    public static void ResetForTesting()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.DeleteKey(LegacyHighScoreKey);
        PlayerPrefs.Save();
        data = null;
        CoinsChanged?.Invoke();
        SkinCatalog.NotifyChanged();
    }

    // Her oyunun sonunda bir kez çağrılır
    public static void RecordGame(int score, int floors, int perfects)
    {
        SaveData d = Data;
        d.gamesPlayed++;
        d.totalPerfects += perfects;
        d.bestScore = Mathf.Max(d.bestScore, score);
        d.bestFloor = Mathf.Max(d.bestFloor, floors);
        Save();
    }

    private static void Load()
    {
        data = null;
        string json = PlayerPrefs.GetString(SaveKey, "");
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                data = JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning("PlayerData: kayıt okunamadı, sıfırdan başlanıyor. " + e.Message);
            }
        }

        if (data == null)
        {
            // İlk açılış ya da eski sürümden güncelleme: önceki en yüksek skoru taşı
            data = new SaveData { bestScore = PlayerPrefs.GetInt(LegacyHighScoreKey, 0) };
        }

        // Ücretsiz temalar herkeste var ve hiçbir şey seçilmediyse onlar seçili (eski kayıtlar için de)
        if (data.ownedSkins == null)
            data.ownedSkins = new List<string>();
        foreach (string free in new[] { SkinCatalog.DefaultBlockSkin, SkinCatalog.DefaultSkySkin })
            if (!data.ownedSkins.Contains(free))
                data.ownedSkins.Add(free);
        if (string.IsNullOrEmpty(data.selectedBlockSkin)) data.selectedBlockSkin = SkinCatalog.DefaultBlockSkin;
        if (string.IsNullOrEmpty(data.selectedSkySkin)) data.selectedSkySkin = SkinCatalog.DefaultSkySkin;
        Save();
    }

    // Kaydı hafızada günceller ama diske hemen yazmaz (Unity çıkışta ve bir sonraki Save'de yazar)
    private static void SaveQuiet()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
    }

    private static void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }
}
