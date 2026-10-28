using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

public class CinematicManager : MonoBehaviour
{
    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Loading UI")]
    [SerializeField] private GameObject loadingPanel;

    private void Start()
    {
        if (videoPlayer == null)
        {
            Debug.LogError("No se asignó el VideoPlayer.");
            return;
        }

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }

        videoPlayer.loopPointReached += OnVideoFinished;
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        StartCoroutine(LoadGame());
    }

    private IEnumerator LoadGame()
    {
        // Mostrar pantalla de carga
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }

        // Esperar un frame para asegurarnos
        // de que la UI se dibuje
        yield return null;

        // Cargar la escena del juego
        AsyncOperation operation =
            SceneManager.LoadSceneAsync("ConexionJugadores");

        // Evita que la escena se active inmediatamente
        operation.allowSceneActivation = false;

        // Esperar hasta que esté casi completamente cargada
        while (operation.progress < 0.9f)
        {
            yield return null;
        }

        // La escena está lista
        operation.allowSceneActivation = true;
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }
}