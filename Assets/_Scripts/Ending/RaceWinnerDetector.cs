using UnityEngine;

// Condición de victoria de la última prueba (carrera del monociclo, PuzzleMonociclo):
// gana el PRIMER jugador que llega al final de su cuerda (AutomaticPlayerMovement.HasFinished)
// sin haberse caído (PlayerInstability.IsFallen: quien se cae deja de avanzar y pierde).
// No cambia las reglas de la carrera: solo observa su resultado y avisa a GameEndingManager.
public class RaceWinnerDetector : MonoBehaviour
{
    [SerializeField] private RaceStartManager raceStartManager;
    [SerializeField] private GameEndingManager endingManager;

    [Tooltip("Comprobaciones por segundo.")]
    [SerializeField] private float checksPerSecond = 10f;

    private float nextCheck;
    private bool reported;


    private void Awake()
    {
        if (raceStartManager == null)
            raceStartManager = FindAnyObjectByType<RaceStartManager>();

        if (endingManager == null)
            endingManager = GetComponent<GameEndingManager>();
    }


    private void Update()
    {
        if (reported || Time.time < nextCheck || raceStartManager == null || !raceStartManager.RaceStarted)
            return;

        nextCheck = Time.time + 1f / Mathf.Max(1f, checksPerSecond);

        foreach (PlayerIdentity player in ClownSpawner.ActivePlayers())
        {
            AutomaticPlayerMovement movement = player.GetComponent<AutomaticPlayerMovement>();
            PlayerInstability instability = player.GetComponent<PlayerInstability>();

            if (movement == null || !movement.HasFinished)
                continue;

            if (instability != null && instability.IsFallen)
                continue;

            reported = true;
            Debug.Log($"🏁 [FINAL] Player {player.PlayerId} llegó primero al final de su cuerda: GANA.");

            if (endingManager != null)
                endingManager.DeclareWinner(player.PlayerId);

            return;
        }
    }
}
