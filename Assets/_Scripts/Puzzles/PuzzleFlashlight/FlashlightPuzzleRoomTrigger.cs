using UnityEngine;


[RequireComponent(typeof(Collider))]
public class FlashlightPuzzleRoomTrigger : MonoBehaviour
{

    // =========================================================
    // PUZZLE GENERAL
    // =========================================================

    [Header("Puzzle General")]
    [SerializeField] private PuzzleController puzzleController;


    // =========================================================
    // PUZZLE ESPECÍFICO
    // =========================================================

    [Header("Specific Puzzle")]
    [SerializeField] private SkrytyeSlovaController skrytyeSlovaController;

    [SerializeField] private bool activateSpecificPuzzle = true;


    // =========================================================
    // FLASHLIGHTS
    // =========================================================

    [Header("Flashlights")]
    [SerializeField] private FlashlightController[] flashlights =
        new FlashlightController[4];


    // =========================================================
    // CONFIGURACIÓN
    // =========================================================

    [Header("Activation")]
    [SerializeField] private int requiredPlayers = 4;

    [SerializeField] private bool activateOnlyOnce = true;

    [SerializeField] private bool disableTriggerAfterActivation = false;


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;


    // =========================================================
    // ESTADO
    // =========================================================

    private bool[] playersPassed =
        new bool[5];

    private bool puzzleActivated =
        false;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {

        ResetPassedPlayers();


        // -------------------------------------------------
        // Comprobar Collider
        // -------------------------------------------------

        Collider triggerCollider =
            GetComponent<Collider>();


        if (triggerCollider == null)
        {
            Debug.LogError(
                $"🔦 [FLASHLIGHT PUZZLE] " +
                $"El objeto {gameObject.name} no tiene Collider."
            );

            return;
        }


        if (!triggerCollider.isTrigger)
        {
            Debug.LogError(
                $"🔦 [FLASHLIGHT PUZZLE] ERROR: " +
                $"{gameObject.name} tiene Collider pero " +
                $"IS TRIGGER está desactivado."
            );
        }
        else
        {
            DebugLog(
                $"🔦 [FLASHLIGHT PUZZLE] Trigger preparado correctamente -> " +
                $"{gameObject.name}"
            );
        }


        // -------------------------------------------------
        // Validar cantidad de jugadores
        // -------------------------------------------------

        requiredPlayers =
            Mathf.Clamp(
                requiredPlayers,
                1,
                4
            );


        DebugLog(
            $"🔦 [FLASHLIGHT PUZZLE] " +
            $"Jugadores requeridos: {requiredPlayers}"
        );


        // -------------------------------------------------
        // Referencias principales
        // -------------------------------------------------

        if (puzzleController == null)
        {
            Debug.LogWarning(
                $"🔦 [FLASHLIGHT PUZZLE] " +
                $"No hay PuzzleController asignado."
            );
        }
        else
        {
            DebugLog(
                $"🔦 [FLASHLIGHT PUZZLE] " +
                $"PuzzleController asignado -> " +
                $"{puzzleController.gameObject.name}"
            );
        }


        if (
            activateSpecificPuzzle &&
            skrytyeSlovaController == null
        )
        {
            Debug.LogWarning(
                $"🔦 [FLASHLIGHT PUZZLE] " +
                $"Activate Specific Puzzle está activo, " +
                $"pero SkrytyeSlovaController no está asignado."
            );
        }
        else if (
            skrytyeSlovaController != null
        )
        {
            DebugLog(
                $"🔦 [FLASHLIGHT PUZZLE] " +
                $"SkrytyeSlovaController asignado -> " +
                $"{skrytyeSlovaController.gameObject.name}"
            );
        }
    }


    // =========================================================
    // TRIGGER ENTER
    // =========================================================

