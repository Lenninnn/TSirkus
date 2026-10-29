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
        // El prefab del jugador no trae texto de tickets: sin él no debe romper
        // el resto de suscriptores de OnTicketsChanged (p. ej. el sonido del ticket).
        if (ticketText == null)
            return;

        ticketText.text = $"TICKETS: {amount}";
    }
}