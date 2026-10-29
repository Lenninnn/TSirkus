using UnityEngine;

public class FlashlightWordDetector : MonoBehaviour
{
    // =============================================================
    // DETECTION
    // =============================================================

    // Una palabra se revela cuando queda dentro de la parte brillante del haz (no solo bajo
    // el centro exacto), dentro del alcance de la luz, sin nada delante, durante revealTime.

    [Header("Detection")]
    [Tooltip("Distancia máxima. Nunca más allá del alcance (range) de la luz.")]
    [SerializeField] private float detectionDistance = 15f;

    [Tooltip("Semiángulo (grados) del haz que revela. 5° ≈ 0.6 m a 7 m: hay que apuntar a la palabra (las vecinas están a ~8°), pero no al centímetro.")]
    [SerializeField] private float revealConeAngle = 5f;

    [Tooltip("Distancia máxima (m) entre el eje del haz y la palabra. Con la distancia el cono se estrecha: de lejos hay que apuntar más fino.")]
    [SerializeField] private float revealRadius = 0.5f;

    [Tooltip("Segundos seguidos iluminando la palabra para revelarla (evita revelar al barrer por accidente).")]
    [SerializeField] private float revealTime = 0.25f;

    [Tooltip("Capas que pueden tapar la palabra.")]
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

    private float currentWordLitTime;

    private HiddenWord[] words;

    private readonly RaycastHit[] occlusionHits =
        new RaycastHit[16];


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

        float maxDistance =
            flashlightLight != null
                ? Mathf.Min(detectionDistance, flashlightLight.range)
                : detectionDistance;


        if (drawDebugRay)
        {
            Debug.DrawRay(
                origin,
                direction * maxDistance,
                Color.red
            );
        }


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
        // PALABRA DE ESTE JUGADOR MÁS CENTRADA EN EL HAZ
        // ---------------------------------------------------------

        HiddenWord litWord =
            null;

        float bestAngle =
            revealConeAngle;

        foreach (HiddenWord word in GetWords())
        {
            if (
                word == null ||
                word.IsRevealed ||
                word.AssignedPlayerId != playerId ||
                !word.gameObject.activeInHierarchy
            )
            {
                continue;
            }

            Vector3 toWord =
                WordCenter(word) - origin;

            float distance =
                toWord.magnitude;

            if (distance > maxDistance)
            {
                continue;
            }

            float angle =
                Vector3.Angle(direction, toWord);

            if (
                angle > bestAngle ||
                distance * Mathf.Sin(angle * Mathf.Deg2Rad) > revealRadius ||
                !IsUnobstructed(origin, toWord, distance, word)
            )
            {
                continue;
            }

            litWord =
                word;

            bestAngle =
                angle;
        }


        if (litWord != currentWord)
        {
            currentWord =
                litWord;

            currentWordLitTime =
                0f;
        }

        if (currentWord == null)
        {
            return;
        }

        currentWordLitTime +=
            Time.deltaTime;

        DebugLog(
            $"🔤🔦 [P{playerId}] " +
            $"Iluminando '{currentWord.Word}' " +
            $"({bestAngle:F1}° del centro, {currentWordLitTime:F2} s)"
        );

        if (currentWordLitTime < revealTime)
        {
            return;
        }


        // ---------------------------------------------------------
        // INTENTAR REVELAR
        // ---------------------------------------------------------

        if (playerCamera == null)
        {
            FindPlayerCamera();
        }

        HiddenWord hiddenWord =
            currentWord;

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

            currentWord =
                null;
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


    private HiddenWord[] GetWords()
    {
        if (words == null || words.Length == 0)
        {
            words =
                FindObjectsByType<HiddenWord>();
        }

        return words;
    }


    private static Vector3 WordCenter(
        HiddenWord word
    )
    {
        Collider wordCollider =
            word.GetComponentInChildren<Collider>();

        return wordCollider != null
            ? wordCollider.bounds.center
            : word.transform.position;
    }


    // Nada (salvo el propio jugador o la palabra) entre la linterna y la palabra.
    private bool IsUnobstructed(
        Vector3 origin,
        Vector3 toWord,
        float distance,
        HiddenWord word
    )
    {
        int count =
            Physics.RaycastNonAlloc(
                origin,
                toWord / distance,
                occlusionHits,
                distance - 0.05f,
                detectionLayers,
                QueryTriggerInteraction.Ignore
            );

        for (int i = 0; i < count; i++)
        {
            Transform hit =
                occlusionHits[i].transform;

            if (
                hit.root == transform.root ||
                hit.GetComponentInParent<HiddenWord>() == word
            )
            {
                continue;
            }

            return false;
        }

        return true;
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