using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Grafik kalitesi: Yüksek ya da Düşük. Düşükte ışıltı (bloom) ve gölgeler kapanır;
// ucuz telefonlarda oyun akıcı kalsın diye. İlk açılışta telefonun belleğine ve
// işlemcisine bakılarak otomatik seçilir; oyuncu ayarlardan değiştirebilir.
public static class GraphicsQuality
{
    private const string PrefKey = "GraphicsHigh";

    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Changed = null;
    }

    public static bool High
    {
        get
        {
            if (!PlayerPrefs.HasKey(PrefKey))
                PlayerPrefs.SetInt(PrefKey, DetectHighEnd() ? 1 : 0);
            return PlayerPrefs.GetInt(PrefKey) == 1;
        }
        set
        {
            PlayerPrefs.SetInt(PrefKey, value ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }

    // 3 GB'tan az bellek ya da 4 ve daha az çekirdek: düşük kalite ile başla
    private static bool DetectHighEnd()
    {
        return SystemInfo.systemMemorySize >= 3000 && SystemInfo.processorCount > 4;
    }

    // Sahnedeki kamera ve ışığa uygular (GraphicsQualityApplier çağırır)
    public static void Apply(Camera camera, Light sun)
    {
        bool high = High;
        if (camera != null)
        {
            var data = camera.GetComponent<UniversalAdditionalCameraData>();
            if (data != null)
                data.renderPostProcessing = high;
        }
        if (sun != null)
            sun.shadows = high ? LightShadows.Soft : LightShadows.None;
    }
}
