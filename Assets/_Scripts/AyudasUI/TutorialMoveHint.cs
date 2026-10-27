using System.Collections;
using UnityEngine;

public class TutorialMoveHint : MonoBehaviour
{
    [Header("Jugador")]
    [SerializeField] private int playerId = 1;

    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animación")]
    [SerializeField] private float fadeDuration = 0.35f;
    [SerializeField] private float movementThreshold = 0.15f;

    private MobileInputManager inputManager;
    private bool completed;

    private IEnumerator Start()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        inputManager = FindFirstObjectByType<MobileInputManager>();

        canvasGroup.alpha = 0f;

        // Pequeña espera al cargar la partida
        yield return new WaitForSecondsRealtime(0.6f);

        yield return Fade(0f, 1f);

        // Esperar hasta que ESTE jugador empiece a caminar
        while (!completed)
        {
            if (inputManager == null)
            {
                inputManager = FindFirstObjectByType<MobileInputManager>();
                yield return null;
                continue;
            }

            Vector2 movement = inputManager.GetMovement(playerId);

            if (movement.magnitude > movementThreshold)
            {
                completed = true;

                yield return Fade(canvasGroup.alpha, 0f);

                gameObject.SetActive(false);
                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator Fade(float from, float to)
    {
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime;

            canvasGroup.alpha =
                Mathf.Lerp(from, to, time / fadeDuration);

            yield return null;
        }

        canvasGroup.alpha = to;
    }
}