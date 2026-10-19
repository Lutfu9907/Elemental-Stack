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
