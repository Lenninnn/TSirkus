using UnityEngine;

public class HighlightableInteractable : MonoBehaviour, IInteractable, IHighlightable
{
    [Header("Interaction")]
    [SerializeField] private string objectMessage =
        "Interacción realizada.";

    [Header("Visual highlight")]
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private Material highlightMaterial;

    private Material[][] originalMaterials;
    private bool isHighlighted;

    private void Awake()
    {
        // Si no se asignaron Renderers manualmente,
        // busca todos los Renderers del objeto y sus hijos.
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers =
                GetComponentsInChildren<Renderer>(true);
        }

        originalMaterials = new Material[targetRenderers.Length][];

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] != null)
            {
                originalMaterials[i] =
                    targetRenderers[i].materials;
            }
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
            objectMessage
        );
    }

    public void SetHighlight(bool highlighted)
    {
        if (isHighlighted == highlighted)
            return;

        isHighlighted = highlighted;

        if (highlighted)
        {
            ApplyHighlight();
        }
        else
        {
            RestoreOriginalMaterials();
        }
    }

    private void ApplyHighlight()
    {
        if (highlightMaterial == null)
        {
            Debug.LogWarning(
                "No hay Highlight Material asignado en " +
                gameObject.name
            );

            return;
        }

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer renderer = targetRenderers[i];

            if (renderer == null)
                continue;

            Material[] materials =
                new Material[renderer.materials.Length];

            for (int j = 0; j < materials.Length; j++)
            {
                materials[j] = highlightMaterial;
            }

            renderer.materials = materials;
        }
    }

    private void RestoreOriginalMaterials()
    {
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer renderer = targetRenderers[i];

            if (renderer == null)
                continue;

            if (originalMaterials[i] == null)
                continue;

            renderer.materials = originalMaterials[i];
        }
    }

    private void OnDisable()
    {
        RestoreOriginalMaterials();
    }
}