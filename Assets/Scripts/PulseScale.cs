using UnityEngine;

// UI öğesini hafifçe büyütüp küçülterek "dokun" çağrısına dikkat çeker.
// Oyun Time.timeScale = 0 ile beklerken de çalışsın diye ölçeksiz zaman kullanır.
public class PulseScale : MonoBehaviour
{
    public float amount = 0.06f;
    public float speed = 3f;

    private Vector3 baseScale;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        float s = 1f + Mathf.Sin(Time.unscaledTime * speed) * amount;
        transform.localScale = baseScale * s;
    }
}
