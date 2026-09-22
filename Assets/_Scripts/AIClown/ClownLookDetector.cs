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

    [Tooltip("Capas que puede detectar el Raycast.")]
    [SerializeField] private LayerMask clownLayers = ~0;

    [Header("Debug")]
    [SerializeField] private bool showDebugRay = true;
    [SerializeField] private bool showDebugMessages = true;

    [Tooltip("Muestra información del estado cada cierto tiempo.")]
    [SerializeField] private bool showStatusMessages = true;

    [Tooltip("Cada cuántos segundos se muestra el estado general.")]
    [SerializeField] private float statusInterval = 1f;

    private ClownAI detectedClown;

    private float statusTimer;

    private bool lastLookingState = false;

    private void Awake()
    {
        Debug.Log(
            $"[CLOWN LOOK] {gameObject.name} -> Awake()"
        );

        // -----------------------------------------------------
        // CÁMARA
        // -----------------------------------------------------

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }

        if (playerCamera != null)
        {
            Debug.Log(
                $"[CLOWN LOOK] {gameObject.name} -> Cámara encontrada: {playerCamera.name}"
            );
        }
        else
        {
            Debug.LogError(
                $"[CLOWN LOOK] {gameObject.name} -> NO SE ENCONTRÓ LA CÁMARA."
            );
        }

        // -----------------------------------------------------
        // PLAYER IDENTITY
        // -----------------------------------------------------

        if (playerIdentity == null)
        {
            playerIdentity = GetComponent<PlayerIdentity>();
        }

        if (playerIdentity != null)
        {
            Debug.Log(
                $"[CLOWN LOOK] {gameObject.name} -> PlayerIdentity encontrado."
            );
        }
        else
        {
            Debug.LogError(
                $"[CLOWN LOOK] {gameObject.name} -> NO SE ENCONTRÓ PlayerIdentity."
            );
        }

        // -----------------------------------------------------
        // MANAGER
        // -----------------------------------------------------

        if (clownEncounterManager == null)
        {
            clownEncounterManager =
                FindObjectOfType<ClownEncounterManager>();
        }

        if (clownEncounterManager != null)
        {
            Debug.Log(
                $"[CLOWN LOOK] {gameObject.name} -> ClownEncounterManager encontrado."
            );
        }
        else
        {
            Debug.LogError(
                $"[CLOWN LOOK] {gameObject.name} -> NO SE ENCONTRÓ ClownEncounterManager."
            );
        }

        // -----------------------------------------------------
        // CONFIGURACIÓN
        // -----------------------------------------------------

        Debug.Log(
            $"[CLOWN LOOK] {gameObject.name} -> " +
            $"Look Distance: {lookDistance} | " +
            $"LayerMask: {clownLayers.value}"
        );
    }

    private void Update()
    {
        DetectClown();

        ShowStatus();
    }

    // =========================================================
    // DETECTAR PAYASO
    // =========================================================

    private void DetectClown()
    {
        // -----------------------------------------------------
        // COMPROBAR CÁMARA
        // -----------------------------------------------------

        if (playerCamera == null)
        {
            ReportLooking(false);
            return;
        }

        // -----------------------------------------------------
        // COMPROBAR PLAYER IDENTITY
        // -----------------------------------------------------

        if (playerIdentity == null)
        {
            ReportLooking(false);
            return;
        }

        // -----------------------------------------------------
        // COMPROBAR MANAGER
        // -----------------------------------------------------

        if (clownEncounterManager == null)
        {
            ReportLooking(false);
            return;
        }

        // -----------------------------------------------------
        // COMPROBAR ENCUENTRO
        // -----------------------------------------------------

        if (!clownEncounterManager.IsEncounterActive)
        {
            if (detectedClown != null)
            {
                Debug.Log(
                    $"[CLOWN LOOK] {gameObject.name} -> " +
                    $"El encuentro terminó. Dejando de detectar."
                );
            }

            detectedClown = null;

            DrawDebugRay(false);

            return;
        }

        // -----------------------------------------------------
        // CREAR RAYO
        // -----------------------------------------------------

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        // -----------------------------------------------------
        // RAYCAST
        // -----------------------------------------------------

        bool hitSomething = Physics.Raycast(
            ray,
            out RaycastHit hit,
            lookDistance,
            clownLayers,
            QueryTriggerInteraction.Ignore
        );

        // -----------------------------------------------------
        // DEBUG RAY
        // -----------------------------------------------------

        DrawDebugRay(hitSomething);

        // -----------------------------------------------------
        // NO GOLPEÓ NADA
        // -----------------------------------------------------

        if (!hitSomething)
        {
            if (lastLookingState)
            {
                Debug.Log(
                    $"[CLOWN LOOK] {gameObject.name} -> " +
                    $"DEJÓ DE MIRAR AL PAYASO: el Raycast no golpeó nada."
                );
            }

            detectedClown = null;

            ReportLooking(false);

            lastLookingState = false;

            return;
        }

        // -----------------------------------------------------
        // GOLPEÓ ALGO
        // -----------------------------------------------------

        if (showDebugMessages)
        {
            Debug.Log(
                $"[CLOWN LOOK] {gameObject.name} -> " +
                $"Raycast golpeó: {hit.collider.name} " +
                $"| Distancia: {hit.distance:F2}m"
            );
        }

        // -----------------------------------------------------
        // BUSCAR CLOWNAI
        // -----------------------------------------------------

        ClownAI clown =
            hit.collider.GetComponentInParent<ClownAI>();

        // -----------------------------------------------------
        // NO ES EL PAYASO
        // -----------------------------------------------------

        if (clown == null)
        {
            if (showDebugMessages)
            {
                Debug.Log(
                    $"[CLOWN LOOK] {gameObject.name} -> " +
                    $"El objeto golpeado NO tiene ClownAI."
                );
            }

            detectedClown = null;

            ReportLooking(false);

            lastLookingState = false;

            return;
        }

        // -----------------------------------------------------
        // ENCONTRÓ UN CLOWNAI
        // -----------------------------------------------------

        if (showDebugMessages)
        {
            Debug.Log(
                $"[CLOWN LOOK] {gameObject.name} -> " +
                $"ENCONTRÓ ClownAI: {clown.gameObject.name}"
            );
        }

        // -----------------------------------------------------
        // COMPROBAR QUE SEA EL PAYASO ACTUAL
        // -----------------------------------------------------

        if (
            clownEncounterManager.CurrentClown !=
            clown.gameObject
        )
        {
            if (showDebugMessages)
            {
                Debug.LogWarning(
                    $"[CLOWN LOOK] {gameObject.name} -> " +
                    $"Encontró un ClownAI, PERO NO ES EL PAYASO DEL ENCUENTRO ACTUAL."
                );
            }

            detectedClown = null;

            ReportLooking(false);

            lastLookingState = false;

            return;
        }

        // -----------------------------------------------------
        // PAYASO CORRECTO DETECTADO
        // -----------------------------------------------------

        detectedClown = clown;

        ReportLooking(true);

        // -----------------------------------------------------
        // SOLO MOSTRAR MENSAJE CUANDO CAMBIA A TRUE
        // -----------------------------------------------------

        if (!lastLookingState)
        {
            Debug.Log(
                $"[CLOWN LOOK] {gameObject.name} -> " +
                $"======================================"
            );

            Debug.Log(
                $"[CLOWN LOOK] {gameObject.name} -> " +
                $"¡ESTÁ MIRANDO AL PAYASO!"
            );

            Debug.Log(
                $"[CLOWN LOOK] {gameObject.name} -> " +
                $"Payaso: {clown.gameObject.name}"
            );

            Debug.Log(
                $"[CLOWN LOOK] {gameObject.name} -> " +
                $"Distancia: {hit.distance:F2}m"
            );

            Debug.Log(
                $"[CLOWN LOOK] {gameObject.name} -> " +
                $"REPORTANDO TRUE AL MANAGER"
            );

            Debug.Log(
                $"[CLOWN LOOK] {gameObject.name} -> " +
                $"======================================"
            );
        }

        lastLookingState = true;
    }

    // =========================================================
    // REPORTAR AL MANAGER
    // =========================================================

    private void ReportLooking(
        bool isLookingAtClown
    )
    {
        if (clownEncounterManager == null)
        {
            return;
        }

        if (playerIdentity == null)
        {
            return;
        }

        if (!clownEncounterManager.IsEncounterActive)
        {
            return;
        }

        clownEncounterManager.ReportPlayerLooking(
            playerIdentity,
            isLookingAtClown
        );
    }

    // =========================================================
    // DEBUG STATUS
    // =========================================================

    private void ShowStatus()
    {
        if (!showStatusMessages)
        {
            return;
        }

        statusTimer += Time.deltaTime;

        if (statusTimer < statusInterval)
        {
            return;
        }

        statusTimer = 0f;

        string cameraStatus =
            playerCamera != null
                ? playerCamera.name
                : "NULL";

        string identityStatus =
            playerIdentity != null
                ? "OK"
                : "NULL";

        string managerStatus =
            clownEncounterManager != null
                ? "OK"
                : "NULL";

        string encounterStatus =
            clownEncounterManager != null
                ? clownEncounterManager.IsEncounterActive.ToString()
                : "UNKNOWN";

        string clownStatus =
            detectedClown != null
                ? detectedClown.gameObject.name
                : "NINGUNO";

        Debug.Log(
            $"[CLOWN STATUS] {gameObject.name} | " +
            $"Camera: {cameraStatus} | " +
            $"Identity: {identityStatus} | " +
            $"Manager: {managerStatus} | " +
            $"Encounter: {encounterStatus} | " +
            $"Clown detectado: {clownStatus}"
        );
    }

    // =========================================================
    // DEBUG RAY
    // =========================================================

    private void DrawDebugRay(
        bool hitSomething
    )
    {
        if (!showDebugRay)
        {
            return;
        }

        if (playerCamera == null)
        {
            return;
        }

        Debug.DrawRay(
            playerCamera.transform.position,
            playerCamera.transform.forward * lookDistance,
            hitSomething ? Color.green : Color.red
        );
    }

    // =========================================================
    // GETTERS
    // =========================================================

    public bool IsLookingAtClown()
    {
        return detectedClown != null;
    }

    public ClownAI GetDetectedClown()
    {
        return detectedClown;
    }

    // =========================================================
    // DESACTIVAR
    // =========================================================

    private void OnDisable()
    {
        if (
            clownEncounterManager != null &&
            playerIdentity != null &&
            clownEncounterManager.IsEncounterActive
        )
        {
            clownEncounterManager.ReportPlayerLooking(
                playerIdentity,
                false
            );
        }

        detectedClown = null;
        lastLookingState = false;
    }
}


