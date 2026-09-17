using UnityEngine;

public class PuzzleSwitch : MonoBehaviour, IInteractable
{
    [Header("Puzzle")]
    [SerializeField] private FourSwitchPuzzleController puzzleController;

    [Header("State")]
    [SerializeField] private bool isActivated;

    private Vector3 initialScale;

    private void Awake()
    {
        initialScale = transform.localScale;
    }

    public void Interact(PlayerIdentity player)
    {
        if (isActivated)
            return;

        bool accepted = puzzleController.RegisterPlayer(player);

        if (!accepted)
            return;

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
    }

    public void ResetSwitch()
    {
        isActivated = false;
        transform.localScale = initialScale;
    }
}