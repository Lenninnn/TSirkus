using System.Collections;
using UnityEngine;

public class TutorialImageFade : MonoBehaviour
{
    [Header("Referencia")]
    public CanvasGroup canvasGroup;

    [Header("Tiempos")]
    public float delayBeforeShow = 0.5f;
    public float fadeInDuration = 0.4f;
    public float visibleDuration = 3f;
    public float fadeOutDuration = 0.4f;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    private void Start()
    {
        StartCoroutine(PlayTutorial());
    }

    private IEnumerator PlayTutorial()
    {
        yield return new WaitForSeconds(delayBeforeShow);

        yield return StartCoroutine(FadeCanvas(0f, 1f, fadeInDuration));

        yield return new WaitForSeconds(visibleDuration);

        yield return StartCoroutine(FadeCanvas(1f, 0f, fadeOutDuration));
    }

    private IEnumerator FadeCanvas(float from, float to, float duration)
    {
        float time = 0f;
        canvasGroup.alpha = from;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            canvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        canvasGroup.alpha = to;
    }
}