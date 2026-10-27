using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class DoorCinematicController : MonoBehaviour
{
    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Pantalla de cinemática")]
    [SerializeField] private CanvasGroup cinematicOverlay;

    [Header("Puerta")]
    [SerializeField] private PuzzleDoor puzzleDoor;

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
        }

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;

            videoPlayer.loopPointReached += OnVideoFinished;
        }
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

        // AHORA sí abrimos la puerta.
        if (puzzleDoor != null)
        {
            puzzleDoor.OpenDoor();
        }

        UnlockPlayers();

        Debug.Log(
            "🎬 CINEMÁTICA TERMINADA → PUERTA ABIERTA"
        );
    }


    private void OpenDoorImmediately()
    {
        if (puzzleDoor != null)
        {
            puzzleDoor.OpenDoor();
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
        }
    }
}