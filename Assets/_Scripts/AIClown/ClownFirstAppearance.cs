using System.Collections.Generic;
using UnityEngine;

public class ClownFirstAppearance : MonoBehaviour
{
    [Header("Tiempo de aparición")]
    [Tooltip("Tiempo máximo que el payaso puede permanecer durante la primera aparición.")]
    [SerializeField] private float maximumAppearanceTime = 20f;

    [Tooltip("Tiempo que el payaso permanece después de que un jugador lo haya visto.")]
    [SerializeField] private float timeAfterBeingSeen = 3f;

    [Header("Detección visual")]
    [Tooltip("Ángulo máximo desde el centro de la cámara para considerar que el jugador está mirando al payaso.")]
    [Range(1f, 90f)]
    [SerializeField] private float visionAngle = 35f;

    [Tooltip("Distancia máxima desde la cámara para detectar al payaso.")]
    [SerializeField] private float maximumViewDistance = 100f;

    [Tooltip("Capas que pueden bloquear la visión del jugador.")]
    [SerializeField] private LayerMask lineOfSightLayers = ~0;

    [Tooltip("Si está activo, se utilizará un Raycast para comprobar que no haya una pared entre jugador y payaso.")]
    [SerializeField] private bool requireLineOfSight = true;

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

    private bool IsCameraLookingAtTarget(
        Camera playerCamera,
        Vector3 targetPosition)
    {
        Vector3 direction =
            targetPosition -
            playerCamera.transform.position;

        float distance =
            direction.magnitude;

        if (distance <= 0.01f)
        {
            return true;
        }

        if (distance >
            maximumViewDistance)
        {
            return false;
        }

        direction.Normalize();

        float angle =
            Vector3.Angle(
                playerCamera.transform.forward,
                direction
            );

        if (angle >
            visionAngle)
        {
            if (showVisionDebug)
            {
                Debug.DrawLine(
                    playerCamera.transform.position,
                    targetPosition,
                    Color.red
                );
            }

            return false;
        }

        // -----------------------------------------------------
        // COMPROBAR LÍNEA DE VISIÓN
        // -----------------------------------------------------

        if (requireLineOfSight)
        {
            Ray ray =
                new Ray(
                    playerCamera.transform.position,
                    direction
                );

            RaycastHit hit;

            bool blocked =
                Physics.Raycast(
                    ray,
                    out hit,
                    distance,
                    lineOfSightLayers,
                    QueryTriggerInteraction.Ignore
                );

            if (blocked)
            {
                // Si el primer objeto golpeado NO es el payaso
                // ni uno de sus hijos, algo está bloqueando la visión.

                if (
                    hit.transform != transform &&
                    !hit.transform.IsChildOf(transform)
                )
                {
                    if (showVisionDebug)
                    {
                        Debug.DrawLine(
                            playerCamera.transform.position,
                            hit.point,
                            Color.red
                        );
                    }

                    return false;
                }
            }
        }

        if (showVisionDebug)
        {
            Debug.DrawLine(
                playerCamera.transform.position,
                targetPosition,
                Color.green
            );
        }

        return true;
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