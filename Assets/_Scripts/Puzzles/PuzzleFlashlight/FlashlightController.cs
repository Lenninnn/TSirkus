using UnityEngine;

// Linterna de UN jugador, apuntada inclinando su celular.
//
// DATOS QUE LLEGAN (MobileInputManager.GetAccelerometer):
// accelerationIncludingGravity del navegador, en m/s², ejes del teléfono:
// x = derecha de la pantalla, y = arriba de la pantalla, z = sale de la pantalla.
// Con el teléfono quieto el vector es la gravedad (|a| ≈ 9.81) y su dirección dice cómo está
// inclinado. Algunos teléfonos lo entregan con TODOS los signos invertidos.
//
// CÓMO SE INTERPRETA:
// 1. Calibración: al activar la linterna se promedia la dirección del vector durante
//    calibrationTime. Esa postura (la que tenga el jugador) pasa a ser "neutral".
// 2. Filtro: se suaviza el vector para quitar el temblor de la mano; los tirones (|a| lejos de
//    la gravedad calibrada) casi no cuentan, así un movimiento brusco no hace saltar el haz.
// 3. Ángulos reales del teléfono respecto a la neutral, independientes de la postura y de la
//    convención de signos del teléfono:
//    - arriba/abajo: giro alrededor del eje x del teléfono = cambio de atan2(y, z).
//    - izquierda/derecha: inclinación hacia el borde derecho/izquierdo = atan2(-x, |yz|),
//      con el signo fijado por la postura neutral (así no se invierte en teléfonos con signos
//      invertidos).
// 4. Zona muerta continua (sin saltos), ganancia en grados de linterna por grado de teléfono,
//    límites y suavizado final independiente del framerate.
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
    // APUNTADO
    // =========================================================

    [Header("Apuntado (grados de linterna por grado de teléfono)")]
    [Tooltip("1.5: con ±30° de muñeca (rango cómodo) el haz recorre ±45°.")]
    [SerializeField] private float horizontalGain = 1.5f;

    [SerializeField] private float verticalGain = 1.5f;

    [Tooltip("Solo si con el teléfono real el haz va al revés.")]
    [SerializeField] private bool invertHorizontal = false;

    [SerializeField] private bool invertVertical = false;

    [Tooltip("Grados que el haz apunta hacia arriba en la postura neutral.")]
    [SerializeField] private float neutralUpAngle = 0f;


    // =========================================================
    // LÍMITES
    // =========================================================

    [Header("Límites del haz (grados)")]
    [SerializeField] private float horizontalLimit = 60f;

    [SerializeField] private float upLimit = 40f;

    [SerializeField] private float downLimit = 30f;


    // =========================================================
    // ZONA MUERTA Y FILTROS
    // =========================================================

    [Header("Zona muerta y filtros")]
    [Tooltip("Grados de teléfono ignorados alrededor de la neutral. Sin salto al salir de ella.")]
    [SerializeField] private float deadZoneDegrees = 1.5f;

    [Tooltip("Constante de tiempo del filtro del sensor (s). 0.06 s = corte ~2.7 Hz: quita el temblor (8-12 Hz) y deja pasar el movimiento intencional (< 2 Hz).")]
    [SerializeField] private float sensorSmoothTime = 0.06f;

    [Tooltip("Constante de tiempo del suavizado del haz (s).")]
    [SerializeField] private float aimSmoothTime = 0.06f;

    [Tooltip("Desviación de |a| respecto a la gravedad (fracción) a partir de la cual la muestra casi no cuenta. 0.15 = 1.5 m/s²: girar la muñeca no la alcanza, un tirón de la mano sí.")]
    [SerializeField] private float shakeTolerance = 0.15f;


    // =========================================================
    // CALIBRATION
    // =========================================================

    [Header("Calibration")]
    [SerializeField] private bool autoCalibrate = true;

    [Tooltip("Segundos de datos que se promedian para fijar la postura neutral.")]
    [SerializeField] private float calibrationTime = 0.4f;

    [Tooltip("Si una lectura se aparta más que esto (grados) del promedio, el teléfono se está moviendo y la calibración vuelve a empezar.")]
    [SerializeField] private float calibrationStillAngle = 4f;

    [Tooltip("Tras este tiempo (s) se acepta la postura aunque el teléfono no esté quieto.")]
    [SerializeField] private float calibrationMaxWait = 2f;

    [Tooltip("Lecturas con |a| menor se consideran 'sin datos' (el servidor envía ceros al conectar).")]
    [SerializeField] private float minValidMagnitude = 0.3f;


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

    private bool calibrated = false;

    private Vector3 calibrationSum;

    private float calibrationMagnitudeSum;

    private int calibrationSamples;

    private float calibrationTimer;

    private float calibrationWait;

    private float neutralMagnitude;

    private Vector3 neutralPlaneAxis;

    private float neutralPitch;

    private float neutralSide;


    // =========================================================
    // ROTATION
    // =========================================================

    private Vector3 filteredAcceleration;

    private float currentHorizontal;

    private float currentVertical;

    private float targetHorizontal;

    private float targetVertical;


    // =========================================================
    // DEBUG
    // =========================================================

    private float debugTimer;


    // Ángulos del teléfono respecto a la neutral (x = derecha, y = arriba), en grados.
    public Vector2 PhoneAngles { get; private set; }

    // Ángulos actuales del haz respecto al cuerpo (x = derecha, y = arriba), en grados.
    public Vector2 BeamAngles => new Vector2(currentHorizontal, currentVertical);


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

        ResetAim();

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

        mobileInput =
            FindAnyObjectByType<MobileInputManager>();

        if (mobileInput == null)
        {
            Debug.LogError(
                "🔦❌ [FLASHLIGHT] " +
                "NO SE ENCONTRÓ MobileInputManager."
            );

            return;
        }

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
        TrySyncPlayerId();

        if (mobileInput == null || !playerIdAssigned || !flashlightActive)
        {
            return;
        }

        if (!mobileInput.IsPlayerConnected(playerId))
        {
            DebugLog(
                $"🔦 [FLASHLIGHT] " +
                $"Player {playerId} no está conectado."
            );

            return;
        }

        Vector3 accelerometer =
            mobileInput.GetAccelerometer(
                playerId
            );

        float magnitude =
            accelerometer.magnitude;

        // ---------------------------------------------------------
        // SIN DATOS (ceros tras conectar, o el teléfono aún no envía):
        // el haz se queda donde está y no se calibra con ceros.
        // ---------------------------------------------------------

        if (magnitude < minValidMagnitude)
        {
            DebugLog(
                $"🔦 [P{playerId}] Esperando datos del acelerómetro..."
            );

            return;
        }

        // ---------------------------------------------------------
        // CALIBRACIÓN
        // ---------------------------------------------------------

        if (!calibrated)
        {
            if (autoCalibrate)
            {
                AccumulateCalibration(
                    accelerometer
                );
            }

            return;
        }

        // ---------------------------------------------------------
        // FILTRO DEL SENSOR (con rechazo de sacudidas)
        // ---------------------------------------------------------

        float shake =
            Mathf.Abs(magnitude - neutralMagnitude) /
            Mathf.Max(0.0001f, neutralMagnitude);

        float trust =
            Mathf.Clamp(
                1f - shake / Mathf.Max(0.0001f, shakeTolerance),
                0.05f,
                1f
            );

        filteredAcceleration =
            Vector3.Lerp(
                filteredAcceleration,
                accelerometer,
                Blend(sensorSmoothTime) * trust
            );

        // ---------------------------------------------------------
        // ÁNGULOS DEL TELÉFONO RESPECTO A LA NEUTRAL
        // ---------------------------------------------------------

        Vector2 phone =
            PhoneAnglesFrom(
                filteredAcceleration
            );

        float side =
            phone.x - neutralSide;

        float pitch =
            Mathf.DeltaAngle(
                neutralPitch,
                phone.y
            );

        PhoneAngles =
            new Vector2(side, pitch);

        // ---------------------------------------------------------
        // ZONA MUERTA + GANANCIA + LÍMITES
        // ---------------------------------------------------------

        targetHorizontal =
            Mathf.Clamp(
                ApplyDeadZone(side) * horizontalGain * (invertHorizontal ? -1f : 1f),
                -horizontalLimit,
                horizontalLimit
            );

        targetVertical =
            Mathf.Clamp(
                ApplyDeadZone(pitch) * verticalGain * (invertVertical ? -1f : 1f),
                -downLimit,
                upLimit
            );

        // ---------------------------------------------------------
        // SUAVIZADO DEL HAZ
        // ---------------------------------------------------------

        float blend =
            Blend(aimSmoothTime);

        currentHorizontal =
            Mathf.Lerp(
                currentHorizontal,
                targetHorizontal,
                blend
            );

        currentVertical =
            Mathf.Lerp(
                currentVertical,
                targetVertical,
                blend
            );

        ApplyRotation();

        DebugLog(
            $"🔦 [P{playerId}] Teléfono -> " +
            $"lado: {side:F1}° | arriba: {pitch:F1}° " +
            $"=> Haz -> derecha: {currentHorizontal:F1}° | arriba: {currentVertical:F1}°"
        );
    }


    // =========================================================
    // MATEMÁTICA DEL SENSOR
    // =========================================================

    // x = inclinación lateral (+ derecha), y = rotación alrededor del eje x del teléfono
    // (+ la parte trasera apunta más arriba). Grados.
    private Vector2 PhoneAnglesFrom(
        Vector3 acceleration
    )
    {
        float pitch =
            Mathf.Atan2(
                acceleration.y,
                acceleration.z
            ) * Mathf.Rad2Deg;

        // El signo de la componente en el plano yz (respecto al eje fijado al calibrar)
        // compensa a los teléfonos que entregan todos los signos invertidos.
        float planeSign =
            Vector3.Dot(acceleration, neutralPlaneAxis) >= 0f ? 1f : -1f;

        float side =
            Mathf.Atan2(
                -acceleration.x * planeSign,
                new Vector2(acceleration.y, acceleration.z).magnitude
            ) * Mathf.Rad2Deg;

        return new Vector2(side, pitch);
    }


    // Zona muerta continua: dentro vale 0 y fuera resta su ancho (no hay salto en el borde).
    private float ApplyDeadZone(
        float degrees
    )
    {
        float magnitude =
            Mathf.Max(0f, Mathf.Abs(degrees) - deadZoneDegrees);

        return Mathf.Sign(degrees) * magnitude;
    }


    // Mezcla exponencial independiente del framerate.
    private static float Blend(
        float smoothTime
    )
    {
        if (smoothTime <= 0f)
        {
            return 1f;
        }

        return 1f - Mathf.Exp(-Time.deltaTime / smoothTime);
    }


    // Promedia la postura mientras el teléfono está quieto (los jugadores pueden venir caminando
    // cuando empieza la prueba).
    private void AccumulateCalibration(
        Vector3 accelerometer
    )
    {
        Vector3 direction =
            accelerometer.normalized;

        calibrationWait +=
            Time.deltaTime;

        if (
            calibrationSamples >= 3 &&
            calibrationWait < calibrationMaxWait &&
            Vector3.Angle(direction, calibrationSum) > calibrationStillAngle
        )
        {
            calibrationSum = Vector3.zero;
            calibrationMagnitudeSum = 0f;
            calibrationSamples = 0;
            calibrationTimer = 0f;
        }

        calibrationSum +=
            direction;

        calibrationMagnitudeSum +=
            accelerometer.magnitude;

        calibrationSamples++;

        calibrationTimer +=
            Time.deltaTime;

        if (calibrationTimer < calibrationTime)
        {
            return;
        }

        FinishCalibration(
            calibrationSum.normalized,
            calibrationMagnitudeSum / calibrationSamples
        );
    }


    private void FinishCalibration(
        Vector3 direction,
        float magnitude
    )
    {
        neutralMagnitude =
            magnitude;

        // Eje del plano yz alineado con la neutral, elegido sin depender del signo global
        // (el ángulo se lleva a (-90°, 90°]).
        float planeAngle =
            Mathf.Atan2(direction.z, direction.y) * Mathf.Rad2Deg;

        if (planeAngle > 90f)
        {
            planeAngle -= 180f;
        }
        else if (planeAngle <= -90f)
        {
            planeAngle += 180f;
        }

        neutralPlaneAxis =
            new Vector3(
                0f,
                Mathf.Cos(planeAngle * Mathf.Deg2Rad),
                Mathf.Sin(planeAngle * Mathf.Deg2Rad)
            );

        Vector3 neutralVector =
            direction * magnitude;

        filteredAcceleration =
            neutralVector;

        Vector2 neutralAngles =
            PhoneAnglesFrom(neutralVector);

        neutralSide =
            neutralAngles.x;

        neutralPitch =
            neutralAngles.y;

        ResetAim();

        calibrated =
            true;

        Debug.Log(
            $"🔦 [FLASHLIGHT] " +
            $"CALIBRADO P{playerId} -> " +
            $"neutral: {neutralVector.x:F2}, {neutralVector.y:F2}, {neutralVector.z:F2} " +
            $"(|a| {magnitude:F2}, {calibrationSamples} muestras)"
        );

        if (magnitude < 3f)
        {
            Debug.LogWarning(
                $"🔦⚠️ [FLASHLIGHT] P{playerId}: |a| = {magnitude:F2}. " +
                $"Se esperaba ≈ 9.81 (m/s², gravedad incluida). Si el teléfono envía en g (≈ 1) funciona igual; " +
                $"si envía aceleración SIN gravedad (≈ 0) la linterna no puede orientarse."
            );
        }
    }


    private void ApplyRotation()
    {
        if (flashlightPivot == null)
        {
            return;
        }

        // Euler X positivo baja el haz.
        flashlightPivot.localRotation =
            Quaternion.Euler(
                -(currentVertical + neutralUpAngle),
                currentHorizontal,
                0f
            );
    }


    // =========================================================
    // BUSCAR PLAYER IDENTITY
    // =========================================================

    private PlayerIdentity FindPlayerIdentity()
    {
        PlayerIdentity identity =
            GetComponent<PlayerIdentity>();

        if (identity != null)
        {
            return identity;
        }

        identity =
            GetComponentInParent<PlayerIdentity>();

        if (identity != null)
        {
            return identity;
        }

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

        if (
            identityId < 1 ||
            identityId > 4
        )
        {
            return;
        }

        if (
            !playerIdAssigned ||
            playerId != identityId
        )
        {
            playerId =
                identityId;

            playerIdAssigned =
                true;

            ResetCalibrationState();

            ResetAim();

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

        // La postura con la que el jugador sostiene el teléfono al empezar es la neutral.
        ResetCalibrationState();

        ResetAim();

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

        ResetCalibrationState();

        ResetAim();

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


    public bool IsFlashlightActive()
    {
        return flashlightActive;
    }


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

        ResetCalibrationState();

        ResetAim();

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

    // Fija YA la postura actual como neutral (con la última lectura válida).
    public void Calibrate()
    {
        if (mobileInput == null)
        {
            mobileInput =
                FindAnyObjectByType<MobileInputManager>();
        }

        TrySyncPlayerId();

        if (mobileInput == null || !playerIdAssigned)
        {
            Debug.LogWarning(
                "🔦 [FLASHLIGHT] " +
                "No se puede calibrar: falta MobileInputManager o Player ID."
            );

            return;
        }

        Vector3 accelerometer =
            mobileInput.GetAccelerometer(
                playerId
            );

        if (accelerometer.magnitude < minValidMagnitude)
        {
            Debug.LogWarning(
                $"🔦 [FLASHLIGHT] " +
                $"No se puede calibrar P{playerId}: no llegan datos del acelerómetro."
            );

            return;
        }

        calibrationSamples = 1;

        FinishCalibration(
            accelerometer.normalized,
            accelerometer.magnitude
        );
    }


    // Vuelve a tomar la postura neutral con las próximas lecturas.
    public void ResetCalibration()
    {
        ResetCalibrationState();

        ResetAim();

        Debug.Log(
            $"🔦 [FLASHLIGHT] " +
            $"Calibración reiniciada P{playerId}"
        );
    }


    public bool IsCalibrated()
    {
        return calibrated;
    }


    private void ResetCalibrationState()
    {
        calibrated = false;
        calibrationSum = Vector3.zero;
        calibrationMagnitudeSum = 0f;
        calibrationSamples = 0;
        calibrationTimer = 0f;
        calibrationWait = 0f;
    }


    private void ResetAim()
    {
        currentHorizontal = 0f;
        currentVertical = 0f;
        targetHorizontal = 0f;
        targetVertical = 0f;
        PhoneAngles = Vector2.zero;

        ApplyRotation();
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
