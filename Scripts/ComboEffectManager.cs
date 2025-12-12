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
        if (combo < 3) return;

        int milestone = 0;

        if (combo >= 3 && combo < 5) milestone = 3;
        else if (combo >= 5 && combo < 10) milestone = 5;
        else if (combo >= 10 && combo < 15) milestone = 10;
        else if (combo >= 15) milestone = 15;

        if (milestone <= lastComboMilestone) return; 
        lastComboMilestone = milestone;

        StartCoroutine(FlashScreenRoutine(combo));
        PlayComboText(combo);
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
    private void PlayComboText(int combo)
    {
        if (combo < 3) return;

        if (combo < 5)
            comboPopupText.text = "GOOD!";
        else if (combo < 10)
            comboPopupText.text = "GREAT!";
        else if (combo < 15)
            comboPopupText.text = "AMAZING!";
        else
            comboPopupText.text = "SUPER STACK!";

        StartCoroutine(ComboTextRoutine());
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
