using UnityEngine;

public class PlayerStateHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private PlayerStateController stateController;

    [SerializeField]
    private GameObject capturedOverlay;

    private void Awake()
    {
        if (stateController == null)
        {
            stateController =
                GetComponentInParent<PlayerStateController>();
        }

        if (capturedOverlay == null)
        {
            Transform overlay =
                transform.Find("CapturedOverlay");

            if (overlay != null)
            {
                capturedOverlay =
                    overlay.gameObject;
            }
        }
    }

    private void OnEnable()
    {
        if (stateController != null)
        {
            stateController.StateChanged += OnStateChanged;
        }
    }

    private void Start()
    {
        if (stateController != null)
        {
            UpdateHUD(stateController.CurrentState);
        }
        else
        {
            Debug.LogWarning(
                $"{gameObject.name}: no encontró PlayerStateController."
            );
        }
    }

    private void OnDisable()
    {
        if (stateController != null)
        {
            stateController.StateChanged -= OnStateChanged;
        }
    }

    private void OnStateChanged(
        PlayerState previousState,
        PlayerState newState
    )
    {
        Debug.LogWarning(
            $"HUD | {transform.root.name}: " +
            $"{previousState} → {newState}"
        );

        UpdateHUD(newState);
    }

    private void UpdateHUD(PlayerState state)
    {
        if (capturedOverlay == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: CapturedOverlay no está asignado."
            );

            return;
        }

        bool showCaptured =
            state == PlayerState.Captured;

        capturedOverlay.SetActive(showCaptured);

        Debug.LogWarning(
            $"HUD CAPTURADO | {transform.root.name}: " +
            $"{showCaptured}"
        );
    }
}