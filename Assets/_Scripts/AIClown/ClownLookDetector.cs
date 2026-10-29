using UnityEngine;

// Va en cada jugador (prefab Player Variant). Comprueba si SU cámara ve realmente al payaso
// (ClownVisibility: dentro del viewport, delante, con línea de visión, a distancia) ~16 veces
// por segundo, escalonado por jugador para repartir los raycasts entre frames.
//
// - Visto a <= seeDistance durante confirmChecks comprobaciones seguidas → el payaso desaparece
//   (ClownAI.NotifySeenBy) y el encuentro registra que este jugador miró.
// - Visto más lejos (hasta lookDistance) → el payaso se queda quieto mientras alguien lo mira:
//   una silueta al fondo del pasillo que no se mueve.
// Como cada jugador tiene el suyo, basta con que CUALQUIERA de los 4 lo vea.
public class ClownLookDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerIdentity playerIdentity;
    [SerializeField] private ClownEncounterManager clownEncounterManager;

    [Header("Visión")]
    [Tooltip("Hasta esta distancia, si el jugador lo ve de verdad, el payaso desaparece.")]
    [SerializeField] private float seeDistance = 22f;

    [Tooltip("Más allá de seeDistance y hasta aquí: el jugador lo distingue a lo lejos y el payaso se queda quieto.")]
    [SerializeField] private float lookDistance = 100f;

    [Tooltip("Comprobaciones por segundo.")]
    [SerializeField] private float checksPerSecond = 16f;

    [Tooltip("Comprobaciones seguidas viéndolo antes de que desaparezca (2 ≈ 0.1 s: inmediato, pero un frame suelto no cuenta).")]
    [SerializeField] private int confirmChecks = 2;

    [Header("Debug")]
    [SerializeField] private bool showDebugRay = true;
    [SerializeField] private bool showDebugMessages = true;

    private ClownAI detectedClown;

    private int consecutiveSeen;

    private float nextCheck;

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
                FindAnyObjectByType<ClownEncounterManager>();
        }
    }


    private void Start()
    {
        // Escalonado: cada jugador comprueba en un instante distinto del intervalo.
        nextCheck = Time.time + (GetPlayerId() % 4) / (4f * Mathf.Max(1f, checksPerSecond));
    }


    private void Update()
    {
        if (Time.time < nextCheck)
        {
            return;
        }

        nextCheck = Time.time + 1f / Mathf.Max(1f, checksPerSecond);

        DetectClown();
    }


    private void DetectClown()
    {
        ClownAI clown = ClownAI.Active;

        if (clown == null || playerCamera == null || playerIdentity == null || clown.IsVanishing)
        {
            ResetDetection();
            return;
        }

        bool visible = ClownVisibility.CanCameraSee(
            playerCamera,
            clown.transform,
            clown.VisualBounds,
            lookDistance,
            out float distance
        );

        clown.ReportObservation(visible);

        if (showDebugRay && visible)
        {
            Debug.DrawLine(
                playerCamera.transform.position,
                clown.VisualBounds.center,
                distance <= seeDistance ? Color.red : Color.yellow,
                1f / Mathf.Max(1f, checksPerSecond)
            );
        }

        if (!visible || distance > seeDistance)
        {
            ResetDetection();
            return;
        }

        consecutiveSeen++;

        if (consecutiveSeen < confirmChecks)
        {
            return;
        }

        if (detectedClown != clown)
        {
            detectedClown = clown;

            if (showDebugMessages)
            {
                Debug.Log(
                    $"👁️🤡 P{GetPlayerId()} VE al payaso a {distance:F1} m"
                );
            }

            ReportLooking(true);
            clown.NotifySeenBy(playerIdentity);
        }
    }


    private void ReportLooking(bool isLookingAtClown)
    {
        if (clownEncounterManager == null || playerIdentity == null)
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


    private void ResetDetection()
    {
        detectedClown = null;
        consecutiveSeen = 0;
    }


    private int GetPlayerId()
    {
        if (playerIdentity != null)
        {
            return playerIdentity.PlayerId;
        }

        return -1;
    }


    public bool IsLookingAtClown()
    {
        return detectedClown != null;
    }


    public ClownAI GetDetectedClown()
    {
        return detectedClown;
    }


    private void OnDisable()
    {
        ResetDetection();
    }
}
