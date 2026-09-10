using UnityEngine;
using UnityEngine.Events;

public class PuzzleController : MonoBehaviour
{
    [Header("Puzzle State")]
    [SerializeField] private PuzzleState currentState = PuzzleState.Inactive;

    public PuzzleState CurrentState => currentState;

    [Header("Puzzle Events")]
    public UnityEvent onPuzzleActivated;
    public UnityEvent onPuzzleCompleted;
    public UnityEvent onPuzzleFailed;
    public UnityEvent onPuzzleReset;

    [ContextMenu("Activate Puzzle")]
    public void ActivatePuzzle()
    {
        if (currentState != PuzzleState.Inactive)
            return;

        currentState = PuzzleState.Active;

        Debug.Log($"{gameObject.name}: Puzzle ACTIVADO");

        onPuzzleActivated?.Invoke();
    }

    [ContextMenu("Complete Puzzle")]
    public void CompletePuzzle()
    {
        if (currentState != PuzzleState.Active)
            return;

        currentState = PuzzleState.Success;

        Debug.Log($"{gameObject.name}: Puzzle COMPLETADO");

        onPuzzleCompleted?.Invoke();
    }

    [ContextMenu("Fail Puzzle")]
    public void FailPuzzle()
    {
        if (currentState != PuzzleState.Active)
            return;

        currentState = PuzzleState.Failed;

        Debug.Log($"{gameObject.name}: Puzzle FALLIDO");

        onPuzzleFailed?.Invoke();
    }

    [ContextMenu("Reset Puzzle")]
    public void ResetPuzzle()
    {
        currentState = PuzzleState.Inactive;

        Debug.Log($"{gameObject.name}: Puzzle REINICIADO");

        onPuzzleReset?.Invoke();
    }
}