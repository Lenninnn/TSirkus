
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RaceStartManager : MonoBehaviour
{
    [Header("Configuración de inicio")]
    [Min(1)]
    public int requiredPlayersToStart = 2;

    [Header("Pruebas previas")]
    [Tooltip("La carrera no empieza hasta que estas pruebas estén superadas (orden de la historia).")]
    public PuzzleController[] requiredPuzzles;

    [Header("Evento al iniciar")]
    public UnityEvent onRaceStarted;

    private readonly HashSet<int> playersOnBases = new HashSet<int>();
    private float nextLockedNotice;

    public bool RaceStarted { get; private set; }

    public int PlayersReady => playersOnBases.Count;

    private void Start()
    {
        Debug.Log("RaceStartManager está activo. Jugadores necesarios: "
                  + requiredPlayersToStart);
    }

    // Si los cuatro ya estaban sobre las bases cuando se completó la última prueba previa,
    // la carrera empieza sin que tengan que bajarse y volver a subir.
    private void Update()
    {
        if (!RaceStarted && PlayersReady >= requiredPlayersToStart && PreviousTrialsCompleted)
            CheckIfReady();
    }

    public void RegisterPlayerOnBase(int playerId)
    {
        if (RaceStarted)
        {
            Debug.LogWarning("La carrera ya comenzó. No se registra al jugador "
                             + playerId);
            return;
        }

        if (playerId <= 0)
        {
            Debug.LogWarning("ID de jugador inválido: " + playerId);
            return;
        }

        // Add devuelve false si ese ID ya estaba registrado.
        bool added = playersOnBases.Add(playerId);

        if (added)
        {
            Debug.Log("Jugador " + playerId + " entró a una base. "
                      + "Jugadores listos: " + PlayersReady + "/"
                      + requiredPlayersToStart);
        }
        else
        {
            Debug.Log("El jugador " + playerId
                      + " ya estaba registrado en una base.");
        }

        CheckIfReady();
    }

    public void UnregisterPlayerFromBase(int playerId)
    {
        if (RaceStarted || playerId <= 0)
            return;

        if (playersOnBases.Remove(playerId))
        {
            Debug.Log("Jugador " + playerId + " salió de su base. "
                      + "Jugadores listos: " + PlayersReady + "/"
                      + requiredPlayersToStart);
        }
    }

    public bool PreviousTrialsCompleted
    {
        get
        {
            if (requiredPuzzles == null)
                return true;

            foreach (PuzzleController puzzle in requiredPuzzles)
            {
                if (puzzle != null && puzzle.CurrentState != PuzzleState.Success)
                    return false;
            }

            return true;
        }
    }

    // Reintento: nadie llegó a la meta. Los jugadores vuelven a sus bases para empezar otra vez.
    public void ResetRace()
    {
        RaceStarted = false;
        playersOnBases.Clear();
        Debug.Log("RaceStartManager: carrera reiniciada. Esperando a los jugadores en sus bases.");
    }

    private void CheckIfReady()
    {
        if (RaceStarted)
            return;

        if (PlayersReady >= requiredPlayersToStart && !PreviousTrialsCompleted)
        {
            Debug.Log("La carrera aún no puede empezar: faltan pruebas anteriores.");

            if (TrialCompleteBanner.Instance != null && Time.time >= nextLockedNotice)
            {
                nextLockedNotice = Time.time + 10f;
                TrialCompleteBanner.Instance.Show(null, "LA FUNCIÓN AÚN NO TERMINA", "SUPEREN LAS PRUEBAS ANTERIORES");
            }

            return;
        }

        if (PlayersReady >= requiredPlayersToStart)
        {
            RaceStarted = true;

            Debug.Log("¡La carrera comienza con "
                      + PlayersReady + " jugadores!");

            onRaceStarted?.Invoke();
        }
    }

    public bool IsPlayerOnBase(int playerId)
    {
        return playersOnBases.Contains(playerId);
    }
}
