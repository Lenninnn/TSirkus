using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Muestra las ayudas contextuales (sprites tipo aviso) sin bloquear a los jugadores.
// Las HintZone / HintEventTrigger piden ayudas con Request(); aquí se ordenan
// por prioridad y se muestran de una en una.
public class ContextHintController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image hintImage;

    [Header("Tiempos")]
    [SerializeField] private float fadeDuration = 0.35f;

    [Tooltip("Evita que el mismo toque de ACCIÓN que activó algo cierre la ayuda al instante.")]
    [SerializeField] private float minVisibleBeforeAction = 0.6f;

    [Header("Input")]
    [SerializeField] private MobileInputManager mobileInput;

    [Header("Esperar mientras estén visibles")]
    [Tooltip("Intro, cinemática, tutorial inicial... Las ayudas esperan a que se oculten.")]
    [SerializeField] private CanvasGroup[] waitWhileVisible;

    private readonly List<HintDefinition> pending = new List<HintDefinition>();
    private readonly HashSet<HintDefinition> shown = new HashSet<HintDefinition>();
    private readonly bool[] previousAction = new bool[5];

    private HintDefinition current;
    private float visibleTime;
    private bool hiding;


    // =====================================================
    // AWAKE
    // =====================================================

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (hintImage != null)
        {
            hintImage.raycastTarget = false;
            hintImage.preserveAspect = true;
        }

        if (mobileInput == null)
            mobileInput = FindAnyObjectByType<MobileInputManager>();
    }


    // =====================================================
    // PEDIR AYUDA
    // =====================================================

    public void Request(HintDefinition hint)
    {
        if (hint == null)
            return;

        if (hint == current || pending.Contains(hint))
            return;

        if (hint.showOncePerMatch && shown.Contains(hint))
            return;

        pending.Add(hint);
    }


    public bool WasShown(HintDefinition hint)
    {
        return hint != null && shown.Contains(hint);
    }


    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        if (current != null)
        {
            UpdateCurrent();
            return;
        }

        if (pending.Count == 0 || IsWaiting())
            return;

        ShowNext();
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
            visibleTime >= minVisibleBeforeAction &&
            AnyActionJustPressed();

        if (timeUp || actionDismiss)
        {
            StartCoroutine(HideRoutine());
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
        shown.Add(current);

        visibleTime = 0f;
        hiding = false;
        CacheActionState();

        if (hintImage != null)
            hintImage.sprite = current.sprite;

        StartCoroutine(Fade(0f, 1f));

        Debug.Log($"💡 AYUDA MOSTRADA: {current.name}");
    }


    private IEnumerator HideRoutine()
    {
        hiding = true;

        yield return Fade(canvasGroup != null ? canvasGroup.alpha : 1f, 0f);

        Debug.Log($"💡 AYUDA OCULTA: {current.name}");

        current = null;
        hiding = false;
    }


    private IEnumerator Fade(float from, float to)
    {
        if (canvasGroup == null)
            yield break;

        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(time / fadeDuration));
            yield return null;
        }

        canvasGroup.alpha = to;
    }


    // =====================================================
    // ESPERAR A OTRAS PANTALLAS
    // =====================================================

    private bool IsWaiting()
    {
        if (waitWhileVisible == null)
            return false;

        foreach (CanvasGroup group in waitWhileVisible)
        {
            if (group != null && group.gameObject.activeInHierarchy && group.alpha > 0.01f)
                return true;
        }

        return false;
    }


    // =====================================================
    // ACCIÓN (P1 - P4)
    // =====================================================

    private bool AnyActionJustPressed()
    {
        if (mobileInput == null)
            return false;

        bool justPressed = false;

        for (int playerId = 1; playerId <= 4; playerId++)
        {
            bool pressed = mobileInput.IsActionPressed(playerId);

            if (pressed && !previousAction[playerId])
                justPressed = true;

            previousAction[playerId] = pressed;
        }

        return justPressed;
    }


    // Si alguien ya tenía ACCIÓN pulsado al aparecer la ayuda,
    // debe soltarlo y volver a pulsar para cerrarla.
    private void CacheActionState()
    {
        for (int playerId = 1; playerId <= 4; playerId++)
        {
            previousAction[playerId] =
                mobileInput != null && mobileInput.IsActionPressed(playerId);
        }
    }
}
