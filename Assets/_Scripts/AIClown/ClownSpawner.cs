using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClownSpawner : MonoBehaviour
{
    [Header("Referencia del payaso")]
    [Tooltip("Prefab del payaso que se creará en la escena.")]
    [SerializeField] private GameObject clownPrefab;

    [Header("Puntos de aparición")]
    [Tooltip("Todos los puntos que puede utilizar el sistema de apariciones normales.")]
    [SerializeField] private ClownSpawnPoint[] spawnPoints;

    [Header("Distancia respecto a jugadores")]
    [Tooltip(
        "Distancia máxima desde un SpawnPoint hasta cualquier PlayerIdentity " +
        "para permitir que el payaso aparezca ahí."
    )]
    [SerializeField] private float maxSpawnDistanceFromPlayers = 15f;

    [Tooltip(
        "Permite cierta variedad entre SpawnPoints que están prácticamente " +
        "a la misma distancia de los jugadores."
    )]
    [SerializeField] private float nearestPointTolerance = 3f;

    [Header("Primera aparición")]
    [Tooltip("Punto específico donde aparecerá el payaso la primera vez.")]
    [SerializeField] private ClownSpawnPoint firstAppearanceSpawnPoint;

    [Tooltip("Si está activo, la primera aparición utilizará el punto específico.")]
    [SerializeField] private bool useSpecificFirstAppearancePoint = true;

    [Header("Control de apariciones")]
    [Tooltip("Debe estar desactivado para que el payaso NO aparezca antes de abrir la puerta.")]
    [SerializeField] private bool startAutomatically = false;

    [Tooltip("Permite que solamente exista un payaso al mismo tiempo.")]
    [SerializeField] private bool onlyOneClownAtATime = true;

    [Header("Tiempo entre apariciones")]
    [SerializeField] private float minimumTimeBetweenAppearances = 25f;
    [SerializeField] private float maximumTimeBetweenAppearances = 50f;

    [Header("Tiempo visible")]
    [SerializeField] private float minimumVisibleDuration = 4f;
    [SerializeField] private float maximumVisibleDuration = 9f;

    [Header("Orientación")]
    [Tooltip("Si está activo, el payaso utilizará la rotación del SpawnPoint.")]
    [SerializeField] private bool useSpawnPointRotation = true;

    [Header("Encuentro")]
    [Tooltip("Manager que controla las apariciones y el encuentro de mirar/no mirar al payaso.")]
    [SerializeField] private ClownEncounterManager clownEncounterManager;

    [Header("Depuración")]
    [SerializeField] private bool showDebugMessages = true;

    private GameObject currentClown;

    private Coroutine appearanceCoroutine;

    private bool appearancesEnabled;

    private bool encounterActive;

    public bool AppearancesEnabled => appearancesEnabled;
    public bool IsClownPresent => currentClown != null;
    public GameObject CurrentClown => currentClown;

    private void Start()
    {
        if (startAutomatically)
        {
            EnableAppearances();
        }
    }

    // =========================================================
    // ACTIVAR SISTEMA
    // =========================================================

    public void EnableAppearances()
    {
        if (appearancesEnabled)
        {
            DebugLog(
                "Las apariciones ya están activadas."
            );

            return;
        }

        if (!ValidateConfiguration())
        {
            Debug.LogError(
                $"{gameObject.name}: No se pueden activar las apariciones porque la configuración es inválida."
            );

            return;
        }

        appearancesEnabled = true;

        if (appearanceCoroutine != null)
        {
            StopCoroutine(
                appearanceCoroutine
            );
        }

        appearanceCoroutine =
            StartCoroutine(
                AppearanceLoop()
            );

        DebugLog(
            "Sistema de apariciones ACTIVADO."
        );
    }

    // =========================================================
    // DESACTIVAR SISTEMA
    // =========================================================

    public void DisableAppearances()
    {
        appearancesEnabled = false;

        if (appearanceCoroutine != null)
        {
            StopCoroutine(
                appearanceCoroutine
            );

            appearanceCoroutine = null;
        }

        // -----------------------------------------------------
        // Si había un encuentro activo, finalizarlo.
        // -----------------------------------------------------

        if (encounterActive)
        {
            if (clownEncounterManager != null)
            {
                clownEncounterManager.FinishEncounter(
                    true
                );
            }
            else
            {
                DespawnClown();
            }
        }
        else
        {
            DespawnClown();
        }

        DebugLog(
            "Sistema de apariciones DESACTIVADO."
        );
    }

    // =========================================================
    // PRIMERA APARICIÓN
    // =========================================================

    public void TriggerFirstAppearance()
    {
        DebugLog(
            "Se solicitó la PRIMERA APARICIÓN."
        );

        if (!ValidateConfiguration())
        {
            Debug.LogError(
                $"{gameObject.name}: No se puede realizar la primera aparición."
            );

            return;
        }

        if (
            onlyOneClownAtATime &&
            currentClown != null
        )
        {
            DebugLog(
                "No se puede realizar la primera aparición porque ya existe un payaso."
            );

            return;
        }

        ClownSpawnPoint selectedPoint = null;

        if (useSpecificFirstAppearancePoint)
        {
            if (
                firstAppearanceSpawnPoint != null &&
                firstAppearanceSpawnPoint.CanBeUsed
            )
            {
                selectedPoint =
                    firstAppearanceSpawnPoint;
            }
            else
            {
                Debug.LogWarning(
                    $"{gameObject.name}: El SpawnPoint específico de la primera aparición no es válido."
                );
            }
        }

        if (selectedPoint == null)
        {
            selectedPoint =
                SelectRandomSpawnPoint();
        }

        if (selectedPoint == null)
        {
            Debug.LogError(
                $"{gameObject.name}: No existe ningún SpawnPoint válido para la primera aparición."
            );

            return;
        }

        if (appearanceCoroutine != null)
        {
            StopCoroutine(
                appearanceCoroutine
            );

            appearanceCoroutine = null;
        }

        bool spawned =
            SpawnClownAtPoint(
                selectedPoint
            );

        if (!spawned)
        {
            return;
        }

        DebugLog(
            "Primera aparición creada correctamente."
        );

        ClownFirstAppearance firstAppearance =
            currentClown.GetComponent<ClownFirstAppearance>();

        if (firstAppearance == null)
        {
            firstAppearance =
                currentClown.AddComponent<ClownFirstAppearance>();

            DebugLog(
                "ClownFirstAppearance añadido automáticamente al payaso."
            );
        }

        firstAppearance.Initialize(
            this
        );
    }

    // =========================================================
    // FINALIZAR PRIMERA APARICIÓN
    // =========================================================

    public void FinishFirstAppearance()
    {
        if (!appearancesEnabled)
        {
            appearancesEnabled = true;
        }

        DebugLog(
            "La primera aparición terminó."
        );

        // -----------------------------------------------------
        // Si existiera un encuentro, terminarlo correctamente.
        // -----------------------------------------------------

        if (encounterActive)
        {
            if (clownEncounterManager != null)
            {
                clownEncounterManager.FinishEncounter(
                    true
                );
            }
            else
            {
                DespawnClown();
            }
        }
        else
        {
            DespawnClown();
        }

        if (appearanceCoroutine != null)
        {
            StopCoroutine(
                appearanceCoroutine
            );
        }

        appearanceCoroutine =
            StartCoroutine(
                AppearanceLoop()
            );

        DebugLog(
            "Sistema de apariciones normales iniciado."
        );
    }

    // =========================================================
    // BUCLE NORMAL
    // =========================================================

    private IEnumerator AppearanceLoop()
    {
        while (appearancesEnabled)
        {
            float timeToNextAppearance =
                Random.Range(
                    minimumTimeBetweenAppearances,
                    maximumTimeBetweenAppearances
                );

            DebugLog(
                $"Próxima aparición en aproximadamente {timeToNextAppearance:F1} segundos."
            );

            yield return new WaitForSeconds(
                timeToNextAppearance
            );

            if (!appearancesEnabled)
            {
                yield break;
            }

            if (
                onlyOneClownAtATime &&
                currentClown != null
            )
            {
                DebugLog(
                    "No se crea otra aparición porque ya hay un payaso presente."
                );

                continue;
            }

            bool spawned =
                TrySpawnClown();

            if (!spawned)
            {
                DebugLog(
                    "No se pudo realizar la aparición."
                );

                continue;
            }

            float visibleDuration =
                Random.Range(
                    minimumVisibleDuration,
                    maximumVisibleDuration
                );

            DebugLog(
                $"El payaso permanecerá visible durante {visibleDuration:F1} segundos."
            );

            yield return new WaitForSeconds(
                visibleDuration
            );

            if (!appearancesEnabled)
            {
                yield break;
            }

            // -------------------------------------------------
            // IMPORTANTE
            //
            // Si existe un encuentro activo, no destruimos
            // directamente al payaso desde aquí.
            // El Manager controla el final del encuentro.
            // -------------------------------------------------

            if (encounterActive)
            {
                DebugLog(
                    "Terminó el tiempo visible, pero el encuentro sigue activo. " +
                    "El payaso NO será eliminado automáticamente."
                );

                continue;
            }

            DespawnClown();
        }
    }

    // =========================================================
    // APARICIÓN NORMAL
    // =========================================================

    public bool TrySpawnClown()
    {
        if (clownPrefab == null)
        {
            Debug.LogError(
                $"{gameObject.name}: No hay un Clown Prefab asignado."
            );

            return false;
        }

        if (
            onlyOneClownAtATime &&
            currentClown != null
        )
        {
            DebugLog(
                "Ya existe un payaso en la escena."
            );

            return false;
        }

        ClownSpawnPoint selectedPoint =
            SelectRandomSpawnPoint();

        if (selectedPoint == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: No hay ningún SpawnPoint válido y cercano a los jugadores."
            );

            return false;
        }

        bool spawned =
            SpawnClownAtPoint(
                selectedPoint
            );

        if (!spawned)
        {
            return false;
        }

        // =====================================================
        // INICIAR ENCUENTRO
        // =====================================================

        if (clownEncounterManager != null)
        {
            clownEncounterManager.StartEncounter(
                currentClown
            );

            DebugLog(
                "Se solicitó al ClownEncounterManager iniciar el encuentro."
            );
        }
        else
        {
            Debug.LogWarning(
                $"{gameObject.name}: No hay ClownEncounterManager asignado. " +
                "El payaso apareció, pero no iniciará el sistema de mirar/no mirar."
            );
        }

        return true;
    }

    // =========================================================
    // CREAR PAYASO
    // =========================================================

    private bool SpawnClownAtPoint(
        ClownSpawnPoint selectedPoint
    )
    {
        if (selectedPoint == null)
        {
            return false;
        }

        if (clownPrefab == null)
        {
            Debug.LogError(
                $"{gameObject.name}: Clown Prefab no asignado."
            );

            return false;
        }

        Vector3 spawnPosition =
            selectedPoint.SpawnPosition;

        Quaternion spawnRotation =
            useSpawnPointRotation
                ? selectedPoint.SpawnRotation
                : Quaternion.identity;

        currentClown =
            Instantiate(
                clownPrefab,
                spawnPosition,
                spawnRotation
            );

        currentClown.name =
            "Clown_Runtime";

        DebugLog(
            $"Payaso creado en: {selectedPoint.gameObject.name}"
        );

        return true;
    }

    // =========================================================
    // SELECCIONAR SPAWNPOINT
    // =========================================================

    private ClownSpawnPoint SelectRandomSpawnPoint()
    {
        if (
            spawnPoints == null ||
            spawnPoints.Length == 0
        )
        {
            return null;
        }

        // -----------------------------------------------------
        // BUSCAR PLAYERS ACTIVOS
        // -----------------------------------------------------

        PlayerIdentity[] foundPlayers =
            FindObjectsOfType<PlayerIdentity>();

        List<PlayerIdentity> activePlayers =
            new List<PlayerIdentity>();

        foreach (
            PlayerIdentity player
            in foundPlayers
        )
        {
            if (player == null)
            {
                continue;
            }

            if (!player.isActiveAndEnabled)
            {
                continue;
            }

            activePlayers.Add(
                player
            );
        }

        if (activePlayers.Count == 0)
        {
            DebugLog(
                "No se encontraron PlayerIdentity activos. " +
                "No se puede seleccionar un SpawnPoint cercano."
            );

            return null;
        }

        DebugLog(
            $"Se encontraron {activePlayers.Count} PlayerIdentity activos para buscar el SpawnPoint."
        );

        // -----------------------------------------------------
        // VARIABLES
        // -----------------------------------------------------

        float maxDistance =
            Mathf.Max(
                0f,
                maxSpawnDistanceFromPlayers
            );

        float maxDistanceSqr =
            maxDistance * maxDistance;

        float nearestDistanceSqr =
            float.MaxValue;

        List<ClownSpawnPoint> validPoints =
            new List<ClownSpawnPoint>();

        // -----------------------------------------------------
        // PRIMER RECORRIDO
        // -----------------------------------------------------

        foreach (
            ClownSpawnPoint point
            in spawnPoints
        )
        {
            if (point == null)
            {
                continue;
            }

            if (!point.CanBeUsed)
            {
                continue;
            }

            float pointClosestDistanceSqr =
                float.MaxValue;

            PlayerIdentity closestPlayer =
                null;

            foreach (
                PlayerIdentity player
                in activePlayers
            )
            {
                float distanceSqr =
                    (
                        point.SpawnPosition -
                        player.transform.position
                    ).sqrMagnitude;

                if (distanceSqr < pointClosestDistanceSqr)
                {
                    pointClosestDistanceSqr =
                        distanceSqr;

                    closestPlayer =
                        player;
                }
            }

            if (pointClosestDistanceSqr > maxDistanceSqr)
            {
                DebugLog(
                    $"SpawnPoint DESCARTADO: {point.gameObject.name}. " +
                    $"Distancia mínima: " +
                    $"{Mathf.Sqrt(pointClosestDistanceSqr):F1}m."
                );

                continue;
            }

            if (pointClosestDistanceSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr =
                    pointClosestDistanceSqr;
            }

            DebugLog(
                $"SpawnPoint CANDIDATO: {point.gameObject.name}. " +
                $"Jugador más cercano: " +
                $"{(closestPlayer != null ? closestPlayer.gameObject.name : "desconocido")}. " +
                $"Distancia: {Mathf.Sqrt(pointClosestDistanceSqr):F1}m."
            );
        }

        // -----------------------------------------------------
        // NO HAY PUNTOS
        // -----------------------------------------------------

        if (nearestDistanceSqr == float.MaxValue)
        {
            DebugLog(
                $"No hay SpawnPoints dentro de {maxDistance:F1}m de ningún jugador activo."
            );

            return null;
        }

        // -----------------------------------------------------
        // SEGUNDO RECORRIDO
        // -----------------------------------------------------

        float tolerance =
            Mathf.Max(
                0f,
                nearestPointTolerance
            );

        float maximumAllowedDistance =
            Mathf.Sqrt(
                nearestDistanceSqr
            ) + tolerance;

        float maximumAllowedDistanceSqr =
            maximumAllowedDistance *
            maximumAllowedDistance;

        foreach (
            ClownSpawnPoint point
            in spawnPoints
        )
        {
            if (point == null)
            {
                continue;
            }

            if (!point.CanBeUsed)
            {
                continue;
            }

            float pointClosestDistanceSqr =
                float.MaxValue;

            foreach (
                PlayerIdentity player
                in activePlayers
            )
            {
                float distanceSqr =
                    (
                        point.SpawnPosition -
                        player.transform.position
                    ).sqrMagnitude;

                if (distanceSqr < pointClosestDistanceSqr)
                {
                    pointClosestDistanceSqr =
                        distanceSqr;
                }
            }

            if (
                pointClosestDistanceSqr <=
                maximumAllowedDistanceSqr
            )
            {
                validPoints.Add(
                    point
                );

                DebugLog(
                    $"SpawnPoint SELECCIONABLE: {point.gameObject.name}. " +
                    $"Distancia mínima: " +
                    $"{Mathf.Sqrt(pointClosestDistanceSqr):F1}m."
                );
            }
        }

        if (validPoints.Count == 0)
        {
            DebugLog(
                "No se encontraron SpawnPoints dentro del margen de tolerancia."
            );

            return null;
        }

        // -----------------------------------------------------
        // ELEGIR ENTRE LOS MÁS CERCANOS
        // -----------------------------------------------------

        int randomIndex =
            Random.Range(
                0,
                validPoints.Count
            );

        ClownSpawnPoint selectedPoint =
            validPoints[
                randomIndex
            ];

        DebugLog(
            $"SpawnPoint seleccionado: " +
            $"{selectedPoint.gameObject.name}. " +
            $"Distancia mínima global: " +
            $"{Mathf.Sqrt(nearestDistanceSqr):F1}m. " +
            $"Puntos cercanos disponibles: " +
            $"{validPoints.Count}."
        );

        return selectedPoint;
    }

    // =========================================================
    // ENCUENTRO
    // =========================================================

    public bool NotifyEncounterStarted()
    {
        if (currentClown == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: No se puede iniciar el estado de encuentro porque no hay payaso."
            );

            return false;
        }

        if (encounterActive)
        {
            DebugLog(
                "El encuentro ya estaba activo."
            );

            return false;
        }

        encounterActive = true;

        DebugLog(
            "Estado de encuentro ACTIVADO."
        );

        return true;
    }

    public void NotifyEncounterFinished(
        bool despawnClown = true
    )
    {
        encounterActive = false;

        DebugLog(
            "Estado de encuentro DESACTIVADO."
        );

        if (despawnClown)
        {
            DespawnClown();
        }
    }

    // =========================================================
    // DESPAWN
    // =========================================================

    public void DespawnClown()
    {
        encounterActive = false;

        if (currentClown == null)
        {
            return;
        }

        Destroy(
            currentClown
        );

        currentClown = null;

        DebugLog(
            "Payaso eliminado de la escena."
        );
    }

    // =========================================================
    // VALIDACIÓN
    // =========================================================

    private bool ValidateConfiguration()
    {
        if (clownPrefab == null)
        {
            Debug.LogError(
                $"{gameObject.name}: Debes asignar el Clown Prefab."
            );

            return false;
        }

        if (
            spawnPoints == null ||
            spawnPoints.Length == 0
        )
        {
            Debug.LogError(
                $"{gameObject.name}: Debes asignar al menos un SpawnPoint."
            );

            return false;
        }

        bool hasValidPoint =
            false;

        foreach (
            ClownSpawnPoint point
            in spawnPoints
        )
        {
            if (
                point != null &&
                point.CanBeUsed
            )
            {
                hasValidPoint = true;
                break;
            }
        }

        if (!hasValidPoint)
        {
            Debug.LogError(
                $"{gameObject.name}: No hay ningún SpawnPoint habilitado."
            );

            return false;
        }

        if (maxSpawnDistanceFromPlayers < 0f)
        {
            Debug.LogError(
                $"{gameObject.name}: La distancia máxima respecto a los jugadores no puede ser negativa."
            );

            return false;
        }

        if (nearestPointTolerance < 0f)
        {
            Debug.LogError(
                $"{gameObject.name}: La tolerancia de SpawnPoint no puede ser negativa."
            );

            return false;
        }

        if (minimumTimeBetweenAppearances < 0f)
        {
            Debug.LogError(
                $"{gameObject.name}: El tiempo mínimo no puede ser negativo."
            );

            return false;
        }

        if (
            maximumTimeBetweenAppearances <
            minimumTimeBetweenAppearances
        )
        {
            Debug.LogError(
                $"{gameObject.name}: El tiempo máximo debe ser mayor o igual al mínimo."
            );

            return false;
        }

        if (minimumVisibleDuration < 0f)
        {
            Debug.LogError(
                $"{gameObject.name}: La duración mínima no puede ser negativa."
            );

            return false;
        }

        if (
            maximumVisibleDuration <
            minimumVisibleDuration
        )
        {
            Debug.LogError(
                $"{gameObject.name}: La duración máxima debe ser mayor o igual a la mínima."
            );

            return false;
        }

        return true;
    }

    // =========================================================
    // DEBUG
    // =========================================================

    private void DebugLog(
        string message
    )
    {
        if (!showDebugMessages)
        {
            return;
        }

        Debug.Log(
            $"{gameObject.name}: {message}"
        );
    }

    // =========================================================
    // PRUEBAS
    // =========================================================

    [ContextMenu("Probar aparición")]
    private void TestSpawn()
    {
        TrySpawnClown();
    }

    [ContextMenu("Probar primera aparición")]
    private void TestFirstAppearance()
    {
        TriggerFirstAppearance();
    }

    [ContextMenu("Probar desaparición")]
    private void TestDespawn()
    {
        DespawnClown();
    }

    [ContextMenu("Activar apariciones")]
    private void TestEnableAppearances()
    {
        EnableAppearances();
    }

    [ContextMenu("Desactivar apariciones")]
    private void TestDisableAppearances()
    {
        DisableAppearances();
    }
}