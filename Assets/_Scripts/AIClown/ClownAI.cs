using UnityEngine;
using UnityEngine.AI;

public class ClownAI : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private NavMeshAgent navMeshAgent;

    [Tooltip("Distancia a la que el payaso considera que llegó al jugador.")]
    [SerializeField] private float captureDistance = 1.5f;

    [Header("Depuración")]
    [SerializeField] private bool showDebugMessages = true;

    private Transform currentTarget;
    private bool hasTarget;
    private bool hasCaptured;

    public bool HasTarget => hasTarget;
    public Transform CurrentTarget => currentTarget;
    public bool HasCaptured => hasCaptured;

    private void Awake()
    {
        if (navMeshAgent == null)
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
        }
    }

    private void Update()
    {
        if (!hasTarget || currentTarget == null)
        {
            return;
        }

        if (hasCaptured)
        {
            return;
        }

        MoveTowardsTarget();

        CheckCaptureDistance();
    }

    public void SetTarget(Transform target)
    {
        if (target == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: Se intentó asignar un objetivo NULL."
            );

            return;
        }

        currentTarget = target;
        hasTarget = true;
        hasCaptured = false;

        if (navMeshAgent == null)
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
        }

        if (navMeshAgent == null)
        {
            Debug.LogError(
                $"{gameObject.name}: No tiene un NavMeshAgent."
            );

            return;
        }

        navMeshAgent.isStopped = false;

        navMeshAgent.SetDestination(
            currentTarget.position
        );

        DebugLog(
            $"Nuevo objetivo: {currentTarget.name}"
        );
    }

    private void MoveTowardsTarget()
    {
        if (navMeshAgent == null)
        {
            return;
        }

        if (!navMeshAgent.isOnNavMesh)
        {
            Debug.LogWarning(
                $"{gameObject.name}: El payaso no está colocado sobre un NavMesh."
            );

            return;
        }

        navMeshAgent.SetDestination(
            currentTarget.position
        );
    }

    private void CheckCaptureDistance()
    {
        float distance = Vector3.Distance(
            transform.position,
            currentTarget.position
        );

        if (distance <= captureDistance)
        {
            CaptureTarget();
        }
    }

    private void CaptureTarget()
    {
        if (hasCaptured)
        {
            return;
        }

        hasCaptured = true;

        if (navMeshAgent != null)
        {
            navMeshAgent.isStopped = true;
        }

        DebugLog(
            $"El payaso llegó al objetivo: {currentTarget.name}"
        );

        DebugLog(
            $"CAPTURA: {currentTarget.name}"
        );

        // BLOQUE 2:
        // Aquí conectaremos el sistema de estados del jugador.
    }

    public void ClearTarget()
    {
        currentTarget = null;
        hasTarget = false;
        hasCaptured = false;

        if (navMeshAgent != null)
        {
            navMeshAgent.isStopped = true;
        }

        DebugLog("Objetivo eliminado.");
    }

    private void DebugLog(string message)
    {
        if (!showDebugMessages)
        {
            return;
        }

        Debug.Log(
            $"{gameObject.name}: {message}"
        );
    }
    [ContextMenu("Probar objetivo")]
private void TestTarget()
{
    GameObject player = GameObject.FindWithTag("Player");

    if (player == null)
    {
        Debug.LogWarning(
            $"{gameObject.name}: No se encontró ningún objeto con Tag 'Player'."
        );

        return;
    }

    SetTarget(player.transform);
}
}