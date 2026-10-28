using UnityEngine;

public class BalloonPuzzleController : MonoBehaviour
{
    [Header("Puzzle Controller")]
    [Tooltip("PuzzleController asociado a este puzzle.")]
    [SerializeField]
    private PuzzleController puzzleController;

    [Header("Bases de los Jugadores")]
    [Tooltip("Asigna las cuatro bases en orden: Player 1, Player 2, Player 3 y Player 4.")]
    [SerializeField]
    private BalloonBase[] balloonBases;

    [Header("Controlador Central de Globos")]
    [Tooltip("Arrastra aquí el objeto que contiene el único BalloonController.")]
    [SerializeField]
    private BalloonController balloonController;

    [Header("Configuración del Puzzle")]
    [Tooltip("Tiempo durante el cual los cuatro globos deben permanecer al 100 %.")]
    [SerializeField]
    private float requiredHoldTime = 5f;

    [Tooltip("Muestra información de depuración en la consola.")]
    [SerializeField]
    private bool enableDebugLogs = true;

    [Tooltip("Muestra información de depuración cada cierto tiempo.")]
    [SerializeField]
    private bool enablePeriodicDebug = true;

    [Tooltip("Intervalo entre mensajes periódicos de depuración.")]
    [SerializeField]
    private float periodicDebugInterval = 2f;

    [Header("Estado del Puzzle")]
    [SerializeField]
    private bool puzzleActivated = false;

    [SerializeField]
    private bool allPlayersOnBases = false;

    [SerializeField]
    private float currentHoldTime = 0f;

    [Header("Registro de Errores")]
    [SerializeField]
    private int failedAttempts = 0;

    [SerializeField]
    private float totalTimeBelowFullInflation = 0f;

    [Header("Estado de Depuración")]
    [SerializeField]
    private bool lastInflationPermission = false;

    [SerializeField]
    private float debugTimer = 0f;

    private bool wasAllBalloonsFull = false;

    // =====================================================
    // PROPIEDADES PÚBLICAS
    // =====================================================

    public bool PuzzleActivated => puzzleActivated;

    public bool AllPlayersOnBases => allPlayersOnBases;

    public float CurrentHoldTime => currentHoldTime;

    public int FailedAttempts => failedAttempts;

    public float TotalTimeBelowFullInflation =>
        totalTimeBelowFullInflation;

    // =====================================================
    // AWAKE
    // =====================================================

    private void Awake()
    {
        DebugLog("========== BALLOON PUZZLE AWAKE ==========");

        if (puzzleController == null)
        {
            puzzleController = GetComponent<PuzzleController>();

            if (puzzleController != null)
            {
                DebugLog(
                    "PuzzleController encontrado automáticamente " +
                    "en el mismo objeto."
                );
            }
        }

        if (puzzleController == null)
        {
            Debug.LogError(
                $"{gameObject.name}: No se encontró un PuzzleController. " +
                "Asigna uno manualmente en el Inspector."
            );
        }
        else
        {
            DebugLog(
                $"PuzzleController asignado correctamente. " +
                $"Estado actual: {puzzleController.CurrentState}"
            );
        }

        if (balloonBases == null)
        {
            Debug.LogError(
                $"{gameObject.name}: El array balloonBases es null."
            );
        }
        else if (balloonBases.Length != 4)
        {
            Debug.LogError(
                $"{gameObject.name}: Debes asignar exactamente 4 bases. " +
                $"Actualmente hay {balloonBases.Length}."
            );
        }
        else
        {
            DebugLog("Array de bases configurado con 4 elementos.");

            for (int i = 0; i < balloonBases.Length; i++)
            {
                if (balloonBases[i] == null)
                {
                    Debug.LogError(
                        $"Base en el índice {i} está vacía. " +
                        $"Debería corresponder al Player {i + 1}."
                    );
                }
                else
                {
                    DebugLog(
                        $"Base índice {i}: {balloonBases[i].name} | " +
                        $"Jugador requerido: P{balloonBases[i].RequiredPlayerId}"
                    );
                }
            }
        }

        if (balloonController == null)
        {
            Debug.LogError(
                $"{gameObject.name}: No se asignó el BalloonController central. " +
                "Arrastra el BalloonManager al campo Balloon Controller."
            );
        }
        else
        {
            DebugLog(
                $"BalloonController asignado correctamente: " +
                $"{balloonController.gameObject.name}"
            );
        }
    }

    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        DebugLog("========== BALLOON PUZZLE START ==========");

