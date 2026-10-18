using UnityEngine;

// Ortografik kameranın boyunu ekran oranına göre ayarlar; böylece dar (uzun) telefonlarda
// da kayan blok ekranın dışına taşmaz.
[RequireComponent(typeof(Camera))]
public class CameraFitWidth : MonoBehaviour
{
    [Tooltip("Ekranın her zaman göstereceği yarım genişlik (dünya birimi)")]
    public float halfWidth = 4.3f;
    [Tooltip("Geniş ekranlarda kamera bundan daha fazla yaklaşmaz")]
    public float minOrthographicSize = 6f;

    private Camera cam;
    private float lastAspect;

    void Awake()
    {
        cam = GetComponent<Camera>();
        Fit();
    }

    void Update()
    {
        if (!Mathf.Approximately(cam.aspect, lastAspect))
            Fit();
    }

    private void Fit()
    {
        lastAspect = cam.aspect;
        if (lastAspect <= 0f) return;
        cam.orthographicSize = Mathf.Max(minOrthographicSize, halfWidth / lastAspect);
    }
}
