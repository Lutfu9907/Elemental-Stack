using UnityEngine;

// Kısa titreşimler: blok konunca hafif, perfect'te orta, oyun bitince güçlü.
// Android'de titreşim süresi ve gücü ayarlanabildiği için doğrudan Android API'si kullanılır;
// Unity'nin Handheld.Vibrate'i yarım saniye titrediği için bu oyuna fazla uzun.
public static class Haptics
{
    private const string EnabledPrefKey = "Haptics";

    private static bool loaded;
    private static bool enabled;

    public static bool Enabled
    {
        get
        {
            if (!loaded)
            {
                enabled = PlayerPrefs.GetInt(EnabledPrefKey, 1) == 1;
                loaded = true;
            }
            return enabled;
        }
        set
        {
            enabled = value;
            loaded = true;
            PlayerPrefs.SetInt(EnabledPrefKey, value ? 1 : 0);
        }
    }

    public static void Light() => Vibrate(10, 60);
    public static void Medium() => Vibrate(18, 140);
    public static void Heavy() => Vibrate(40, 255);

#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject vibrator;
    private static int sdkLevel;

    private static void Init()
    {
        if (vibrator != null) return;
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
        {
            vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            sdkLevel = version.GetStatic<int>("SDK_INT");
        }
    }
#endif

    private static void Vibrate(long milliseconds, int amplitude)
    {
        if (!Enabled) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            Init();
            if (vibrator == null) return;

            if (sdkLevel >= 26)
            {
                using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                using (var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, amplitude))
                    vibrator.Call("vibrate", effect);
            }
            else
            {
                vibrator.Call("vibrate", milliseconds);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Haptics: " + e.Message);
        }
#endif
    }

    // Hiç çağrılmaz. Unity, Android'e titreşim iznini (VIBRATE) yalnızca kodda
    // Handheld.Vibrate geçiyorsa ekler; bu satır izni garanti eder.
    private static void RequestVibratePermission()
    {
        Handheld.Vibrate();
    }
}
