using UnityEngine;

public class Block : MonoBehaviour
{
    public BlockElementType elementType;

    [Header("Renderer")]
    public SpriteRenderer sr;

    [Header("Effects (Auto Found)")]
    public ParticleSystem fireEffect;
    public ParticleSystem iceEffect;
    public ParticleSystem windEffect;
    public ParticleSystem electricEffect;

    private void Awake()
    {
        // Eðer Inspector’da atanmamýþsa otomatik bul
        if (sr == null)
            sr = GetComponentInChildren<SpriteRenderer>();

        if (fireEffect == null)
            fireEffect = transform.Find("FireEffect")?.GetComponent<ParticleSystem>();

        if (iceEffect == null)
            iceEffect = transform.Find("IceEffect")?.GetComponent<ParticleSystem>();

        if (windEffect == null)
            windEffect = transform.Find("WindEffect")?.GetComponent<ParticleSystem>();

        if (electricEffect == null)
            electricEffect = transform.Find("ElectricEffect")?.GetComponent<ParticleSystem>();
    }

    private void Start()
    {
        ApplyElementVisuals();
    }

    public void ApplyElementVisuals()
    {
        // Tüm efektleri kapat
        DisableAllEffects();

        switch (elementType)
        {
            case BlockElementType.Fire:
                sr.color = new Color(1f, 0.3f, 0.2f);
                PlayEffect(fireEffect);
                break;

            case BlockElementType.Ice:
                sr.color = new Color(0.3f, 0.7f, 1f);
                PlayEffect(iceEffect);
                break;

            case BlockElementType.Wind:
                sr.color = new Color(0.7f, 1f, 0.7f);
                PlayEffect(windEffect);
                break;

            case BlockElementType.Electric:
                sr.color = new Color(1f, 1f, 0.3f);
                PlayEffect(electricEffect);
                break;
        }
    }
    private void DisableAllEffects()
    {
        if (fireEffect != null) fireEffect.Stop();
        if (iceEffect != null) iceEffect.Stop();
        if (windEffect != null) windEffect.Stop();
        if (electricEffect != null) electricEffect.Stop();
    }

    private void PlayEffect(ParticleSystem fx)
    {
        if (fx != null)
        {
            fx.Stop();
            fx.Clear();
            fx.Play();
        }
    }
}
