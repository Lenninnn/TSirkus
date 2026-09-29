using UnityEngine;


public class FlashlightPuzzleRoomTrigger : MonoBehaviour
{

    // =========================================================
    // PUZZLE
    // =========================================================

    [Header("Puzzle")]
    [SerializeField] private PuzzleController puzzleController;


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
    // ESTADO
    // =========================================================

    private bool[] playersPassed = new bool[5];

    private bool puzzleActivated = false;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        ResetPassedPlayers();
    }


    private void OnTriggerEnter(Collider other)
    {

        // -------------------------------------------------
        // No volver a activar si este trigger ya terminó.
        // -------------------------------------------------

        if (
            activateOnlyOnce &&
            puzzleActivated
        )
        {
            return;
        }


        // -------------------------------------------------
        // Buscar la identidad del jugador.
        // Funciona aunque el collider esté en un hijo.
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
            return;
        }


        int playerId =
            identity.PlayerId;


        // -------------------------------------------------
        // Solo aceptamos Players 1-4.
        // -------------------------------------------------

        if (
            playerId < 1 ||
            playerId > 4
        )
        {
            return;
        }


        // -------------------------------------------------
        // Ese jugador ya había pasado la puerta.
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
            $"🔦 [FLASHLIGHT PUZZLE] PLAYER {playerId} " +
            $"pasó la puerta -> {passedCount}/{requiredPlayers}"
        );


        // -------------------------------------------------
        // Cuando pasan los cuatro, activar el puzzle.
        // -------------------------------------------------

        if (passedCount >= requiredPlayers)
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
            "🔦🔦🔦 [FLASHLIGHT PUZZLE] LOS 4 JUGADORES " +
            "PASARON LA PUERTA -> ACTIVANDO PUZZLE 🔦🔦🔦"
        );


        // -------------------------------------------------
        // Cambiar el estado del PuzzleController.
        // Esto reemplaza el Activate Puzzle manual.
        // -------------------------------------------------

        if (puzzleController != null)
        {
            puzzleController.ActivatePuzzle();
        }
        else
        {
            Debug.LogError(
                "🔦 [FLASHLIGHT PUZZLE] Falta asignar " +
                "PuzzleController en el Inspector."
            );
        }


        // -------------------------------------------------
        // Encender las cuatro linternas.
        // -------------------------------------------------

        ActivateAllFlashlights();


        // -------------------------------------------------
        // Opcional: apagar el trigger después de activar.
        // -------------------------------------------------

        if (disableTriggerAfterActivation)
        {

            Collider triggerCollider =
                GetComponent<Collider>();


            if (triggerCollider != null)
            {
                triggerCollider.enabled =
                    false;
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
                "🔦 [FLASHLIGHT PUZZLE] No hay FlashlightControllers " +
                "asignados en el Inspector."
            );

            return;
        }


        int activatedCount =
            0;


        foreach (
            FlashlightController flashlight in flashlights
        )
        {

            if (flashlight == null)
            {
                Debug.LogWarning(
                    "🔦 [FLASHLIGHT PUZZLE] Hay un slot de " +
                    "FlashlightController vacío."
                );

                continue;
            }


            flashlight.ActivateFlashlight();


            activatedCount++;


            Debug.Log(
                $"🔦 [FLASHLIGHT PUZZLE] Linterna activada -> " +
                $"Player {flashlight.GetPlayerId()}"
            );
        }


        Debug.Log(
            $"🔦 [FLASHLIGHT PUZZLE] Linternas activadas: " +
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
            if (playersPassed[i])
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
            "🔦 [FLASHLIGHT PUZZLE] Trigger reiniciado."
        );
    }


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
}