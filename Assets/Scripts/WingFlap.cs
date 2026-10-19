using UnityEngine;

// Gökyüzündeki kuşların kanat çırpması: dikeyde hafifçe büzülüp açılır
public class WingFlap : MonoBehaviour
{
    public float phase;
    private Vector3 baseScale;

    void Start()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        float flap = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 7f + phase));
        transform.localScale = new Vector3(baseScale.x, baseScale.y * flap, baseScale.z);
    }
}