        // Al comenzar, el inflado siempre está bloqueado.
        DisableBalloonInflation();

        // Revisar el estado inicial de las bases.
        CheckPlayersOnBases();

        DebugLog(
            $"Estado inicial: puzzleActivated = {puzzleActivated} | " +
            $"allPlayersOnBases = {allPlayersOnBases}"
        );

        if (puzzleController != null)
        {
            DebugLog(
                $"Estado inicial del PuzzleController: " +
                $"{puzzleController.CurrentState}"
            );
        }

        DebugLog(
            "El puzzle solo se activará cuando los cuatro jugadores " +
            "estén correctamente colocados en sus bases."
        );
    }

    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        // Comprobar siempre las bases.
        CheckPlayersOnBases();

        // Mostrar información periódica.
        if (enablePeriodicDebug)
        {
            debugTimer += Time.deltaTime;

            if (debugTimer >= periodicDebugInterval)
            {
                debugTimer = 0f;
                PrintPeriodicDebug();
            }
        }

        // Si el puzzle aún no está activado,
        // solo puede activarse cuando todos estén en sus bases.
        if (!puzzleActivated)
        {
            if (allPlayersOnBases)
            {
                DebugLog(
                    "Todos los jugadores están en sus bases. " +
                    "Intentando activar el puzzle."
                );

                ActivateBalloonPuzzle();
            }
            else
            {
                DisableBalloonInflation();
            }

            return;
        }

        // Verificar el estado del PuzzleController.
        if (puzzleController == null)
        {
            Debug.LogError(
                "No se puede continuar: puzzleController es null."
            );

            DisableBalloonInflation();
            return;
        }

        if (puzzleController.CurrentState != PuzzleState.Active)
        {
            DebugLog(
                $"El PuzzleController no está activo. " +
                $"Estado actual: {puzzleController.CurrentState}. " +
                "Se bloquea el inflado."
            );

            DisableBalloonInflation();
            return;
        }

        // Actualizar el permiso de inflado.
        UpdateBalloonInflationPermission();

        // Revisar la finalización del puzzle.
        CheckBalloonCompletion();

        // Registrar el tiempo bajo el 100 %.
        UpdateErrorTime();
    }

    // =====================================================
    // ACTIVAR PUZZLE
    // =====================================================

    public void ActivateBalloonPuzzle()
    {
        DebugLog("========== INTENTANDO ACTIVAR PUZZLE ==========");

        if (puzzleActivated)
        {
            DebugLog(
                "El puzzle ya estaba activado. No se vuelve a activar."
            );

            return;
        }

        if (!allPlayersOnBases)
        {
            DebugLog(
                "No se puede activar el puzzle porque no todos " +
                "los jugadores están en sus bases."
            );

            DisableBalloonInflation();
            return;
        }

        if (puzzleController == null)
        {
            Debug.LogError(
                "No se puede activar el puzzle porque falta " +
                "la referencia al PuzzleController."
            );

            DisableBalloonInflation();
            return;
        }

        DebugLog(
            $"Estado del PuzzleController antes de activar: " +
            $"{puzzleController.CurrentState}"
        );

        // Si está inactivo, intentamos activarlo.
        if (puzzleController.CurrentState == PuzzleState.Inactive)
        {
            DebugLog(
                "El PuzzleController está Inactive. " +
                "Llamando a ActivatePuzzle()."
            );

            puzzleController.ActivatePuzzle();

            DebugLog(
                $"Estado del PuzzleController después de ActivatePuzzle(): " +
                $"{puzzleController.CurrentState}"
            );
        }

        // Confirmar que el PuzzleController está activo.
        if (puzzleController.CurrentState != PuzzleState.Active)
        {
            Debug.LogWarning(
                "El PuzzleController no quedó en estado Active. " +
                $"Estado actual: {puzzleController.CurrentState}. " +
                "El inflado permanecerá bloqueado."
            );

            DisableBalloonInflation();
            return;
        }

        puzzleActivated = true;
        currentHoldTime = 0f;
        failedAttempts = 0;
        totalTimeBelowFullInflation = 0f;
        wasAllBalloonsFull = false;

        UpdateBalloonInflationPermission();

        Debug.Log(
            "🎈 PUZZLE DE GLOBOS ACTIVADO CORRECTAMENTE."
        );
    }

    // =====================================================
    // DESACTIVAR PUZZLE
    // =====================================================

    public void DeactivateBalloonPuzzle()
    {
        DebugLog("========== DESACTIVANDO PUZZLE ==========");

        puzzleActivated = false;
        currentHoldTime = 0f;
        wasAllBalloonsFull = false;

        DisableBalloonInflation();

        DebugLog(
            "Puzzle de globos desactivado. " +
            "El inflado está bloqueado."
        );
    }

    // =====================================================
    // ENTRADA A UNA BASE
    // =====================================================

    public void OnPlayerEnteredBase(
        BalloonBase baseObject,
        PlayerIdentity player
    )
    {
        if (baseObject == null)
        {
            Debug.LogWarning(
                "OnPlayerEnteredBase recibió una base null."
            );

            return;
        }

        if (player == null)
        {
            Debug.LogWarning(
                $"La base {baseObject.name} notificó una entrada, " +
                "pero el PlayerIdentity es null."
            );

            return;
        }

        Debug.Log(
            $"🎈 EVENTO DE ENTRADA: Player {player.PlayerId} " +
            $"entró en {baseObject.name}. " +
            $"Base requiere Player {baseObject.RequiredPlayerId}."
        );

        CheckPlayersOnBases();

        DebugLog(
            $"Después de la entrada: " +
            $"allPlayersOnBases = {allPlayersOnBases}"
        );

        if (allPlayersOnBases && !puzzleActivated)
        {
            DebugLog(
                "Las cuatro bases están ocupadas correctamente. " +
                "Se iniciará el puzzle."
            );

            ActivateBalloonPuzzle();
        }
        else if (!allPlayersOnBases)
        {
            DebugLog(
                "Todavía no están todos los jugadores en sus bases."
            );
        }
    }

    // =====================================================
    // SALIDA DE UNA BASE
    // =====================================================

    public void OnPlayerExitedBase(
        BalloonBase baseObject,
        PlayerIdentity player
    )
    {
        if (baseObject == null)
        {
            Debug.LogWarning(
                "OnPlayerExitedBase recibió una base null."
            );

            return;
        }

        if (player == null)
        {
            Debug.LogWarning(
                $"La base {baseObject.name} notificó una salida, " +
                "pero el PlayerIdentity es null."
            );

            return;
        }

        Debug.Log(
            $"⬅️ EVENTO DE SALIDA: Player {player.PlayerId} " +
            $"salió de {baseObject.name}."
        );

        currentHoldTime = 0f;
        wasAllBalloonsFull = false;

        CheckPlayersOnBases();

        if (!allPlayersOnBases)
        {
            DebugLog(
                "Falta al menos un jugador en su base. " +
                "El inflado será bloqueado."
            );

            DisableBalloonInflation();
        }
    }

    // =====================================================
    // COMPROBAR BASES
    // =====================================================

    private void CheckPlayersOnBases()
    {
        if (balloonBases == null)
        {
            allPlayersOnBases = false;
            return;
        }

        if (balloonBases.Length != 4)
        {
            allPlayersOnBases = false;
            return;
        }

        bool previousState = allPlayersOnBases;

        allPlayersOnBases = true;

        for (int i = 0; i < balloonBases.Length; i++)
        {
            BalloonBase currentBase = balloonBases[i];

            if (currentBase == null)
            {
                allPlayersOnBases = false;

                DebugLog(
                    $"Base índice {i} es null. " +
                    "No se puede validar el puzzle."
                );

                continue;
            }

            bool correctPlayerDetected =
                currentBase.IsCorrectPlayerOnBase();

            if (!correctPlayerDetected)
            {
                allPlayersOnBases = false;
            }

            // Mostrar detalles de cada base solo si se solicita.
            if (enableDebugLogs)
            {
                string occupantInfo = "Ninguno";

                if (currentBase.OccupyingPlayer != null)
                {
                    occupantInfo =
                        $"Player {currentBase.OccupyingPlayer.PlayerId}";
                }

                Debug.Log(
                    $"[BASE CHECK] {currentBase.name} | " +
                    $"Requiere: P{currentBase.RequiredPlayerId} | " +
                    $"Ocupante: {occupantInfo} | " +
                    $"Ocupada: {currentBase.IsOccupied} | " +
                    $"Correcta: {correctPlayerDetected}"
                );
            }
        }

        if (previousState != allPlayersOnBases)
        {
            Debug.Log(
                $"🔄 CAMBIO DE ESTADO DE BASES: " +
                $"allPlayersOnBases = {allPlayersOnBases}"
            );
        }
    }

    // =====================================================
    // PERMITIR O BLOQUEAR INFLADO
    // =====================================================

    private void UpdateBalloonInflationPermission()
    {
        if (balloonController == null)
        {
            Debug.LogError(
                "No se puede actualizar el permiso de inflado: " +
                "balloonController es null."
            );

            return;
        }

        bool canInflate =
            puzzleActivated &&
            allPlayersOnBases &&
            puzzleController != null &&
            puzzleController.CurrentState == PuzzleState.Active;

        if (canInflate != lastInflationPermission)
        {
            Debug.Log(
                $"🎈 CAMBIO DE PERMISO DE INFLADO: " +
                $"{lastInflationPermission} → {canInflate}"
            );

            Debug.Log(
                $"Detalles: puzzleActivated={puzzleActivated}, " +
                $"allPlayersOnBases={allPlayersOnBases}, " +
                $"puzzleControllerActive=" +
                $"{(puzzleController != null && puzzleController.CurrentState == PuzzleState.Active)}"
            );

            lastInflationPermission = canInflate;
        }

        balloonController.SetInflationEnabled(canInflate);
    }

    // =====================================================
    // COMPROBAR COMPLETADO
    // =====================================================

    private void CheckBalloonCompletion()
    {
        if (!allPlayersOnBases)
        {
            if (currentHoldTime > 0f)
            {
                DebugLog(
                    "El temporizador se reinició porque un jugador " +
                    "ya no está en su base."
                );
            }

            currentHoldTime = 0f;
            wasAllBalloonsFull = false;
            return;
        }

        if (balloonController == null)
        {
            return;
        }

        bool allBalloonsFull =
            balloonController.AreAllBalloonsFullyInflated();

        if (allBalloonsFull)
        {
            if (!wasAllBalloonsFull)
            {
                Debug.Log(
                    "🎈 Los cuatro globos llegaron al 100 %. " +
                    "Comenzando temporizador de finalización."
                );

                wasAllBalloonsFull = true;
            }

            currentHoldTime += Time.deltaTime;

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"⏱️ Tiempo al 100 %: " +
                    $"{currentHoldTime:F2}/{requiredHoldTime:F2} segundos."
                );
            }

            if (currentHoldTime >= requiredHoldTime)
            {
                CompleteBalloonPuzzle();
            }
        }
        else
        {
            if (wasAllBalloonsFull)
            {
                failedAttempts++;

                Debug.Log(
                    $"⚠️ Un globo bajó del 100 %. " +
                    $"Intento perdido: {failedAttempts}"
                );
            }

            currentHoldTime = 0f;
            wasAllBalloonsFull = false;
        }
    }

    // =====================================================
    // REGISTRAR TIEMPO BAJO EL 100 %
    // =====================================================

    private void UpdateErrorTime()
    {
        if (!allPlayersOnBases)
        {
            return;
        }

        if (balloonController == null)
        {
            return;
        }

        bool allBalloonsFull =
            balloonController.AreAllBalloonsFullyInflated();

        if (!allBalloonsFull)
        {
            totalTimeBelowFullInflation += Time.deltaTime;
        }
    }

    // =====================================================
    // COMPLETAR PUZZLE
    // =====================================================

    private void CompleteBalloonPuzzle()
    {
        DebugLog("========== COMPLETANDO PUZZLE ==========");

        if (puzzleController == null)
        {
            Debug.LogError(
                "No se puede completar el puzzle: " +
                "puzzleController es null."
            );

            return;
        }

        if (puzzleController.CurrentState != PuzzleState.Active)
        {
            Debug.LogWarning(
                "No se puede completar el puzzle porque el " +
                $"PuzzleController no está Active. " +
                $"Estado actual: {puzzleController.CurrentState}"
            );

            return;
        }

        puzzleController.CompletePuzzle();

        puzzleActivated = false;

        DisableBalloonInflation();

        Debug.Log(
            "🎈🎉 ¡PUZZLE DE GLOBOS COMPLETADO!"
        );
    }

    // =====================================================
    // DESACTIVAR INFLADO
    // =====================================================

    private void DisableBalloonInflation()
    {
        if (balloonController == null)
        {
            return;
        }

        balloonController.SetInflationEnabled(false);

        if (lastInflationPermission)
        {
            DebugLog(
                "Inflado bloqueado."
            );
        }

        lastInflationPermission = false;
    }

    // =====================================================
    // REINICIAR PUZZLE
    // =====================================================

    public void ResetBalloonPuzzle()
    {
        DebugLog("========== REINICIANDO PUZZLE ==========");

        currentHoldTime = 0f;
        failedAttempts = 0;
        totalTimeBelowFullInflation = 0f;
        wasAllBalloonsFull = false;
        allPlayersOnBases = false;
        puzzleActivated = false;
        debugTimer = 0f;
        lastInflationPermission = false;

        if (balloonBases != null)
        {
            foreach (BalloonBase balloonBase in balloonBases)
            {
                if (balloonBase != null)
                {
                    balloonBase.ResetBase();
                }
            }
        }

        if (balloonController != null)
        {
            balloonController.ResetBalloons();
            balloonController.SetInflationEnabled(false);

            DebugLog(
                "Globos reiniciados y permiso de inflado desactivado."
            );
        }

        if (puzzleController != null)
        {
            puzzleController.ResetPuzzle();

            DebugLog(
                $"PuzzleController reiniciado. " +
                $"Estado actual: {puzzleController.CurrentState}"
            );
        }

        Debug.Log(
            "🔄 Puzzle de globos reiniciado completamente."
        );
    }

    // =====================================================
    // DEBUG PERIÓDICO
    // =====================================================

    private void PrintPeriodicDebug()
    {
        string puzzleState = "NULL";

        if (puzzleController != null)
        {
            puzzleState =
                puzzleController.CurrentState.ToString();
        }

        Debug.Log(
            $"[BALLOON DEBUG] " +
            $"PuzzleActivado={puzzleActivated} | " +
            $"TodosEnBases={allPlayersOnBases} | " +
            $"EstadoPuzzle={puzzleState} | " +
            $"PermisoInflado={lastInflationPermission} | " +
            $"TiempoSostenido={currentHoldTime:F2}s"
        );
    }

    // =====================================================
    // DEBUG GENERAL
    // =====================================================

    private void DebugLog(string message)
    {
        if (!enableDebugLogs)
        {
            return;
        }

        Debug.Log($"[BalloonPuzzleController] {message}");
    }
}