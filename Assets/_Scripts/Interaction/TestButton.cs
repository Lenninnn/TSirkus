using UnityEngine;

public class TestButton : MonoBehaviour, IInteractable
{
    private bool activated = false;

    public void Interact()
    {
        activated = !activated;

        Debug.Log(
            activated
                ? "Botón ACTIVADO"
                : "Botón DESACTIVADO"
        );

        transform.localScale = activated
            ? new Vector3(1f, 0.3f, 1f)
            : Vector3.one;
    }
}