using UnityEngine;

public class TestButton : MonoBehaviour, IInteractable
{
    private bool activated = false;

    public void Interact(PlayerIdentity player)
    {
        activated = !activated;

        Debug.Log(
            activated
                ? "Botón ACTIVADO"
                : "Botón DESACTIVADO"
        );

        Debug.Log(
    $"Player {player.PlayerId} interactuó con el botón"
);

        transform.localScale = activated
            ? new Vector3(1f, 0.3f, 1f)
            : Vector3.one;
    }
}