using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private MobileInputManager mobileInputManager;
    [SerializeField] private PlayerIdentity playerIdentity;

    [Header("Player")]
    [SerializeField] private int playerId = 1;

    [Header("Raycast distances")]
    [SerializeField] private float highlightDistance = 5f;
    [SerializeField] private float interactionDistance = 3f;

    [Header("Raycast settings")]
    [SerializeField] private LayerMask interactableLayers = ~0;

    [Header("Highlight")]
    [SerializeField] private Material highlightMaterial;

    private IInteractable currentInteractable;
    private Collider currentCollider;

    private Renderer[] currentRenderers;
    private Material[][] originalMaterials;

    private bool previousActionPressed;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }

        if (playerIdentity == null)
        {
            playerIdentity = GetComponent<PlayerIdentity>();
        }

        if (mobileInputManager == null)
        {
            mobileInputManager = FindObjectOfType<MobileInputManager>();
        }
    }

    private void Update()
    {
        UpdateRaycast();
        CheckMobileAction();
        CheckKeyboardAction();
    }

    private void UpdateRaycast()
    {
        if (playerCamera == null)
            return;

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        bool hitSomething = Physics.Raycast(
            ray,
            out RaycastHit hit,
            highlightDistance,
            interactableLayers,
            QueryTriggerInteraction.Ignore
        );

        Debug.DrawRay(
            ray.origin,
            ray.direction * highlightDistance,
            hitSomething ? Color.green : Color.red
        );

        IInteractable detectedInteractable = null;
        Collider detectedCollider = null;

        if (hitSomething)
        {
            detectedCollider = hit.collider;

            detectedInteractable =
                hit.collider.GetComponentInParent<IInteractable>();
        }

        if (detectedInteractable != currentInteractable)
        {
            RemoveHighlight();

            currentInteractable = detectedInteractable;
            currentCollider = detectedCollider;

            if (currentInteractable != null)
            {
                Debug.Log(
                    "Raycast detectó: " +
                    ((MonoBehaviour)currentInteractable).gameObject.name
                );

                ApplyHighlight();
            }
        }
        else
        {
            currentCollider = detectedCollider;
        }
    }

    private void ApplyHighlight()
    {
        if (currentInteractable == null)
            return;

        if (highlightMaterial == null)
        {
            Debug.LogWarning(
                "No hay Highlight Material asignado en PlayerInteractor."
            );
            return;
        }

        MonoBehaviour interactableObject =
            currentInteractable as MonoBehaviour;

        if (interactableObject == null)
            return;

        // Busca todos los Renderers del objeto y sus hijos.
        currentRenderers =
            interactableObject.GetComponentsInChildren<Renderer>(true);

        if (currentRenderers == null || currentRenderers.Length == 0)
        {
            Debug.LogWarning(
                "El objeto detectado no tiene ningún Renderer: " +
                interactableObject.gameObject.name
            );

            return;
        }

        originalMaterials = new Material[currentRenderers.Length][];

        for (int i = 0; i < currentRenderers.Length; i++)
        {
            Renderer renderer = currentRenderers[i];

            if (renderer == null)
                continue;

            // Guardamos todos los materiales originales.
            originalMaterials[i] = renderer.materials;

            // Creamos una matriz con el material de resaltado
            // para todos los slots del Renderer.
            Material[] highlightMaterials =
                new Material[renderer.materials.Length];

            for (int j = 0; j < highlightMaterials.Length; j++)
            {
                highlightMaterials[j] = highlightMaterial;
            }

            renderer.materials = highlightMaterials;
        }

        Debug.Log(
            "Resaltado aplicado a " +
            currentRenderers.Length +
            " Renderer(s)."
        );
    }

    private void RemoveHighlight()
    {
        if (currentRenderers == null || originalMaterials == null)
            return;

        for (int i = 0; i < currentRenderers.Length; i++)
        {
            if (currentRenderers[i] == null)
                continue;

            if (originalMaterials[i] == null)
                continue;

            currentRenderers[i].materials = originalMaterials[i];
        }

        currentRenderers = null;
        originalMaterials = null;
    }

    private void CheckMobileAction()
    {
        if (mobileInputManager == null)
            return;

        bool actionPressed =
            mobileInputManager.IsActionPressed(playerId);

        bool actionJustPressed =
            actionPressed && !previousActionPressed;

        if (actionJustPressed)
        {
            TryInteract();
        }

        previousActionPressed = actionPressed;
    }

    private void CheckKeyboardAction()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        if (currentInteractable == null)
            return;

        if (currentCollider == null)
            return;

        if (playerIdentity == null)
        {
            Debug.LogWarning(
                "PlayerInteractor no tiene PlayerIdentity asignado."
            );

            return;
        }

        if (playerCamera == null)
            return;

        Vector3 closestPoint =
            currentCollider.ClosestPoint(playerCamera.transform.position);

        float distance = Vector3.Distance(
            playerCamera.transform.position,
            closestPoint
        );

        if (distance > interactionDistance)
        {
            Debug.Log(
                "El objeto está demasiado lejos para interactuar."
            );

            return;
        }

        currentInteractable.Interact(playerIdentity);
    }

    public bool HasTarget()
    {
        return currentInteractable != null;
    }

    public bool CanInteract()
    {
        if (currentInteractable == null)
            return false;

        if (currentCollider == null)
            return false;

        if (playerCamera == null)
            return false;

        Vector3 closestPoint =
            currentCollider.ClosestPoint(playerCamera.transform.position);

        float distance = Vector3.Distance(
            playerCamera.transform.position,
            closestPoint
        );

        return distance <= interactionDistance;
    }

    private void OnDisable()
    {
        RemoveHighlight();
    }
}