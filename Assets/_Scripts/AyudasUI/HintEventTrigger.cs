using UnityEngine;

// Pide una ayuda desde un evento (UnityEvent de un puzzle, una palanca,
// una animación...). Ejemplo: PuzzleController.onPuzzleActivated → ShowToAll().
// Cada jugador la ve en su propio cuadrante.
public class HintEventTrigger : MonoBehaviour
{
    [SerializeField] private HintDefinition hint;
    [SerializeField] private ContextHintController controller;


    private void Awake()
    {
        if (controller == null)
            controller = FindAnyObjectByType<ContextHintController>();
    }


    public void ShowToAll()
    {
        if (controller != null)
            controller.RequestForAll(hint);
    }


    // Para eventos que conocen al jugador (1-4).
    public void ShowToPlayer(int playerId)
    {
        if (controller != null)
            controller.Request(hint, playerId);
    }
}
