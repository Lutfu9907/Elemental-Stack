using System;
using System.IO;
using UnityEngine;

// Oyun sonunda kulenin ekran görüntüsünü ve skoru telefonun paylaşma menüsüne gönderir
// (WhatsApp, Instagram, mesajlar...). Ek eklenti gerekmez:
//  - Android 10 ve üstü: görüntü galeriye (Pictures/ElementalStack) kaydedilir ve oradan paylaşılır;
//    bunun için izin gerekmez.
//  - Android 9 ve altı: izin istememek için sadece yazı paylaşılır.
//  - Editör: görüntü proje klasöründeki Screenshots/ altına kaydedilir.
public static class ShareService
{
    public static void Share(string text, Texture2D image)
    {
        byte[] png = image != null ? image.EncodeToPNG() : null;

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            ShareAndroid(text, png);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Share: " + e.Message);
        }
#elif UNITY_EDITOR
        if (png != null)
        {
            string folder = Path.Combine(Directory.GetCurrentDirectory(), "Screenshots");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "share_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
            File.WriteAllBytes(path, png);
            Debug.Log("Share (editör): " + text + "\nGörüntü: " + path);
        }
        else
        {
            Debug.Log("Share (editör): " + text);
        }
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static void ShareAndroid(string text, byte[] png)
    {
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var intentClass = new AndroidJavaClass("android.content.Intent"))
        using (var intent = new AndroidJavaObject("android.content.Intent"))
        using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
        {
            intent.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
            intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), text);

            AndroidJavaObject uri = null;
            if (png != null && version.GetStatic<int>("SDK_INT") >= 29)
                uri = SaveToGallery(activity, png);

            if (uri != null)
            {
                intent.Call<AndroidJavaObject>("setType", "image/png");
                intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_STREAM"), uri);
                intent.Call<AndroidJavaObject>("addFlags", intentClass.GetStatic<int>("FLAG_GRANT_READ_URI_PERMISSION"));
            }
            else
            {
                intent.Call<AndroidJavaObject>("setType", "text/plain");
            }

            using (AndroidJavaObject chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, Localization.Get("share")))
                activity.Call("startActivity", chooser);
            uri?.Dispose();
        }
    }

    // Görüntüyü MediaStore ile galeriye yazar ve içerik adresini (content://) döner
    private static AndroidJavaObject SaveToGallery(AndroidJavaObject activity, byte[] png)
    {
        using (var resolver = activity.Call<AndroidJavaObject>("getContentResolver"))
        using (var values = new AndroidJavaObject("android.content.ContentValues"))
        using (var media = new AndroidJavaClass("android.provider.MediaStore$Images$Media"))
        {
            values.Call("put", "_display_name", "ElementalStack_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
            values.Call("put", "mime_type", "image/png");
            values.Call("put", "relative_path", "Pictures/ElementalStack");

            AndroidJavaObject collection = media.GetStatic<AndroidJavaObject>("EXTERNAL_CONTENT_URI");
            AndroidJavaObject uri = resolver.Call<AndroidJavaObject>("insert", collection, values);
            if (uri == null) return null;

            using (var stream = resolver.Call<AndroidJavaObject>("openOutputStream", uri))
            {
                var bytes = new sbyte[png.Length];
                Buffer.BlockCopy(png, 0, bytes, 0, png.Length);
                stream.Call("write", bytes);
                stream.Call("close");
            }
            return uri;
        }
    }
#endif
}
