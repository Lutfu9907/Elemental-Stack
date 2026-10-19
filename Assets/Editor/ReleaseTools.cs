using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// Store release settings: package name, version, Android build options and the app icon.
/// Applied every time the scene is built, and from the menu: Tower Stack > Release > Apply Android Settings.
/// The icon and store pictures are drawn by tools/store-art.js (node tools/store-art.js).
/// </summary>
public static class ReleaseTools
{
    // The package name can never change once the app is on Google Play
    public const string PackageName = "com.lutfudolay.elementalstack";
    public const string CompanyName = "LD Games";
    public const string FirstVersion = "1.0.0";

    private const string IconPath = "Assets/Art/AppIcon.png";
    private const string IconBackgroundPath = "Assets/Art/AppIconBackground.png";
    private const string IconForegroundPath = "Assets/Art/AppIconForeground.png";
    private const string ScreenshotFolder = "Screenshots";

    [MenuItem("Tower Stack/Release/Apply Android Settings")]
    public static void ApplyAndroidSettings()
    {
        PlayerSettings.companyName = CompanyName;
        PlayerSettings.productName = "Elemental Stack";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, PackageName);

        // Unity's untouched defaults become 1.0.0; a version set by hand for an update is kept
        if (PlayerSettings.bundleVersion == "0.1" || PlayerSettings.bundleVersion == "1.0" || string.IsNullOrEmpty(PlayerSettings.bundleVersion))
            PlayerSettings.bundleVersion = FirstVersion;
        PlayerSettings.Android.bundleVersionCode = Mathf.Max(1, PlayerSettings.Android.bundleVersionCode);

        // Google Play needs 64-bit (IL2CPP + ARM64); 32-bit ARM keeps older cheap phones in
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;          // Android 7.1, the lowest Unity 6.3 supports
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;     // newest installed SDK
        EditorUserBuildSettings.buildAppBundle = true;                                        // Play wants .aab

        ApplyIcons();
        Debug.Log("Elemental Stack: Android ayarları uygulandı (" + PackageName + ", v" + PlayerSettings.bundleVersion
                  + " / " + PlayerSettings.Android.bundleVersionCode + ").");
    }

    private static void ApplyIcons()
    {
        Texture2D icon = ImportIcon(IconPath, false);
        if (icon == null)
        {
            Debug.LogWarning("Elemental Stack: " + IconPath + " yok. 'node tools/store-art.js' ile oluşturulabilir.");
            return;
        }
        PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);

        // Android 8+ adaptive icon (background + foreground layers). Its type lives in the Android
        // module, which is only there when Android build support is installed, so it is looked up by name.
        Texture2D background = ImportIcon(IconBackgroundPath, false);
        Texture2D foreground = ImportIcon(IconForegroundPath, true);
        PlatformIconKind adaptive = AndroidIconKind("Adaptive");
        if (adaptive == null || background == null || foreground == null) return;

        PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, adaptive);
        foreach (PlatformIcon platformIcon in icons)
            platformIcon.SetTextures(background, foreground);
        PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, adaptive, icons);
    }

    private static Texture2D ImportIcon(string path, bool transparent)
    {
        if (!File.Exists(path)) return null;
        AssetDatabase.ImportAsset(path);
        if (AssetImporter.GetAtPath(path) is TextureImporter importer)
        {
            bool changed = importer.textureCompression != TextureImporterCompression.Uncompressed
                           || importer.mipmapEnabled
                           || importer.alphaIsTransparency != transparent;
            if (changed)
            {
                importer.textureType = TextureImporterType.Default;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = transparent;
                importer.SaveAndReimport();
            }
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static PlatformIconKind AndroidIconKind(string name)
    {
        Type kinds = Type.GetType("UnityEditor.Android.AndroidPlatformIconKind, UnityEditor.Android.Extensions");
        if (kinds == null) return null;
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
        PropertyInfo property = kinds.GetProperty(name, flags);
        if (property != null) return property.GetValue(null) as PlatformIconKind;
        FieldInfo field = kinds.GetField(name, flags);
        return field != null ? field.GetValue(null) as PlatformIconKind : null;
    }

    // Store screenshots: set the Game view to 1080x1920, press Play, set up the moment and use this menu.
    [MenuItem("Tower Stack/Release/Save Store Screenshot")]
    private static void SaveStoreScreenshot()
    {
        Directory.CreateDirectory(ScreenshotFolder);
        string file = Path.Combine(ScreenshotFolder, "store_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
        ScreenCapture.CaptureScreenshot(file);
        Debug.Log("Elemental Stack: mağaza ekran görüntüsü kaydedildi → " + Path.GetFullPath(file));
    }

    [MenuItem("Tower Stack/Release/Save Store Screenshot", true)]
    private static bool SaveStoreScreenshotValidate() => EditorApplication.isPlaying;
}
