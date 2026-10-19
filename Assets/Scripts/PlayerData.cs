using System;
using UnityEngine;

// Oyuncunun kalıcı verileri: coin, en yüksek skor ve istatistikler (ileride skin'ler ve görevler de buraya).
// Hepsi tek bir JSON olarak PlayerPrefs'te saklanır; ileride bulut kayda taşımak kolay olsun diye.
public static class PlayerData
{
    [Serializable]
    private class SaveData
    {
        public int version = 1;
        public int coins;
        public int bestScore;
        public int bestFloor;
        public int gamesPlayed;
        public int totalPerfects;
    }

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
            Save();
        }
    }

    private static void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }
}
