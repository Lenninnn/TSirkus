
using UnityEngine;

[DisallowMultipleComponent]
public class AutomaticPlayerMovement : MonoBehaviour
{
    [Header("Movimiento automático")]
    [SerializeField, Min(0f)]
    private float movementSpeed = 1.5f;

    [Header("Velocidad según la inestabilidad")]
    [Tooltip("Multiplicador máximo de la velocidad normal.")]
    [SerializeField, Min(1f)]
    private float maximumSpeedMultiplier = 2.2f;

    [Tooltip("Suavidad con la que cambia la velocidad.")]
    [SerializeField, Min(0.1f)]
    private float speedChangeSmoothness = 4f;

    [SerializeField]
    private bool stopAtFinish = true;

    [SerializeField, Min(0.001f)]
    private float finishTolerance = 0.05f;

    [Header("Referencias")]
    [SerializeField]
    private RaceStartManager raceStartManager;

    [SerializeField]
    private CharacterController characterController;

    [SerializeField]
    private PlayerInstability playerInstability;

    [Header("Estado")]
    [SerializeField]
    private bool hasStarted;

    [SerializeField]
    private bool hasFinished;

    private PlayerIdentity playerIdentity;
    private Transform startPoint;
    private Transform finishPoint;

    private bool referencesReady;
    private float currentSpeedMultiplier = 1f;

    public bool HasStarted => hasStarted;
    public bool HasFinished => hasFinished;

    private void Awake()
    {
        playerIdentity = GetComponent<PlayerIdentity>();

        if (playerIdentity == null)
        {
            Debug.LogError(
                $"AutomaticPlayerMovement: {name} no tiene PlayerIdentity.",
                this
            );

            enabled = false;
            return;
        }

        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (playerInstability == null)
            playerInstability = GetComponent<PlayerInstability>();

        if (raceStartManager == null)
            raceStartManager = FindFirstObjectByType<RaceStartManager>();

        if (raceStartManager == null)
        {
            Debug.LogError(
                "AutomaticPlayerMovement: no se encontró RaceStartManager.",
                this
            );

            enabled = false;
            return;
        }

        FindRoute();
    }

    private void FindRoute()
    {
        int playerId = playerIdentity.PlayerId;

        string startName = $"StartPoint{playerId}";
        string finishName = $"FinishPoint{playerId}";

        Transform[] allTransforms =
            FindObjectsByType<Transform>(FindObjectsSortMode.None);

        foreach (Transform item in allTransforms)
        {
            if (item.name == startName)
                startPoint = item;
            else if (item.name == finishName)
                finishPoint = item;
        }

        if (startPoint == null || finishPoint == null)
        {
            Debug.LogError(
                $"AutomaticPlayerMovement: no se encontraron " +
                $"{startName} y/o {finishName} para el jugador {playerId}.",
                this
            );

            enabled = false;
            return;
        }

        if (Vector3.Distance(startPoint.position, finishPoint.position)
            <= finishTolerance)
        {
            Debug.LogError(
                $"Los puntos del jugador {playerId} están demasiado cerca.",
                this
            );

            enabled = false;
            return;
        }

        referencesReady = true;

        Debug.Log(
            $"Jugador {playerId}: recorrido asignado correctamente.",
            this
        );
    }

    private void Update()
    {
        if (!referencesReady || hasFinished)
            return;

        if (raceStartManager == null ||
            !raceStartManager.RaceStarted)
            return;

        // Si perdió, no debe seguir avanzando.
        if (playerInstability != null &&
            playerInstability.IsFallen)
            return;

        hasStarted = true;

        UpdateSpeed();
        MoveAlongRope();
    }

    private void UpdateSpeed()
    {
        float targetMultiplier = 1f;

        if (playerInstability != null)
        {
            float instability =
                Mathf.Clamp01(playerInstability.Instability);

            // Más inestabilidad = más velocidad.
            targetMultiplier = Mathf.Lerp(
                1f,
                maximumSpeedMultiplier,
                instability
            );
        }

        currentSpeedMultiplier = Mathf.Lerp(
            currentSpeedMultiplier,
            targetMultiplier,
            1f - Mathf.Exp(
                speedChangeSmoothness * Time.deltaTime * -1f
            )
        );
    }

    private void MoveAlongRope()
    {
        Vector3 start = startPoint.position;
        Vector3 finish = finishPoint.position;

        Vector3 route = finish - start;
        float totalDistance = route.magnitude;

        if (totalDistance <= finishTolerance)
        {
            FinishMovement();
            return;
        }

        Vector3 direction = route / totalDistance;

        float progress = Vector3.Dot(
            transform.position - start,
            direction
        );

        float remainingDistance = totalDistance - progress;

        if (remainingDistance <= finishTolerance)
        {
            FinishMovement();
            return;
        }

        float effectiveSpeed =
            movementSpeed * currentSpeedMultiplier;

        float distance = Mathf.Min(
            effectiveSpeed * Time.deltaTime,
            remainingDistance
        );

        Vector3 movement = direction * distance;

        if (characterController != null &&
            characterController.enabled)
        {
            characterController.Move(movement);
        }
        else
        {
            transform.position += movement;
        }

        float updatedProgress = Vector3.Dot(
            transform.position - start,
            direction
        );

        if (updatedProgress >= totalDistance - finishTolerance)
            FinishMovement();
    }

    private void FinishMovement()
    {
        if (hasFinished)
            return;

        hasFinished = true;

        Debug.Log(
            $"Jugador {playerIdentity.PlayerId} llegó al final de su cuerda.",
            this
        );

        if (stopAtFinish)
            enabled = false;
    }

    public void StopMovement()
    {
        enabled = false;
    }

    public void ResumeMovement()
    {
        if (!hasFinished && referencesReady &&
            (playerInstability == null ||
             !playerInstability.IsFallen))
        {
            enabled = true;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (startPoint == null || finishPoint == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(startPoint.position, 0.12f);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(finishPoint.position, 0.12f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(startPoint.position, finishPoint.position);
    }
}
