using UnityEngine;

public class BalloonController : MonoBehaviour
{
    [Header("Referencias a los cuatro globos")]
    [Tooltip("Element 0 = Player 1, Element 1 = Player 2, Element 2 = Player 3, Element 3 = Player 4")]
    [SerializeField] private Transform[] balloons = new Transform[4];

    [Header("Configuración del globo")]
    [SerializeField] private float inflationSpeed = 0.8f;
    [SerializeField] private float deflationSpeed = 0.18f;
    [SerializeField] private float maxInflation = 2.5f;
    [SerializeField] private float minimumBlowToInflate = 0.08f;

    [Header("Escala inicial")]
    [SerializeField] private float initialScale = 0.5f;

    [Header("Control del puzzle")]
    [SerializeField] private bool inflationEnabled = false;

    [Header("Estado de los globos")]
    [SerializeField, Range(0f, 1f)]
    private float[] inflations = new float[4];

    private bool[] balloonFinished;
    private Vector3[] originalScales;

    private MobileInputManager mobileInput;

    private void Awake()
    {
        mobileInput = FindFirstObjectByType<MobileInputManager>();

        if (balloons == null || balloons.Length != 4)
        {
            Debug.LogError(
                "🎈 BalloonController: Debes asignar exactamente cuatro globos."
            );

            return;
        }

        inflations = new float[4];
        balloonFinished = new bool[4];
        originalScales = new Vector3[4];

        for (int i = 0; i < balloons.Length; i++)
        {
            if (balloons[i] == null)
            {
                Debug.LogError(
                    $"🎈 BalloonController: Falta asignar el globo del Player {i + 1}."
                );

                continue;
            }

            originalScales[i] = balloons[i].localScale;
            inflations[i] = 0f;
            balloonFinished[i] = false;

            balloons[i].localScale =
                originalScales[i] * initialScale;
        }

        Debug.Log(
            "🎈 BalloonController central iniciado correctamente."
        );
    }

    private void Update()
    {
        if (!inflationEnabled)
            return;

        if (balloons == null || balloons.Length != 4)
            return;

        if (mobileInput == null)
        {
            mobileInput = FindFirstObjectByType<MobileInputManager>();

            if (mobileInput == null)
                return;
        }

        for (int i = 0; i < balloons.Length; i++)
        {
            UpdateBalloon(i);
        }
    }

    private void UpdateBalloon(int index)
    {
        if (balloons[index] == null)
            return;

        // El índice 0 corresponde al PlayerId 1.
        int playerId = index + 1;

        float blowIntensity =
            mobileInput.GetBlowIntensity(playerId);

        bool isBlowing =
            blowIntensity >= minimumBlowToInflate;

        if (isBlowing)
        {
            inflations[index] +=
                blowIntensity *
                inflationSpeed *
                Time.deltaTime;
        }
        else
        {
            inflations[index] -=
                deflationSpeed *
                Time.deltaTime;
        }

        inflations[index] =
            Mathf.Clamp01(inflations[index]);

        UpdateBalloonVisual(index);

        balloonFinished[index] =
            inflations[index] >= 0.999f;
    }

    private void UpdateBalloonVisual(int index)
    {
        if (balloons[index] == null)
            return;

        float currentScale =
            Mathf.Lerp(
                initialScale,
                maxInflation,
                inflations[index]
            );

        balloons[index].localScale =
            originalScales[index] * currentScale;
    }

    public void SetInflationEnabled(bool enabled)
    {
        inflationEnabled = enabled;

        Debug.Log(
            $"🎈 Inflado de globos permitido: {enabled}"
        );
    }

    public bool IsInflationEnabled()
    {
        return inflationEnabled;
    }

    public float GetInflationProgress(int playerId)
    {
        int index = playerId - 1;

        if (index < 0 || index >= inflations.Length)
            return 0f;

        return inflations[index];
    }

    public bool IsFullyInflated(int playerId)
    {
        int index = playerId - 1;

        if (index < 0 || index >= balloonFinished.Length)
            return false;

        return balloonFinished[index];
    }

    public bool AreAllBalloonsFullyInflated()
    {
        if (balloonFinished == null || balloonFinished.Length != 4)
            return false;

        for (int i = 0; i < balloonFinished.Length; i++)
        {
            if (!balloonFinished[i])
                return false;
        }

        return true;
    }

    public void ResetBalloons()
    {
        if (balloons == null || originalScales == null)
            return;

        for (int i = 0; i < balloons.Length; i++)
        {
            inflations[i] = 0f;
            balloonFinished[i] = false;

            if (balloons[i] != null)
            {
                balloons[i].localScale =
                    originalScales[i] * initialScale;
            }
        }

        Debug.Log(
            "🎈 Los cuatro globos fueron reiniciados."
        );
    }
}