using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class DoorCinematicController : MonoBehaviour
{
    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Pantalla de cinemática")]
    [SerializeField] private CanvasGroup cinematicOverlay;

    [Tooltip("Debe ser mayor que el sortingOrder de los PlayerHUD (100) para cubrir toda la pantalla.")]
    [SerializeField] private int overlaySortingOrder = 1000;

    [Header("Cards / overlays de instrucciones")]
    [Tooltip("Se dejan en alpha 0 al iniciar y al terminar la cinemática.")]
    [SerializeField] private CanvasGroup[] instructionCanvasGroups;

    [Tooltip("Se desactivan al iniciar y al terminar la cinemática.")]
    [SerializeField] private GameObject[] instructionObjects;

    [Header("Puerta")]
    [SerializeField] private PuzzleDoor puzzleDoor;

    [Tooltip("Lo activa onPuzzleCompleted; se desactiva al abrir la puerta para dejar libre el corredor.")]
    [SerializeField] private GameObject successObject;

    private bool cinematicPlaying = false;

    private readonly List<Behaviour> blockedComponents =
        new List<Behaviour>();


    private void Awake()
    {
        if (cinematicOverlay != null)
        {
            cinematicOverlay.alpha = 0f;
            cinematicOverlay.interactable = false;
            cinematicOverlay.blocksRaycasts = false;

            BringOverlayToFront();
        }

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;

            videoPlayer.loopPointReached += OnVideoFinished;
            videoPlayer.errorReceived += OnVideoError;
        }
    }


    // Los PlayerHUD usan Canvas overlay con sortingOrder 100;
    // sin un Canvas propio el video queda debajo de ellos.
    private void BringOverlayToFront()
    {
        Canvas overlayCanvas =
            cinematicOverlay.GetComponent<Canvas>();

        if (overlayCanvas == null)
        {
            overlayCanvas =
                cinematicOverlay.gameObject.AddComponent<Canvas>();
        }

        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = overlaySortingOrder;
    }


    // =====================================================
    // LLAMAR DESDE EL PUZZLE
    // =====================================================

    public void PlayDoorCinematic()
    {
        if (cinematicPlaying)
            return;

        if (videoPlayer == null)
        {
            Debug.LogWarning(
                "DoorCinematicController: falta VideoPlayer."
            );

            OpenDoorImmediately();
            return;
        }

        cinematicPlaying = true;

        LockPlayers();

        HideInstructionOverlays();

        if (cinematicOverlay != null)
        {
            cinematicOverlay.alpha = 1f;
            cinematicOverlay.blocksRaycasts = true;
        }

        videoPlayer.Stop();
        videoPlayer.frame = 0;

        if (videoPlayer.isPrepared)
        {
            videoPlayer.Play();
        }
        else
        {
            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.Prepare();
        }

        Debug.Log("🎬 CINEMÁTICA DE PUERTA INICIADA");
    }


    private void OnVideoPrepared(VideoPlayer source)
    {
        source.prepareCompleted -= OnVideoPrepared;

        if (!cinematicPlaying)
            return;

        source.frame = 0;
        source.Play();
    }


    // =====================================================
    // TERMINÓ EL VIDEO
    // =====================================================

    private void OnVideoFinished(VideoPlayer source)
    {
        if (!cinematicPlaying)
            return;

        cinematicPlaying = false;

        if (cinematicOverlay != null)
        {
            cinematicOverlay.alpha = 0f;
            cinematicOverlay.blocksRaycasts = false;
        }

        HideInstructionOverlays();

        // AHORA sí abrimos la puerta.
        if (puzzleDoor != null)
        {
            puzzleDoor.OpenDoor();
        }

        DisableSuccessObject();

        UnlockPlayers();

        Debug.Log(
            "🎬 CINEMÁTICA TERMINADA → PUERTA ABIERTA"
        );
    }


    // Si el video falla, no dejamos a los jugadores bloqueados.
    private void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogWarning(
            $"DoorCinematicController: error de video ({message}). Se abre la puerta igualmente."
        );

        OnVideoFinished(source);
    }


    private void HideInstructionOverlays()
    {
        if (instructionCanvasGroups != null)
        {
            foreach (CanvasGroup group in instructionCanvasGroups)
            {
                if (group == null)
                    continue;

                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }
        }

        if (instructionObjects != null)
        {
            foreach (GameObject instruction in instructionObjects)
            {
                if (instruction != null)
                {
                    instruction.SetActive(false);
                }
            }
        }
    }


    private void OpenDoorImmediately()
    {
        if (puzzleDoor != null)
        {
            puzzleDoor.OpenDoor();
        }

        DisableSuccessObject();
    }


    private void DisableSuccessObject()
    {
        if (successObject != null)
        {
            successObject.SetActive(false);
        }
    }


    // =====================================================
    // BLOQUEAR JUGADORES DURANTE EL VIDEO
    // =====================================================

    private void LockPlayers()
    {
        blockedComponents.Clear();

        PlayerIdentity[] players =
            FindObjectsByType<PlayerIdentity>(
                FindObjectsSortMode.None
            );

        foreach (PlayerIdentity player in players)
        {
            DisableAndRemember(
                player.GetComponent<PlayerMovement>()
            );

            DisableAndRemember(
                player.GetComponent<PlayerLook>()
            );

            DisableAndRemember(
                player.GetComponent<PlayerInteractor>()
            );
        }
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
        blockedComponents.Add(component);
    }


    // =====================================================
    // DEVOLVER EL CONTROL
    // =====================================================

    private void UnlockPlayers()
    {
        foreach (Behaviour component in blockedComponents)
        {
            if (component != null)
            {
                component.enabled = true;
            }
        }

        blockedComponents.Clear();
    }


    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.errorReceived -= OnVideoError;
        }
    }
}