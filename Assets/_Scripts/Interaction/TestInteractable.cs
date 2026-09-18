using UnityEngine;

public class TestInteractable : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private string interactionMessage =
        "Interacción realizada correctamente.";

    [Header("Highlight")]
    [SerializeField] private Material highlightMaterial;

    private Renderer objectRenderer;
    private Material originalMaterial;

    private void Awake()
    {
        objectRenderer = GetComponentInChildren<Renderer>();

        if (objectRenderer != null)
        {
            originalMaterial = objectRenderer.material;
        }
    }

    public void Interact(PlayerIdentity player)
    {
        string playerName = "Jugador desconocido";

        if (player != null)
        {
            playerName = player.gameObject.name;
        }

        Debug.Log(
            playerName + " interactuó con " +
            gameObject.name + ". " +
            interactionMessage
        );
    }

    public void SetHighlight(bool highlighted)
    {
        if (objectRenderer == null)
            return;

        if (highlighted && highlightMaterial != null)
        {
            objectRenderer.material = highlightMaterial;
        }
        else if (!highlighted && originalMaterial != null)
        {
            objectRenderer.material = originalMaterial;
        }
    }
}