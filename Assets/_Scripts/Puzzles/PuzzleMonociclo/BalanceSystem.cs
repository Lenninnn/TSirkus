
using UnityEngine;
using UnityEngine.UI;

public class BalanceSystem : MonoBehaviour
{
    public enum AccelerometerAxis
    {
        X,
        Y,
        Z
    }

    [Header("Referencias")]
    [SerializeField] private MobileInputManager inputManager;
    [SerializeField] private RaceStartManager raceStartManager;

    [Header("Jugador")]
    [Min(1)]
    [SerializeField] private int playerId = 3;

    [Header("Barra de equilibrio")]
    [SerializeField] private RectTransform barBackground;
    [SerializeField] private RectTransform safeZone;
    [SerializeField] private RectTransform balanceIndicator;

    [Header("Lectura del celular")]
    [SerializeField] private AccelerometerAxis axis =
        AccelerometerAxis.Y;

    [SerializeField] private bool invertAxis = false;

    [Tooltip("Ajusta cuánto se mueve el indicador al inclinar el celular.")]
    [SerializeField] private float sensitivity = 0.25f;

    [Tooltip("Mayor valor = respuesta más rápida.")]
    [SerializeField] private float smoothing = 8f;

    [Header("Zona segura")]
    [Range(0.15f, 0.7f)]
    [SerializeField] private float safeZoneWidth = 0.30f;

    [Header("Movimiento aleatorio de la zona segura")]
    [Tooltip("Tiempo mínimo entre cambios de dirección.")]
    [SerializeField] private float minDirectionChangeTime = 0.35f;

    [Tooltip("Tiempo máximo entre cambios de dirección.")]
    [SerializeField] private float maxDirectionChangeTime = 1.5f;

    [Tooltip("Velocidad mínima de la zona segura.")]
    [SerializeField] private float minSafeSpeed = 0.25f;

    [Tooltip("Velocidad máxima de la zona segura.")]
    [SerializeField] private float maxSafeSpeed = 0.8f;

    [Header("Calibración")]
    [SerializeField] private float calibrationDuration = 0.8f;

    [Header("Pruebas")]
    [SerializeField] private bool startAutomaticallyForTesting = false;

    public bool IsBalancing { get; private set; }
    public bool IsCalibrating { get; private set; }
    public bool IsBalanced { get; private set; }

    private Vector3 calibrationSum;
    private Vector3 calibrationBaseline;
    private int calibrationSamples;
    private float calibrationTimer;

    // Posiciones normalizadas: -1 = izquierda, 0 = centro, 1 = derecha.
    private float filteredPosition;
    private float currentSafeCenter;
    private float safeDirection = 1f;

    private float directionChangeTimer;
    private float nextDirectionChange;
    private float currentSafeSpeed;

    private float barHalfWidth;
    private bool referencesValid;

    private void OnEnable()
    {
        if (raceStartManager != null)
        {
            raceStartManager.onRaceStarted.AddListener(BeginBalance);
        }
    }

    private void OnDisable()
    {
        if (raceStartManager != null)
        {
            raceStartManager.onRaceStarted.RemoveListener(BeginBalance);
        }
    }

    private void Start()
    {
        if (barBackground == null ||
            safeZone == null ||
            balanceIndicator == null)
        {
            Debug.LogError(
                "BalanceSystem: asigna los tres elementos de la barra.",
                this
            );

            enabled = false;
            return;
        }

        ConfigureRect(safeZone);
        ConfigureRect(balanceIndicator);

        referencesValid = true;
        barHalfWidth = barBackground.rect.width / 2f;

        // La zona comienza cerca del extremo izquierdo.
        currentSafeCenter = -(1f - safeZoneWidth / 2f);
        safeDirection = 1f;

        InitializeRandomMovement();
        UpdateUI();

        if (raceStartManager != null &&
            raceStartManager.RaceStarted)
        {
            BeginBalance();
        }
        else if (startAutomaticallyForTesting)
        {
            BeginBalance();
        }
    }

    private void ConfigureRect(RectTransform element)
    {
        element.anchorMin = new Vector2(0.5f, 0.5f);
        element.anchorMax = new Vector2(0.5f, 0.5f);
        element.pivot = new Vector2(0.5f, 0.5f);
    }

    private void InitializeRandomMovement()
    {
        // Asegurar que los valores mínimos y máximos estén ordenados.
        float minTime = Mathf.Min(
            minDirectionChangeTime,
            maxDirectionChangeTime
        );

        float maxTime = Mathf.Max(
            minDirectionChangeTime,
            maxDirectionChangeTime
        );

        float minSpeed = Mathf.Min(minSafeSpeed, maxSafeSpeed);
        float maxSpeed = Mathf.Max(minSafeSpeed, maxSafeSpeed);

        currentSafeSpeed = Random.Range(minSpeed, maxSpeed);
        nextDirectionChange = Random.Range(minTime, maxTime);
        directionChangeTimer = 0f;
    }

    public void BeginBalance()
    {
        if (IsBalancing || IsCalibrating)
            return;

        if (inputManager == null)
        {
            Debug.LogError(
                "BalanceSystem: falta MobileInputManager.",
                this
            );

            return;
        }

        calibrationSum = Vector3.zero;
        calibrationSamples = 0;
        calibrationTimer = 0f;

        IsBalancing = false;
        IsCalibrating = true;

        Debug.Log(
            "Calibrando jugador " + playerId +
            ". Mantén el celular quieto."
        );
    }

