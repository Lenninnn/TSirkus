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
    // FLASHLIGHT
    // =============================================================

    [Header("Flashlight")]
    [SerializeField] private FlashlightController flashlightController;

    [SerializeField] private Light flashlightLight;


    // =============================================================
    // PLAYER CAMERA
    // =============================================================

    [Header("Player Camera")]
    [SerializeField] private Camera playerCamera;


    // =============================================================
    // DEBUG
    // =============================================================

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    [SerializeField] private bool drawDebugRay = true;

    [SerializeField] private float debugInterval = 0.5f;


    // =============================================================
    // STATE
    // =============================================================

    private float debugTimer;

    private HiddenWord currentWord;


    // =============================================================
    // UNITY
    // =============================================================

    private void Awake()
    {
        // ---------------------------------------------------------
        // FLASHLIGHT CONTROLLER
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
        // LIGHT
        // ---------------------------------------------------------

        if (flashlightLight == null)
        {
            flashlightLight =
                GetComponentInChildren<Light>();
        }


        // ---------------------------------------------------------
        // PLAYER CAMERA
        // ---------------------------------------------------------

        FindPlayerCamera();


        // ---------------------------------------------------------
        // VALIDATION
        // ---------------------------------------------------------

        if (flashlightController == null)
        {
            Debug.LogError(
                $"🔤🔦❌ [WORD DETECTOR] " +
                $"{gameObject.name} no encontró FlashlightController."
            );
        }
    }


    // =============================================================
    // FIND PLAYER CAMERA
    // =============================================================

    private void FindPlayerCamera()
    {
        if (playerCamera != null)
        {
            return;
        }


        Transform playerRoot =
            transform.root;

        if (playerRoot != null)
        {
            Camera[] cameras =
                playerRoot.GetComponentsInChildren<Camera>(true);

            if (cameras != null && cameras.Length > 0)
            {
                for (int i = 0; i < cameras.Length; i++)
                {
                    Camera candidate =
                        cameras[i];

                    if (candidate == null)
                    {
                        continue;
                    }

                    if (candidate.gameObject == gameObject)
                    {
                        continue;
                    }

                    playerCamera =
                        candidate;

                    break;
                }
            }
        }


        if (playerCamera == null)
        {
            PlayerIdentity identity =
                GetComponentInParent<PlayerIdentity>();

            if (identity != null)
            {
                Camera[] allCameras =
                    FindObjectsOfType<Camera>(true);

                for (int i = 0; i < allCameras.Length; i++)
                {
                    Camera candidate =
                        allCameras[i];

                    if (candidate == null)
                    {
                        continue;
                    }

                    PlayerIdentity cameraIdentity =
                        candidate.GetComponentInParent<PlayerIdentity>();

                    if (cameraIdentity == null)
                    {
                        continue;
                    }

                    if (
                        cameraIdentity.PlayerId ==
                        identity.PlayerId
                    )
                    {
                        playerCamera =
                            candidate;

                        break;
                    }
                }
            }
        }


        if (playerCamera == null)
        {
            Debug.LogWarning(
                $"🔤🔦⚠️ [WORD DETECTOR] " +
                $"{gameObject.name} no encontró una cámara " +
                $"perteneciente al jugador. " +
                $"Puedes asignarla manualmente en Player Camera."
            );
        }
        else
        {
            Debug.Log(
                $"🔤🔦📷 [WORD DETECTOR] " +
                $"{gameObject.name} -> cámara privada: " +
                $"{playerCamera.name}"
            );
        }
    }


    // =============================================================
    // UPDATE
    // =============================================================

    private void Update()
    {
        if (flashlightController == null)
        {
            return;
        }


        if (
            !flashlightController
                .IsFlashlightActive()
        )
        {
            return;
        }


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


        // ---------------------------------------------------------
        // DEBUG RAY
        // ---------------------------------------------------------

        if (drawDebugRay)
        {
            Debug.DrawRay(
                origin,
                direction * detectionDistance,
                Color.red
            );
        }


        // ---------------------------------------------------------
        // SPHERE CAST
        // ---------------------------------------------------------

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


        if (!detected)
        {
            return;
        }


        // ---------------------------------------------------------
        // BUSCAR HIDDEN WORD
        // ---------------------------------------------------------

        HiddenWord hiddenWord =
            hit.collider
                .GetComponentInParent<HiddenWord>();


        if (hiddenWord == null)
        {
            return;
        }


        // ---------------------------------------------------------
        // YA REVELADA
        // ---------------------------------------------------------

        if (hiddenWord.IsRevealed)
        {
            return;
        }


        // ---------------------------------------------------------
        // PLAYER ID
        // ---------------------------------------------------------

        int playerId =
            flashlightController.GetPlayerId();


        if (
            playerId < 1 ||
            playerId > 4
        )
        {
            DebugLog(
                $"🔤🔦🚫 [WORD DETECTOR] " +
                $"La linterna no tiene Player ID válido."
            );

            return;
        }


        // ---------------------------------------------------------
        // DEBUG
        // ---------------------------------------------------------

        currentWord =
            hiddenWord;


        DebugLog(
            $"🔤🔦 [P{playerId}] " +
            $"Apuntando a '{hiddenWord.Word}' " +
            $"| pertenece a P{hiddenWord.AssignedPlayerId}"
        );


        // ---------------------------------------------------------
        // ASEGURAR CÁMARA
        // ---------------------------------------------------------

        if (playerCamera == null)
        {
            FindPlayerCamera();
        }


        // ---------------------------------------------------------
        // INTENTAR REVELAR
        // ---------------------------------------------------------

        bool revealed =
            hiddenWord.TryRevealForPlayer(
                playerId,
                playerCamera
            );


        if (revealed)
        {
            Debug.Log(
                $"🔤✨ [WORD DETECTOR] " +
                $"PLAYER {playerId} reveló " +
                $"'{hiddenWord.Word}'"
            );
        }
        else
        {
            DebugLog(
                $"🔤🚫 [WORD DETECTOR] " +
                $"PLAYER {playerId} NO puede revelar " +
                $"'{hiddenWord.Word}' " +
                $"| pertenece a P{hiddenWord.AssignedPlayerId}"
            );
        }
    }


    // =============================================================
    // DEBUG LOG
    // =============================================================

    private void DebugLog(
        string message
    )
    {
        if (!debugLogs)
        {
            return;
        }


        debugTimer +=
            Time.deltaTime;


        if (
            debugTimer >=
            debugInterval
        )
        {
            Debug.Log(
                message
            );


            debugTimer =
                0f;
        }
    }


    // =============================================================
    // CONTEXT MENU
    // =============================================================

    [ContextMenu("Detect Word")]
    private void DebugDetectWord()
    {
        if (flashlightController == null)
        {
            Awake();
        }

        DetectWord();
    }
}