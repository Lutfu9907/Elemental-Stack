using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ComboEffectManager : MonoBehaviour
{
    public static ComboEffectManager Instance;

    [Header("UI References")]
    public Image flashImage;               // Canvas → FlashImage
    public TextMeshProUGUI comboPopupText; // Canvas → ComboPopupText
    private bool isPlayingText = false;
    private int lastShownComboTier = 0;

    [Header("Flash Settings")]
    public float flashDuration = 0.25f;
    private int lastComboMilestone = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        if (comboPopupText == null)
            Debug.LogError("comboPopupText is NULL !!!");
        if (flashImage == null)
            Debug.LogError("flashImage is NULL !!!");

    }

    public void PlayPerfectComboEffect(int combo)
    {
        int tier = GetComboTier(combo);

        if (tier == 0 || tier == lastShownComboTier)
            return;

        lastShownComboTier = tier;

        StartCoroutine(FlashScreenRoutine(combo));
        PlayComboTextByTier(tier);
    }
    private int GetComboTier(int combo)
    {
        if (combo >= 15) return 4;
        if (combo >= 10) return 3;
        if (combo >= 5) return 2;
        if (combo >= 3) return 1;
        return 0;
    }

    private void PlayComboTextByTier(int tier)
    {
        switch (tier)
        {
            case 1:
                comboPopupText.text = "GOOD!";
                break;
            case 2:
                comboPopupText.text = "GREAT!";
                break;
            case 3:
                comboPopupText.text = "AMAZING!";
                break;
            case 4:
                comboPopupText.text = "SUPER STACK!";
                break;
        }

        StartCoroutine(ComboTextRoutine());
    }
    private IEnumerator FlashScreenRoutine(int combo)
    {
        float strength = 0.2f;

        if (combo >= 5) strength = 0.35f;
        if (combo >= 10) strength = 0.55f;
        if (combo >= 15) strength = 0.75f;

        flashImage.color = new Color(1, 1, 1, strength);

        float t = 0f;
        while (t < flashDuration)
        {
            t += Time.deltaTime;
            float n = t / flashDuration;

            flashImage.color = new Color(1, 1, 1, Mathf.Lerp(strength, 0f, n));

            yield return null;
        }

        flashImage.color = new Color(1, 1, 1, 0f);
    }
    public void ResetComboTier()
    {
        lastShownComboTier = 0;
    }
    private IEnumerator ComboTextRoutine()
    {
        comboPopupText.alpha = 1f;

        RectTransform rt = comboPopupText.rectTransform;
        Vector2 startPos = rt.anchoredPosition;

        float rise = 30f;         
        float duration = 1.5f;    
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float n = t / duration;

            rt.anchoredPosition = startPos + Vector2.up * rise * n;
            comboPopupText.alpha = Mathf.Lerp(1f, 0f, Mathf.Pow(n, 0.5f)); 

            yield return null;
        }

        rt.anchoredPosition = startPos;
        comboPopupText.alpha = 0f;

        isPlayingText = false;
    }
}
