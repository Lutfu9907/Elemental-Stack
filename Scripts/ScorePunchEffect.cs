using UnityEngine;
using TMPro;
using System.Collections;

public class ScorePunchEffect : MonoBehaviour
{
    private RectTransform rt;
    private Vector3 baseScale;
    private Coroutine routine;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        baseScale = rt.localScale;
    }

    public void Punch(float strength = 1f)
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(PunchRoutine(strength));
    }

    IEnumerator PunchRoutine(float strength)
    {
        float upScale = 1.08f + (strength * 0.02f);
        float t = 0f;

        // Grow
        while (t < 0.08f)
        {
            t += Time.deltaTime;
            rt.localScale = Vector3.Lerp(baseScale, baseScale * upScale, t / 0.08f);
            yield return null;
        }

        t = 0f;
        // Return
        while (t < 0.12f)
        {
            t += Time.deltaTime;
            rt.localScale = Vector3.Lerp(rt.localScale, baseScale, t / 0.12f);
            yield return null;
        }

        rt.localScale = baseScale;
    }
}
