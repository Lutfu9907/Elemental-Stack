using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameLanguage { English, Turkish }

// Oyundaki tüm metinlerin çevirileri. Varsayılan dil İngilizce; seçilen dil kalıcı olarak saklanır.
// Yeni metin eklemek için Table'a bir anahtar ve iki çeviri (EN, TR) eklemek yeterli.
public static class Localization
{
    private const string LanguagePrefKey = "Language";

    // Anahtar → { İngilizce, Türkçe }
    private static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
    {
        { "title",        new[] { "Elemental\nStack", "Elemental\nStack" } },
        { "tap_to_start", new[] { "Tap to start", "Başlamak için dokun" } },
        { "best",         new[] { "Best: {0}", "En yüksek skor: {0}" } },
        { "best_label",   new[] { "Best", "En yüksek skor" } },
        { "score_label",  new[] { "Score", "Skor" } },
        { "game_over",    new[] { "Game over", "Oyun bitti" } },
        { "new_best",     new[] { "New best!", "Yeni rekor!" } },
        { "play_again",   new[] { "Play again", "Tekrar oyna" } },
        { "perfect",      new[] { "Perfect!", "Mükemmel!" } },
        { "combo_good",   new[] { "Good!", "İyi!" } },
        { "combo_great",  new[] { "Great!", "Harika!" } },
        { "combo_amazing", new[] { "Amazing!", "Muhteşem!" } },
        { "combo_super",  new[] { "Super stack!", "Süper kule!" } },
        { "language",     new[] { "EN", "TR" } },
        { "coins",           new[] { "coins", "coin" } },
        { "next_zone",       new[] { "Next: {0} · floor {1}", "Sıradaki hedef: {0} · {1}. kat" } },
        { "peak_zone",       new[] { "Peak: {0}", "Zirve: {0}" } },
        { "record",          new[] { "Best", "Rekor" } },
        { "zone_clouds",     new[] { "Clouds", "Bulutlar" } },
        { "zone_sky",        new[] { "High sky", "Yüksek gökyüzü" } },
        { "zone_aurora",     new[] { "Aurora", "Kutup ışıkları" } },
        { "zone_space",      new[] { "Space", "Uzay" } },
        { "zone_moon",       new[] { "Moon", "Ay" } },
        { "zone_clouds_reached", new[] { "Above the clouds!", "Bulutların üstündesin!" } },
        { "zone_sky_reached",    new[] { "Into the high sky!", "Yüksek gökyüzündesin!" } },
        { "zone_aurora_reached", new[] { "Aurora lights!", "Kutup ışıklarındasın!" } },
        { "zone_space_reached",  new[] { "You reached space!", "Uzaya ulaştın!" } },
        { "zone_moon_reached",   new[] { "Hello, Moon!", "Ay'a ulaştın!" } },
        { "missions",              new[] { "Missions", "Görevler" } },
        { "missions_button",       new[] { "Missions {0}/{1}", "Görevler {0}/{1}" } },
        { "mission_complete",      new[] { "Mission complete!", "Görev tamam!" } },
        { "missions_all_done",     new[] { "All missions done!", "Tüm görevler tamam!" } },
        { "missions_bonus",        new[] { "Finish all three: +{0}", "Üçünü bitir: +{0}" } },
        { "mission_perfects_total", new[] { "Make {0} perfects", "{0} perfect yap" } },
        { "mission_perfect_streak", new[] { "Get {0} perfects in a row", "Üst üste {0} perfect yap" } },
        { "mission_reach_floor",   new[] { "Reach floor {0}", "{0}. kata çık" } },
        { "mission_place_element", new[] { "Place {0} {1} blocks", "{0} tane {1} bloğu yerleştir" } },
        { "mission_play_games",    new[] { "Play {0} games", "{0} oyun oyna" } },
        { "mission_earn_coins",    new[] { "Earn {0} coins", "{0} coin kazan" } },
        { "streak_days",           new[] { "Login streak: {0} days", "Giriş serisi: {0} gün" } },
        { "daily_reward",          new[] { "Daily reward", "Günlük ödül" } },
        { "day_n",                 new[] { "Day {0}", "{0}. gün" } },
        { "collect",               new[] { "Collect", "Al" } },
        { "daily_hint",            new[] { "Come back tomorrow for a bigger reward", "Yarın gel, ödül büyüsün" } },
        { "share",           new[] { "Share", "Paylaş" } },
        { "share_text",      new[] { "I built a {0}-floor tower in Elemental Stack! Can you beat it? {1}",
                                     "Elemental Stack'te {0} katlık kule yaptım! Geçebilir misin? {1}" } },
        { "shop",            new[] { "Shop", "Mağaza" } },
        { "shop_blocks",     new[] { "Blocks", "Bloklar" } },
        { "shop_sky",        new[] { "Sky", "Gökyüzü" } },
        { "shop_selected",   new[] { "Selected", "Seçili" } },
        { "shop_owned",      new[] { "Owned", "Sende var" } },
        { "shop_not_enough", new[] { "Not enough coins", "Yeterli coin yok" } },
        { "close",           new[] { "Close", "Kapat" } },
        { "skin_classic",    new[] { "Classic", "Klasik" } },
        { "skin_candy",      new[] { "Candy", "Şeker" } },
        { "skin_wood",       new[] { "Wood", "Ahşap" } },
        { "skin_neon",       new[] { "Neon", "Neon" } },
        { "skin_gold",       new[] { "Gold", "Altın" } },
        { "sky_day",         new[] { "Day", "Gündüz" } },
        { "sky_sunset",      new[] { "Sunset", "Gün batımı" } },
        { "sky_space",       new[] { "Space", "Uzay" } },
        { "new_element",  new[] { "New element:", "Yeni element:" } },
        { "element_ice",           new[] { "Ice", "Buz" } },
        { "element_ice_desc",      new[] { "Slides slowly", "Yavaş kayar" } },
        { "element_fire",          new[] { "Fire", "Ateş" } },
        { "element_fire_desc",     new[] { "Fast, but perfects are easier", "Hızlı ama perfect daha kolay" } },
        { "element_wind",          new[] { "Wind", "Rüzgâr" } },
        { "element_wind_desc",     new[] { "Gusty speed, perfect gives +2", "Hızı değişir, perfect +2 puan" } },
        { "element_electric",      new[] { "Electric", "Elektrik" } },
        { "element_electric_desc", new[] { "Perfect makes the block grow", "Perfect bloğu büyütür" } },
    };

