using UnityEngine;

// Sonidos de estados/eventos de UN jugador, escuchando eventos que ya existen:
// PlayerStateController.StateChanged (captura, rescate) y PlayerTickets.OnTicketsChanged.
// No cambia ninguna lógica. Va en la raíz del Player.
public class PlayerEventAudio : MonoBehaviour
{
    [Header("Captura / rescate")]
    [Tooltip("Sonido en el mundo cuando ESTE jugador es capturado.")]
    [SerializeField] private SoundCue capturedCue;

    [Tooltip("Sting global de peligro al capturar a cualquier jugador.")]
    [SerializeField] private SoundCue capturedStingCue;

    [Tooltip("Al volver de Captured/Cursed a Alive (rescate).")]
    [SerializeField] private SoundCue rescuedCue;

    [Header("Tickets")]
    [SerializeField] private SoundCue ticketPickupCue;

    private PlayerStateController stateController;
    private PlayerTickets tickets;
    private int lastTickets;


    private void Awake()
    {
        stateController = GetComponent<PlayerStateController>();
        tickets = GetComponent<PlayerTickets>();

        if (tickets != null)
            lastTickets = tickets.Tickets;
    }


    private void OnEnable()
    {
        if (stateController != null) stateController.StateChanged += OnStateChanged;
        if (tickets != null) tickets.OnTicketsChanged += OnTicketsChanged;
    }


    private void OnDisable()
    {
        if (stateController != null) stateController.StateChanged -= OnStateChanged;
        if (tickets != null) tickets.OnTicketsChanged -= OnTicketsChanged;
    }


    private void OnStateChanged(PlayerState previous, PlayerState current)
    {
        GameAudio audio = GameAudio.Instance;

        if (audio == null)
            return;

        if (current == PlayerState.Captured)
        {
            audio.PlayCueAt(capturedCue, transform.position);
            audio.PlayCue(capturedStingCue);
        }
        else if (current == PlayerState.Alive && previous != PlayerState.Alive)
        {
            audio.PlayCueAt(rescuedCue, transform.position);
        }
    }


    // Solo cuando sube (recoger); un reset a 0 no suena.
    private void OnTicketsChanged(int amount)
    {
        if (amount > lastTickets && GameAudio.Instance != null)
            GameAudio.Instance.PlayCueAt(ticketPickupCue, transform.position);

        lastTickets = amount;
    }
}
