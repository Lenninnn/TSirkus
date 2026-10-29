
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerInstability : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private BalanceSystem balanceSystem;
    [SerializeField] private AutomaticPlayerMovement automaticMovement;
    [SerializeField] private PlayerBalanceVisual balanceVisual;

    [Header("Inestabilidad")]
    [SerializeField, Min(0.1f)]
    private float maximumTimeOutside = 4f;

    [SerializeField, Range(0f, 1f)]
    private float recoveryRate = 0.65f;

    [SerializeField, Range(0f, 1f)]
    private float fallThreshold = 1f;

    [Header("Salidas de la SafeZone")]
    [SerializeField, Min(1)]
    private int maximumExits = 5;

    [Header("Estado")]
    [SerializeField] private bool isParticipating;
    [SerializeField] private float instability;
    [SerializeField] private int exitCount;
    [SerializeField] private bool isFallen;

    private PlayerIdentity playerIdentity;
    private bool wasOutsideSafeZone;
    private bool initialized;

    public float Instability => instability;
    public int ExitCount => exitCount;
    public bool IsFallen => isFallen;
    public bool IsParticipating => isParticipating;

    private void Awake()
    {
        playerIdentity = GetComponent<PlayerIdentity>();

        if (automaticMovement == null)
            automaticMovement = GetComponent<AutomaticPlayerMovement>();

        if (balanceVisual == null)
            balanceVisual = GetComponent<PlayerBalanceVisual>();

        if (playerIdentity == null)
        {
            Debug.LogError(
                $"{name}: falta PlayerIdentity.",
                this
            );

            enabled = false;
            return;
        }

        FindBalanceSystem();

        if (balanceSystem == null)
        {
            Debug.LogError(
                $"{name}: no se encontró BalanceSystem para el jugador " +
                $"{playerIdentity.PlayerId}.",
                this
            );

            enabled = false;
            return;
        }

        initialized = true;
    }

    private void FindBalanceSystem()
    {
        BalanceSystem[] systems =
            FindObjectsByType<BalanceSystem>(
                FindObjectsSortMode.None
            );

        foreach (BalanceSystem system in systems)
        {
            if (system != null &&
                system.PlayerId == playerIdentity.PlayerId)
            {
                balanceSystem = system;
                return;
            }
        }
    }

    
private void Update()
{
    if (!initialized || isFallen || !isParticipating)
        return;

    if (balanceSystem == null)
        return;

    // No contar inestabilidad antes de completar la calibración.
    if (!balanceSystem.IsBalancing || balanceSystem.IsCalibrating)
    {
        wasOutsideSafeZone = false;
        return;
    }

    bool outsideSafeZone = !balanceSystem.IsBalanced;

    // Contar únicamente una transición de zona segura
    // a zona insegura.
    if (outsideSafeZone && !wasOutsideSafeZone)
    {
        exitCount++;

        Debug.Log(
            $"{name}: salida de SafeZone {exitCount}/{maximumExits}",
            this
        );
    }

    wasOutsideSafeZone = outsideSafeZone;

    // Recuperarse dentro de la zona segura.
    if (!outsideSafeZone)
    {
        instability = Mathf.MoveTowards(
            instability,
            0f,
            (Time.deltaTime / maximumTimeOutside) * recoveryRate
        );

        return;
    }

    // Acumular inestabilidad únicamente fuera de la zona segura.
    instability += Time.deltaTime / maximumTimeOutside;
    instability = Mathf.Clamp01(instability);

    if (exitCount >= maximumExits ||
        instability >= fallThreshold)
    {
        Fall();
    }
}


    public void SetPuzzleParticipation(bool active)
    {
        if (!initialized || isFallen)
            return;

        isParticipating = active;

        if (!active)
        {
            wasOutsideSafeZone = false;
            instability = 0f;
            exitCount = 0;
        }

        Debug.Log(
            $"{name}: participación en puzzle = {isParticipating}",
            this
        );
    }

    private void Fall()
    {
        if (isFallen)
            return;

        isFallen = true;
        instability = 1f;

        if (automaticMovement != null)
            automaticMovement.StopMovement();

        Debug.Log(
            $"{name}: el jugador {playerIdentity.PlayerId} ha caído.",
            this
        );
    }
}