    private void OnTriggerEnter(Collider other)
    {

        DebugLog(
            $"🔦 [FLASHLIGHT PUZZLE] " +
            $"COLLIDER ENTRÓ AL TRIGGER -> " +
            $"Objeto: {other.name} | " +
            $"Root: {other.transform.root.name}"
        );


        // -------------------------------------------------
        // No volver a activar si ya terminó
        // -------------------------------------------------

        if (
            activateOnlyOnce &&
            puzzleActivated
        )
        {
            DebugLog(
                "🔦 [FLASHLIGHT PUZZLE] " +
                "El puzzle ya fue activado. Ignorando entrada."
            );

            return;
        }


        // -------------------------------------------------
        // Buscar PlayerIdentity
        // -------------------------------------------------

        PlayerIdentity identity =
            other.GetComponentInParent<PlayerIdentity>();


        // -------------------------------------------------
        // Segundo intento
        // -------------------------------------------------

        if (identity == null)
        {
            identity =
                other.GetComponent<PlayerIdentity>();
        }


        // -------------------------------------------------
        // Tercer intento:
        // mirar en el root del objeto que entró.
        // -------------------------------------------------

        if (identity == null)
        {
            identity =
                other.transform.root
                    .GetComponent<PlayerIdentity>();
        }


        // -------------------------------------------------
        // Si no encontramos identidad
        // -------------------------------------------------

        if (identity == null)
        {
            Debug.LogWarning(
                $"🔦 [FLASHLIGHT PUZZLE] " +
                $"El objeto {other.name} entró al trigger, " +
                $"pero NO se encontró PlayerIdentity."
            );

            return;
        }


        // -------------------------------------------------
        // Obtener Player ID
        // -------------------------------------------------

        int playerId =
            identity.PlayerId;


        DebugLog(
            $"🔦 [FLASHLIGHT PUZZLE] " +
            $"PlayerIdentity detectado -> PLAYER {playerId}"
        );


        // -------------------------------------------------
        // Validar Player ID
        // -------------------------------------------------

        if (
            playerId < 1 ||
            playerId > 4
        )
        {
            Debug.LogWarning(
                $"🔦 [FLASHLIGHT PUZZLE] " +
                $"Player ID inválido: {playerId}"
            );

            return;
        }


        // -------------------------------------------------
        // Evitar contar dos veces
        // -------------------------------------------------

        if (
            playersPassed[playerId]
        )
        {
            DebugLog(
                $"🔦 [FLASHLIGHT PUZZLE] " +
                $"PLAYER {playerId} ya había pasado."
            );

            return;
        }


        // -------------------------------------------------
        // Registrar jugador
        // -------------------------------------------------

        playersPassed[playerId] =
            true;


        int passedCount =
            GetPlayersPassedCount();


        Debug.Log(
            $"🔦 [FLASHLIGHT PUZZLE] " +
            $"PLAYER {playerId} PASÓ EL TRIGGER -> " +
            $"{passedCount}/{requiredPlayers}"
        );


        // -------------------------------------------------
        // Comprobar si ya pasaron suficientes jugadores
        // -------------------------------------------------

        if (
            passedCount >= requiredPlayers
        )
        {
            ActivatePuzzle();
        }
    }


    // =========================================================
    // ACTIVAR PUZZLE
    // =========================================================

