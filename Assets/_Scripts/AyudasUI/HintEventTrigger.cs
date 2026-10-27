using UnityEngine;

// Pide una ayuda desde un evento (UnityEvent de un puzzle, una palanca,
// una animación...). Ejemplo: PuzzleController.onPuzzleActivated → Show().
public class HintEventTrigger : MonoBehaviour
{
    [SerializeField] private HintDefinition hint;
    [SerializeField] private ContextHintController controller;


    private void Awake()
    {
        if (controller == null)
            controller = FindAnyObjectByType<ContextHintController>();
    }


    public void Show()
    {
        if (controller != null)
            controller.Request(hint);
    }


    // Permite reutilizar un mismo trigger con distintas ayudas desde el inspector.
    public void Show(HintDefinition otherHint)
    {
        if (controller != null)
            controller.Request(otherHint);
    }
}
