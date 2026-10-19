// Kule yükseldikçe ulaşılan bölgeler. Her bölge bir kez kutlanır ve coin ödülü verir.
public static class AltitudeZones
{
    public struct Zone
    {
        public int floor;
        public string key;      // Localization: zone_<key> (ad) ve zone_<key>_reached (kutlama)
        public int reward;      // coin
    }

    public static readonly Zone[] All =
    {
        new Zone { floor = 25, key = "clouds", reward = 25 },
        new Zone { floor = 50, key = "sky", reward = 50 },
        new Zone { floor = 75, key = "aurora", reward = 75 },
        new Zone { floor = 100, key = "space", reward = 100 },
        new Zone { floor = 150, key = "moon", reward = 150 },
    };

    // Bu kata tam olarak ulaşınca açılan bölgenin sırası; yoksa -1
    public static int IndexReachedAt(int floor)
    {
        for (int i = 0; i < All.Length; i++)
            if (All[i].floor == floor)
                return i;
        return -1;
    }

    // En iyi kata göre sıradaki hedef; hepsi geçildiyse -1
    public static int NextIndex(int bestFloor)
    {
        for (int i = 0; i < All.Length; i++)
            if (bestFloor < All[i].floor)
                return i;
        return -1;
    }

    public static string Name(int index) => Localization.Get("zone_" + All[index].key);
}
