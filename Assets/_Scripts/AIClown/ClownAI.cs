using UnityEngine;
using UnityEngine.AI;

// Comportamiento del payaso mientras está en escena (lo crea y lo quita ClownSpawner).
//
// Modelo animado (AnimationsClown/Payaso, Humanoid): Idle / Walk / Run en un blend tree según la
// velocidad REAL del NavMeshAgent (parámetro "Speed"). El agente mueve al personaje; el Animator
// nunca (Apply Root Motion = false), así no se pelean y no hay deriva.
// Si un jugador lo ve de verdad y de cerca, desaparece (NotifySeenBy → spawner). Visto de lejos
// sigue caminando (freezeWhileWatchedFromAfar = false); con la opción activa se queda quieto.
//
// Sin objetivo: ronda por el NavMesh (destinos aleatorios, pausas, se gira hacia los jugadores).
// Con objetivo (ClownEncounterManager eligió a quien no miró): lo acecha y, si llega sin ser
// visto, lo captura (PlayerStateController.Capture → rescate con la palanca).
public class ClownAI : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private NavMeshAgent navMeshAgent;

    [Tooltip("Distancia a la que el payaso considera que llegó al jugador.")]
    [SerializeField] private float captureDistance = 1.5f;

    [Header("Rondar")]
    [Tooltip("Radio (m) en el que elige destinos al rondar.")]
    [SerializeField] private float roamRadius = 12f;

    [Tooltip("Nunca elige un destino de ronda a menos de esto de un jugador.")]
    [SerializeField] private float roamMinPlayerDistance = 5f;

    [SerializeField] private float roamSpeed = 2.2f;

    [Tooltip("Pausa (s) al llegar a cada destino. Aleatoria en el rango.")]
    [SerializeField] private Vector2 pauseRange = new Vector2(1.5f, 5f);

    [Tooltip("Grados por segundo al girarse hacia el jugador más cercano mientras espera.")]
    [SerializeField] private float turnSpeed = 180f;

    [Header("Acecho")]
    [SerializeField] private float chaseSpeed = 3.4f;

    [Header("Captura")]
    [Tooltip("Si está activo, alcanzar al objetivo lo deja Capturado (PlayerStateController).")]
    [SerializeField] private bool capturePlayers = true;

    [Tooltip("Nunca captura al último jugador libre (sin nadie libre no hay rescate posible).")]
    [SerializeField] private bool neverCaptureLastFreePlayer = true;

    [Header("Animación")]
    [Tooltip("Animator del modelo (hijo). Parámetro float \"Speed\" en m/s.")]
    [SerializeField] private Animator animator;

    [Tooltip("Suavizado (s) del parámetro Speed: transiciones rápidas sin saltos.")]
    [SerializeField] private float speedDampTime = 0.1f;

    [Header("Observación")]
    [Tooltip("Si está activo, se queda quieto mientras alguien lo distingue a lo lejos (más allá de la distancia en la que desaparece). Era necesario cuando el modelo no tenía animaciones.")]
    [SerializeField] private bool freezeWhileWatchedFromAfar = false;

    [Tooltip("Segundos que sigue quieto después del último reporte de que alguien lo ve.")]
    [SerializeField] private float observationMemory = 0.25f;

    [Header("Depuración")]
    [SerializeField] private bool showDebugMessages = true;

    private Transform currentTarget;
    private bool hasTarget;
    private bool hasCaptured;

    private ClownSpawner spawner;
    private Collider bodyCollider;
    private float lastObservedTime = -100f;
    private float pauseTimer;
    private bool roamingToDestination;
    private float stuckTimer;
    private float nextRepath;
    private bool warnedNoNavMesh;

    private static readonly int SpeedParameter = Animator.StringToHash("Speed");

    public static ClownAI Active { get; private set; }

    public bool HasTarget => hasTarget;
    public Transform CurrentTarget => currentTarget;
    public bool HasCaptured => hasCaptured;

    // Falso durante la primera aparición (escena preparada: se queda quieto).
    public bool RoamingEnabled { get; set; } = true;
    public bool IsVanishing { get; private set; }
    public bool IsObserved => Time.time - lastObservedTime < observationMemory;
    public bool IsMoving => navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh &&
                            !navMeshAgent.isStopped && navMeshAgent.velocity.sqrMagnitude > 0.01f;

    public Bounds VisualBounds => bodyCollider != null
        ? bodyCollider.bounds
        : new Bounds(transform.position + Vector3.up * 1.5f, new Vector3(0.9f, 3f, 0.9f));


    private void Awake()
    {
        if (navMeshAgent == null)
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
        }

        bodyCollider = GetComponent<Collider>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
    }


    private void OnEnable()
    {
        Active = this;
    }


    private void OnDisable()
    {
        if (Active == this)
            Active = null;
    }


    public void Initialize(ClownSpawner owner)
    {
        spawner = owner;
        pauseTimer = Random.Range(pauseRange.x, pauseRange.y);
    }


    private void Update()
    {
        if (IsVanishing || hasCaptured || !HasNavMesh())
        {
            return;
        }

        // Aparición ambiental: recorrido corto y lejano, sin acechar ni capturar.
        if (IsAmbient)
        {
            UpdateAmbient();
            return;
        }

        // Alguien lo distingue a lo lejos: opcionalmente se queda quieto como una figura.
        if (freezeWhileWatchedFromAfar && IsObserved)
        {
            Freeze();
            return;
        }

        if (hasTarget && currentTarget != null)
        {
            Stalk();
            return;
        }

        if (RoamingEnabled)
        {
            Roam();
        }
        else
        {
            Freeze();
            FaceNearestPlayer();
        }
    }


    // Idle / Walk / Run salen de la velocidad real: quieto = Idle, nunca camina en el sitio.
    private void LateUpdate()
    {
        if (animator == null)
        {
            return;
        }

        float speed = 0f;

        if (!IsVanishing && !hasCaptured && navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
        {
            speed = navMeshAgent.velocity.magnitude;
        }

        animator.SetFloat(SpeedParameter, speed, speedDampTime, Time.deltaTime);
    }


    // =========================================================
    // OBSERVACIÓN (ClownLookDetector)
    // =========================================================

    public void ReportObservation(bool observed)
    {
        if (observed)
        {
            lastObservedTime = Time.time;
        }
    }


    public void NotifySeenBy(PlayerIdentity player)
    {
        if (IsVanishing)
        {
            return;
        }

        if (spawner != null)
        {
            spawner.OnClownSeen(this, player);
        }
    }


    // =========================================================
    // APARICIÓN AMBIENTAL (antes de la primera puerta)
    // =========================================================

    // Recorrido: observa quieto → camina al primer punto → observa → se retira al último punto.
    // Nunca acecha ni captura (SetTarget se ignora). El spawner lo quita al terminar.
    public bool IsAmbient { get; private set; }
    public bool AmbientFinished { get; private set; }

    private Vector3[] ambientRoute;
    private int ambientIndex;
    private float ambientWait;
    private float ambientMidWatch;
    private bool ambientWalking;

    public void BeginAmbient(Vector3[] route, float firstWatch, float midWatch)
    {
        IsAmbient = true;
        AmbientFinished = false;
        RoamingEnabled = false;
        ambientRoute = route;
        ambientIndex = 0;
        ambientWait = firstWatch;
        ambientMidWatch = midWatch;
        ambientWalking = false;
        currentTarget = null;
        hasTarget = false;
    }


    private void UpdateAmbient()
    {
        if (ambientWait > 0f)
        {
            ambientWait -= Time.deltaTime;
            Freeze();
            FaceNearestPlayer();
            return;
        }

        if (ambientRoute == null || ambientIndex >= ambientRoute.Length)
        {
            AmbientFinished = true;
            Freeze();
            return;
        }

        navMeshAgent.speed = roamSpeed;
        navMeshAgent.isStopped = false;

        if (!ambientWalking)
        {
            ambientWalking = navMeshAgent.SetDestination(ambientRoute[ambientIndex]);
            stuckTimer = 0f;

            if (!ambientWalking)
                ambientIndex++;

            return;
        }

        bool arrived = !navMeshAgent.pathPending &&
                       navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance + 0.2f;

        if (navMeshAgent.desiredVelocity.sqrMagnitude > 0.04f && navMeshAgent.velocity.sqrMagnitude < 0.01f)
            stuckTimer += Time.deltaTime;
        else
            stuckTimer = 0f;

        if (arrived || stuckTimer > 2f)
        {
            ambientWalking = false;
            ambientIndex++;
            ambientWait = ambientIndex < ambientRoute.Length ? ambientMidWatch : 0f;
        }
    }


    public void MarkVanishing()
    {
        IsVanishing = true;
        Freeze();
    }


    // =========================================================
    // RONDAR
    // =========================================================

    private void Roam()
    {
        navMeshAgent.speed = roamSpeed;

        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.deltaTime;
            Freeze();
            FaceNearestPlayer();
            return;
        }

        navMeshAgent.isStopped = false;

        if (!roamingToDestination)
        {
            roamingToDestination = TryPickRoamDestination();
            stuckTimer = 0f;
            return;
        }

        if (!navMeshAgent.pathPending &&
            navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance + 0.2f)
        {
            roamingToDestination = false;
            pauseTimer = Random.Range(pauseRange.x, pauseRange.y);
            return;
        }

        // Atascado (quiere moverse y no avanza): nuevo destino.
        if (navMeshAgent.desiredVelocity.sqrMagnitude > 0.04f &&
            navMeshAgent.velocity.sqrMagnitude < 0.01f)
        {
            stuckTimer += Time.deltaTime;

            if (stuckTimer > 1.5f)
            {
                DebugLog("Atascado: cambia de destino.");
                roamingToDestination = false;
            }
        }
        else
        {
            stuckTimer = 0f;
        }
    }


    private bool TryPickRoamDestination()
    {
        Transform nearest = NearestPlayer(out _);
        NavMeshPath path = new NavMeshPath();

        for (int attempt = 0; attempt < 12; attempt++)
        {
            // La mitad de las veces ronda hacia la zona de los jugadores (presencia); si no, al azar.
            Vector3 center = nearest != null && Random.value < 0.5f
                ? Vector3.Lerp(transform.position, nearest.position, 0.5f)
                : transform.position;

            Vector2 offset = Random.insideUnitCircle * roamRadius;
            Vector3 candidate = center + new Vector3(offset.x, 0f, offset.y);

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                continue;

            if (Vector3.Distance(hit.position, transform.position) < 3f)
                continue;

            if (DistanceToNearestPlayer(hit.position) < roamMinPlayerDistance)
                continue;

            if (!navMeshAgent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete)
                continue;

            navMeshAgent.SetPath(path);
            return true;
        }

        pauseTimer = Random.Range(pauseRange.x, pauseRange.y);
        return false;
    }


    // =========================================================
    // ACECHAR / CAPTURAR
    // =========================================================

    public void SetTarget(Transform target)
    {
        if (IsAmbient)
        {
            return;
        }

        if (target == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: Se intentó asignar un objetivo NULL."
            );

            return;
        }

        currentTarget = target;
        hasTarget = true;
        hasCaptured = false;
        nextRepath = 0f;

        DebugLog(
            $"Acecha a: {currentTarget.name}"
        );
    }


    private void Stalk()
    {
        PlayerStateController state = currentTarget.GetComponent<PlayerStateController>();

        if (state != null && !state.IsAlive)
        {
            DebugLog($"{currentTarget.name} ya no está libre: deja de acecharlo.");
            ClearTarget();
            return;
        }

        navMeshAgent.speed = chaseSpeed;
        navMeshAgent.isStopped = false;

        if (Time.time >= nextRepath)
        {
            nextRepath = Time.time + 0.25f;
            navMeshAgent.SetDestination(currentTarget.position);
        }

        if (Vector3.Distance(transform.position, currentTarget.position) <= captureDistance)
        {
            CaptureTarget();
        }
    }


    private void CaptureTarget()
    {
        if (hasCaptured)
        {
            return;
        }

        hasCaptured = true;
        Freeze();

        PlayerStateController state = currentTarget.GetComponent<PlayerStateController>();
        bool captured = false;

        if (capturePlayers && state != null && state.IsAlive &&
            (!neverCaptureLastFreePlayer || CountFreePlayers() > 1))
        {
            state.Capture();
            captured = true;
        }

        DebugLog(
            captured
                ? $"CAPTURA: {currentTarget.name}"
                : $"Alcanzó a {currentTarget.name} sin capturarlo (último jugador libre o captura desactivada)."
        );

        if (spawner != null)
        {
            spawner.OnClownCaptured(this);
        }
    }


    public void ClearTarget()
    {
        currentTarget = null;
        hasTarget = false;
        hasCaptured = false;
        roamingToDestination = false;

        if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.ResetPath();
        }

        DebugLog("Objetivo eliminado.");
    }


    // =========================================================
    // UTILIDADES
    // =========================================================

    private bool HasNavMesh()
    {
        if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
        {
            return true;
        }

        if (!warnedNoNavMesh)
        {
            warnedNoNavMesh = true;
            Debug.LogWarning(
                $"{gameObject.name}: El payaso no está sobre un NavMesh (TSirkus/Payaso/Configurar payaso en GAME)."
            );
        }

        return false;
    }


    private void Freeze()
    {
        if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.velocity = Vector3.zero;
        }
    }


    private void FaceNearestPlayer()
    {
        Transform nearest = NearestPlayer(out _);

        if (nearest == null)
        {
            return;
        }

        Vector3 direction = nearest.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.LookRotation(direction),
            turnSpeed * Time.deltaTime
        );
    }


    private static Transform NearestPlayer(Vector3 position, out float distance)
    {
        Transform best = null;
        distance = float.MaxValue;

        foreach (PlayerIdentity player in ClownSpawner.ActivePlayers())
        {
            float d = Vector3.Distance(position, player.transform.position);

            if (d < distance)
            {
                distance = d;
                best = player.transform;
            }
        }

        return best;
    }


    private Transform NearestPlayer(out float distance) => NearestPlayer(transform.position, out distance);


    private static float DistanceToNearestPlayer(Vector3 position)
    {
        NearestPlayer(position, out float distance);
        return distance;
    }


    private static int CountFreePlayers()
    {
        int count = 0;

        foreach (PlayerIdentity player in ClownSpawner.ActivePlayers())
        {
            PlayerStateController state = player.GetComponent<PlayerStateController>();

            if (state == null || state.IsAlive)
                count++;
        }

        return count;
    }


    private void DebugLog(string message)
    {
        if (!showDebugMessages)
        {
            return;
        }

        Debug.Log(
            $"🤡 {gameObject.name}: {message}"
        );
    }


    [ContextMenu("Probar objetivo")]
    private void TestTarget()
    {
        Transform nearest = NearestPlayer(out _);

        if (nearest == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: No hay jugadores."
            );

            return;
        }

        SetTarget(nearest);
    }
}
