using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// HUD de ayudas de UN jugador. Vive dentro del cuadrante de su cámara
// (camera.rect) y tiene su propia cola, historial y cierre con ACCIÓN.
// Lo crea ContextHintController la primera vez que ese jugador recibe una ayuda.
public class PlayerHintHUD : MonoBehaviour
{
    public int PlayerId { get; private set; }

    public HintDefinition CurrentHint => current;

    private ContextHintController owner;
    private Camera playerCamera;
    private RectTransform root;
    private CanvasGroup group;
    private Image image;

    private readonly List<HintDefinition> pending = new List<HintDefinition>();
    private readonly HashSet<HintDefinition> seen = new HashSet<HintDefinition>();

    private HintDefinition current;
    private float visibleTime;
    private bool hiding;
    private bool previousAction;


    // =====================================================
    // CONFIGURACIÓN
    // =====================================================

    public void Setup(
        ContextHintController controller, int playerId, Camera cameraForPlayer,
        Image template, Vector2 hintSize, float topOffset)
    {
        owner = controller;
        PlayerId = playerId;
        playerCamera = cameraForPlayer;

        root = (RectTransform)transform;
        root.pivot = new Vector2(0.5f, 0.5f);
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        image = Instantiate(template, transform);
        image.name = "HintImage";
        image.gameObject.SetActive(true);
        image.raycastTarget = false;
        image.preserveAspect = true;

        RectTransform imageRect = image.rectTransform;
        imageRect.anchorMin = new Vector2(0.5f, 1f);
        imageRect.anchorMax = new Vector2(0.5f, 1f);
        imageRect.pivot = new Vector2(0.5f, 1f);
        imageRect.anchoredPosition = new Vector2(0f, -topOffset);
        imageRect.sizeDelta = hintSize;

        FitToCamera();
    }


    // =====================================================
    // PEDIR AYUDA
    // =====================================================

    public void Request(HintDefinition hint)
    {
        if (hint == null || hint == current || pending.Contains(hint))
            return;

        if (hint.showOncePerMatch && seen.Contains(hint))
            return;

        pending.Add(hint);
    }


    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        FitToCamera();

        if (current != null)
        {
            UpdateCurrent();
            return;
        }

        if (pending.Count == 0 || owner.IsWaiting())
            return;

        ShowNext();
    }


    // Mismo criterio que PlayerStateHUD: anclar al viewport de la cámara.
    private void FitToCamera()
    {
        if (playerCamera == null)
            return;

        Rect viewport = playerCamera.rect;
        root.anchorMin = viewport.min;
        root.anchorMax = viewport.max;
    }


    private void UpdateCurrent()
    {
        if (hiding)
            return;

        visibleTime += Time.unscaledDeltaTime;

        bool timeUp =
            current.hideAfterTime &&
            visibleTime >= current.visibleSeconds;

        bool actionDismiss =
            current.hideWithAction &&
            visibleTime >= owner.MinVisibleBeforeAction &&
            ActionJustPressed();

        if (timeUp || actionDismiss)
        {
            StartCoroutine(HideRoutine(actionDismiss ? "ACCIÓN" : "tiempo"));
        }
    }


    // =====================================================
    // MOSTRAR / OCULTAR
    // =====================================================

    private void ShowNext()
    {
        // Mayor prioridad primero; a igual prioridad, la que se pidió antes.
        int best = 0;

        for (int i = 1; i < pending.Count; i++)
        {
            if (pending[i].priority > pending[best].priority)
                best = i;
        }

        current = pending[best];
        pending.RemoveAt(best);
        seen.Add(current);

        visibleTime = 0f;
        hiding = false;

        // Si ya tenía ACCIÓN pulsado, debe soltarlo y volver a pulsar.
        previousAction = IsActionPressed();

        image.sprite = current.sprite;

        StartCoroutine(Fade(0f, 1f));

        Debug.Log($"💡 AYUDA MOSTRADA [P{PlayerId}]: {current.name}");
    }


    private IEnumerator HideRoutine(string reason)
    {
        hiding = true;

        yield return Fade(group.alpha, 0f);

        Debug.Log($"💡 AYUDA OCULTA [P{PlayerId}] ({reason}): {current.name}");

        current = null;
        hiding = false;
    }


    private IEnumerator Fade(float from, float to)
    {
        float duration = owner.FadeDuration;
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
