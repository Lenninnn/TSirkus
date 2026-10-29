
using UnityEngine;

public class RaceBalanceUIManager : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private RaceStartManager raceStartManager;
    [SerializeField] private CanvasGroup raceCanvasGroup;

    [Header("Paneles de jugadores")]
    [Tooltip("Índice 0 = jugador 1, índice 1 = jugador 2, etc.")]
    [SerializeField] private CanvasGroup[] playerPanels = new CanvasGroup[4];

    private void Start()
    {
        if (raceStartManager == null || raceCanvasGroup == null)
        {
            Debug.LogError(
                "RaceBalanceUIManager: falta RaceStartManager o CanvasGroup.",
                this
            );
            enabled = false;
            return;
        }

        // Ocultar la interfaz al iniciar la escena.
        SetCanvasVisible(false);

        // Ocultar los cuatro paneles inicialmente.
        for (int i = 0; i < playerPanels.Length; i++)
        {
            SetPanelVisible(i, false);
        }

        // Por si el puzzle comenzó antes de que se ejecute este Start.
        RefreshUI();
    }

    private void Update()
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (raceStartManager == null || raceCanvasGroup == null)
            return;

        bool raceStarted = raceStartManager.RaceStarted;

        SetCanvasVisible(raceStarted);

        if (!raceStarted)
        {
            for (int i = 0; i < playerPanels.Length; i++)
            {
                SetPanelVisible(i, false);
            }

            return;
        }

        // Los índices de los paneles corresponden a los IDs 1 a 4.
        for (int i = 0; i < playerPanels.Length; i++)
        {
            int playerId = i + 1;

            bool playerReady =
                raceStartManager.IsPlayerOnBase(playerId);

            SetPanelVisible(i, playerReady);
        }
    }

    private void SetCanvasVisible(bool visible)
    {
        raceCanvasGroup.alpha = visible ? 1f : 0f;
        raceCanvasGroup.interactable = visible;
        raceCanvasGroup.blocksRaycasts = visible;
    }

    private void SetPanelVisible(int index, bool visible)
    {
        if (index < 0 || index >= playerPanels.Length)
            return;

        CanvasGroup panel = playerPanels[index];

        if (panel == null)
            return;

        panel.alpha = visible ? 1f : 0f;
        panel.interactable = visible;
        panel.blocksRaycasts = visible;
    }
}
