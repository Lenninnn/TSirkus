using System.Collections.Generic;
using UnityEngine;

public class FourSwitchPuzzleController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PuzzleController puzzleController;

    [Header("Switches")]
    [SerializeField] private PuzzleSwitch[] switches;

    [Header("Settings")]
    [SerializeField] private int requiredPlayers = 4;

    private readonly HashSet<int> activatedPlayers =
        new HashSet<int>();

    public int ActivatedCount => activatedPlayers.Count;

    public bool RegisterPlayer(PlayerIdentity player)
    {
        if (player == null)
            return false;

        if (puzzleController.CurrentState == PuzzleState.Inactive)
        {
            puzzleController.ActivatePuzzle();
        }

        if (puzzleController.CurrentState != PuzzleState.Active)
            return false;

        int playerId = player.PlayerId;

        // No permitir contar dos veces al mismo jugador
        if (!activatedPlayers.Add(playerId))
        {
            Debug.Log(
                $"Player {playerId} ya participó en este puzzle."
            );

            return false;
        }

        Debug.Log(
            $"Player {playerId} registrado: " +
            $"{activatedPlayers.Count}/{requiredPlayers}"
        );

        if (activatedPlayers.Count >= requiredPlayers)
        {
            puzzleController.CompletePuzzle();
        }

        return true;
    }

    [ContextMenu("Reset Four Switch Puzzle")]
    public void ResetFourSwitchPuzzle()
    {
        activatedPlayers.Clear();

        foreach (PuzzleSwitch puzzleSwitch in switches)
        {
            if (puzzleSwitch != null)
                puzzleSwitch.ResetSwitch();
        }

        puzzleController.ResetPuzzle();

        Debug.Log("Puzzle de 4 interruptores reiniciado.");
    }
}