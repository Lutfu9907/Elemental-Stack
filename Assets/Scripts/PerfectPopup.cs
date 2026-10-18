using UnityEngine;
using TMPro;
using System.Collections;

public class PerfectPopup : MonoBehaviour
{
    [Header("Animation Settings")]
    public float riseDistance = 45f;
    public float duration = 0.65f;
    public float startScale = 0.8f;
    public float peakScale = 1.2f;

    private CanvasGroup cg;
    private TextMeshProUGUI tmp;
    private RectTransform rect;
    private Vector2 startPos;

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        tmp = GetComponentInChildren<TextMeshProUGUI>();
        rect = GetComponent<RectTransform>();

        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
    }

    private void Start()
    {
        StartCoroutine(AnimRoutine());
    }

    private IEnumerator AnimRoutine()
    {
        float t = 0f;

        rect.localScale = Vector3.one * startScale;
        startPos = rect.anchoredPosition;

        while (t < duration)
        {
            t += Time.deltaTime;
            float n = t / duration;

            // scale
            float s = n < 0.4f
                ? Mathf.Lerp(startScale, peakScale, n / 0.4f)
                : Mathf.Lerp(peakScale, 1f, (n - 0.4f) / 0.6f);

            rect.localScale = Vector3.one * s;

            // rise
            rect.anchoredPosition = startPos + Vector2.up * riseDistance * n;

            // fade
            cg.alpha = 1f - n;

            yield return null;
        }

        Destroy(gameObject);
    }

    // combo 2 ve üstündeyse yazının sonuna "x{combo}" eklenir: "Perfect! x3"
    public static void Spawn(Vector3 worldPos, int combo = 1)
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found for PerfectPopup");
            return;
        }

        GameObject prefab = Resources.Load<GameObject>("PerfectPopup");
        if (prefab == null)
        {
            Debug.LogError("PerfectPopup prefab missing in Resources!");
            return;
        }

        GameObject popup = Instantiate(prefab, canvas.transform);
        RectTransform rt = popup.GetComponent<RectTransform>();

        TextMeshProUGUI label = popup.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            string text = Localization.Get("perfect");
            label.text = combo > 1 ? text + " x" + combo : text;
        }

        // World → Screen
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        // Screen → UI anchored
        Vector2 uiPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            screenPos,
            canvas.worldCamera,
            out uiPos
        );

        uiPos.y += 45f;     

        rt.anchoredPosition = uiPos;
    }
}
