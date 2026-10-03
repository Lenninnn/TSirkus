using UnityEngine;

public class FlashlightWordDetector : MonoBehaviour
{
    // =============================================================
    // DETECTION
    // =============================================================

    [Header("Detection")]
    [SerializeField] private float detectionDistance = 15f;

    [SerializeField] private float detectionRadius = 0.15f;

    [SerializeField] private LayerMask detectionLayers = ~0;

    // =============================================================
    // REQUIRE FLASHLIGHT
    // =============================================================

    [Header("Flashlight")]
    [SerializeField] private FlashlightController flashlightController;

    [SerializeField] private Light flashlightLight;

    // =============================================================
    // DEBUG
    // =============================================================

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    [SerializeField] private bool drawDebugRay = true;

    [SerializeField] private float debugInterval = 0.5f;

    private float debugTimer;

    private HiddenWord currentWord;

    // =============================================================
    // UNITY
    // =============================================================

    private void Awake()
    {
        Debug.Log(
            $"🔎 [WORD DETECTOR] Awake -> {gameObject.name}"
        );

        // ---------------------------------------------------------
        // BUSCAR FLASHLIGHT CONTROLLER
        // ---------------------------------------------------------

        if (flashlightController == null)
        {
            flashlightController =
                GetComponent<FlashlightController>();
        }

        if (flashlightController == null)
        {
            flashlightController =
                GetComponentInParent<FlashlightController>();
        }

        // ---------------------------------------------------------
        // BUSCAR LIGHT
        // ---------------------------------------------------------

        if (flashlightLight == null)
        {
            flashlightLight =
                GetComponentInChildren<Light>();
        }

        // ---------------------------------------------------------
        // DEBUG
        // ---------------------------------------------------------

        if (flashlightController == null)
        {
            Debug.LogWarning(
                $"🔎 [WORD DETECTOR] No se encontró " +
                $"FlashlightController en {gameObject.name}."
            );
        }

        if (flashlightLight == null)
        {
            Debug.LogWarning(
                $"🔎 [WORD DETECTOR] No se encontró Light " +
                $"en {gameObject.name}."
            );
        }
    }

    private void Update()
    {
        // ---------------------------------------------------------
        // SI LA LINTERNA NO EXISTE
        // ---------------------------------------------------------

        if (flashlightController == null)
            return;

        // ---------------------------------------------------------
        // SI LA LINTERNA ESTÁ APAGADA
        // ---------------------------------------------------------

        if (!flashlightController.IsFlashlightActive())
            return;

        // ---------------------------------------------------------
        // SI TENEMOS UNA LIGHT Y ESTÁ APAGADA
        // ---------------------------------------------------------

        if (
            flashlightLight != null &&
            !flashlightLight.enabled
        )
        {
            return;
        }

        DetectWord();
    }

    // =============================================================
    // DETECT WORD
    // =============================================================

    private void DetectWord()
    {
        Vector3 origin =
            transform.position;

        Vector3 direction =
            transform.forward;

        RaycastHit hit;

        bool detected =
            Physics.SphereCast(
                origin,
                detectionRadius,
                direction,
                out hit,
                detectionDistance,
                detectionLayers,
                QueryTriggerInteraction.Collide
            );

        // ---------------------------------------------------------
        // DEBUG RAY
        // ---------------------------------------------------------

        if (drawDebugRay)
        {
            Debug.DrawRay(
                origin,
                direction * detectionDistance,
                detected ? Color.green : Color.red
            );
        }

        // ---------------------------------------------------------
        // NOTHING DETECTED
        // ---------------------------------------------------------

        if (!detected)
        {
            DebugLog(
                $"🔎 [WORD DETECTOR] " +
                $"Nada detectado."
            );

            return;
        }

        // ---------------------------------------------------------
        // CHECK HIDDEN WORD
        // ---------------------------------------------------------

        HiddenWord hiddenWord =
            hit.collider.GetComponentInParent<HiddenWord>();

        if (hiddenWord == null)
        {
            DebugLog(
                $"🔎 [WORD DETECTOR] Detectado: " +
                $"{hit.collider.gameObject.name} " +
                $"pero NO es HiddenWord."
            );

            return;
        }

        // ---------------------------------------------------------
        // WORD ALREADY REVEALED
        // ---------------------------------------------------------

        if (hiddenWord.IsRevealed)
        {
            DebugLog(
                $"🔎 [WORD DETECTOR] " +
                $"Palabra ya revelada: {hiddenWord.Word}"
            );

            return;
        }

        // ---------------------------------------------------------
        // PLAYER ID
        // ---------------------------------------------------------

        int playerId =
            flashlightController.GetPlayerId();

        if (playerId < 1 || playerId > 4)
        {
            Debug.LogWarning(
                $"🔎 [WORD DETECTOR] " +
                $"Player ID inválido: {playerId}"
            );

            return;
        }

        // ---------------------------------------------------------
        // INTENTAR REVELAR
        // ---------------------------------------------------------

        bool revealed =
            hiddenWord.TryRevealForPlayer(
                playerId
            );

        // ---------------------------------------------------------
        // RESULTADO
        // ---------------------------------------------------------

        if (revealed)
        {
            if (currentWord != hiddenWord)
            {
                currentWord =
                    hiddenWord;

                Debug.Log(
                    $"🔦🔤 [WORD DETECTOR] " +
                    $"PLAYER {playerId} ENCONTRÓ: " +
                    $"{hiddenWord.Word}"
                );
            }
        }
        else
        {
            DebugLog(
                $"🔎 [WORD DETECTOR] " +
                $"PLAYER {playerId} apuntó a " +
                $"\"{hiddenWord.Word}\" pero no puede reclamarla."
            );
        }
    }

    // =============================================================
    // DEBUG
    // =============================================================

    private void DebugLog(string message)
    {
        if (!debugLogs)
            return;

        debugTimer +=
            Time.deltaTime;

        if (debugTimer >= debugInterval)
        {
            Debug.Log(message);

            debugTimer = 0f;
        }
    }

    // =============================================================
    // PUBLIC CONFIGURATION
    // =============================================================

    public void SetFlashlightController(
        FlashlightController controller
    )
    {
        flashlightController =
            controller;
    }

    public void SetFlashlightLight(
        Light lightSource
    )
    {
        flashlightLight =
            lightSource;
    }

    public void SetDetectionDistance(
        float distance
    )
    {
        if (distance <= 0f)
        {
            Debug.LogWarning(
                "🔎 [WORD DETECTOR] " +
                "Detection Distance debe ser mayor que 0."
            );

            return;
        }

        detectionDistance =
            distance;
    }

    public void SetDetectionRadius(
        float radius
    )
    {
        if (radius < 0f)
        {
            Debug.LogWarning(
                "🔎 [WORD DETECTOR] " +
                "Detection Radius no puede ser negativo."
            );

            return;
        }

        detectionRadius =
            radius;
    }

    // =============================================================
    // CONTEXT MENU
    // =============================================================

    [ContextMenu("Test Detection")]
    private void TestDetection()
    {
        DetectWord();
    }
}