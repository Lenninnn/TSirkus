using UnityEngine;

public class PlayerStateHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private PlayerStateController stateController;

    [SerializeField]
    private GameObject capturedOverlay;

    [SerializeField]
    private Canvas playerCanvas;

    [SerializeField]
    private Camera playerCamera;


    // =====================================================
    // AWAKE
    // =====================================================

    private void Awake()
    {
        // Buscar PlayerStateController del jugador.
        if (stateController == null)
        {
            stateController =
                GetComponentInParent<PlayerStateController>();
        }


        // Buscar Canvas.
        if (playerCanvas == null)
        {
            playerCanvas =
                GetComponent<Canvas>();
        }


        // Buscar cámara de ESTE jugador.
        if (playerCamera == null)
        {
            PlayerStateController player =
                GetComponentInParent<PlayerStateController>();

            if (player != null)
            {
                playerCamera =
                    player.GetComponentInChildren<Camera>(true);
            }
        }


        // Buscar overlay automáticamente.
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


        // El Canvas será Overlay.
        if (playerCanvas != null)
        {
            playerCanvas.renderMode =
                RenderMode.ScreenSpaceOverlay;

            playerCanvas.sortingOrder = 100;
        }
    }


    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        ConfigurePlayerViewport();

        if (stateController != null)
        {
            UpdateHUD(
                stateController.CurrentState
            );
        }
    }


    // =====================================================
    // EVENTS
    // =====================================================

    private void OnEnable()
    {
        if (stateController != null)
        {
            stateController.StateChanged +=
                OnStateChanged;
        }
    }


    private void OnDisable()
    {
        if (stateController != null)
        {
            stateController.StateChanged -=
                OnStateChanged;
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


    // =====================================================
    // CONFIGURAR CUADRANTE
    // =====================================================

    private void ConfigurePlayerViewport()
    {
        if (playerCamera == null)
        {
            Debug.LogWarning(
                $"{transform.root.name}: " +
                "No se encontró PlayerCamera."
            );

            return;
        }

        if (capturedOverlay == null)
        {
            Debug.LogWarning(
                $"{transform.root.name}: " +
                "No se encontró CapturedOverlay."
            );

            return;
        }


        RectTransform overlayRect =
            capturedOverlay.GetComponent<RectTransform>();

        if (overlayRect == null)
            return;


        Rect cameraRect =
            playerCamera.rect;


        // Usamos exactamente el mismo cuadrante
        // que utiliza la cámara de este jugador.

        overlayRect.anchorMin =
            new Vector2(
                cameraRect.xMin,
                cameraRect.yMin
            );

        overlayRect.anchorMax =
            new Vector2(
                cameraRect.xMax,
                cameraRect.yMax
            );


        overlayRect.offsetMin =
            Vector2.zero;

        overlayRect.offsetMax =
            Vector2.zero;


        overlayRect.localScale =
            Vector3.one;


        Debug.Log(
            $"HUD configurado → {transform.root.name} | " +
            $"Viewport: {cameraRect}"
        );
    }


    // =====================================================
    // ACTUALIZAR HUD
    // =====================================================

    private void UpdateHUD(
        PlayerState state
    )
    {
        if (capturedOverlay == null)
            return;


        bool isCaptured =
            state == PlayerState.Captured;


        capturedOverlay.SetActive(
            isCaptured
        );


        Debug.LogWarning(
            $"HUD CAPTURADO | {transform.root.name}: " +
            $"{isCaptured}"
        );
    }


    // =====================================================
    // TEST
    // =====================================================

    [ContextMenu("TEST → Mostrar CapturedOverlay")]
    private void TestShowCaptured()
    {
        if (capturedOverlay != null)
        {
            capturedOverlay.SetActive(true);
        }
    }


    [ContextMenu("TEST → Ocultar CapturedOverlay")]
    private void TestHideCaptured()
    {
        if (capturedOverlay != null)
        {
            capturedOverlay.SetActive(false);
        }
    }
}