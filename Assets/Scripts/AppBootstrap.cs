using UnityEngine;

// İlk sahne yüklenmeden önce mobil çalışma ayarlarını uygular.
public static class AppBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        // vSync kapalı olmalı, yoksa targetFrameRate yok sayılır (editör/masaüstü)
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        // Oynarken ekran kararmasın
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }
}
