using System.Collections;
using System.Linq;
using UnityEngine;

// Condición de victoria de la última prueba (carrera del monociclo, PuzzleMonociclo):
// gana el PRIMER jugador que llega al final de su cuerda (AutomaticPlayerMovement.HasFinished)
// sin haberse caído (PlayerInstability.IsFallen: quien se cae deja de avanzar y pierde).
// No cambia las reglas de la carrera: solo observa su resultado y avisa a GameEndingManager.
// Si TODOS los que corrían se caen, nadie puede ganar: cartel PRUEBA FALLIDA y los cuatro
// vuelven a sus bases para intentarlo otra vez (nunca se declara ganador a quien cayó).
public class RaceWinnerDetector : MonoBehaviour
{
    [SerializeField] private RaceStartManager raceStartManager;
    [SerializeField] private GameEndingManager endingManager;

    [Tooltip("Comprobaciones por segundo.")]
    [SerializeField] private float checksPerSecond = 10f;

    [Tooltip("Segundos con el cartel de prueba fallida antes de volver a las bases.")]
    [SerializeField] private float retryDelay = 3.5f;

    private float nextCheck;
    private bool reported;
    private bool retrying;


    private void Awake()
    {
        if (raceStartManager == null)
            raceStartManager = FindAnyObjectByType<RaceStartManager>();

        if (endingManager == null)
            endingManager = GetComponent<GameEndingManager>();
    }


    private void Update()
    {
        if (reported || retrying || Time.time < nextCheck || raceStartManager == null || !raceStartManager.RaceStarted)
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

        if (AllRacersFallen())
            StartCoroutine(RetryRace());
    }


    private static bool AllRacersFallen()
    {
        var racers = ClownSpawner.ActivePlayers()
            .Where(p => p.GetComponent<AutomaticPlayerMovement>() != null && p.GetComponent<AutomaticPlayerMovement>().HasStarted)
            .ToList();

        return racers.Count > 0 && racers.All(p =>
        {
            PlayerInstability instability = p.GetComponent<PlayerInstability>();
            return instability != null && instability.IsFallen && !p.GetComponent<AutomaticPlayerMovement>().HasFinished;
        });
    }


    private IEnumerator RetryRace()
    {
        retrying = true;
        Debug.Log("🏁 [FINAL] Todos cayeron: nadie gana. Se reinicia la carrera.");

        if (TrialCompleteBanner.Instance != null)
            TrialCompleteBanner.Instance.Show(null, "PRUEBA FALLIDA", "INTÉNTENLO DE NUEVO");

        yield return new WaitForSeconds(retryDelay);

        raceStartManager.ResetRace();

        foreach (RacePlayerBase raceBase in FindObjectsByType<RacePlayerBase>())
            raceBase.ResetBase();

        foreach (PlayerIdentity player in ClownSpawner.ActivePlayers())
        {
            player.GetComponent<PlayerInstability>()?.ResetForRetry();
            player.GetComponent<AutomaticPlayerMovement>()?.ResetForRetry();

            foreach (BalanceSystem balance in FindObjectsByType<BalanceSystem>())
            {
                if (balance.PlayerId == player.PlayerId)
                    balance.ResetForRetry();
            }

            // Las bases aceptan a cualquiera (expectedPlayerId = 0): se usa la base con su número.
            RacePlayerBase home = FindObjectsByType<RacePlayerBase>().FirstOrDefault(b =>
                b.expectedPlayerId == player.PlayerId ||
                (b.expectedPlayerId == 0 && b.transform.parent != null && b.transform.parent.name.Trim() == "Base" + player.PlayerId));

            if (home == null)
                continue;

            // Sobre su base, mirando hacia las cuerdas: al reaparecer dentro del trigger se registra solo.
            CharacterController body = player.GetComponent<CharacterController>();
            if (body != null) body.enabled = false;
            player.transform.SetPositionAndRotation(
                new Vector3(home.transform.position.x, player.transform.position.y, home.transform.position.z),
                Quaternion.LookRotation(Vector3.forward));
            if (body != null) body.enabled = true;
        }

        retrying = false;
    }
}
