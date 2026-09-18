using UnityEngine;

public class TestInteractable : MonoBehaviour, IInteractable, IHighlightable
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
        {
            Debug.LogWarning(
                "No se encontró Renderer en " + gameObject.name
            );

            return;
        }

        if (highlighted)
        {
            if (highlightMaterial != null)
            {
                objectRenderer.material = highlightMaterial;
            }
            else
            {
                Debug.LogWarning(
                    "No hay Highlight Material asignado en " +
                    gameObject.name
                );
            }
        }
        else
        {
            if (originalMaterial != null)
            {
                objectRenderer.material = originalMaterial;
            }
        }
    }
}