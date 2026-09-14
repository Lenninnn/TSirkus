using UnityEngine;
using TMPro;

public class PlayerTicketHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerTickets playerTickets;
    [SerializeField] private TMP_Text ticketText;

    private void OnEnable()
    {
        if (playerTickets != null)
        {
            playerTickets.OnTicketsChanged += UpdateHUD;
        }
    }

    private void Start()
    {
        if (playerTickets != null)
        {
            UpdateHUD(playerTickets.Tickets);
        }
    }

    private void OnDisable()
    {
        if (playerTickets != null)
        {
            playerTickets.OnTicketsChanged -= UpdateHUD;
        }
    }

    private void UpdateHUD(int amount)
    {
        ticketText.text = $"TICKETS: {amount}";
    }
}