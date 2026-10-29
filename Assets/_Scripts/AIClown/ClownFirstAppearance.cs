using System.Collections.Generic;
using UnityEngine;

public class ClownFirstAppearance : MonoBehaviour
{
    [Header("Tiempo de aparición")]
    [Tooltip("Tiempo máximo que el payaso puede permanecer durante la primera aparición.")]
    [SerializeField] private float maximumAppearanceTime = 20f;

    [Tooltip("Tiempo que el payaso permanece después de que un jugador lo haya visto.")]
    [SerializeField] private float timeAfterBeingSeen = 1f;

    [Header("Detección visual")]
    [Tooltip("Distancia máxima desde la cámara para detectar al payaso (campo de visión y paredes: ClownVisibility).")]
    [SerializeField] private float maximumViewDistance = 100f;

    [Header("Punto de observación")]
    [Tooltip("Transform opcional que representa la parte del payaso que debe estar visible. Si está vacío, se utiliza este objeto.")]
    [SerializeField] private Transform visualTarget;

    [Header("Jugadores")]
    [Tooltip("Si está activo, el sistema buscará automáticamente las cámaras de los jugadores.")]
    [SerializeField] private bool findPlayerCamerasAutomatically = true;

    [Tooltip("Cámaras de los jugadores. Se utilizan si la búsqueda automática no encuentra alguna.")]
    [SerializeField] private Camera[] playerCameras = new Camera[4];

    [Header("Depuración")]
    [SerializeField] private bool showDebugMessages = true;

    [SerializeField] private bool showVisionDebug = false;

    private ClownSpawner clownSpawner;

    private bool initialized;

    public bool IsRunning => initialized;
    private bool hasBeenSeen;

    private float appearanceTimer;
    private float seenTimer;

    private readonly List<Camera> detectedCameras =
        new List<Camera>();

    // =========================================================
    // INICIALIZACIÓN
    // =========================================================

    public void Initialize(
        ClownSpawner spawner)
    {
        clownSpawner = spawner;

        initialized = true;
        hasBeenSeen = false;

        appearanceTimer = 0f;
        seenTimer = 0f;

        if (visualTarget == null)
        {
            visualTarget = transform;
        }

        FindPlayerCameras();

        DebugLog(
            "Primera aparición inicializada."
        );

        DebugLog(
            $"Cámaras encontradas: {detectedCameras.Count}"
        );
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        appearanceTimer +=
            Time.deltaTime;

        // -----------------------------------------------------
        // TIEMPO MÁXIMO
        // -----------------------------------------------------

        if (appearanceTimer >=
            maximumAppearanceTime)
        {
            DebugLog(
                "Se alcanzó el tiempo máximo de la primera aparición."
            );

            FinishAppearance();

            return;
        }

        // -----------------------------------------------------
        // SI YA FUE VISTO
        // -----------------------------------------------------

        if (hasBeenSeen)
        {
            seenTimer +=
                Time.deltaTime;

            if (seenTimer >=
                timeAfterBeingSeen)
            {
                DebugLog(
                    "El payaso ya fue visto y terminó su tiempo de permanencia."
                );

                FinishAppearance();
            }

            return;
        }

        // -----------------------------------------------------
        // BUSCAR CÁMARAS
        // -----------------------------------------------------

        if (findPlayerCamerasAutomatically &&
            appearanceTimer % 1f < Time.deltaTime)
        {
            FindPlayerCameras();
        }

        // -----------------------------------------------------
        // COMPROBAR SI ALGUIEN LO VE
        // -----------------------------------------------------

        if (IsAnyPlayerLookingAtClown())
        {
            hasBeenSeen = true;

            seenTimer = 0f;

            DebugLog(
                "¡EL PAYASO HA SIDO VISTO!"
            );
        }
    }

    // =========================================================
    // BUSCAR CÁMARAS DE JUGADORES
    // =========================================================

    private void FindPlayerCameras()
    {
        detectedCameras.Clear();

        // -----------------------------------------------------
        // PRIMERO: CÁMARAS ASIGNADAS MANUALMENTE
        // -----------------------------------------------------

        if (playerCameras != null)
        {
            for (
                int i = 0;
                i < playerCameras.Length;
                i++
            )
            {
                Camera camera =
                    playerCameras[i];

                if (camera == null)
                {
                    continue;
                }

                if (!detectedCameras.Contains(camera))
                {
                    detectedCameras.Add(
                        camera
                    );
                }
            }
        }

        // -----------------------------------------------------
        // SEGUNDO: BÚSQUEDA AUTOMÁTICA
        // -----------------------------------------------------

        if (!findPlayerCamerasAutomatically)
        {
            return;
        }

        PlayerIdentity[] players =
            FindObjectsOfType<PlayerIdentity>();

        foreach (
            PlayerIdentity player
            in players
        )
        {
            if (player == null)
            {
                continue;
            }

            Camera playerCamera =
                player.GetComponentInChildren<Camera>();

            if (playerCamera == null)
            {
                continue;
            }

            if (!detectedCameras.Contains(
                playerCamera))
            {
                detectedCameras.Add(
                    playerCamera
                );
            }
        }
    }

    // =========================================================
    // COMPROBAR SI ALGÚN JUGADOR LO VE
    // =========================================================

    private bool IsAnyPlayerLookingAtClown()
    {
        if (visualTarget == null)
        {
            visualTarget =
                transform;
        }

        if (detectedCameras.Count == 0)
        {
            return false;
        }

        Vector3 targetPosition =
            visualTarget.position;

        foreach (
            Camera playerCamera
            in detectedCameras
        )
        {
            if (playerCamera == null)
            {
                continue;
            }

            if (IsCameraLookingAtTarget(
                playerCamera,
                targetPosition))
            {
                return true;
            }
        }

        return false;
    }

    // =========================================================
    // COMPROBAR CÁMARA
    // =========================================================

    // Mismo criterio que durante el juego (ClownVisibility): dentro del viewport de la cámara,
    // con línea de visión a la cabeza, pecho, cintura o pies, y a menos de maximumViewDistance.
    private bool IsCameraLookingAtTarget(
        Camera playerCamera,
        Vector3 targetPosition)
    {
        Collider body = GetComponent<Collider>();

        Bounds bounds = body != null
            ? body.bounds
            : new Bounds(targetPosition + Vector3.up * 1.5f, new Vector3(0.9f, 3f, 0.9f));

        bool visible = ClownVisibility.CanCameraSee(
            playerCamera,
            transform,
            bounds,
            maximumViewDistance,
            out _
        );

        if (showVisionDebug)
        {
            Debug.DrawLine(
                playerCamera.transform.position,
                bounds.center,
                visible ? Color.green : Color.red
            );
        }

        return visible;
    }

    // =========================================================
    // TERMINAR
    // =========================================================

    private void FinishAppearance()
    {
        if (!initialized)
        {
            return;
        }

        initialized = false;

        DebugLog(
            "Primera aparición finalizada."
        );

        if (clownSpawner != null)
        {
            clownSpawner.FinishFirstAppearance();
        }
        else
        {
            DebugLog(
                "No existe referencia al ClownSpawner."
            );
        }
    }

    // =========================================================
    // DEBUG
    // =========================================================

    private void DebugLog(
        string message)
    {
        if (!showDebugMessages)
        {
            return;
        }

        Debug.Log(
            $"{gameObject.name}: {message}"
        );
    }
}