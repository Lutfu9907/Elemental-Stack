using System.Text;
using UnityEngine;

// Oyun içi olayların tek giriş noktası (oyun başladı, bitti, devam edildi, satın alındı...).
// Şimdilik sadece editörde ve geliştirme sürümünde konsola yazar; ileride Firebase ya da
// Unity Analytics eklenince olaylar Event içinden oraya iletilir, oyun kodu değişmez.
public static class GameAnalytics
{
    public static void Event(string name, params (string key, object value)[] data)
    {
        if (!Debug.isDebugBuild) return;

        var line = new StringBuilder("[Analytics] ").Append(name);
        foreach (var (key, value) in data)
            line.Append(' ').Append(key).Append('=').Append(value);
        Debug.Log(line.ToString());
    }
}
