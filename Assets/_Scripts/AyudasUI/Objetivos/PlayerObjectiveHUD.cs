using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Cuadro de objetivo de UN jugador, dentro del cuadrante de su cámara.
// No toca al jugador: solo muestra, espera y se oculta (o se cierra con su ACCIÓN).
public class PlayerObjectiveHUD : MonoBehaviour
{
    public int PlayerId { get; private set; }

    public ObjectiveDefinition CurrentObjective => current;

    private ObjectivePromptController owner;
    private Camera playerCamera;
    private RectTransform root;
    private CanvasGroup group;
    private Image image;

    private ObjectiveDefinition current;
    private Coroutine routine;
    private bool previousAction;


    public void Setup(
        ObjectivePromptController controller, int playerId, Camera cameraForPlayer,
        Image template, Vector2 anchorMin, Vector2 anchorMax)
    {
        owner = controller;
        PlayerId = playerId;
        playerCamera = cameraForPlayer;

        root = (RectTransform)transform;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        image = Instantiate(template, transform);
        image.name = "ObjectiveImage";
        image.gameObject.SetActive(true);
        image.raycastTarget = false;
        image.preserveAspect = true;

        // Posición relativa al cuadrante: escala con la resolución.
        RectTransform imageRect = image.rectTransform;
        imageRect.anchorMin = anchorMin;
        imageRect.anchorMax = anchorMax;
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        FitToCamera();
    }


    // Un objetivo nuevo reemplaza al que se esté mostrando.
    public void Show(ObjectiveDefinition objective)
    {
        if (objective == null)
            return;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(ShowRoutine(objective));
    }


    private void Update()
    {
        FitToCamera();
    }


    // Mismo criterio que PlayerStateHUD / PlayerHintHUD: anclar al viewport de la cámara.
    private void FitToCamera()
    {
        if (playerCamera == null)
            return;

        Rect viewport = playerCamera.rect;
        root.anchorMin = viewport.min;
        root.anchorMax = viewport.max;
    }


    private IEnumerator ShowRoutine(ObjectiveDefinition objective)
    {
        current = objective;
        image.sprite = objective.sprite;

        Debug.Log($"🎯 OBJETIVO MOSTRADO [P{PlayerId}]: {objective.name}");

        yield return Fade(group.alpha, 1f, objective.fadeSeconds);

        // Si ya tenía ACCIÓN pulsado, debe soltarlo y volver a pulsar.
        previousAction = IsActionPressed();

        float visible = 0f;
        string reason = "tiempo";

        while (visible < objective.visibleSeconds)
        {
            visible += Time.unscaledDeltaTime;

            if (objective.closeWithAction && ActionJustPressed())
            {
                reason = "ACCIÓN";
                break;
            }

            yield return null;
        }

        yield return Fade(group.alpha, 0f, objective.fadeSeconds);

        Debug.Log($"🎯 OBJETIVO OCULTO [P{PlayerId}] ({reason}): {objective.name}");

        current = null;
        routine = null;
    }


    private IEnumerator Fade(float from, float to, float duration)
    {
        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(time / duration));
            yield return null;
        }

        group.alpha = to;
    }


    // =====================================================
    // ACCIÓN (solo de ESTE jugador)
    // =====================================================

    private bool IsActionPressed()
    {
        return owner.MobileInput != null && owner.MobileInput.IsActionPressed(PlayerId);
    }


    private bool ActionJustPressed()
    {
        bool pressed = IsActionPressed();
        bool justPressed = pressed && !previousAction;
        previousAction = pressed;
        return justPressed;
    }
}
