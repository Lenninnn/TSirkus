using UnityEngine;

public class TicketPickup : MonoBehaviour, IInteractable
{
    [Header("Ticket")]
    [SerializeField] private int amount = 1;

    private bool collected = false;

    public void Interact(PlayerIdentity player)
    {
        if (collected || player == null)
            return;

        PlayerTickets playerTickets =
            player.GetComponent<PlayerTickets>();

        if (playerTickets == null)
        {
            Debug.LogWarning(
                $"Player {player.PlayerId} no tiene PlayerTickets."
            );

            return;
        }

        collected = true;

        playerTickets.AddTicket(amount);

        Debug.Log(
            $"Player {player.PlayerId} recogió {amount} ticket(s)."
        );

        gameObject.SetActive(false);
    }
}