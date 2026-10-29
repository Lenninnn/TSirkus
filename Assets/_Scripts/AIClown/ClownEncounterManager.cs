using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClownEncounterManager : MonoBehaviour
{
    [Header("Configuración del encuentro")]
    [Tooltip("Tiempo total que tienen los jugadores para reaccionar.")]
    [SerializeField] private float reactionWindow = 3f;

    [Tooltip("Al terminar la ventana sin que nadie lo vea: probabilidad de que aceche a un jugador que no lo miró. Si no, sigue rondando.")]
    [Range(0f, 1f)]
    [SerializeField] private float chaseChance = 0.35f;

    [Header("Referencias")]
    [SerializeField] private ClownSpawner clownSpawner;

    [Header("Debug")]
    [SerializeField] private bool showDebugMessages = true;

    private GameObject currentClown;

    private bool encounterActive;

    private Coroutine encounterCoroutine;

    private readonly List<PlayerIdentity> players =
        new List<PlayerIdentity>();

    private readonly HashSet<PlayerIdentity> playersWhoLooked =
        new HashSet<PlayerIdentity>();

    // ============================================================
    // PUBLIC
    // ============================================================

    public bool IsEncounterActive =>
        encounterActive;

    public GameObject CurrentClown =>
        currentClown;

    // ============================================================
    // AWAKE
    // ============================================================

    private void Awake()
    {
        if (clownSpawner == null)
        {
            clownSpawner =
                FindObjectOfType<ClownSpawner>();
        }
    }

    // ============================================================
    // START ENCOUNTER
    // ============================================================

    public bool StartEncounter(GameObject clown)
    {
        if (clown == null)
        {
            Debug.LogWarning(
                "[CLOWN ENCOUNTER] " +
                "Se intentó iniciar un encuentro con Clown NULL."
            );

            return false;
        }

        if (encounterActive)
        {
            Debug.LogWarning(
                "[CLOWN ENCOUNTER] " +
                "Ya existe un encuentro activo."
            );

            return false;
        }

        // --------------------------------------------------------
        // LIMPIAR ENCUENTRO ANTERIOR
        // --------------------------------------------------------

        ResetEncounterState();

        // --------------------------------------------------------
        // BUSCAR JUGADORES
        // --------------------------------------------------------

        FindCurrentPlayers();

        if (players.Count == 0)
        {
            Debug.LogWarning(
                "[CLOWN ENCOUNTER] " +
                "No se encontraron jugadores vivos."
            );

            return false;
        }

        // --------------------------------------------------------
        // GUARDAR CLOWN
        // --------------------------------------------------------

        currentClown = clown;

        encounterActive = true;

        // --------------------------------------------------------
        // AVISAR AL SPAWNER
        // --------------------------------------------------------

        if (clownSpawner != null)
        {
            clownSpawner.NotifyEncounterStarted();
        }

        // --------------------------------------------------------
        // DEBUG
        // --------------------------------------------------------

        Debug.Log(
            "=========================================="
        );

        Debug.Log(
            "[CLOWN ENCOUNTER] ENCUENTRO INICIADO"
        );

        Debug.Log(
            $"[CLOWN ENCOUNTER] Payaso: " +
            $"{currentClown.name}"
        );

        Debug.Log(
            $"[CLOWN ENCOUNTER] Jugadores: " +
            $"{players.Count}"
        );

        foreach (PlayerIdentity player in players)
        {
            Debug.Log(
                $"[CLOWN ENCOUNTER] " +
                $"Jugador registrado: Player {player.PlayerId}"
            );
        }

        Debug.Log(
            $"[CLOWN ENCOUNTER] " +
            $"Ventana de reacción: {reactionWindow:F2}s"
        );

        Debug.Log(
            "=========================================="
        );

        // --------------------------------------------------------
        // INICIAR CORRUTINA
        // --------------------------------------------------------

        encounterCoroutine =
            StartCoroutine(
                EncounterRoutine()
            );

        return true;
    }

    // ============================================================
    // BUSCAR JUGADORES
    // ============================================================

    private void FindCurrentPlayers()
    {
        players.Clear();

        PlayerIdentity[] foundPlayers =
            FindObjectsOfType<PlayerIdentity>();

        foreach (PlayerIdentity player in foundPlayers)
        {
            if (player == null)
                continue;

            // Solo jugadores reales (con cámara): en GAME hay otros objetos con PlayerIdentity.
            if (!player.isActiveAndEnabled ||
                player.GetComponentInChildren<Camera>() == null)
                continue;

            PlayerStateController stateController =
                player.GetComponent<PlayerStateController>();

            // Si tiene PlayerStateController,
            // solo los Alive participan.
            if (stateController != null &&
                !stateController.IsAlive)
            {
                continue;
            }

            players.Add(player);
        }
    }

    // ============================================================
    // REACTION WINDOW
    // ============================================================

    private IEnumerator EncounterRoutine()
    {
        float elapsed = 0f;

        while (elapsed < reactionWindow)
        {
            elapsed += Time.deltaTime;

            yield return null;
        }

        FinishLookWindow();
    }

    // ============================================================
    // RECIBIR REPORTE DE MIRADA
    // ============================================================

    public void ReportPlayerLooking(
        PlayerIdentity player,
        bool isLooking)
    {
        if (!encounterActive)
            return;

        if (player == null)
            return;

        if (currentClown == null)
            return;

        // --------------------------------------------------------
        // COMPROBAR QUE EL JUGADOR PERTENECE AL ENCUENTRO
        // --------------------------------------------------------

        if (!players.Contains(player))
        {
            Debug.LogWarning(
                $"[CLOWN ENCOUNTER] " +
                $"Player {player.PlayerId} " +
                $"NO está registrado en este encuentro."
            );

            return;
        }

        // --------------------------------------------------------
        // YA ESTÁ REGISTRADO
        // --------------------------------------------------------

        if (playersWhoLooked.Contains(player))
        {
            return;
        }

        // --------------------------------------------------------
        // EL DETECTOR CONFIRMÓ QUE VIO AL PAYASO
        // --------------------------------------------------------

        if (isLooking)
        {
            playersWhoLooked.Add(player);

            Debug.Log(
                "------------------------------------------"
            );

            Debug.Log(
                $"[CLOWN ENCOUNTER] " +
                $"PLAYER {player.PlayerId} " +
                $"CONFIRMÓ QUE VIO AL PAYASO."
            );

            Debug.Log(
                $"[CLOWN ENCOUNTER] " +
                $"Jugadores que ya MIRARON: " +
                $"{playersWhoLooked.Count}/{players.Count}"
            );

            Debug.Log(
                "------------------------------------------"
            );
        }
    }

    // ============================================================
    // TERMINAR VENTANA DE MIRADA
    // ============================================================

    private void FinishLookWindow()
    {
        if (!encounterActive)
            return;

        Debug.Log(
            "=========================================="
        );

        Debug.Log(
            "[CLOWN ENCOUNTER] " +
            "VENTANA DE REACCIÓN TERMINADA."
        );

        Debug.Log(
            $"[CLOWN ENCOUNTER] " +
            $"Jugadores totales: {players.Count}"
        );

        Debug.Log(
            $"[CLOWN ENCOUNTER] " +
            $"Jugadores que MIRARON: " +
            $"{playersWhoLooked.Count}"
        );

        // --------------------------------------------------------
        // MOSTRAR ESTADO DE CADA JUGADOR
        // --------------------------------------------------------

        foreach (PlayerIdentity player in players)
        {
            bool looked =
                playersWhoLooked.Contains(player);

            Debug.Log(
                $"[CLOWN ENCOUNTER] " +
                $"Player {player.PlayerId} | " +
                $"Miró: {looked}"
            );
        }

        Debug.Log(
            "=========================================="
        );

        // --------------------------------------------------------
        // SELECCIONAR VÍCTIMA
        // --------------------------------------------------------

        if (Random.value > chaseChance)
        {
            if (showDebugMessages)
            {
                Debug.Log(
                    "[CLOWN ENCOUNTER] Nadie lo vio: esta vez solo ronda."
                );
            }

            FinishLookPhase();

            return;
        }

        PlayerIdentity target =
            SelectCaptureTarget();

        if (target == null)
        {
            Debug.LogWarning(
                "[CLOWN ENCOUNTER] " +
                "No se pudo seleccionar objetivo."
            );

            FinishLookPhase();

            return;
        }

        // --------------------------------------------------------
        // BUSCAR CLOWNAI
        // --------------------------------------------------------

        ClownAI clownAI =
            currentClown.GetComponentInParent<ClownAI>();

        if (clownAI == null)
        {
            Debug.LogError(
                "[CLOWN ENCOUNTER] " +
                "No se encontró ClownAI en el payaso."
            );

            FinishLookPhase();

            return;
        }

        // --------------------------------------------------------
        // OBJETIVO FINAL
        // --------------------------------------------------------

        Debug.Log(
            "=========================================="
        );

        Debug.Log(
            $"[CLOWN ENCOUNTER] " +
            $"OBJETIVO FINAL: Player {target.PlayerId}"
        );

        Debug.Log(
            $"[CLOWN ENCOUNTER] " +
            $"El clown comenzará a perseguirlo."
        );

        Debug.Log(
            "=========================================="
        );

        // --------------------------------------------------------
        // DAR OBJETIVO AL CLOWN
        // --------------------------------------------------------

        clownAI.SetTarget(
            target.transform
        );

        // --------------------------------------------------------
        // TERMINAR FASE DE MIRADA
        // --------------------------------------------------------

        FinishLookPhase();
    }

    // ============================================================
    // SELECCIONAR OBJETIVO
    // ============================================================

    private PlayerIdentity SelectCaptureTarget()
    {
        List<PlayerIdentity> playersWhoDidNotLook =
            new List<PlayerIdentity>();

        // --------------------------------------------------------
        // BUSCAR LOS QUE NO MIRARON
        // --------------------------------------------------------

        foreach (PlayerIdentity player in players)
        {
            if (player == null)
                continue;

            if (!playersWhoLooked.Contains(player))
            {
                playersWhoDidNotLook.Add(player);
            }
        }

        // --------------------------------------------------------
        // HAY JUGADORES QUE NO MIRARON
        // --------------------------------------------------------

        if (playersWhoDidNotLook.Count > 0)
        {
            int randomIndex =
                Random.Range(
                    0,
                    playersWhoDidNotLook.Count
                );

            PlayerIdentity selectedPlayer =
                playersWhoDidNotLook[randomIndex];

            Debug.Log(
                "[CLOWN ENCOUNTER] " +
                "NO TODOS MIRARON AL PAYASO."
            );

            Debug.Log(
                $"[CLOWN ENCOUNTER] " +
                $"Jugadores que NO MIRARON: " +
                $"{playersWhoDidNotLook.Count}"
            );

            Debug.Log(
                $"[CLOWN ENCOUNTER] " +
                $"Víctima seleccionada entre ellos: " +
                $"Player {selectedPlayer.PlayerId}"
            );

            return selectedPlayer;
        }

        // --------------------------------------------------------
        // TODOS MIRARON
        // --------------------------------------------------------

        if (players.Count > 0)
        {
            int randomIndex =
                Random.Range(
                    0,
                    players.Count
                );

            PlayerIdentity selectedPlayer =
                players[randomIndex];

            Debug.Log(
                "[CLOWN ENCOUNTER] " +
                "TODOS MIRARON AL PAYASO."
            );

            Debug.Log(
                $"[CLOWN ENCOUNTER] " +
                $"Se seleccionará una víctima aleatoria " +
                $"entre TODOS."
            );

            Debug.Log(
                $"[CLOWN ENCOUNTER] " +
                $"Víctima seleccionada: " +
                $"Player {selectedPlayer.PlayerId}"
            );

            return selectedPlayer;
        }

        return null;
    }

    // ============================================================
    // TERMINAR FASE DE MIRADA
    // ============================================================

    private void FinishLookPhase()
    {
        if (encounterCoroutine != null)
        {
            StopCoroutine(
                encounterCoroutine
            );

            encounterCoroutine = null;
        }

        // IMPORTANTE:
        // Aquí dejamos de aceptar reportes de mirada.
        // El ClownAI ya tiene su objetivo.
        encounterActive = false;

        playersWhoLooked.Clear();

        // NO hacemos:
        // currentClown = null;
        //
        // porque el ClownAI todavía está utilizando
        // el clown actual.
    }

    // ============================================================
    // TERMINAR ENCUENTRO COMPLETO
    // ============================================================

    public void FinishEncounter(
        bool despawnClown = true)
    {
        if (encounterCoroutine != null)
        {
            StopCoroutine(
                encounterCoroutine
            );

            encounterCoroutine = null;
        }

        encounterActive = false;

        if (clownSpawner != null)
        {
            clownSpawner.NotifyEncounterFinished(
                despawnClown
            );
        }

        ResetEncounterState();
    }

    // ============================================================
    // RESET
    // ============================================================

    private void ResetEncounterState()
    {
        encounterActive = false;

        currentClown = null;

        players.Clear();

        playersWhoLooked.Clear();

        encounterCoroutine = null;
    }
}