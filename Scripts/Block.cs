using UnityEngine;

public class Block : MonoBehaviour
{
    public BlockElementType elementType;

    [Header("Renderer")]
    public SpriteRenderer sr;   // Blok sprite

    [Header("Effects")]
    public ParticleSystem fireEffect;
    public ParticleSystem iceEffect;
    public ParticleSystem windEffect;
    public ParticleSystem electricEffect;

    void Start()
    {
        ApplyElementVisuals();
    }
    public void ApplyElementVisuals()
    {
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
    private void PlayEffect(ParticleSystem fx)
    {
        if (fx != null)
        {
            fx.Play();
        }
    }
}