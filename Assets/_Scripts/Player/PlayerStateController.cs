using System;
using UnityEngine;

public class PlayerStateController : MonoBehaviour
{
    [Header("Player State")]
    [SerializeField]
    private PlayerState currentState = PlayerState.Alive;

    [Header("References")]
    [SerializeField]
    private PlayerMovement playerMovement;

    public PlayerState CurrentState => currentState;

    public bool IsAlive =>
        currentState == PlayerState.Alive;

    public bool IsCaptured =>
        currentState == PlayerState.Captured;

    public bool IsCursed =>
        currentState == PlayerState.Cursed;

    public event Action<PlayerState, PlayerState> StateChanged;

    private void Awake()
    {
        if (playerMovement == null)
        {
            playerMovement =
                GetComponent<PlayerMovement>();
        }
    }

    private void Start()
    {
        ApplyCurrentState();
    }

    public void SetState(PlayerState newState)
    {
        if (currentState == newState)
            return;

        PlayerState previousState = currentState;

        currentState = newState;

        Debug.LogWarning(
            $"ESTADO CAMBIADO | {gameObject.name}: " +
            $"{previousState} → {currentState}"
        );

        ApplyCurrentState();

        StateChanged?.Invoke(
            previousState,
            currentState
        );
    }

    private void ApplyCurrentState()
    {
        if (playerMovement == null)
        {
            Debug.LogWarning(
                $"{gameObject.name} no tiene PlayerMovement."
            );

            return;
        }

        switch (currentState)
        {
            case PlayerState.Alive:

                playerMovement.enabled = true;

                break;

            case PlayerState.Captured:

                playerMovement.enabled = false;

                break;

            case PlayerState.Cursed:

                // Por ahora el jugador maldito
                // todavía puede moverse.
                playerMovement.enabled = true;

                break;
        }
    }

    public void Capture()
    {
        SetState(PlayerState.Captured);
    }

    public void Curse()
    {
        SetState(PlayerState.Cursed);
    }

    public void Rescue()
    {
        SetState(PlayerState.Alive);
    }

    [ContextMenu("TEST → Captured")]
    private void TestCaptured()
    {
        Capture();
    }

    [ContextMenu("TEST → Cursed")]
    private void TestCursed()
    {
        Curse();
    }

    [ContextMenu("TEST → Alive")]
    private void TestAlive()
    {
        Rescue();
    }
}