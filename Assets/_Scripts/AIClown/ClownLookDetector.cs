using UnityEngine;

public class ClownLookDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerIdentity playerIdentity;
    [SerializeField] private ClownEncounterManager clownEncounterManager;

    [Header("Raycast")]
    [Tooltip("Distancia máxima a la que el jugador puede detectar al payaso.")]
    [SerializeField] private float lookDistance = 15f;

    [Header("Debug")]
    [SerializeField] private bool showDebugRay = true;
    [SerializeField] private bool showDebugMessages = true;

    private ClownAI detectedClown;

    private bool lastLookingState;

    private float lookTimer;

    // ============================================================
    // AWAKE
    // ============================================================

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera =
                GetComponentInChildren<Camera>();
        }

        if (playerIdentity == null)
        {
            playerIdentity =
                GetComponent<PlayerIdentity>();
        }

        if (clownEncounterManager == null)
        {
            clownEncounterManager =
                FindObjectOfType<ClownEncounterManager>();
        }

        Debug.Log(
            $"[CLOWN TEST] {gameObject.name} inicializado | " +
            $"Player: {GetPlayerId()} | " +
            $"Camera: {(playerCamera != null ? playerCamera.name : "NULL")} | " +
            $"Manager: {(clownEncounterManager != null ? "OK" : "NULL")}"
        );
    }

    // ============================================================
    // UPDATE
    // ============================================================

    private void Update()
    {
        DetectClown();
    }

    // ============================================================
    // DETECCIÓN
    // ============================================================

    private void DetectClown()
    {
        // --------------------------------------------------------
        // CAMERA
        // --------------------------------------------------------

        if (playerCamera == null)
        {
            Debug.LogError(
                $"[CLOWN TEST] Player {GetPlayerId()} → " +
                $"CAMERA NULL"
            );

            ResetDetection();

            return;
        }

        // --------------------------------------------------------
        // PLAYER IDENTITY
        // --------------------------------------------------------

        if (playerIdentity == null)
        {
            Debug.LogError(
                $"[CLOWN TEST] {gameObject.name} → " +
                $"PLAYER IDENTITY NULL"
            );

            ResetDetection();

            return;
        }

        // --------------------------------------------------------
        // RAYCAST
        //
        // IMPORTANTE:
        // Usamos ~0 temporalmente.
        //
        // Esto significa:
        // TODAS LAS CAPAS.
        //
        // Así descartamos que el problema sea el LayerMask.
        // --------------------------------------------------------

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        bool hitSomething = Physics.Raycast(
            ray,
            out RaycastHit hit,
            lookDistance,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        // --------------------------------------------------------
        // DEBUG RAY
        // --------------------------------------------------------

        if (showDebugRay)
        {
            Debug.DrawRay(
                ray.origin,
                ray.direction * lookDistance,
                hitSomething
                    ? Color.green
                    : Color.red
            );
        }

        // --------------------------------------------------------
        // NO GOLPEÓ NADA
        // --------------------------------------------------------

        if (!hitSomething)
        {
            if (showDebugMessages)
            {
                Debug.Log(
                    $"[CLOWN TEST] Player {GetPlayerId()} → " +
                    $"NO está golpeando ningún collider."
                );
            }

            ResetDetection();

            return;
        }

        // --------------------------------------------------------
        // GOLPEÓ ALGO
        // --------------------------------------------------------

        if (showDebugMessages)
        {
            Debug.Log(
                $"[CLOWN TEST] Player {GetPlayerId()} → " +
                $"GOLPEÓ: {hit.collider.gameObject.name}"
            );

            Debug.Log(
                $"[CLOWN TEST] Player {GetPlayerId()} → " +
                $"ROOT: {hit.collider.transform.root.gameObject.name}"
            );

            Debug.Log(
                $"[CLOWN TEST] Player {GetPlayerId()} → " +
                $"DISTANCIA: {hit.distance:F2}m"
            );

            Debug.Log(
                $"[CLOWN TEST] Player {GetPlayerId()} → " +
                $"LAYER: {LayerMask.LayerToName(hit.collider.gameObject.layer)}"
            );
        }

        // --------------------------------------------------------
        // BUSCAR CLOWNAI
        // --------------------------------------------------------

        ClownAI clown =
            hit.collider.GetComponentInParent<ClownAI>();

        // --------------------------------------------------------
        // NO ENCONTRÓ CLOWNAI
        // --------------------------------------------------------

        if (clown == null)
        {
            if (showDebugMessages)
            {
                Debug.Log(
                    $"[CLOWN TEST] Player {GetPlayerId()} → " +
                    $"EL OBJETO GOLPEADO NO TIENE CLOWNAI."
                );
            }

            ResetDetection();

            return;
        }

        // --------------------------------------------------------
        // ENCONTRÓ CLOWNAI
        // --------------------------------------------------------

        if (showDebugMessages)
        {
            Debug.Log(
                $"[CLOWN TEST] ======================================"
            );

            Debug.Log(
                $"[CLOWN TEST] PLAYER {GetPlayerId()} → " +
                $"¡¡¡ENCONTRÓ AL CLOWN!!!"
            );

            Debug.Log(
                $"[CLOWN TEST] ClownAI: " +
                $"{clown.gameObject.name}"
            );

            Debug.Log(
                $"[CLOWN TEST] Clown Root: " +
                $"{clown.transform.root.gameObject.name}"
            );

            Debug.Log(
                $"[CLOWN TEST] ======================================"
            );
        }

        // --------------------------------------------------------
        // GUARDAR CLOWN
        // --------------------------------------------------------

        detectedClown = clown;

        // --------------------------------------------------------
        // ACUMULAR TIEMPO DE MIRADA
        // --------------------------------------------------------

        lookTimer += Time.deltaTime;

        // --------------------------------------------------------
        // PRIMER MOMENTO MIRANDO
        // --------------------------------------------------------

        if (!lastLookingState)
        {
            if (showDebugMessages)
            {
                Debug.Log(
                    $"[CLOWN LOOK] PLAYER {GetPlayerId()} " +
                    $"ESTÁ MIRANDO AL PAYASO."
                );
            }
        }

        lastLookingState = true;

        // --------------------------------------------------------
        // MOSTRAR TIEMPO
        // --------------------------------------------------------

        if (showDebugMessages)
        {
            Debug.Log(
                $"[CLOWN LOOK] Player {GetPlayerId()} → " +
                $"Tiempo mirando: {lookTimer:F2}s"
            );
        }

        // --------------------------------------------------------
        // CONFIRMAR AL MANAGER
        //
        // Después de 0.4 segundos continuos.
        // --------------------------------------------------------

        if (lookTimer >= 0.4f)
        {
            ReportLooking(true);
        }
    }

    // ============================================================
    // REPORTAR AL MANAGER
    // ============================================================

    private void ReportLooking(bool isLookingAtClown)
    {
        if (clownEncounterManager == null)
        {
            Debug.LogError(
                $"[CLOWN TEST] Player {GetPlayerId()} → " +
                $"NO HAY ClownEncounterManager."
            );

            return;
        }

        if (playerIdentity == null)
        {
            Debug.LogError(
                $"[CLOWN TEST] Player → " +
                $"NO HAY PlayerIdentity."
            );

            return;
        }

        // --------------------------------------------------------
        // COMPROBAR ENCUENTRO
        // --------------------------------------------------------

        if (!clownEncounterManager.IsEncounterActive)
        {
            if (showDebugMessages)
            {
                Debug.Log(
                    $"[CLOWN TEST] Player {GetPlayerId()} → " +
                    $"EL ENCUENTRO NO ESTÁ ACTIVO."
                );
            }

            return;
        }

        // --------------------------------------------------------
        // ENVIAR AL MANAGER
        // --------------------------------------------------------

        clownEncounterManager.ReportPlayerLooking(
            playerIdentity,
            isLookingAtClown
        );

        if (isLookingAtClown && showDebugMessages)
        {
            Debug.Log(
                $"[CLOWN LOOK] ======================================"
            );

            Debug.Log(
                $"[CLOWN LOOK] Player {GetPlayerId()} → " +
                $"VIO AL PAYASO"
            );

            Debug.Log(
                $"[CLOWN LOOK] ENVIADO AL " +
                $"ClownEncounterManager"
            );

            Debug.Log(
                $"[CLOWN LOOK] ======================================"
            );
        }
    }

    // ============================================================
    // RESET
    // ============================================================

    private void ResetDetection()
    {
        if (lastLookingState && showDebugMessages)
        {
            Debug.Log(
                $"[CLOWN LOOK] Player {GetPlayerId()} → " +
                $"DEJÓ DE MIRAR AL PAYASO."
            );
        }

        // Si estaba mirando y deja de mirar,
        // avisamos al manager.
        if (lastLookingState)
        {
            ReportLooking(false);
        }

        detectedClown = null;

        lastLookingState = false;

        lookTimer = 0f;
    }

    // ============================================================
    // PLAYER ID
    // ============================================================

    private int GetPlayerId()
    {
        if (playerIdentity != null)
        {
            return playerIdentity.PlayerId;
        }

        return -1;
    }

    // ============================================================
    // GETTERS
    // ============================================================

    public bool IsLookingAtClown()
    {
        return detectedClown != null;
    }

    public ClownAI GetDetectedClown()
    {
        return detectedClown;
    }

    // ============================================================
    // DISABLE
    // ============================================================

    private void OnDisable()
    {
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
        lastLookingState = false;
        lookTimer = 0f;
    }
}