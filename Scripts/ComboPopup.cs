using UnityEngine;
using TMPro;
using System.Collections;

public class ComboPopup : MonoBehaviour
{
    public float riseDistance = 1f;
    public float duration = 0.6f;

    private CanvasGroup cg;
    private TextMeshProUGUI text;
    private Vector3 startScale;
    private Vector3 startPos;

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        text = GetComponentInChildren<TextMeshProUGUI>();

        cg.alpha = 1f;

        startPos = transform.position;
        startScale = transform.localScale;
    }
    void Start()
    {
        Debug.Log("COMBO POPUP INSTANTIATED at " + transform.position);
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
        float t = 0f;

        // Ýlk ufak patlama efekti
        transform.localScale = startScale * 0.8f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float n = t / duration;

            // Scale: hafif büyüme
            transform.localScale = Vector3.Lerp(startScale * 0.8f, startScale * 1.1f, n);

            // Yükselme
            transform.position = startPos + Vector3.up * riseDistance * n;

            // Fade out
            cg.alpha = 1f - n;

            yield return null;
        }

        Destroy(gameObject);
    }

    public static void Spawn(Vector3 pos, int combo)
    {
        GameObject prefab = Resources.Load<GameObject>("ComboPopup");
        if (prefab == null)
        {
            Debug.LogError("Resources/ComboPopup PREFAB bulunamadý!");
            return;
        }

        GameObject go = Instantiate(prefab, pos, Quaternion.identity);
        go.GetComponent<ComboPopup>().SetCombo(combo);
    }
}
