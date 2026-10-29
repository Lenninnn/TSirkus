using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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

    [Tooltip("Segundos tras recuperar el control (fin de la cinemática) antes de la primera aparición.")]
    [SerializeField] private float firstAppearanceDelay = 2f;

    [Tooltip("Espera máxima (s) a que se cierre el aviso del objetivo antes de la primera aparición.")]
    [SerializeField] private float maxWaitForObjective = 12f;

    [Header("Control de apariciones")]
    [Tooltip("Debe estar desactivado para que el payaso NO aparezca antes de abrir la puerta.")]
    [SerializeField] private bool startAutomatically = false;

    [Tooltip("Permite que solamente exista un payaso al mismo tiempo.")]
    [SerializeField] private bool onlyOneClownAtATime = true;

    [Header("Tiempo entre apariciones")]
    [Tooltip("Pausa aleatoria (s) desde que desaparece hasta que puede volver a aparecer.")]
    [SerializeField] private float minimumTimeBetweenAppearances = 25f;
    [SerializeField] private float maximumTimeBetweenAppearances = 50f;

    [Header("Tiempo visible")]
    [Tooltip("Tiempo máximo (s) rondando si nadie lo ve. Aleatorio en el rango.")]
    [SerializeField] private float minimumVisibleDuration = 4f;
    [SerializeField] private float maximumVisibleDuration = 9f;

    [Header("Aparición segura")]
    [Tooltip("Nunca aparece más cerca que esto (m) de ningún jugador.")]
    [SerializeField] private float minSpawnDistanceFromPlayers = 7f;

    [Tooltip("Bandas de distancia (m) al jugador más cercano: cerca hasta nearBandMax, media hasta midBandMax, lejos hasta maxSpawnDistanceFromPlayers.")]
    [SerializeField] private float nearBandMax = 11f;
    [SerializeField] private float midBandMax = 18f;

    [Tooltip("Probabilidad de cada banda: x = cerca, y = media, z = lejos.")]
    [SerializeField] private Vector3 bandWeights = new Vector3(0.2f, 0.45f, 0.35f);

    [Tooltip("Puntos aleatorios del NavMesh que se prueban además de los SpawnPoints.")]
    [SerializeField] private int randomCandidates = 24;

    [Tooltip("Probabilidad de buscar los puntos aleatorios detrás o a los lados del jugador.")]
    [Range(0f, 1f)]
    [SerializeField] private float behindBias = 0.75f;

    [Tooltip("No aparece donde alguna cámara vería cualquier parte del payaso hasta esta distancia.")]
    [SerializeField] private float unseenCheckDistance = 120f;

    [Header("Apariciones ambientales (antes de la primera puerta)")]
    [Tooltip("Desde el inicio de la partida hasta que se abre la primera puerta, el payaso aparece de vez en cuando a lo lejos, observa, camina un poco y se retira. No acecha ni captura.")]
    [SerializeField] private bool ambientBeforeFirstDoor = true;

    [Tooltip("Segundos desde el inicio hasta la primera aparición ambiental (aleatorio en el rango).")]
    [SerializeField] private Vector2 ambientFirstDelay = new Vector2(20f, 40f);

    [Tooltip("Pausa (s) entre apariciones ambientales, desde que se va hasta la siguiente.")]
    [SerializeField] private Vector2 ambientInterval = new Vector2(45f, 90f);

    [Tooltip("Duración máxima (s) de cada aparición ambiental.")]
    [SerializeField] private Vector2 ambientDuration = new Vector2(14f, 22f);

    [Tooltip("Distancia (m) al jugador más cercano. El mínimo es mayor que la distancia a la que desaparece al ser visto (22 m): así se le puede ver caminar.")]
    [SerializeField] private float ambientMinDistance = 25f;
    [SerializeField] private float ambientMaxDistance = 45f;

    [Tooltip("Segundos quieto observando al aparecer y entre tramos.")]
    [SerializeField] private Vector2 ambientWatchTime = new Vector2(2.5f, 5f);

    [Tooltip("Largo (m) del tramo que camina antes de retirarse.")]
    [SerializeField] private Vector2 ambientWalkDistance = new Vector2(6f, 11f);

    [Tooltip("Nunca aparece ni camina a menos de esto (m) de interruptores, palancas o zonas de interacción.")]
    [SerializeField] private float interactionClearance = 4f;

    [Header("Salida")]
    [Tooltip("Si al acabar su tiempo alguien lo mira de lejos, espera hasta esto (s) a que deje de mirarlo para irse.")]
    [SerializeField] private float maxWaitToLeaveUnseen = 10f;

    [Tooltip("Segundos tras alcanzar a un jugador antes de desaparecer.")]
    [SerializeField] private float vanishAfterCapture = 1.2f;

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
    private Coroutine firstAppearanceRoutine;
    private Coroutine ambientRoutine;
    private bool lastAmbientFailed;

    public bool AppearancesEnabled => appearancesEnabled;
    public bool IsClownPresent => currentClown != null;
    public GameObject CurrentClown => currentClown;

    // Última aparición, para diagnóstico: posición, distancia al jugador más cercano y origen.
    public string LastSpawnInfo { get; private set; }

    private static readonly List<PlayerIdentity> activePlayers = new List<PlayerIdentity>();
    private static float activePlayersRefresh = -1f;

    // Jugadores reales (con cámara) y activos. Se refresca como mucho una vez por segundo.
    public static List<PlayerIdentity> ActivePlayers()
    {
        if (Time.time < activePlayersRefresh && activePlayers.TrueForAll(p => p != null))
        {
            return activePlayers;
        }

        activePlayersRefresh = Time.time + 1f;
        activePlayers.Clear();

        foreach (PlayerIdentity player in FindObjectsByType<PlayerIdentity>())
        {
            if (player.isActiveAndEnabled && player.GetComponentInChildren<Camera>() != null)
            {
                activePlayers.Add(player);
            }
        }

        return activePlayers;
    }

    private void Start()
    {
        if (startAutomatically)
        {
            EnableAppearances();
        }
        else if (ambientBeforeFirstDoor)
        {
            BlockClosedDoors();
            ambientRoutine = StartCoroutine(AmbientLoop());
        }
    }


    // El NavMesh se horneó sin la puerta de la primera prueba (el payaso normal solo existe con
    // ella abierta). Mientras está cerrada, un obstáculo invisible hijo de la puerta corta el
    // NavMesh: el payaso ambiental no la atraviesa. Al abrirse, la puerta se desactiva y el
    // obstáculo con ella. Solo en ejecución: la escena no cambia.
    private static void BlockClosedDoors()
    {
        foreach (PuzzleDoor door in FindObjectsByType<PuzzleDoor>())
        {
            GameObject visual = door.DoorVisual;

            if (visual == null || !visual.activeInHierarchy || visual.transform.Find("ClownDoorObstacle") != null)
                continue;

            Bounds bounds = new Bounds();
            bool found = false;

            foreach (Collider collider in visual.GetComponentsInChildren<Collider>())
            {
                if (collider.isTrigger) continue;
                if (!found) { bounds = collider.bounds; found = true; }
                else bounds.Encapsulate(collider.bounds);
            }

            if (!found)
            {
                foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>())
                {
                    if (!found) { bounds = renderer.bounds; found = true; }
                    else bounds.Encapsulate(renderer.bounds);
                }
            }

            if (!found)
                continue;

            var obstacleObject = new GameObject("ClownDoorObstacle");
            obstacleObject.transform.SetParent(visual.transform, false);
            obstacleObject.transform.SetPositionAndRotation(bounds.center, Quaternion.identity);
            obstacleObject.transform.localScale = new Vector3(
                1f / Mathf.Max(0.0001f, visual.transform.lossyScale.x),
                1f / Mathf.Max(0.0001f, visual.transform.lossyScale.y),
                1f / Mathf.Max(0.0001f, visual.transform.lossyScale.z));

            NavMeshObstacle obstacle = obstacleObject.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = bounds.size + new Vector3(0.6f, 0f, 0.6f);
            obstacle.carving = true;
            obstacle.carveOnlyStationary = true;
        }
    }


    // =========================================================
    // APARICIONES AMBIENTALES
    // =========================================================

    private IEnumerator AmbientLoop()
    {
        yield return new WaitForSeconds(Random.Range(ambientFirstDelay.x, ambientFirstDelay.y));

        while (true)
        {
            bool spawned = currentClown == null && TrySpawnAmbient();
            lastAmbientFailed = currentClown == null && !spawned;

            if (spawned)
            {
                ClownAI clown = currentClown.GetComponent<ClownAI>();
                float lifetime = Random.Range(ambientDuration.x, ambientDuration.y);
                float elapsed = 0f;

                while (currentClown != null && elapsed < lifetime && !clown.AmbientFinished)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                // Se va cuando nadie lo está mirando (como mucho maxWaitToLeaveUnseen).
                float waited = 0f;

                while (currentClown != null && clown.IsObserved && waited < maxWaitToLeaveUnseen)
                {
                    waited += Time.deltaTime;
                    yield return null;
                }

                if (currentClown != null)
                {
                    Vanish("aparición ambiental: se retira");
                }
            }

            // Sin punto visible (todos mirando a todos lados): reintenta antes.
            float pause = currentClown == null && lastAmbientFailed
                ? 8f
                : Random.Range(ambientInterval.x, ambientInterval.y);
            DebugLog($"Próxima aparición ambiental en aproximadamente {pause:F1} segundos.");
            yield return new WaitForSeconds(pause);
        }
    }


    private void StopAmbient()
    {
        if (ambientRoutine != null)
        {
            StopCoroutine(ambientRoutine);
            ambientRoutine = null;
        }

        ClownAI clown = currentClown != null ? currentClown.GetComponent<ClownAI>() : null;

        if (clown != null && clown.IsAmbient)
        {
            Vanish("termina la fase ambiental (se abrió la puerta)");
        }
    }


    // Lejos (25-45 m: se le puede ver caminar sin que desaparezca), fuera de la vista de las 4
    // cámaras al aparecer, preferiblemente con línea de visión libre hacia algún jugador (para que
    // pueda verse al girarse), lejos de zonas de interacción y con un recorrido corto válido.
    public bool TrySpawnAmbient()
    {
        if (clownPrefab == null || currentClown != null)
        {
            return false;
        }

        List<PlayerIdentity> players = ActivePlayers();

        if (players.Count == 0)
        {
            return false;
        }

        List<Camera> cameras = new List<Camera>();

        foreach (PlayerIdentity player in players)
        {
            cameras.Add(player.GetComponentInChildren<Camera>());
        }

        List<Vector3> interactions = InteractionPoints();
        List<Vector3> visible = new List<Vector3>();

        for (int i = 0; i < 120; i++)
        {
            PlayerIdentity player = players[Random.Range(0, players.Count)];
            Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(ambientMinDistance, ambientMaxDistance);
            Vector3 candidate = player.transform.position + new Vector3(offset.x, 0f, offset.y);

            if (!IsAmbientSpot(candidate, players, interactions, out Vector3 position))
                continue;

            Bounds body = new Bounds(position + Vector3.up * 1.5f, new Vector3(0.9f, 3f, 0.9f));

            if (ClownVisibility.IsObservedByAny(cameras, body, unseenCheckDistance))
                continue;

            // Solo puntos que algún jugador vería si se girara: un payaso tras la puerta cerrada
            // (o tras una pared) no asusta a nadie.
            if (HasLineOfSightFromAnyPlayer(position, cameras))
                visible.Add(position);
        }

        if (visible.Count == 0)
        {
            DebugLog("Aparición ambiental: ningún punto lejano visible ahora mismo.");
            return false;
        }

        Vector3 spawn = visible[Random.Range(0, visible.Count)];

        if (!BuildAmbientRoute(spawn, players, interactions, out Vector3[] route))
        {
            DebugLog("Aparición ambiental: sin recorrido válido desde el punto elegido.");
            return false;
        }

        NearestPlayerDistance(spawn, out PlayerIdentity nearest);
        Vector3 look = nearest.transform.position - spawn;
        look.y = 0f;

        if (!SpawnClownAt(spawn, look.sqrMagnitude > 0.01f ? Quaternion.LookRotation(look) : Quaternion.identity,
                $"ambiental ({visible.Count} puntos visibles)"))
        {
            return false;
        }

        currentClown.GetComponent<ClownAI>().BeginAmbient(
            route,
            Random.Range(ambientWatchTime.x, ambientWatchTime.y),
            Random.Range(ambientWatchTime.x, ambientWatchTime.y)
        );

        return true;
    }


    // Tramo corto de paseo y luego retirada alejándose del jugador más cercano.
    private bool BuildAmbientRoute(Vector3 spawn, List<PlayerIdentity> players, List<Vector3> interactions, out Vector3[] route)
    {
        route = null;
        NearestPlayerDistance(spawn, out PlayerIdentity nearest);
        Vector3 away = spawn - nearest.transform.position;
        away.y = 0f;
        away.Normalize();

        Vector3 walk = Vector3.zero;
        bool hasWalk = false;

        for (int i = 0; i < 20 && !hasWalk; i++)
        {
            // Paseo de lado respecto a los jugadores (cruza su campo de visión a lo lejos).
            Vector3 side = Quaternion.Euler(0f, Random.value < 0.5f ? 90f : -90f, 0f) * away;
            Vector3 direction = Vector3.Slerp(side, away, Random.Range(0f, 0.4f)).normalized;
            hasWalk = IsAmbientSpot(spawn + direction * Random.Range(ambientWalkDistance.x, ambientWalkDistance.y),
                players, interactions, out walk) && IsReachable(spawn, walk);
        }

        if (!hasWalk)
        {
            return false;
        }

        Vector3 exit = walk;

        for (int i = 0; i < 20; i++)
        {
            Vector3 direction = Quaternion.Euler(0f, Random.Range(-50f, 50f), 0f) * away;

            if (IsAmbientSpot(walk + direction * Random.Range(6f, 10f), players, interactions, out Vector3 candidate) &&
                IsReachable(walk, candidate))
            {
                exit = candidate;
                break;
            }
        }

        route = new[] { walk, exit };
        return true;
    }


    private bool IsAmbientSpot(Vector3 candidate, List<PlayerIdentity> players, List<Vector3> interactions, out Vector3 position)
    {
        position = candidate;

        if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            return false;

        position = hit.position;
        float distance = NearestPlayerDistance(position, out PlayerIdentity nearest);

        if (nearest == null || distance < ambientMinDistance - 2f || distance > ambientMaxDistance + 5f)
            return false;

        if (Mathf.Abs(position.y - nearest.transform.position.y) > 2.5f)
            return false;

        foreach (Vector3 interaction in interactions)
        {
            if (Vector3.Distance(position, interaction) < interactionClearance)
                return false;
        }

        if (Physics.CheckCapsule(position + Vector3.up * 0.6f, position + Vector3.up * 2.6f, 0.35f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return false;

        return NavMesh.SamplePosition(nearest.transform.position, out NavMeshHit playerHit, 2f, NavMesh.AllAreas) &&
               IsReachable(position, playerHit.position);
    }


    private static bool IsReachable(Vector3 from, Vector3 to)
    {
        NavMeshPath path = new NavMeshPath();
        return NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete;
    }


    // ¿Algún jugador lo vería si se girara? (línea libre desde su cámara al pecho del payaso).
    private static bool HasLineOfSightFromAnyPlayer(Vector3 position, List<Camera> cameras)
    {
        Vector3 chest = position + Vector3.up * 1.8f;

        foreach (Camera camera in cameras)
        {
            Vector3 origin = camera.transform.position;
            Vector3 toChest = chest - origin;
            RaycastHit[] hits = Physics.RaycastAll(origin, toChest.normalized, toChest.magnitude - 0.1f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            bool blocked = false;

            foreach (RaycastHit hit in hits)
            {
                if (hit.transform.root != camera.transform.root)
                {
                    blocked = true;
                    break;
                }
            }

            if (!blocked)
                return true;
        }

        return false;
    }


    // Interruptores, palancas y disparadores de pruebas: el payaso ambiental no se acerca.
    private static List<Vector3> InteractionPoints()
    {
        List<Vector3> points = new List<Vector3>();

        foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>())
        {
            if (behaviour is IInteractable || behaviour is PlayerPuzzleTrigger || behaviour is FlashlightPuzzleRoomTrigger)
                points.Add(behaviour.transform.position);
        }

        return points;
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
        StopAmbient();

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

    // La puerta llama aquí en el mismo frame en que termina la cinemática. La primera aparición
    // espera a que el overlay de la cinemática haya desaparecido y los jugadores tengan el control,
    // y a que se cierre el aviso del objetivo (con un máximo), para no pisar la transición.
    public void TriggerFirstAppearance()
    {
        if (firstAppearanceRoutine != null)
        {
            return;
        }

        // Se abrió la puerta: terminan las apariciones ambientales y empieza el sistema normal.
        StopAmbient();

        firstAppearanceRoutine = StartCoroutine(FirstAppearanceWhenReady());
    }


    private IEnumerator FirstAppearanceWhenReady()
    {
        DebugLog(
            "Primera aparición pedida: espera al final de la cinemática y del objetivo."
        );

        yield return null;

        while (GameAudio.Instance != null && GameAudio.Instance.AmbiencePaused)
        {
            yield return null;
        }

        yield return new WaitForSeconds(firstAppearanceDelay);

        float waited = 0f;

        while (waited < maxWaitForObjective && IsAnyObjectiveVisible())
        {
            waited += Time.deltaTime;
            yield return null;
        }

        firstAppearanceRoutine = null;
        SpawnFirstAppearance();
    }


    private static bool IsAnyObjectiveVisible()
    {
        foreach (PlayerObjectiveHUD hud in FindObjectsByType<PlayerObjectiveHUD>())
        {
            if (hud.CurrentObjective != null)
            {
                return true;
            }
        }

        return false;
    }


    private void SpawnFirstAppearance()
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

        // Primera aparición: escena preparada, se queda quieto mirando a los jugadores.
        ClownAI firstClown = currentClown.GetComponent<ClownAI>();

        if (firstClown != null)
        {
            firstClown.RoamingEnabled = false;
        }
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

    // Ritmo: pausa aleatoria → aparece fuera de la vista → ronda/acecha hasta que alguien lo ve
    // (desaparece) o se acaba su tiempo (se va cuando nadie lo mira) → otra pausa aleatoria.
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
                continue;
            }

            if (!TrySpawnClown())
            {
                // Sin punto válido ahora (todos a la vista): reintenta pronto.
                yield return new WaitForSeconds(3f);
                continue;
            }

            float lifetime =
                Random.Range(
                    minimumVisibleDuration,
                    maximumVisibleDuration
                );

            float elapsed = 0f;

            while (currentClown != null && elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Se acabó su tiempo: se va cuando nadie lo esté mirando (sin "pop" a la vista).
            float waited = 0f;
            ClownAI clown = currentClown != null ? currentClown.GetComponent<ClownAI>() : null;

            while (
                clown != null &&
                (clown.IsObserved || clown.HasTarget) &&
                waited < maxWaitToLeaveUnseen
            )
            {
                waited += Time.deltaTime;
                yield return null;
            }

            if (currentClown != null)
            {
                Vanish("se le acabó el tiempo");
            }
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

        if (!SelectSafeSpawn(out Vector3 position, out Quaternion rotation, out string origin))
        {
            DebugLog(
                "No hay un punto seguro fuera de la vista de los 4 jugadores ahora mismo."
            );

            return false;
        }

        if (!SpawnClownAt(position, rotation, origin))
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

        Quaternion spawnRotation =
            useSpawnPointRotation
                ? selectedPoint.SpawnRotation
                : Quaternion.identity;

        return SpawnClownAt(
            selectedPoint.SpawnPosition,
            spawnRotation,
            selectedPoint.gameObject.name
        );
    }


    private bool SpawnClownAt(
        Vector3 position,
        Quaternion rotation,
        string origin
    )
    {
        // Siempre sobre el NavMesh (los SpawnPoints antiguos tienen alturas de -0.7 a 0).
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            position = hit.position;
        }
        else
        {
            Debug.LogWarning(
                $"{gameObject.name}: {origin} no está sobre el NavMesh; el payaso no podrá moverse."
            );
        }

        currentClown =
            Instantiate(
                clownPrefab,
                position,
                rotation
            );

        currentClown.name =
            "Clown_Runtime";

        ClownAI clownAI = currentClown.GetComponent<ClownAI>();

        if (clownAI != null)
        {
            clownAI.Initialize(this);
        }

        float nearest = NearestPlayerDistance(position, out PlayerIdentity nearestPlayer);

        LastSpawnInfo =
            $"{origin} pos={position} a {nearest:F1} m de " +
            $"P{(nearestPlayer != null ? nearestPlayer.PlayerId : 0)}";

        DebugLog(
            $"🤡 Aparece: {LastSpawnInfo}"
        );

        return true;
    }


    // =========================================================
    // PUNTO DE APARICIÓN SEGURO
    // =========================================================

    // Candidatos: SpawnPoints de la escena + puntos aleatorios del NavMesh alrededor de los
    // jugadores (sobre todo detrás o a los lados). Se elige una banda de distancia al azar
    // (cerca / media / lejos) y dentro de ella un candidato válido al azar. Válido =
    //   sobre el NavMesh, a la altura del jugador, alcanzable caminando desde él (dentro del
    //   escenario), sin paredes ocupando su volumen y SIN que ninguna cámara vea ninguna parte
    //   del payaso en ese punto.
    private bool SelectSafeSpawn(
        out Vector3 position,
        out Quaternion rotation,
        out string origin
    )
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
        origin = null;

        List<PlayerIdentity> players = ActivePlayers();

        if (players.Count == 0)
        {
            return false;
        }

        List<Camera> cameras = new List<Camera>();

        foreach (PlayerIdentity player in players)
        {
            cameras.Add(player.GetComponentInChildren<Camera>());
        }

        float farMax = Mathf.Max(midBandMax + 1f, maxSpawnDistanceFromPlayers);

        List<(Vector3 position, string origin)>[] bands =
        {
            new List<(Vector3, string)>(),
            new List<(Vector3, string)>(),
            new List<(Vector3, string)>()
        };

        // SpawnPoints de la escena.
        if (spawnPoints != null)
        {
            foreach (ClownSpawnPoint point in spawnPoints)
            {
                if (point != null && point.CanBeUsed)
                {
                    TryAddCandidate(point.SpawnPosition, point.gameObject.name, players, cameras, bands, farMax);
                }
            }
        }

        // Puntos aleatorios alrededor de los jugadores.
        for (int i = 0; i < randomCandidates; i++)
        {
            PlayerIdentity player = players[Random.Range(0, players.Count)];
            Transform view = player.GetComponentInChildren<Camera>().transform;

            float yaw = Random.value < behindBias
                ? Random.Range(100f, 260f)
                : Random.Range(0f, 360f);

            Vector3 forward = Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized;
            Vector3 direction = Quaternion.Euler(0f, yaw, 0f) * forward;
            float distance = Random.Range(minSpawnDistanceFromPlayers, farMax);

            TryAddCandidate(
                player.transform.position + direction * distance,
                $"NavMesh cerca de P{player.PlayerId}",
                players, cameras, bands, farMax
            );
        }

        int band = PickBand();

        for (int i = 0; i < 3; i++)
        {
            List<(Vector3 position, string origin)> list = bands[(band + i) % 3];

            if (list.Count == 0)
            {
                continue;
            }

            (Vector3 position, string origin) chosen = list[Random.Range(0, list.Count)];

            position = chosen.position;
            origin = $"{chosen.origin} [{(new[] { "cerca", "media", "lejos" })[(band + i) % 3]}]";

            NearestPlayerDistance(position, out PlayerIdentity nearest);
            Vector3 look = nearest.transform.position - position;
            look.y = 0f;
            rotation = look.sqrMagnitude > 0.01f ? Quaternion.LookRotation(look) : Quaternion.identity;

            return true;
        }

        return false;
    }


    private void TryAddCandidate(
        Vector3 candidate,
        string origin,
        List<PlayerIdentity> players,
        List<Camera> cameras,
        List<(Vector3, string)>[] bands,
        float farMax
    )
    {
        if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            return;
        }

        Vector3 position = hit.position;
        float distance = NearestPlayerDistance(position, out PlayerIdentity nearest);

        if (nearest == null || distance < minSpawnDistanceFromPlayers || distance > farMax)
        {
            return;
        }

        // Mismo piso que el jugador (nada de plataformas o techos).
        if (Mathf.Abs(position.y - nearest.transform.position.y) > 2.5f)
        {
            return;
        }

        // Alcanzable caminando desde el jugador = dentro del escenario jugable.
        if (!NavMesh.SamplePosition(nearest.transform.position, out NavMeshHit playerHit, 2f, NavMesh.AllAreas))
        {
            return;
        }

        NavMeshPath path = new NavMeshPath();

        if (!NavMesh.CalculatePath(position, playerHit.position, NavMesh.AllAreas, path) ||
            path.status != NavMeshPathStatus.PathComplete)
        {
            return;
        }

        // Volumen libre: nada sólido donde iría su cuerpo.
        if (Physics.CheckCapsule(
                position + Vector3.up * 0.6f,
                position + Vector3.up * 2.6f,
                0.35f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
        {
            return;
        }

        // Fuera de la vista de las 4 cámaras.
        Bounds body = new Bounds(position + Vector3.up * 1.5f, new Vector3(0.9f, 3f, 0.9f));

        if (ClownVisibility.IsObservedByAny(cameras, body, unseenCheckDistance))
        {
            return;
        }

        int band = distance <= nearBandMax ? 0 : distance <= midBandMax ? 1 : 2;
        bands[band].Add((position, origin));
    }


    private int PickBand()
    {
        float total = bandWeights.x + bandWeights.y + bandWeights.z;
        float roll = Random.value * Mathf.Max(0.0001f, total);

        if (roll < bandWeights.x) return 0;
        if (roll < bandWeights.x + bandWeights.y) return 1;
        return 2;
    }


    private static float NearestPlayerDistance(Vector3 position, out PlayerIdentity nearest)
    {
        nearest = null;
        float best = float.MaxValue;

        foreach (PlayerIdentity player in ActivePlayers())
        {
            float distance = Vector3.Distance(position, player.transform.position);

            if (distance < best)
            {
                best = distance;
                nearest = player;
            }
        }

        return best;
    }


    // =========================================================
    // DESAPARECER
    // =========================================================

    // Un jugador lo vio de verdad: desaparece ya (y empieza la pausa hasta la próxima aparición).
    public void OnClownSeen(ClownAI clown, PlayerIdentity player)
    {
        if (clown == null || currentClown != clown.gameObject)
        {
            return;
        }

        // La primera aparición tiene su propio guion (ClownFirstAppearance).
        ClownFirstAppearance first = clown.GetComponent<ClownFirstAppearance>();

        if (first != null && first.IsRunning)
        {
            return;
        }

        Vanish($"lo vio P{(player != null ? player.PlayerId : 0)}");
    }


    public void OnClownCaptured(ClownAI clown)
    {
        if (clown == null || currentClown != clown.gameObject)
        {
            return;
        }

        StartCoroutine(VanishAfter(clown.gameObject, vanishAfterCapture, "capturó a su objetivo"));
    }


    private IEnumerator VanishAfter(GameObject clown, float delay, string reason)
    {
        yield return new WaitForSeconds(delay);

        if (currentClown == clown)
        {
            Vanish(reason);
        }
    }


    public void Vanish(string reason)
    {
        if (currentClown == null)
        {
            return;
        }

        DebugLog(
            $"🤡 Desaparece: {reason}"
        );

        if (clownEncounterManager != null && clownEncounterManager.IsEncounterActive)
        {
            clownEncounterManager.FinishEncounter(true);
        }
        else
        {
            DespawnClown();
        }
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

        ClownAI clownAI = currentClown.GetComponent<ClownAI>();

        if (clownAI != null)
        {
            clownAI.MarkVanishing();
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