using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    // =========================================================
    // PLAYER
    // =========================================================

    [Header("Player")]
    [SerializeField] private int playerId = 0;

    [SerializeField] private PlayerIdentity playerIdentity;


    // =========================================================
    // FLASHLIGHT
    // =========================================================

    [Header("Flashlight")]
    [SerializeField] private Transform flashlightPivot;

    [SerializeField] private Light flashlightLight;


    // =========================================================
    // FLASHLIGHT STATE
    // =========================================================

    [Header("Flashlight State")]
    [SerializeField] private bool flashlightActive = false;


    // =========================================================
    // ROTATION
    // =========================================================

    [Header("Rotation")]
    [SerializeField] private float horizontalSensitivity = 35f;

    [SerializeField] private float verticalSensitivity = 35f;

    [SerializeField] private float maxHorizontalAngle = 75f;

    [SerializeField] private float minVerticalAngle = -60f;

    [SerializeField] private float maxVerticalAngle = 60f;


    // =========================================================
    // SMOOTHING
    // =========================================================

    [Header("Smoothing")]
    [SerializeField] private float rotationSmoothSpeed = 10f;


    // =========================================================
    // CALIBRATION
    // =========================================================

    [Header("Calibration")]
    [SerializeField] private bool autoCalibrate = true;


    // =========================================================
    // ACCELEROMETER DEAD ZONE
    // =========================================================

    [Header("Accelerometer Dead Zone")]
    [SerializeField] private float accelerometerDeadZone = 0.10f;


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    [SerializeField] private float debugInterval = 0.5f;


    // =========================================================
    // REFERENCES
    // =========================================================

    private MobileInputManager mobileInput;


    // =========================================================
    // PLAYER ID
    // =========================================================

    private bool playerIdAssigned = false;


    // =========================================================
    // CALIBRACIÓN
    // =========================================================

    private Vector3 initialAccelerometer;

    private bool calibrated = false;


    // =========================================================
    // ROTATION
    // =========================================================

    private float currentHorizontal;

    private float currentVertical;

    private float targetHorizontal;

    private float targetVertical;


    // =========================================================
    // DEBUG
    // =========================================================

    private float debugTimer;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        Debug.Log(
            $"🔦 [FLASHLIGHT] Awake -> {gameObject.name}"
        );


        // ---------------------------------------------------------
        // PIVOT
        // ---------------------------------------------------------

        if (flashlightPivot == null)
        {
            flashlightPivot =
                transform;


            Debug.Log(
                $"🔦 [FLASHLIGHT] " +
                $"No se asignó Pivot. " +
                $"Usando {gameObject.name}"
            );
        }


        // ---------------------------------------------------------
        // PLAYER IDENTITY
        // ---------------------------------------------------------

        playerIdentity =
            FindPlayerIdentity();


        // ---------------------------------------------------------
        // ESTADO INICIAL
        // ---------------------------------------------------------

        flashlightActive = false;

        SetLightEnabled(false);

        ResetRotation();


        // ---------------------------------------------------------
        // INTENTAR ID
        // ---------------------------------------------------------

        TrySyncPlayerId();
    }


    private void Start()
    {
        Debug.Log(
            $"🔦 [FLASHLIGHT] Start -> {gameObject.name}"
        );


        // ---------------------------------------------------------
        // MOBILE INPUT
        // ---------------------------------------------------------

        mobileInput =
            FindObjectOfType<MobileInputManager>();


        if (mobileInput == null)
        {
            Debug.LogError(
                "🔦❌ [FLASHLIGHT] " +
                "NO SE ENCONTRÓ MobileInputManager."
            );

            return;
        }


        Debug.Log(
            "🔦 [FLASHLIGHT] " +
            "MobileInputManager encontrado."
        );


        // ---------------------------------------------------------
        // PLAYER ID
        // ---------------------------------------------------------

        TrySyncPlayerId();


        if (!playerIdAssigned)
        {
            Debug.LogWarning(
                $"🔦⚠️ [FLASHLIGHT] " +
                $"{gameObject.name} todavía NO tiene Player ID."
            );
        }
        else
        {
            Debug.Log(
                $"🔦✅ [FLASHLIGHT] " +
                $"{gameObject.name} -> PLAYER {playerId}"
            );
        }
    }


    private void Update()
    {
        // ---------------------------------------------------------
        // ACTUALIZAR PLAYER ID
        // ---------------------------------------------------------

        TrySyncPlayerId();


        if (mobileInput == null)
        {
            return;
        }


        // ---------------------------------------------------------
        // ESPERAR ID
        // ---------------------------------------------------------

        if (!playerIdAssigned)
        {
            DebugLog(
                $"🔦 [FLASHLIGHT] " +
                $"Esperando Player ID -> {gameObject.name}"
            );

            return;
        }


        // ---------------------------------------------------------
        // LINTERNA APAGADA
        // ---------------------------------------------------------

        if (!flashlightActive)
        {
            return;
        }


        // ---------------------------------------------------------
        // PLAYER CONECTADO
        // ---------------------------------------------------------

        if (
            !mobileInput.IsPlayerConnected(
                playerId
            )
        )
        {
            DebugLog(
                $"🔦 [FLASHLIGHT] " +
                $"Player {playerId} no está conectado."
            );

            return;
        }


        // ---------------------------------------------------------
        // ACELERÓMETRO
        // ---------------------------------------------------------

        Vector3 accelerometer =
            mobileInput.GetAccelerometer(
                playerId
            );


        DebugLog(
            $"🔦 [P{playerId}] Accelerometer -> " +
            $"X: {accelerometer.x:F2} | " +
            $"Y: {accelerometer.y:F2} | " +
            $"Z: {accelerometer.z:F2}"
        );


        // ---------------------------------------------------------
        // CALIBRACIÓN
        // ---------------------------------------------------------

        if (
            autoCalibrate &&
            !calibrated
        )
        {
            Calibrate(
                accelerometer
            );
        }


        if (!calibrated)
        {
            return;
        }


        // ---------------------------------------------------------
        // DELTA
        // ---------------------------------------------------------

        Vector3 delta =
            accelerometer -
            initialAccelerometer;


        // ---------------------------------------------------------
        // DEAD ZONE
        // ---------------------------------------------------------

        if (
            Mathf.Abs(delta.x) <
            accelerometerDeadZone
        )
        {
            delta.x = 0f;
        }


        if (
            Mathf.Abs(delta.y) <
            accelerometerDeadZone
        )
        {
            delta.y = 0f;
        }


        // ---------------------------------------------------------
        // HORIZONTAL
        // ---------------------------------------------------------

        targetHorizontal =
            delta.x *
            horizontalSensitivity;


        targetHorizontal =
            Mathf.Clamp(
                targetHorizontal,
                -maxHorizontalAngle,
                maxHorizontalAngle
            );


        // ---------------------------------------------------------
        // VERTICAL
        // ---------------------------------------------------------

        targetVertical =
            -delta.y *
            verticalSensitivity;


        targetVertical =
            Mathf.Clamp(
                targetVertical,
                minVerticalAngle,
                maxVerticalAngle
            );


        // ---------------------------------------------------------
        // SMOOTH
        // ---------------------------------------------------------

        currentHorizontal =
            Mathf.Lerp(
                currentHorizontal,
                targetHorizontal,
                rotationSmoothSpeed *
                Time.deltaTime
            );


        currentVertical =
            Mathf.Lerp(
                currentVertical,
                targetVertical,
                rotationSmoothSpeed *
                Time.deltaTime
            );


        // ---------------------------------------------------------
        // ROTACIÓN
        // ---------------------------------------------------------

        flashlightPivot.localRotation =
            Quaternion.Euler(
                currentVertical,
                currentHorizontal,
                0f
            );


        DebugLog(
            $"🔦 [P{playerId}] Rotación -> " +
            $"X: {currentVertical:F1} | " +
            $"Y: {currentHorizontal:F1}"
        );
    }


    // =========================================================
    // BUSCAR PLAYER IDENTITY
    // =========================================================

    private PlayerIdentity FindPlayerIdentity()
    {
        // ---------------------------------------------------------
        // MISMO OBJETO
        // ---------------------------------------------------------

        PlayerIdentity identity =
            GetComponent<PlayerIdentity>();


        if (identity != null)
        {
            return identity;
        }


        // ---------------------------------------------------------
        // PADRE
        // ---------------------------------------------------------

        identity =
            GetComponentInParent<PlayerIdentity>();


        if (identity != null)
        {
            return identity;
        }


        // ---------------------------------------------------------
        // RAÍZ
        // ---------------------------------------------------------

        if (transform.root != null)
        {
            identity =
                transform.root
                    .GetComponent<PlayerIdentity>();
        }


        return identity;
    }


    // =========================================================
    // SINCRONIZAR PLAYER ID
    // =========================================================

    private void TrySyncPlayerId()
    {
        // ---------------------------------------------------------
        // SI NO HAY IDENTITY O ES INVÁLIDA, BUSCAR OTRA VEZ
        // ---------------------------------------------------------

        if (
            playerIdentity == null ||
            playerIdentity.PlayerId < 1 ||
            playerIdentity.PlayerId > 4
        )
        {
            playerIdentity =
                FindPlayerIdentity();
        }


        if (playerIdentity == null)
        {
            return;
        }


        int identityId =
            playerIdentity.PlayerId;


        // ---------------------------------------------------------
        // ID INVÁLIDO
        // ---------------------------------------------------------

        if (
            identityId < 1 ||
            identityId > 4
        )
        {
            return;
        }


        // ---------------------------------------------------------
        // ASIGNAR
        // ---------------------------------------------------------

        if (
            !playerIdAssigned ||
            playerId != identityId
        )
        {
            playerId =
                identityId;


            playerIdAssigned =
                true;


            calibrated =
                false;


            currentHorizontal = 0f;
            currentVertical = 0f;

            targetHorizontal = 0f;
            targetVertical = 0f;


            ResetRotation();


            Debug.Log(
                $"🔦🆔 [FLASHLIGHT] " +
                $"{gameObject.name} -> " +
                $"PLAYER {playerId}"
            );
        }
    }


    // =========================================================
    // ACTIVAR
    // =========================================================

    public void ActivateFlashlight()
    {
        // IMPORTANTE:
        // volver a obtener el ID antes de activar.

        TrySyncPlayerId();


        flashlightActive =
            true;


        calibrated =
            false;


        currentHorizontal = 0f;
        currentVertical = 0f;

        targetHorizontal = 0f;
        targetVertical = 0f;


        ResetRotation();


        SetLightEnabled(true);


        Debug.Log(
            $"🔦🔦🔦 [FLASHLIGHT] " +
            $"PLAYER {playerId} -> " +
            $"LINTERNA ACTIVADA 🔦🔦🔦"
        );
    }


    // =========================================================
    // DESACTIVAR
    // =========================================================

    public void DeactivateFlashlight()
    {
        flashlightActive =
            false;


        calibrated =
            false;


        ResetRotation();


        SetLightEnabled(false);


        Debug.Log(
            $"🔦 [FLASHLIGHT] " +
            $"PLAYER {playerId} -> " +
            $"LINTERNA DESACTIVADA"
        );
    }


    // =========================================================
    // SET ACTIVE
    // =========================================================

    public void SetFlashlightActive(
        bool active
    )
    {
        if (active)
        {
            ActivateFlashlight();
        }
        else
        {
            DeactivateFlashlight();
        }
    }


    // =========================================================
    // ACTIVE
    // =========================================================

    public bool IsFlashlightActive()
    {
        return flashlightActive;
    }


    // =========================================================
    // LIGHT
    // =========================================================

    private void SetLightEnabled(
        bool enabledState
    )
    {
        if (flashlightLight != null)
        {
            flashlightLight.enabled =
                enabledState;
        }
    }


    // =========================================================
    // PLAYER ID PUBLIC
    // =========================================================

    public void SetPlayerId(
        int id
    )
    {
        if (
            id < 1 ||
            id > 4
        )
        {
            Debug.LogError(
                $"🔦 [FLASHLIGHT] " +
                $"Player ID inválido: {id}"
            );

            return;
        }


        playerId =
            id;


        playerIdAssigned =
            true;


        calibrated =
            false;


        currentHorizontal = 0f;
        currentVertical = 0f;

        targetHorizontal = 0f;
        targetVertical = 0f;


        ResetRotation();


        Debug.Log(
            $"🔦🔦🔦 [FLASHLIGHT] " +
            $"SetPlayerId -> PLAYER {playerId}"
        );
    }


    public int GetPlayerId()
    {
        TrySyncPlayerId();

        return playerId;
    }


    public bool HasPlayerId()
    {
        TrySyncPlayerId();

        return playerIdAssigned;
    }


    // =========================================================
    // CALIBRAR
    // =========================================================

    public void Calibrate()
    {
        if (mobileInput == null)
        {
            mobileInput =
                FindObjectOfType<MobileInputManager>();
        }


        if (mobileInput == null)
        {
            Debug.LogWarning(
                "🔦 [FLASHLIGHT] " +
                "No se puede calibrar."
            );

            return;
        }


        TrySyncPlayerId();


        if (!playerIdAssigned)
        {
            Debug.LogWarning(
                "🔦 [FLASHLIGHT] " +
                "No se puede calibrar: " +
                "Player ID no asignado."
            );

            return;
        }


        Vector3 accelerometer =
            mobileInput.GetAccelerometer(
                playerId
            );


        Calibrate(
            accelerometer
        );
    }


    private void Calibrate(
        Vector3 accelerometer
    )
    {
        initialAccelerometer =
            accelerometer;


        currentHorizontal = 0f;
        currentVertical = 0f;

        targetHorizontal = 0f;
        targetVertical = 0f;


        calibrated =
            true;


        Debug.Log(
            $"🔦 [FLASHLIGHT] " +
            $"CALIBRADO P{playerId} -> " +
            $"X: {initialAccelerometer.x:F2} | " +
            $"Y: {initialAccelerometer.y:F2} | " +
            $"Z: {initialAccelerometer.z:F2}"
        );
    }


    // =========================================================
    // RESET CALIBRATION
    // =========================================================

    public void ResetCalibration()
    {
        calibrated =
            false;


        currentHorizontal = 0f;
        currentVertical = 0f;

        targetHorizontal = 0f;
        targetVertical = 0f;


        ResetRotation();


        Debug.Log(
            $"🔦 [FLASHLIGHT] " +
            $"Calibración reiniciada P{playerId}"
        );
    }


    public bool IsCalibrated()
    {
        return calibrated;
    }


    // =========================================================
    // RESET ROTATION
    // =========================================================

    private void ResetRotation()
    {
        if (flashlightPivot != null)
        {
            flashlightPivot.localRotation =
                Quaternion.identity;
        }
    }


    // =========================================================
    // DEBUG
    // =========================================================

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
}