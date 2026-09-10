using UnityEngine;

[RequireComponent(typeof(PlayerIdentity))]
public class BalloonController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform balloon;

    [Header("Configuración del Globo")]
    [SerializeField] private float inflationSpeed = 0.8f;

    [SerializeField] private float maxInflation = 2.5f;

    [SerializeField] private float minimumBlowToInflate = 0.08f;

    [Header("Escala Inicial")]
    [SerializeField] private float initialScale = 0.5f;

    [Header("Estado")]
    [SerializeField] private float inflation = 0f;

    [SerializeField] private bool balloonFinished = false;


    private PlayerIdentity identity;

    private MobileInputManager mobileInput;

    private Vector3 originalScale;


    // =====================================================
    // AWAKE
    // =====================================================

    private void Awake()
    {
        identity =
            GetComponent<PlayerIdentity>();


        mobileInput =
            FindFirstObjectByType<MobileInputManager>();


        if (balloon == null)
        {
            Debug.LogError(
                $"🎈 Player {identity.PlayerId}: " +
                "No se asignó el Transform del globo."
            );

            return;
        }


        originalScale =
            balloon.localScale;


        balloon.localScale =
            originalScale *
            initialScale;


        Debug.Log(
            $"🎈 BalloonController iniciado → Player {identity.PlayerId}"
        );
    }


    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        if (
            balloonFinished
        )
        {
            return;
        }


        if (
            mobileInput == null
        )
        {
            mobileInput =
                FindFirstObjectByType<MobileInputManager>();


            if (
                mobileInput == null
            )
            {
                return;
            }
        }


        float blow =
            mobileInput.GetBlowIntensity(
                identity.PlayerId
            );


        UpdateBalloon(
            blow
        );
    }


    // =====================================================
    // INFLAR
    // =====================================================

    private void UpdateBalloon(
        float blowIntensity
    )
    {
        if (
            blowIntensity <
            minimumBlowToInflate
        )
        {
            return;
        }


        // ---------------------------------------------
        // Aumentar inflación
        // ---------------------------------------------

        inflation +=
            blowIntensity *
            inflationSpeed *
            Time.deltaTime;


        inflation =
            Mathf.Clamp01(
                inflation
            );


        // ---------------------------------------------
        // Calcular escala
        // ---------------------------------------------

        float currentScale =
            Mathf.Lerp(
                initialScale,
                maxInflation,
                inflation
            );


        balloon.localScale =
            originalScale *
            currentScale;


        // ---------------------------------------------
        // Debug
        // ---------------------------------------------

        Debug.Log(
            $"🎈 P{identity.PlayerId} → " +
            $"Inflación: {(inflation * 100f):F1}% | " +
            $"Soplido: {blowIntensity:F2}"
        );


        // ---------------------------------------------
        // Globo completamente inflado
        // ---------------------------------------------

        if (
            inflation >=
            1f
        )
        {
            FinishBalloon();
        }
    }


    // =====================================================
    // TERMINAR GLOBO
    // =====================================================

    private void FinishBalloon()
    {
        balloonFinished =
            true;


        Debug.Log(
            $"🎈🎉 ¡GLOBO DEL PLAYER {identity.PlayerId} COMPLETAMENTE INFLADO!"
        );


        balloon.localScale =
            originalScale *
            maxInflation;


        // Aquí posteriormente podemos:
        //
        // - activar una animación
        // - cambiar el material
        // - reproducir sonido
        // - dar puntos
        // - activar una puerta
        // - marcar el puzzle como completado
    }


    // =====================================================
    // REINICIAR GLOBO
    // =====================================================

    public void ResetBalloon()
    {
        inflation =
            0f;


        balloonFinished =
            false;


        if (
            balloon != null
        )
        {
            balloon.localScale =
                originalScale *
                initialScale;
        }


        Debug.Log(
            $"🎈 Globo del Player {identity.PlayerId} reiniciado."
        );
    }


    // =====================================================
    // OBTENER PROGRESO
    // =====================================================

    public float GetInflationProgress()
    {
        return inflation;
    }


    // =====================================================
    // SABER SI TERMINÓ
    // =====================================================

    public bool IsFinished()
    {
        return balloonFinished;
    }
}