using System;
using UnityEngine;

public class PlayerTickets : MonoBehaviour
{
    [SerializeField] private int tickets = 0;

    public int Tickets => tickets;

    public event Action<int> OnTicketsChanged;

    public void AddTicket(int amount = 1)
    {
        if (amount <= 0)
            return;

        tickets += amount;

        Debug.Log(
            $"{gameObject.name} ahora tiene {tickets} ticket(s)."
        );

        OnTicketsChanged?.Invoke(tickets);
    }

    public void ResetTickets()
    {
        tickets = 0;
        OnTicketsChanged?.Invoke(tickets);
    }
}