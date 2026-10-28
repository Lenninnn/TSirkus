using UnityEngine;
using UnityEngine.Events;

public class PuzzleSwitch : MonoBehaviour, IInteractable
{
    [Header("Puzzle")]
    [SerializeField] private FourSwitchPuzzleController puzzleController;

    [Header("State")]
    [SerializeField] private bool isActivated;

    [Header("Eventos (feedback: sonido, efectos)")]
    public UnityEvent onActivated;

    [Tooltip("Ya estaba activado o ese jugador ya participó.")]
    public UnityEvent onRejected;

    private Vector3 initialScale;

    private void Awake()
    {
        initialScale = transform.localScale;
    }

    public void Interact(PlayerIdentity player)
    {
        if (isActivated)
        {
            onRejected?.Invoke();
            return;
        }

        bool accepted = puzzleController.RegisterPlayer(player);

        if (!accepted)
        {
            onRejected?.Invoke();
            return;
        }

        isActivated = true;

        Debug.Log(
            $"{gameObject.name} activado por Player {player.PlayerId}"
        );

        // Feedback provisional: el botón se hunde
        transform.localScale = new Vector3(
            initialScale.x,
            initialScale.y * 0.3f,
            initialScale.z
        );

        onActivated?.Invoke();
    }

    public void ResetSwitch()
    {
        isActivated = false;
        transform.localScale = initialScale;
    }
}
