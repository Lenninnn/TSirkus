using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class RescueLever : MonoBehaviour, IInteractable
{
    [Header("UI")]
    [SerializeField]
    private TMP_Text rescueStatusText;

    [SerializeField]
    private GameObject rescueUI;

    [Header("Debug")]
    [SerializeField]
    private int capturedPlayers;

    [SerializeField]
    private int requiredHelpers;

    [SerializeField]
    private int currentHelpers;

    private readonly HashSet<int> playersWhoHelped =
        new HashSet<int>();

    private float refreshTimer;

    private const float RefreshInterval = 0.25f;


    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        RefreshRescueStatus();
    }


    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        refreshTimer += Time.deltaTime;

        if (refreshTimer < RefreshInterval)
            return;

        refreshTimer = 0f;

        RefreshRescueStatus();
    }


    // =====================================================
    // INTERACCIÓN
    // =====================================================

    public void Interact(PlayerIdentity player)
    {
        if (player == null)
            return;

        PlayerStateController playerState =
            player.GetComponent<PlayerStateController>();

        if (playerState == null)
        {
            Debug.LogWarning(
                $"{player.gameObject.name} no tiene PlayerStateController."
            );

            return;
        }


        List<PlayerStateController> captured;
        List<PlayerStateController> helpers;

        GetCurrentPlayers(
            out captured,
            out helpers
        );


        capturedPlayers = captured.Count;
        requiredHelpers = helpers.Count;


        // =================================================
        // NADIE ESTÁ CAPTURADO
        // =================================================

        if (capturedPlayers == 0)
        {
            ResetProgress();
            RefreshRescueStatus();

            return;
        }


        // =================================================
        // CAPTURADO NO PUEDE AYUDARSE
        // =================================================

        if (
            playerState.CurrentState ==
            PlayerState.Captured
        )
        {
            Debug.LogWarning(
                $"{player.gameObject.name} está capturado."
            );

            return;
        }


        // =================================================
        // SOLO ALIVE PUEDE ACTIVAR EL RESCATE
        // =================================================

        if (
            playerState.CurrentState !=
            PlayerState.Alive
        )
        {
            Debug.LogWarning(
                $"{player.gameObject.name} no está en estado Alive."
            );

            return;
        }


        int playerId =
            player.PlayerId;


        // =================================================
        // YA PARTICIPÓ
        // =================================================

        if (
            playersWhoHelped.Contains(playerId)
        )
        {
            Debug.Log(
                $"Player {playerId} ya participó en el rescate."
            );

            RefreshRescueStatus();

            return;
        }


        // =================================================
        // REGISTRAR AYUDA
        // =================================================

        playersWhoHelped.Add(playerId);

        currentHelpers =
            playersWhoHelped.Count;


        Debug.LogWarning(
            $"Player {playerId} ayudó en el rescate | " +
            $"{currentHelpers}/{requiredHelpers}"
        );


        RefreshRescueStatus();


        // =================================================
        // ¿YA PARTICIPARON TODOS?
        // =================================================

        if (
            currentHelpers >= requiredHelpers &&
            requiredHelpers > 0
        )
        {
            RescueCapturedPlayers(captured);
        }
    }


    // =====================================================
    // OBTENER JUGADORES
    // =====================================================

    private void GetCurrentPlayers(
        out List<PlayerStateController> captured,
        out List<PlayerStateController> helpers
    )
    {
        captured =
            new List<PlayerStateController>();

        helpers =
            new List<PlayerStateController>();


        PlayerStateController[] allPlayers =
            FindObjectsByType<PlayerStateController>(
                FindObjectsSortMode.None
            );


        foreach (
            PlayerStateController state
            in allPlayers
        )
        {
            if (state == null)
                continue;


            if (
                state.CurrentState ==
                PlayerState.Captured
            )
            {
                captured.Add(state);
            }

            else if (
                state.CurrentState ==
                PlayerState.Alive
            )
            {
                helpers.Add(state);
            }
        }
    }


    // =====================================================
    // REFRESCAR UI
    // =====================================================

    private void RefreshRescueStatus()
    {
        List<PlayerStateController> captured;
        List<PlayerStateController> helpers;

        GetCurrentPlayers(
            out captured,
            out helpers
        );


        capturedPlayers =
            captured.Count;

        requiredHelpers =
            helpers.Count;

        currentHelpers =
            playersWhoHelped.Count;


        if (rescueStatusText == null)
            return;


        // =================================================
        // NADIE CAPTURADO
        // =================================================

        if (capturedPlayers == 0)
        {
            rescueStatusText.text =
                "RESCATE\n" +
                "NINGÚN JUGADOR CAPTURADO";

            return;
        }


        // =================================================
        // TODOS CAPTURADOS
        // =================================================

        if (requiredHelpers == 0)
        {
            rescueStatusText.text =
                "RESCATE IMPOSIBLE\n" +
                "TODOS ESTÁN CAPTURADOS";

            return;
        }


        StringBuilder missingPlayers =
            new StringBuilder();


        foreach (
            PlayerStateController helper
            in helpers
        )
        {
            PlayerIdentity identity =
                helper.GetComponent<PlayerIdentity>();

            if (identity == null)
                continue;


            if (
                playersWhoHelped.Contains(
                    identity.PlayerId
                )
            )
            {
                continue;
            }


            if (missingPlayers.Length > 0)
            {
                missingPlayers.Append(" · ");
            }


            missingPlayers.Append(
                "P" + identity.PlayerId
            );
        }


        rescueStatusText.text =
            $"RESCATE\n" +
            $"{currentHelpers} / {requiredHelpers} JUGADORES\n\n" +
            $"FALTAN:\n" +
            $"{missingPlayers}";
    }


    // =====================================================
    // RESCATAR
    // =====================================================

    private void RescueCapturedPlayers(
        List<PlayerStateController> captured
    )
    {
        if (rescueStatusText != null)
        {
            rescueStatusText.text =
                "RESCATE COMPLETADO";
        }


        foreach (
            PlayerStateController capturedPlayer
            in captured
        )
        {
            if (capturedPlayer == null)
                continue;

            capturedPlayer.Rescue();
        }


        Invoke(
            nameof(FinishRescue),
            2f
        );
    }


    // =====================================================
    // FINALIZAR
    // =====================================================

    private void FinishRescue()
    {
        ResetProgress();
        RefreshRescueStatus();
    }


    // =====================================================
    // RESET
    // =====================================================

    private void ResetProgress()
    {
        playersWhoHelped.Clear();

        capturedPlayers = 0;
        requiredHelpers = 0;
        currentHelpers = 0;
    }


    [ContextMenu("TEST → Reset Rescue Lever")]
    private void TestReset()
    {
        ResetProgress();
        RefreshRescueStatus();
    }
}