
using System.Collections.Generic;
using UnityEngine;

public class RacePlayerBase : MonoBehaviour
{
    [Header("Referencias")]
    public RaceStartManager raceStartManager;

    [Header("Jugador asignado")]
    [Tooltip("0 permite cualquier jugador. Usa 1, 2, 3 o 4 para restringir la base.")]
    [Range(0, 4)]
    public int expectedPlayerId = 0;

    private readonly HashSet<int> playersInside = new HashSet<int>();

    private void Start()
    {
        Debug.Log("RacePlayerBase está activo en: " + gameObject.name);
    }

   

    public void ResetBase()
    {
        playersInside.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerIdentity identity =
            other.GetComponentInParent<PlayerIdentity>();

        if (identity == null || raceStartManager == null)
            return;

        int playerId = identity.PlayerId;

        if (playerId <= 0)
            return;

        if (expectedPlayerId != 0 &&
            playerId != expectedPlayerId)
            return;

        if (playersInside.Add(playerId))
        {
            Debug.Log("Jugador " + playerId + " está listo en una base.");

            raceStartManager.RegisterPlayerOnBase(playerId);
        }
    }



    private void OnTriggerExit(Collider other)
    {
        PlayerIdentity identity =
            other.GetComponentInParent<PlayerIdentity>();

        if (identity == null || raceStartManager == null)
            return;

        int playerId = identity.PlayerId;

        if (playersInside.Remove(playerId))
        {
            raceStartManager.UnregisterPlayerFromBase(playerId);

            Debug.Log(
                "Jugador " + playerId +
                " salió de su base."
            );
        }
    }
}
