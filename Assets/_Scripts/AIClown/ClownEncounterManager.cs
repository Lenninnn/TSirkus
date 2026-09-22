using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClownEncounterManager : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Spawner que controla las apariciones del payaso.")]
    [SerializeField] private ClownSpawner clownSpawner;

    [Header("Ventana de reacción")]
    [Tooltip("Tiempo que tienen los jugadores para mirar al payaso.")]
    [SerializeField] private float reactionWindow = 3f;

    [Tooltip(
        "Tiempo continuo que un jugador debe mantener el raycast " +
        "sobre el payaso para considerarse que realmente lo miró."
    )]
    [SerializeField] private float requiredLookDuration = 0.4f;

    [Header("Depuración")]
    [SerializeField] private bool showDebugMessages = true;

    // =========================================================
    // ESTADO
    // =========================================================

    private Coroutine encounterCoroutine;

    private bool encounterActive;

    private GameObject currentClown;

    private readonly List<PlayerIdentity> players =
        new List<PlayerIdentity>();

    private readonly HashSet<int> playersWhoLooked =
        new HashSet<int>();

    private readonly Dictionary<int, float> lookTimers =
        new Dictionary<int, float>();

    // =========================================================
    // PROPIEDADES
    // =========================================================

    public bool IsEncounterActive => encounterActive;

    public GameObject CurrentClown => currentClown;

    // =========================================================
    // INICIAR ENCUENTRO
    // =========================================================

    public void StartEncounter(GameObject clown)
    {
        if (clown == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: No se puede iniciar el encuentro porque el payaso es NULL."
            );

            return;
        }

        if (encounterActive)
        {
            DebugLog(
                "Ya existe un encuentro activo."
            );

            return;
        }

        if (clownSpawner == null)
        {
            Debug.LogError(
                $"{gameObject.name}: No hay ClownSpawner asignado."
            );

            return;
        }

        // -----------------------------------------------------
        // BUSCAR JUGADORES ACTUALES
        // -----------------------------------------------------

        FindCurrentPlayers();

        if (players.Count == 0)
        {
            Debug.LogWarning(
                $"{gameObject.name}: No se encontraron jugadores."
            );

            return;
        }

        currentClown = clown;

        playersWhoLooked.Clear();
        lookTimers.Clear();

        encounterActive = true;

        // -----------------------------------------------------
        // AVISAR AL SPAWNER
        // -----------------------------------------------------

        bool spawnerAccepted =
            clownSpawner.NotifyEncounterStarted();

        if (!spawnerAccepted)
        {
            encounterActive = false;
            currentClown = null;

            return;
        }

        // -----------------------------------------------------
        // INICIAR COROUTINE
        // -----------------------------------------------------

        if (encounterCoroutine != null)
        {
            StopCoroutine(
                encounterCoroutine
            );
        }

        encounterCoroutine =
            StartCoroutine(
                EncounterRoutine()
            );

        DebugLog(
            $"ENCUENTRO INICIADO. " +
            $"Jugadores encontrados: {players.Count}. " +
            $"Ventana: {reactionWindow:F1}s."
        );
    }

    // =========================================================
    // BUSCAR JUGADORES
    // =========================================================

    private void FindCurrentPlayers()
    {
        players.Clear();

        PlayerIdentity[] foundPlayers =
            FindObjectsOfType<PlayerIdentity>();

        foreach (
            PlayerIdentity player
            in foundPlayers
        )
        {
            if (player == null)
            {
                continue;
            }

            players.Add(
                player
            );
        }

        // Ordenar por PlayerId para que los mensajes
        // de depuración sean fáciles de leer.
        players.Sort(
            (a, b) =>
                a.PlayerId.CompareTo(
                    b.PlayerId
                )
        );

        DebugLog(
            $"Se encontraron {players.Count} PlayerIdentity."
        );

        foreach (
            PlayerIdentity player
            in players
        )
        {
            DebugLog(
                $"Jugador encontrado: Player_{player.PlayerId}"
            );
        }
    }

    // =========================================================
    // RUTINA DEL ENCUENTRO
    // =========================================================

    private IEnumerator EncounterRoutine()
    {
        float elapsed = 0f;

        while (
            elapsed < reactionWindow &&
            encounterActive
        )
        {
            elapsed += Time.deltaTime;

            yield return null;
        }

        if (!encounterActive)
        {
            yield break;
        }

        FinishLookWindow();
    }

    // =========================================================
    // REPORTAR MIRADA
    // =========================================================

    public void ReportPlayerLooking(
        PlayerIdentity player,
        bool isLooking
    )
    {
        if (!encounterActive)
        {
            return;
        }

        if (player == null)
        {
            return;
        }

        if (currentClown == null)
        {
            return;
        }

        // -----------------------------------------------------
        // COMPROBAR QUE ESTE PLAYER PERTENECE AL ENCUENTRO
        // -----------------------------------------------------

        bool playerExists = false;

        foreach (
            PlayerIdentity registeredPlayer
            in players
        )
        {
            if (
                registeredPlayer == player
            )
            {
                playerExists = true;
                break;
            }
        }

        if (!playerExists)
        {
            return;
        }

        int playerId =
            player.PlayerId;

        // -----------------------------------------------------
        // SI YA COMPLETÓ LA MIRADA
        // -----------------------------------------------------

        if (
            playersWhoLooked.Contains(
                playerId
            )
        )
        {
            return;
        }

        // -----------------------------------------------------
        // ESTÁ MIRANDO
        // -----------------------------------------------------

        if (isLooking)
        {
            if (
                !lookTimers.ContainsKey(
                    playerId
                )
            )
            {
                lookTimers[playerId] = 0f;
            }

            lookTimers[playerId] +=
                Time.deltaTime;

            // -------------------------------------------------
            // MIRADA CONFIRMADA
            // -------------------------------------------------

            if (
                lookTimers[playerId] >=
                requiredLookDuration
            )
            {
                playersWhoLooked.Add(
                    playerId
                );

                DebugLog(
                    $"Player_{playerId} MIRÓ al payaso."
                );
            }

            return;
        }

        // -----------------------------------------------------
        // DEJÓ DE MIRAR
        // -----------------------------------------------------

        if (
            lookTimers.ContainsKey(
                playerId
            )
        )
        {
            lookTimers[playerId] = 0f;
        }
    }

    // =========================================================
    // TERMINAR VENTANA DE MIRADA
    // =========================================================

    private void FinishLookWindow()
    {
        if (!encounterActive)
        {
            return;
        }

        DebugLog(
            "Terminó la ventana de reacción."
        );

        PlayerIdentity target =
            SelectCaptureTarget();

        if (target == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: No se pudo seleccionar un objetivo."
            );

            FinishEncounter(
                true
            );

            return;
        }

        DebugLog(
            $"OBJETIVO SELECCIONADO: Player_{target.PlayerId}"
        );

        // -----------------------------------------------------
        // BUSCAR ClownAI
        // -----------------------------------------------------

        ClownAI clownAI =
            currentClown.GetComponent<ClownAI>();

        if (clownAI == null)
        {
            Debug.LogError(
                $"{gameObject.name}: El payaso no tiene ClownAI."
            );

            FinishEncounter(
                true
            );

            return;
        }

        // -----------------------------------------------------
        // ENVIAR OBJETIVO AL PAYASO
        // -----------------------------------------------------

        clownAI.SetTarget(
            target.transform
        );

        DebugLog(
            $"ClownAI recibió como objetivo a Player_{target.PlayerId}."
        );
    }

    // =========================================================
    // SELECCIONAR OBJETIVO
    // =========================================================

    private PlayerIdentity SelectCaptureTarget()
    {
        List<PlayerIdentity> playersWhoDidNotLook =
            new List<PlayerIdentity>();

        // -----------------------------------------------------
        // BUSCAR QUIÉNES NO MIRARON
        // -----------------------------------------------------

        foreach (
            PlayerIdentity player
            in players
        )
        {
            if (player == null)
            {
                continue;
            }

            if (
                !playersWhoLooked.Contains(
                    player.PlayerId
                )
            )
            {
                playersWhoDidNotLook.Add(
                    player
                );
            }
        }

        // -----------------------------------------------------
        // PRIORIDAD:
        // QUIENES NO MIRARON
        // -----------------------------------------------------

        if (
            playersWhoDidNotLook.Count > 0
        )
        {
            int randomIndex =
                Random.Range(
                    0,
                    playersWhoDidNotLook.Count
                );

            PlayerIdentity target =
                playersWhoDidNotLook[
                    randomIndex
                ];

            DebugLog(
                $"Hay {playersWhoDidNotLook.Count} jugador(es) que NO miraron."
            );

            return target;
        }

        // -----------------------------------------------------
        // FALLBACK:
        // TODOS MIRARON
        // -----------------------------------------------------

        if (players.Count > 0)
        {
            int randomIndex =
                Random.Range(
                    0,
                    players.Count
                );

            PlayerIdentity target =
                players[
                    randomIndex
                ];

            DebugLog(
                "Todos los jugadores miraron. " +
                "Se seleccionará un jugador aleatorio."
            );

            return target;
        }

        return null;
    }

    // =========================================================
    // FINALIZAR ENCUENTRO
    // =========================================================

    public void FinishEncounter(
        bool despawnClown = true
    )
    {
        if (encounterCoroutine != null)
        {
            StopCoroutine(
                encounterCoroutine
            );

            encounterCoroutine = null;
        }

        encounterActive = false;

        playersWhoLooked.Clear();
        lookTimers.Clear();
        players.Clear();

        currentClown = null;

        if (clownSpawner != null)
        {
            clownSpawner.NotifyEncounterFinished(
                despawnClown
            );
        }

        DebugLog(
            "Encuentro finalizado."
        );
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
}