using UnityEngine;
using TMPro;
using System.Collections;

public class PerfectPopup : MonoBehaviour
{
    [Header("Animation Settings")]
    public float riseDistance = 30f;    
    public float duration = 0.7f;
    public float startScale = 0.8f;
    public float peakScale = 1.15f;

    private CanvasGroup cg;
    private TextMeshProUGUI tmp;
    private RectTransform rect;

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        tmp = GetComponentInChildren<TextMeshProUGUI>();
        rect = GetComponent<RectTransform>();

        if (cg == null) { Debug.LogWarning("PerfectPopup: CanvasGroup missing"); cg = gameObject.AddComponent<CanvasGroup>(); }
        if (tmp == null) Debug.LogWarning("PerfectPopup: TMP child missing");
    }

    private void Start()
    {
        // otomatik oynat
        StartCoroutine(AnimRoutine());
    }

    private IEnumerator AnimRoutine()
    {
        float t = 0f;
        cg.alpha = 1f;
        rect.localScale = Vector3.one * startScale;
        Vector2 startPos = rect.anchoredPosition;

        while (t < duration)
        {
            t += Time.deltaTime;
            float norm = Mathf.Clamp01(t / duration);

            float s = Mathf.Lerp(startScale, peakScale, Mathf.SmoothStep(0f, 1f, norm * 1.2f));
            rect.localScale = Vector3.one * s;

            rect.anchoredPosition = startPos + Vector2.up * riseDistance * norm;

            cg.alpha = 1f - norm;

            yield return null;
        }

        Destroy(gameObject);
    }
    public static void Spawn(Vector3 worldPosition)
    {
        GameObject prefab = Resources.Load<GameObject>("PerfectPopup");
        if (prefab == null)
        {
            Debug.LogError("PerfectPopup.prefab not found in Resources!");
            return;
        }

        Canvas mainCanvas = GameObject.FindObjectOfType<Canvas>();
        if (mainCanvas == null)
        {
            Debug.LogError("No Canvas found in scene for PerfectPopup.");
            return;
        }

        GameObject obj = GameObject.Instantiate(prefab, mainCanvas.transform);

        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.position = screenPos;          
        rt.localScale = Vector3.one;       
    }
}
