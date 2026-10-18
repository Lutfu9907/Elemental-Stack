using UnityEngine;
using TMPro;
using System.Collections;

public class ComboPopup : MonoBehaviour
{
    public float riseDistance = 80f;    
    public float duration = 0.6f;

    private CanvasGroup cg;
    private TextMeshProUGUI text;
    private RectTransform rt;

    private Vector2 startPos;
    private Vector3 startScale;

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        text = GetComponentInChildren<TextMeshProUGUI>();
        rt = GetComponent<RectTransform>();

        startScale = Vector3.one;
        transform.localScale = startScale;
    }

    public void SetCombo(int combo)
    {
        text.text = "Combo x" + combo;
        Play();
    }

    private void Play()
    {
        StopAllCoroutines();
        StartCoroutine(AnimRoutine());
    }

    private IEnumerator AnimRoutine()
    {
        // Spawn pozisyonunda başla
        startPos = rt.anchoredPosition;

        float t = 0f;

        // İlk punch-scale (patlama)
        transform.localScale = startScale * 0.8f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float n = t / duration;

            // Punch scale: 0.8 → 1.15 → 1
            float scaleCurve = n < 0.4f
                ? Mathf.Lerp(0.8f, 1.15f, n / 0.4f)
                : Mathf.Lerp(1.15f, 1f, (n - 0.4f) / 0.6f);

            transform.localScale = startScale * scaleCurve;

            // Yukarı doğru hareket (UI)
            rt.anchoredPosition = startPos + Vector2.up * riseDistance * n;

            // Fade out
            cg.alpha = 1f - n;

            yield return null;
        }

        Destroy(gameObject);
    }

    public static void Spawn(Vector3 worldPos, int combo)
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("NO CANVAS FOUND!");
            return;
        }

        GameObject prefab = Resources.Load<GameObject>("ComboPopup");
        GameObject popup = Instantiate(prefab, canvas.transform);

        RectTransform rt = popup.GetComponent<RectTransform>();

        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        Vector2 uiPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            screenPos,
            canvas.worldCamera,
            out uiPos
        );
        uiPos.y += 100f;
        rt.anchoredPosition = uiPos;

        popup.GetComponent<ComboPopup>().SetCombo(combo);
    }
}
