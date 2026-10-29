
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RaceStartManager : MonoBehaviour
{
    [Header("Configuración de inicio")]
    [Min(1)]
    public int requiredPlayersToStart = 2;

    [Header("Evento al iniciar")]
    public UnityEvent onRaceStarted;

    private readonly HashSet<int> playersOnBases = new HashSet<int>();

    public bool RaceStarted { get; private set; }

    public int PlayersReady => playersOnBases.Count;

    private void Start()
    {
        Debug.Log("RaceStartManager está activo. Jugadores necesarios: "
                  + requiredPlayersToStart);
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

    private void CheckIfReady()
    {
        if (RaceStarted)
            return;

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