    private void ActivatePuzzle()
    {

        if (puzzleActivated)
        {
            return;
        }


        puzzleActivated =
            true;


        Debug.Log(
            "🔦🔦🔦 [FLASHLIGHT PUZZLE] " +
            "REQUISITO DE JUGADORES CUMPLIDO -> " +
            "ACTIVANDO PUZZLE 🔦🔦🔦"
        );


        // =====================================================
        // PUZZLE CONTROLLER GENERAL
        // =====================================================

        if (puzzleController != null)
        {

            DebugLog(
                $"🔦 [FLASHLIGHT PUZZLE] " +
                $"Estado actual PuzzleController: " +
                $"{puzzleController.CurrentState}"
            );


            puzzleController.ActivatePuzzle();


            DebugLog(
                $"🔦 [FLASHLIGHT PUZZLE] " +
                $"Estado después de ActivatePuzzle(): " +
                $"{puzzleController.CurrentState}"
            );
        }
        else
        {
            Debug.LogError(
                "🔦 [FLASHLIGHT PUZZLE] " +
                "Falta asignar PuzzleController."
            );
        }


        // =====================================================
        // PUZZLE ESPECÍFICO: SKRYTYE SLOVA
        // =====================================================

        if (
            activateSpecificPuzzle &&
            skrytyeSlovaController != null
        )
        {

            Debug.Log(
                "🔤🔦 [FLASHLIGHT PUZZLE] " +
                "Activando SkrytyeSlovaController..."
            );


            skrytyeSlovaController.ActivatePuzzle();


            Debug.Log(
                "🔤🔦 [FLASHLIGHT PUZZLE] " +
                "SkrytyeSlovaController ACTIVADO."
            );
        }


        // =====================================================
        // LINTERNA
        // =====================================================

        ActivateAllFlashlights();


        // =====================================================
        // DESACTIVAR TRIGGER
        // =====================================================

        if (
            disableTriggerAfterActivation
        )
        {

            Collider triggerCollider =
                GetComponent<Collider>();


            if (
                triggerCollider != null
            )
            {
                triggerCollider.enabled =
                    false;


                DebugLog(
                    "🔦 [FLASHLIGHT PUZZLE] " +
                    "Trigger desactivado después de activar el puzzle."
                );
            }
        }
    }


    // =========================================================
    // ACTIVAR TODAS LAS LINTERNAS
    // =========================================================

    private void ActivateAllFlashlights()
    {

        if (
            flashlights == null ||
            flashlights.Length == 0
        )
        {
            Debug.LogError(
                "🔦 [FLASHLIGHT PUZZLE] " +
                "No hay FlashlightControllers configurados."
            );

            return;
        }


        int activatedCount =
            0;


        for (
            int i = 0;
            i < flashlights.Length;
            i++
        )
        {

            FlashlightController flashlight =
                flashlights[i];


            if (
                flashlight == null
            )
            {
                Debug.LogWarning(
                    $"🔦 [FLASHLIGHT PUZZLE] " +
                    $"Slot {i + 1} de FlashlightController vacío."
                );

                continue;
            }


            flashlight.ActivateFlashlight();


            activatedCount++;


            Debug.Log(
                $"🔦 [FLASHLIGHT PUZZLE] " +
                $"Linterna activada -> " +
                $"Player {flashlight.GetPlayerId()}"
            );
        }


        Debug.Log(
            $"🔦 [FLASHLIGHT PUZZLE] " +
            $"Linternas activadas: " +
            $"{activatedCount}/{flashlights.Length}"
        );
    }


    // =========================================================
    // CONTAR JUGADORES
    // =========================================================

    private int GetPlayersPassedCount()
    {

        int count =
            0;


        for (
            int i = 1;
            i <= 4;
            i++
        )
        {

            if (
                playersPassed[i]
            )
            {
                count++;
            }
        }


        return count;
    }


    // =========================================================
    // RESET PARA PRUEBAS
    // =========================================================

    [ContextMenu("Reset Flashlight Puzzle Trigger")]
    public void ResetTrigger()
    {

        puzzleActivated =
            false;


        ResetPassedPlayers();


        Debug.Log(
            "🔦 [FLASHLIGHT PUZZLE] " +
            "Trigger reiniciado."
        );
    }


    // =========================================================
    // RESET JUGADORES
    // =========================================================

    private void ResetPassedPlayers()
    {

        if (
            playersPassed == null ||
            playersPassed.Length != 5
        )
        {
            playersPassed =
                new bool[5];
        }


        for (
            int i = 0;
            i < playersPassed.Length;
            i++
        )
        {
            playersPassed[i] =
                false;
        }
    }


    // =========================================================
    // DEBUG
    // =========================================================

    private void DebugLog(
        string message
    )
    {
        if (!debugLogs)
        {
            return;
        }


        Debug.Log(message);
    }
}