    private void Update()
    {
        if (!referencesValid)
            return;

        // Detectar si la carrera ya había comenzado.
        if (!IsBalancing &&
            !IsCalibrating &&
            raceStartManager != null &&
            raceStartManager.RaceStarted)
        {
            BeginBalance();
        }

        if (IsCalibrating)
        {
            UpdateCalibration();
        }
        else if (IsBalancing)
        {
            UpdateBalance();
        }

        // La zona segura se mueve incluso durante la calibración.
        UpdateSafeZone();
        UpdateUI();
    }

    private void UpdateCalibration()
    {
        Vector3 acceleration =
            inputManager.GetAccelerometer(playerId);

        // No incluir lecturas vacías en la calibración.
        if (acceleration.sqrMagnitude > 0.01f)
        {
            calibrationSum += acceleration;
            calibrationSamples++;
        }

        calibrationTimer += Time.deltaTime;

        if (calibrationTimer < calibrationDuration)
            return;

        IsCalibrating = false;

        if (calibrationSamples == 0)
        {
            Debug.LogWarning(
                "No llegan datos del acelerómetro del jugador " +
                playerId + ". Revisa la conexión del celular."
            );

            return;
        }

        calibrationBaseline =
            calibrationSum / calibrationSamples;

        filteredPosition = 0f;
        IsBalancing = true;

        Debug.Log(
            "Equilibrio activado para jugador " + playerId
        );
    }

    private void UpdateBalance()
    {
        Vector3 acceleration =
            inputManager.GetAccelerometer(playerId);

        Vector3 delta = acceleration - calibrationBaseline;

        float axisValue;

        switch (axis)
        {
            case AccelerometerAxis.X:
                axisValue = delta.x;
                break;

            case AccelerometerAxis.Y:
                axisValue = delta.y;
                break;

            default:
                axisValue = delta.z;
                break;
        }

        if (invertAxis)
            axisValue *= -1f;

        float targetPosition = Mathf.Clamp(
            axisValue * sensitivity,
            -1f,
            1f
        );

        filteredPosition = Mathf.Lerp(
            filteredPosition,
            targetPosition,
            1f - Mathf.Exp(-smoothing * Time.deltaTime)
        );
    }

    private void UpdateSafeZone()
    {
        float maxCenter = 1f - safeZoneWidth / 2f;

        // Contar el tiempo para el próximo cambio aleatorio.
        directionChangeTimer += Time.deltaTime;

        if (directionChangeTimer >= nextDirectionChange)
        {
            // Cambiar de dirección en cualquier punto de la barra.
            safeDirection = Random.value < 0.5f ? -1f : 1f;

            float minSpeed = Mathf.Min(minSafeSpeed, maxSafeSpeed);
            float maxSpeed = Mathf.Max(minSafeSpeed, maxSafeSpeed);

            currentSafeSpeed = Random.Range(minSpeed, maxSpeed);

            float minTime = Mathf.Min(
                minDirectionChangeTime,
                maxDirectionChangeTime
            );

            float maxTime = Mathf.Max(
                minDirectionChangeTime,
                maxDirectionChangeTime
            );

            nextDirectionChange = Random.Range(minTime, maxTime);
            directionChangeTimer = 0f;
        }

        // Aplicar movimiento con la dirección y velocidad actuales.
        currentSafeCenter +=
            safeDirection * currentSafeSpeed * Time.deltaTime;

        // Nunca permitir que la zona salga de la barra.
        if (currentSafeCenter >= maxCenter)
        {
            currentSafeCenter = maxCenter;
            safeDirection = -1f;
        }
        else if (currentSafeCenter <= -maxCenter)
        {
            currentSafeCenter = -maxCenter;
            safeDirection = 1f;
        }

        float safeHalfWidth = safeZoneWidth / 2f;

        IsBalanced = IsBalancing &&
            filteredPosition >= currentSafeCenter - safeHalfWidth &&
            filteredPosition <= currentSafeCenter + safeHalfWidth;
    }

    private void UpdateUI()
    {
        float backgroundWidth = barBackground.rect.width;
        barHalfWidth = backgroundWidth / 2f;

        // El indicador puede recorrer todo el ancho disponible.
        float indicatorLimit = Mathf.Max(
            0f,
            barHalfWidth - balanceIndicator.rect.width / 2f
        );

        balanceIndicator.anchoredPosition = new Vector2(
            Mathf.Clamp(filteredPosition, -1f, 1f) * indicatorLimit,
            balanceIndicator.anchoredPosition.y
        );

        // La zona segura ocupa una proporción del fondo.
        float safeWidth = backgroundWidth * safeZoneWidth;

        float safeLimit = Mathf.Max(
            0f,
            barHalfWidth - safeWidth / 2f
        );

        safeZone.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            safeWidth
        );

        float safeX = Mathf.Clamp(
            currentSafeCenter * barHalfWidth,
            -safeLimit,
            safeLimit
        );

        safeZone.anchoredPosition = new Vector2(
            safeX,
            safeZone.anchoredPosition.y
        );
    }
}
