using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FlashlightPuzzleRoomTrigger : MonoBehaviour
{
    // =========================================================
    // PUZZLE
    // =========================================================

    [Header("Puzzle")]
    [SerializeField] private PuzzleController puzzleController;

    [SerializeField] private SkrytyeSlovaController skrytyeSlovaController;


    // =========================================================
    // FLASHLIGHTS
    // =========================================================

    [Header("Flashlights")]
    [SerializeField] private FlashlightController[] flashlights =
        new FlashlightController[4];

    [SerializeField] private bool autoFindFlashlights = true;


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

    private readonly bool[] playersPassed =
        new bool[5];

    private bool puzzleActivated = false;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        ResetPassedPlayers();

        ValidateTrigger();

        FindReferencesIfNecessary();
    }


    // =========================================================
    // VALIDAR TRIGGER
    // =========================================================

    private void ValidateTrigger()
    {
        Collider triggerCollider =
            GetComponent<Collider>();

        if (triggerCollider == null)
        {
            Debug.LogError(
                $"🔦 [FLASHLIGHT PUZZLE] " +
                $"No se encontró Collider en {gameObject.name}."
            );

            return;
        }

        if (!triggerCollider.isTrigger)
        {
            Debug.LogWarning(
                $"🔦 [FLASHLIGHT PUZZLE] " +
                $"{gameObject.name} NO está configurado como Trigger."
            );
        }
    }


    // =========================================================
    // BUSCAR REFERENCIAS
    // =========================================================

    private void FindReferencesIfNecessary()
    {
        // -------------------------------------------------
        // PuzzleController
        // -------------------------------------------------

        if (puzzleController == null)
        {
            puzzleController =
                GetComponent<PuzzleController>();
        }

        if (puzzleController == null)
        {
            puzzleController =
                FindObjectOfType<PuzzleController>();
        }


        // -------------------------------------------------
        // SkrytyeSlovaController
        // -------------------------------------------------

        if (skrytyeSlovaController == null)
        {
            skrytyeSlovaController =
                GetComponent<SkrytyeSlovaController>();
        }

        if (skrytyeSlovaController == null)
        {
            skrytyeSlovaController =
                FindObjectOfType<SkrytyeSlovaController>();
        }


        // -------------------------------------------------
        // Flashlights
        // -------------------------------------------------

        if (autoFindFlashlights)
        {
            FindAllFlashlights();
        }
    }


    // =========================================================
    // TRIGGER ENTER
    // =========================================================

    private void OnTriggerEnter(Collider other)
    {
        // -------------------------------------------------
        // Ya activado
        // -------------------------------------------------

        if (
            activateOnlyOnce &&
            puzzleActivated
        )
        {
            return;
        }


        // -------------------------------------------------
        // DEBUG
        // -------------------------------------------------

        DebugLog(
            $"🔦 [FLASHLIGHT PUZZLE] " +
            $"OnTriggerEnter -> {other.gameObject.name}"
        );


        // -------------------------------------------------
        // Buscar PlayerIdentity
        // -------------------------------------------------

        PlayerIdentity identity =
            other.GetComponentInParent<PlayerIdentity>();


        if (identity == null)
        {
            identity =
                other.GetComponent<PlayerIdentity>();
        }


        if (identity == null)
        {
            Transform root =
                other.transform.root;

            if (root != null)
            {
                identity =
                    root.GetComponent<PlayerIdentity>();
            }
        }


        if (identity == null)
        {
            DebugLog(
                $"🔦 [FLASHLIGHT PUZZLE] " +
                $"El objeto {other.gameObject.name} " +
                $"no tiene PlayerIdentity."
            );

            return;
        }


        // -------------------------------------------------
        // Player ID
        // -------------------------------------------------

        int playerId =
            identity.PlayerId;


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
        // Evitar contar varias partes del mismo jugador
        // -------------------------------------------------

        if (playersPassed[playerId])
        {
            return;
        }


        playersPassed[playerId] =
            true;


        int passedCount =
            GetPlayersPassedCount();


        Debug.Log(
            $"🔦 [FLASHLIGHT PUZZLE] " +
            $"PLAYER {playerId} pasó la puerta -> " +
            $"{passedCount}/{requiredPlayers}"
        );


        // -------------------------------------------------
        // Los cuatro pasaron
        // -------------------------------------------------

        if (
            passedCount >=
            requiredPlayers
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
            "LOS 4 JUGADORES PASARON LA PUERTA"
        );


        Debug.Log(
            "🔦🔦🔦 [FLASHLIGHT PUZZLE] " +
            "ACTIVANDO SKRYTYE SLOVA 🔦🔦🔦"
        );


        // =====================================================
        // 1. ACTIVAR ESTADO GENERAL
        // =====================================================

        if (puzzleController != null)
        {
            puzzleController.ActivatePuzzle();

            DebugLog(
                "🔦 [FLASHLIGHT PUZZLE] " +
                "PuzzleController.ActivatePuzzle() ejecutado."
            );
        }
        else
        {
            Debug.LogWarning(
                "🔦 [FLASHLIGHT PUZZLE] " +
                "No hay PuzzleController asignado/encontrado."
            );
        }


        // =====================================================
        // 2. ACTIVAR SKRYTYE SLOVA
        // =====================================================

        if (skrytyeSlovaController != null)
        {
            skrytyeSlovaController.ActivatePuzzle();

            Debug.Log(
                "🔤🔦 [FLASHLIGHT PUZZLE] " +
                "SkrytyeSlovaController.ActivatePuzzle() ejecutado."
            );
        }
        else
        {
            Debug.LogError(
                "🔤 [FLASHLIGHT PUZZLE] " +
                "NO SE ENCONTRÓ SkrytyeSlovaController."
            );
        }


        // =====================================================
        // 3. RESPALDO: ACTIVAR LINTERNAS DIRECTAMENTE
        // =====================================================

        ActivateAllFlashlights();


        // =====================================================
        // 4. DESACTIVAR TRIGGER
        // =====================================================

        if (disableTriggerAfterActivation)
        {
            Collider triggerCollider =
                GetComponent<Collider>();


            if (triggerCollider != null)
            {
                triggerCollider.enabled =
                    false;

                DebugLog(
                    "🔦 [FLASHLIGHT PUZZLE] " +
                    "Trigger desactivado."
                );
            }
        }
    }


    // =========================================================
    // ACTIVAR TODAS LAS LINTERNAS
    // =========================================================

    private void ActivateAllFlashlights()
    {
        // -------------------------------------------------
        // Auto encontrar
        // -------------------------------------------------

        if (autoFindFlashlights)
        {
            FindAllFlashlights();
        }


        // -------------------------------------------------
        // Validar
        // -------------------------------------------------

        if (
            flashlights == null ||
            flashlights.Length == 0
        )
        {
            Debug.LogError(
                "🔦 [FLASHLIGHT PUZZLE] " +
                "No hay FlashlightControllers."
            );

            return;
        }


        // -------------------------------------------------
        // Activar
        // -------------------------------------------------

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


            if (flashlight == null)
            {
                continue;
            }


            flashlight.ActivateFlashlight();

            activatedCount++;


            Debug.Log(
                $"🔦 [FLASHLIGHT PUZZLE] " +
                $"PLAYER {flashlight.GetPlayerId()} -> " +
                $"LINTERNA ACTIVADA"
            );
        }


        Debug.Log(
            $"🔦🔦🔦 [FLASHLIGHT PUZZLE] " +
            $"LINTERNAS ACTIVADAS: " +
            $"{activatedCount}/{flashlights.Length}"
        );
    }


    // =========================================================
    // BUSCAR TODAS LAS LINTERNAS
    // =========================================================

    private void FindAllFlashlights()
    {
        FlashlightController[] foundFlashlights =
            FindObjectsOfType<FlashlightController>();


        if (
            foundFlashlights == null ||
            foundFlashlights.Length == 0
        )
        {
            Debug.LogWarning(
                "🔦 [FLASHLIGHT PUZZLE] " +
                "No se encontraron FlashlightController " +
                "en la escena."
            );

            return;
        }


        flashlights =
            new FlashlightController[
                foundFlashlights.Length
            ];


        for (
            int i = 0;
            i < foundFlashlights.Length;
            i++
        )
        {
            flashlights[i] =
                foundFlashlights[i];
        }


        DebugLog(
            $"🔦 [FLASHLIGHT PUZZLE] " +
            $"Flashlights encontradas: " +
            $"{flashlights.Length}"
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
            if (playersPassed[i])
            {
                count++;
            }
        }


        return count;
    }


    // =========================================================
    // RESET
    // =========================================================

    [ContextMenu("Reset Flashlight Puzzle Trigger")]
    public void ResetTrigger()
    {
        puzzleActivated =
            false;


        ResetPassedPlayers();


        FindReferencesIfNecessary();


        Debug.Log(
            "🔦 [FLASHLIGHT PUZZLE] " +
            "Trigger reiniciado."
        );
    }


    // =========================================================
    // RESET PLAYERS
    // =========================================================

    private void ResetPassedPlayers()
    {
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

        Debug.Log(
            message
        );
    }
}