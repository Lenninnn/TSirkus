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

    private IInteractable currentInteractable;
    private IHighlightable currentHighlightable;
    private Collider currentCollider;

    private bool previousActionPressed;

    // ==========================================
    // PAYASO
    // ==========================================

    private ClownAI detectedClown;
    private ClownEncounterManager clownEncounterManager;

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

        clownEncounterManager = FindObjectOfType<ClownEncounterManager>();
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
        IHighlightable detectedHighlightable = null;
        Collider detectedCollider = null;

        // ==========================================
        // DETECTAR PAYASO
        // ==========================================

        ClownAI newDetectedClown = null;

        if (hitSomething)
        {
            detectedCollider = hit.collider;

            detectedInteractable =
                hit.collider.GetComponentInParent<IInteractable>();

            detectedHighlightable =
                hit.collider.GetComponentInParent<IHighlightable>();

            newDetectedClown =
                hit.collider.GetComponentInParent<ClownAI>();
        }

        detectedClown = newDetectedClown;

        ReportClownLooking();

        // ==========================================
        // INTERACCIÓN NORMAL
        // ==========================================

        if (detectedInteractable != currentInteractable)
        {
            if (currentHighlightable != null)
            {
                currentHighlightable.SetHighlight(false);
            }

            currentInteractable = detectedInteractable;
            currentHighlightable = detectedHighlightable;
            currentCollider = detectedCollider;

            if (currentHighlightable != null)
            {
                currentHighlightable.SetHighlight(true);
            }

            if (currentInteractable != null)
            {
                MonoBehaviour interactableObject =
                    currentInteractable as MonoBehaviour;

                if (interactableObject != null)
                {
                    Debug.Log(
                        "Raycast detectó: " +
                        interactableObject.gameObject.name
                    );
                }
            }
        }
        else
        {
            currentCollider = detectedCollider;
        }
    }

    // ==========================================
    // AVISAR AL MANAGER SI ESTÁ MIRANDO AL PAYASO
    // ==========================================

    private void ReportClownLooking()
    {
        if (clownEncounterManager == null)
            return;

        if (playerIdentity == null)
            return;

        if (!clownEncounterManager.IsEncounterActive)
            return;

        bool isLookingAtClown =
            detectedClown != null &&
            clownEncounterManager.CurrentClown == detectedClown.gameObject;

        clownEncounterManager.ReportPlayerLooking(
            playerIdentity,
            isLookingAtClown
        );
    }

    // ==========================================
    // INPUT MÓVIL
    // ==========================================

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

    // ==========================================
    // INPUT TECLADO
    // ==========================================

    private void CheckKeyboardAction()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryInteract();
        }
    }

    // ==========================================
    // INTERACCIÓN
    // ==========================================

    private void TryInteract()
    {
        if (currentInteractable == null)
        {
            Debug.Log(
                "No hay objeto interactuable seleccionado."
            );

            return;
        }

        if (currentCollider == null)
        {
            Debug.Log(
                "No hay collider seleccionado."
            );

            return;
        }

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
            currentCollider.ClosestPoint(
                playerCamera.transform.position
            );

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

    // ==========================================
    // MÉTODOS PÚBLICOS
    // ==========================================

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
            currentCollider.ClosestPoint(
                playerCamera.transform.position
            );

        float distance = Vector3.Distance(
            playerCamera.transform.position,
            closestPoint
        );

        return distance <= interactionDistance;
    }

    // ==========================================
    // DESACTIVAR
    // ==========================================

    private void OnDisable()
    {
        if (currentHighlightable != null)
        {
            currentHighlightable.SetHighlight(false);
        }

        if (clownEncounterManager != null &&
            playerIdentity != null &&
            clownEncounterManager.IsEncounterActive)
        {
            clownEncounterManager.ReportPlayerLooking(
                playerIdentity,
                false
            );
        }

        detectedClown = null;

        currentInteractable = null;
        currentHighlightable = null;
        currentCollider = null;
    }
}