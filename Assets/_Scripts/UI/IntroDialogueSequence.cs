using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IntroDialogueSequence : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image dialogImage;
    [SerializeField] private CanvasGroup dialogCanvasGroup;
    [SerializeField] private TMP_Text readyStatus;

    [Header("Diálogos")]
    [SerializeField] private Sprite[] dialogueSprites;

    [Header("Tiempos")]
    [SerializeField] private float fadeDuration = 0.35f;

    [Header("Input")]
    [SerializeField] private MobileInputManager mobileInput;

    // P1 - P4
    private readonly bool[] readyPlayers = new bool[5];
    private readonly bool[] previousActionState = new bool[5];

    private int currentDialogue = 0;

    private bool sequenceActive = false;
    private bool changingDialogue = false;
    private bool sequenceStarted = false;

    // Componentes de los jugadores que bloquearemos temporalmente.
    private readonly List<Behaviour> blockedComponents =
        new List<Behaviour>();


    // =====================================================
    // AWAKE
    // =====================================================

    private void Awake()
    {
        if (dialogCanvasGroup != null)
        {
            dialogCanvasGroup.alpha = 0f;
            dialogCanvasGroup.interactable = false;
            dialogCanvasGroup.blocksRaycasts = false;
        }
    }


    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        if (mobileInput == null)
        {
            mobileInput =
                FindFirstObjectByType<MobileInputManager>();
        }
    }


    // =====================================================
    // COMENZAR SECUENCIA DESDE TRIGGER
    // =====================================================

    public void BeginSequence()
    {
        if (sequenceStarted)
            return;

        sequenceStarted = true;

        StartCoroutine(
            BeginSequenceRoutine()
        );
    }


    private IEnumerator BeginSequenceRoutine()
    {
        if (
            dialogueSprites == null ||
            dialogueSprites.Length == 0
        )
        {
            Debug.LogWarning(
                "IntroDialogueSequence no tiene imágenes asignadas."
            );

            sequenceStarted = false;

            yield break;
        }

        // Esperar a que estén creados los jugadores.
        yield return StartCoroutine(
            WaitForPlayers()
        );

        // Aquí es cuando realmente detenemos a todos.
        LockPlayers();

        currentDialogue = 0;

        ShowCurrentDialogue();

        ResetReadyPlayers();

        sequenceActive = true;

        yield return StartCoroutine(
            Fade(
                0f,
                1f
            )
        );
    }


    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        if (!sequenceActive)
            return;

        if (changingDialogue)
            return;

        if (mobileInput == null)
        {
            mobileInput =
                FindFirstObjectByType<MobileInputManager>();

            if (mobileInput == null)
                return;
        }

        CheckPlayerActions();
    }


    // =====================================================
    // INTERACTUAR DE LOS 4 JUGADORES
    // =====================================================

    private void CheckPlayerActions()
    {
        for (int playerId = 1; playerId <= 4; playerId++)
        {
            bool actionPressed =
                mobileInput.IsActionPressed(
                    playerId
                );

            bool actionJustPressed =
                actionPressed &&
                !previousActionState[playerId];

            if (
                actionJustPressed &&
                !readyPlayers[playerId]
            )
            {
                readyPlayers[playerId] = true;

                Debug.Log(
                    $"🎪 INTRO: PLAYER {playerId} LISTO"
                );

                UpdateReadyStatus();
            }

            previousActionState[playerId] =
                actionPressed;
        }

        if (AreAllPlayersReady())
        {
            StartCoroutine(
                NextDialogue()
            );
        }
    }


    // =====================================================
    // SIGUIENTE DIÁLOGO
    // =====================================================

    private IEnumerator NextDialogue()
    {
        changingDialogue = true;

        yield return StartCoroutine(
            Fade(
                1f,
                0f
            )
        );

        currentDialogue++;

        // =================================================
        // TERMINÓ TODA LA SECUENCIA
        // =================================================

        if (
            currentDialogue >=
            dialogueSprites.Length
        )
        {
            sequenceActive = false;

            UnlockPlayers();

            gameObject.SetActive(false);

            Debug.Log(
                "🎪 INTRO COMPLETADA - COMIENZA EL JUEGO"
            );

            yield break;
        }

        ShowCurrentDialogue();

        ResetReadyPlayers();

        yield return StartCoroutine(
            Fade(
                0f,
                1f
            )
        );

        changingDialogue = false;
    }


    // =====================================================
    // MOSTRAR SPRITE
    // =====================================================

    private void ShowCurrentDialogue()
    {
        if (dialogImage == null)
            return;

        dialogImage.sprite =
            dialogueSprites[currentDialogue];

        dialogImage.preserveAspect = true;
    }


    // =====================================================
    // RESET DE CONFIRMACIONES
    // =====================================================

    private void ResetReadyPlayers()
    {
        for (int i = 1; i <= 4; i++)
        {
            readyPlayers[i] = false;

            /*
             * NO reseteamos previousActionState.
             *
             * Esto obliga a que el jugador suelte
             * INTERACTUAR y vuelva a pulsarlo para
             * confirmar el siguiente diálogo.
             */
        }

        UpdateReadyStatus();
    }


    // =====================================================
    // ¿TODOS LISTOS?
    // =====================================================

    private bool AreAllPlayersReady()
    {
        return
            readyPlayers[1] &&
            readyPlayers[2] &&
            readyPlayers[3] &&
            readyPlayers[4];
    }


    // =====================================================
    // TEXTO P1 / P2 / P3 / P4
    // =====================================================

    private void UpdateReadyStatus()
    {
        if (readyStatus == null)
            return;

        readyStatus.text =
            GetPlayerStatus(1) + "     " +
            GetPlayerStatus(2) + "     " +
            GetPlayerStatus(3) + "     " +
            GetPlayerStatus(4);
    }


    private string GetPlayerStatus(
        int playerId
    )
    {
        return readyPlayers[playerId]
            ? $"P{playerId} ✓"
            : $"P{playerId} ○";
    }


    // =====================================================
    // FADE
    // =====================================================

    private IEnumerator Fade(
        float from,
        float to
    )
    {
        if (dialogCanvasGroup == null)
            yield break;

        float time = 0f;

        dialogCanvasGroup.alpha = from;

        while (time < fadeDuration)
        {
            time +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    time /
                    fadeDuration
                );

            dialogCanvasGroup.alpha =
                Mathf.Lerp(
                    from,
                    to,
                    progress
                );

            yield return null;
        }

        dialogCanvasGroup.alpha = to;
    }


    // =====================================================
    // ESPERAR A QUE EXISTAN LOS 4 PLAYERS
    // =====================================================

    private IEnumerator WaitForPlayers()
    {
        float timeout = 3f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            PlayerIdentity[] players =
                FindObjectsByType<PlayerIdentity>(
                    FindObjectsSortMode.None
                );

            if (players.Length >= 4)
            {
                yield break;
            }

            elapsed +=
                Time.unscaledDeltaTime;

            yield return null;
        }

        Debug.LogWarning(
            "⚠️ INTRO: no se encontraron los 4 jugadores antes del timeout."
        );
    }


    // =====================================================
    // BLOQUEAR JUGADORES
    // =====================================================

    private void LockPlayers()
    {
        blockedComponents.Clear();

        PlayerIdentity[] players =
            FindObjectsByType<PlayerIdentity>(
                FindObjectsSortMode.None
            );

        foreach (
            PlayerIdentity player
            in players
        )
        {
            PlayerMovement movement =
                player.GetComponent<PlayerMovement>();

            PlayerLook look =
                player.GetComponent<PlayerLook>();

            PlayerInteractor interactor =
                player.GetComponent<PlayerInteractor>();

            DisableAndRemember(
                movement
            );

            DisableAndRemember(
                look
            );

            DisableAndRemember(
                interactor
            );
        }

        Debug.Log(
            "🔒 INTRO: jugadores bloqueados"
        );
    }


    private void DisableAndRemember(
        Behaviour component
    )
    {
        if (component == null)
            return;

        if (!component.enabled)
            return;

        component.enabled = false;

        blockedComponents.Add(
            component
        );
    }


    // =====================================================
    // DESBLOQUEAR JUGADORES
    // =====================================================

    private void UnlockPlayers()
    {
        foreach (
            Behaviour component
            in blockedComponents
        )
        {
            if (component != null)
            {
                component.enabled = true;
            }
        }

        blockedComponents.Clear();

        Debug.Log(
            "🔓 INTRO: jugadores desbloqueados"
        );
    }


    // =====================================================
    // SEGURIDAD
    // =====================================================

    private void OnDisable()
    {
        /*
         * Si por cualquier razón este objeto se desactiva
         * mientras la introducción está activa,
         * devolvemos el control a los jugadores.
         */
        if (
            sequenceActive &&
            blockedComponents.Count > 0
        )
        {
            UnlockPlayers();
        }
    }
}