    public static event Action LanguageChanged;

    private static GameLanguage current;
    private static bool loaded;

    // PlayerPrefs statik başlatıcıda okunamadığı için dil ilk kullanımda yüklenir
    public static GameLanguage Current
    {
        get
        {
            if (!loaded)
            {
                current = (GameLanguage)PlayerPrefs.GetInt(LanguagePrefKey, (int)GameLanguage.English);
                loaded = true;
            }
            return current;
        }
    }

    // "Enter Play Mode" ayarında domain reload kapalı olsa bile kayıtlı dil yeniden okunsun
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        LanguageChanged = null;
        loaded = false;
    }

    public static string Get(string key)
    {
        if (Table.TryGetValue(key, out string[] texts))
            return texts[(int)Current];

        Debug.LogWarning("Localization: '" + key + "' için çeviri yok.");
        return key;
    }

    public static string Format(string key, params object[] args)
    {
        return string.Format(Get(key), args);
    }

    public static void SetLanguage(GameLanguage language)
    {
        if (language == Current) return;

        current = language;
        PlayerPrefs.SetInt(LanguagePrefKey, (int)language);
        PlayerPrefs.Save();
        LanguageChanged?.Invoke();
    }

    public static void NextLanguage()
    {
        int count = Enum.GetValues(typeof(GameLanguage)).Length;
        SetLanguage((GameLanguage)(((int)Current + 1) % count));
    }
}
