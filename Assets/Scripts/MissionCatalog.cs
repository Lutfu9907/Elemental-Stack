using System;
using System.Collections.Generic;
using Element = Block.BlockElementType;

public enum MissionType
{
    PerfectsTotal,   // gün içinde toplam N perfect
    PerfectStreak,   // tek oyunda üst üste N perfect
    ReachFloor,      // tek oyunda N. kata çık
    PlaceElement,    // gün içinde N tane X elementli blok yerleştir
    PlayGames,       // gün içinde N oyun oyna
    EarnCoins,       // gün içinde N coin kazan
}

// Günlük görev: türü, hedefi ve ödülü. Kimliği (id) kayıtta saklanır ve buradan geri çözülür.
public struct Mission
{
    public MissionType type;
    public int target;
    public int reward;
    public Element element;

    public string Id => type + ":" + target + ":" + element;

    public static Mission FromId(string id)
    {
        string[] parts = id.Split(':');
        return new Mission
        {
            type = (MissionType)Enum.Parse(typeof(MissionType), parts[0]),
            target = int.Parse(parts[1]),
            element = parts.Length > 2 ? (Element)Enum.Parse(typeof(Element), parts[2]) : Element.None,
            reward = RewardFor((MissionType)Enum.Parse(typeof(MissionType), parts[0]), int.Parse(parts[1])),
        };
    }

    // Ödül, zorluğa göre 30-60 coin
    public static int RewardFor(MissionType type, int target)
    {
        switch (type)
        {
            case MissionType.PerfectStreak: return target >= 8 ? 60 : 40;
            case MissionType.ReachFloor: return target >= 40 ? 60 : target >= 25 ? 45 : 30;
            case MissionType.PerfectsTotal: return target >= 20 ? 50 : 35;
            case MissionType.PlaceElement: return target >= 5 ? 45 : 35;
            case MissionType.EarnCoins: return target >= 100 ? 50 : 35;
            default: return 30;
        }
    }

    public string Describe()
    {
        switch (type)
        {
            case MissionType.PerfectsTotal: return Localization.Format("mission_perfects_total", target);
            case MissionType.PerfectStreak: return Localization.Format("mission_perfect_streak", target);
            case MissionType.ReachFloor: return Localization.Format("mission_reach_floor", target);
            case MissionType.PlaceElement:
                return Localization.Format("mission_place_element", target, Localization.Get("element_" + element.ToString().ToLowerInvariant()));
            case MissionType.PlayGames: return Localization.Format("mission_play_games", target);
            case MissionType.EarnCoins: return Localization.Format("mission_earn_coins", target);
            default: return "";
        }
    }
}

public static class MissionCatalog
{
    public const int MissionsPerDay = 3;
    public const int AllDoneBonus = 50;

    // O günün görevleri: tarihten türetilen tohumla seçilir, oyuncunun seviyesine uyar.
    // Aynı gün tekrar seçilse de aynı görevler gelir.
    public static List<Mission> PickForDay(DateTime day, int bestFloor)
    {
        var rng = new Random(day.Year * 10000 + day.Month * 100 + day.Day);
        var candidates = new List<Mission>
        {
            Make(MissionType.PerfectsTotal, rng.Next(2) == 0 ? 10 : 20),
            Make(MissionType.PerfectStreak, bestFloor >= 30 ? 8 : 5),
            Make(MissionType.ReachFloor, ReachTarget(bestFloor)),
            Make(MissionType.PlayGames, rng.Next(2) == 0 ? 3 : 5),
            Make(MissionType.EarnCoins, bestFloor >= 30 ? 100 : 60),
        };

        // Element görevi sadece oyuncunun ulaşabildiği elementlerle
        var reachable = new List<Element>();
        foreach (Element e in ElementRules.IntroductionOrder)
            if (ElementRules.UnlockFloor(e) <= Math.Max(bestFloor, 1) + 5)
                reachable.Add(e);
        if (reachable.Count > 0)
        {
            Mission place = Make(MissionType.PlaceElement, rng.Next(2) == 0 ? 3 : 5);
            place.element = reachable[rng.Next(reachable.Count)];
            candidates.Add(place);
        }

        // Karıştırıp farklı türlerden üç tane al
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }
        return candidates.GetRange(0, Math.Min(MissionsPerDay, candidates.Count));
    }

    private static Mission Make(MissionType type, int target)
    {
        return new Mission { type = type, target = target, reward = Mission.RewardFor(type, target), element = Element.None };
    }

    // Oyuncunun en iyisinin biraz altında, ulaşılabilir bir kat hedefi
    private static int ReachTarget(int bestFloor)
    {
        int[] targets = { 15, 25, 35, 50 };
        foreach (int t in targets)
            if (t >= bestFloor * 0.7f)
                return t;
        return targets[targets.Length - 1];
    }
}
