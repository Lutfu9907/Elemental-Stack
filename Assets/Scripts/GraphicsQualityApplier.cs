using UnityEngine;

// Sahne açılınca ve ayar değişince grafik kalitesini kamera ve güneş ışığına uygular
public class GraphicsQualityApplier : MonoBehaviour
{
    public Camera targetCamera;
    public Light sun;

    void OnEnable()
    {
        GraphicsQuality.Changed += Apply;
        Apply();
    }

    void OnDisable()
    {
        GraphicsQuality.Changed -= Apply;
    }

    private void Apply()
    {
        GraphicsQuality.Apply(targetCamera, sun);
    }
}
