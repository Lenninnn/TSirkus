using UnityEngine;

public class BalloonBase : MonoBehaviour
{
    [Header("Configuración de la Base")]
    [Tooltip("ID del jugador que debe ocupar esta base.")]
    [SerializeField]
    private int requiredPlayerId = 1;

    [Header("Referencia al Puzzle")]
    [Tooltip("BalloonPuzzleController que administra el puzzle.")]
    [SerializeField]
    private BalloonPuzzleController balloonPuzzleController;

    [Header("Estado")]
    [SerializeField]
    private bool isOccupied = false;

    [SerializeField]
    private PlayerIdentity occupyingPlayer;

    [Header("Depuración")]
    [Tooltip("Muestra mensajes detallados en la Console.")]
    [SerializeField]
    private bool enableDebugLogs = true;

    [Tooltip("Muestra información cuando entra cualquier collider.")]
    [SerializeField]
    private bool logAllTriggerObjects = true;

    private int triggerEnterCount = 0;
    private int triggerExitCount = 0;

    // =====================================================
    // PROPIEDADES PÚBLICAS
    // =====================================================

    public int RequiredPlayerId => requiredPlayerId;

    public bool IsOccupied => isOccupied;

    public PlayerIdentity OccupyingPlayer => occupyingPlayer;

    public int OccupyingPlayerId
    {
        get
        {
            if (occupyingPlayer == null)
            {
                return -1;
            }

            return occupyingPlayer.PlayerId;
        }
    }

    // =====================================================
    // AWAKE
    // =====================================================

    private void Awake()
    {
        DebugLog("========== BASE AWAKE ==========");

        if (balloonPuzzleController == null)
        {
            balloonPuzzleController =
                GetComponentInParent<BalloonPuzzleController>();

            if (balloonPuzzleController != null)
            {
                DebugLog(
                    "BalloonPuzzleController encontrado automáticamente " +
                    "en los padres."
                );
            }
        }

        if (balloonPuzzleController == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: No se encontró BalloonPuzzleController. " +
                "Asígnalo manualmente en el Inspector."
            );
        }
        else
        {
            DebugLog(
                $"BalloonPuzzleController asignado: " +
                $"{balloonPuzzleController.gameObject.name}"
            );
        }

        Collider baseCollider = GetComponent<Collider>();

        if (baseCollider == null)
        {
            Debug.LogError(
                $"{gameObject.name}: Esta base no tiene Collider. " +
                "Añade un Box Collider, Sphere Collider u otro Collider."
            );
        }
        else
        {
            DebugLog(
                $"Collider encontrado: {baseCollider.GetType().Name} | " +
                $"IsTrigger = {baseCollider.isTrigger}"
            );

            if (!baseCollider.isTrigger)
            {
                Debug.LogWarning(
                    $"{gameObject.name}: El Collider de la base " +
                    "no tiene Is Trigger activado."
                );
            }
        }

        Rigidbody baseRigidbody = GetComponent<Rigidbody>();

        if (baseRigidbody != null)
        {
            DebugLog(
                $"Rigidbody encontrado | " +
                $"IsKinematic = {baseRigidbody.isKinematic} | " +
                $"UseGravity = {baseRigidbody.useGravity}"
            );

            if (!baseRigidbody.isKinematic)
            {
                Debug.LogWarning(
                    $"{gameObject.name}: El Rigidbody de la base " +
                    "no es Kinematic. Podría caerse o moverse."
                );
            }

            if (baseRigidbody.useGravity)
            {
                Debug.LogWarning(
                    $"{gameObject.name}: La base tiene Use Gravity activado. " +
                    "Normalmente debería estar desactivado."
                );
            }
        }

        DebugLog(
            $"Base configurada para recibir al Player {requiredPlayerId}."
        );
    }

    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        DebugLog(
            $"Base lista. Requiere Player {requiredPlayerId}. " +
            $"Estado inicial: ocupada = {isOccupied}"
        );
    }

    // =====================================================
    // ENTRADA AL TRIGGER
    // =====================================================

    private void OnTriggerEnter(Collider other)
    {
        triggerEnterCount++;

        if (other == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: OnTriggerEnter recibió un collider null."
            );

            return;
        }

        if (logAllTriggerObjects)
        {
            Debug.Log(
                $"[BASE TRIGGER ENTER] {gameObject.name} detectó " +
                $"el objeto '{other.name}'. " +
                $"Tag='{other.tag}' | Layer={LayerMask.LayerToName(other.gameObject.layer)}"
            );
        }

        PlayerIdentity player =
            other.GetComponentInParent<PlayerIdentity>();

        if (player == null)
        {
            Debug.LogWarning(
                $"🚫 {gameObject.name}: Entró '{other.name}', " +
                "pero no se encontró PlayerIdentity en el objeto " +
                "ni en sus padres."
            );

            Debug.LogWarning(
                $"Revisa que el personaje tenga PlayerIdentity. " +
                $"Collider detectado: {other.name}"
            );

            return;
        }

        Debug.Log(
            $"🔎 {gameObject.name}: Se detectó PlayerIdentity " +
            $"con ID {player.PlayerId}."
        );

        RegisterPlayer(player);
    }

    // =====================================================
    // SALIDA DEL TRIGGER
    // =====================================================

    private void OnTriggerExit(Collider other)
    {
        triggerExitCount++;

        if (other == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: OnTriggerExit recibió un collider null."
            );

            return;
        }

        if (logAllTriggerObjects)
        {
            Debug.Log(
                $"[BASE TRIGGER EXIT] {gameObject.name} detectó " +
                $"la salida del objeto '{other.name}'."
            );
        }

        PlayerIdentity player =
            other.GetComponentInParent<PlayerIdentity>();

        if (player == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: Salió '{other.name}', " +
                "pero no se encontró PlayerIdentity."
            );

            return;
        }

        Debug.Log(
            $"🔎 {gameObject.name}: Salió Player {player.PlayerId}."
        );

        UnregisterPlayer(player);
    }

    // =====================================================
    // REGISTRAR JUGADOR
    // =====================================================

    private void RegisterPlayer(PlayerIdentity player)
    {
        if (player == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: RegisterPlayer recibió null."
            );

            return;
        }

        Debug.Log(
            $"[REGISTER] {gameObject.name}: " +
            $"Intentando registrar Player {player.PlayerId}. " +
            $"Esta base requiere Player {requiredPlayerId}."
        );

        // Validar que sea el jugador correspondiente.
        if (player.PlayerId != requiredPlayerId)
        {
            Debug.LogWarning(
                $"🚫 {gameObject.name}: Player {player.PlayerId} " +
                $"NO puede ocupar esta base. " +
                $"Se requiere Player {requiredPlayerId}."
            );

            return;
        }

        // Si ya está ocupada por el mismo jugador.
        if (isOccupied && occupyingPlayer == player)
        {
            DebugLog(
                $"La base ya estaba ocupada por Player {player.PlayerId}."
            );

            return;
        }

        // Si ya está ocupada por otro jugador.
        if (isOccupied && occupyingPlayer != null)
        {
            Debug.LogWarning(
                $"⚠️ {gameObject.name}: Ya está ocupada por " +
                $"Player {occupyingPlayer.PlayerId}. " +
                $"No se reemplazará."
            );

            return;
        }

        isOccupied = true;
        occupyingPlayer = player;

        Debug.Log(
            $"✅ {gameObject.name}: Player {player.PlayerId} " +
            "se posicionó correctamente."
        );

        DebugLog(
            $"Estado de la base: isOccupied={isOccupied} | " +
            $"OccupyingPlayerId={OccupyingPlayerId}"
        );

        NotifyPuzzlePlayerEntered(player);
    }

    // =====================================================
    // QUITAR JUGADOR
    // =====================================================

    private void UnregisterPlayer(PlayerIdentity player)
    {
        if (player == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: UnregisterPlayer recibió null."
            );

            return;
        }

        DebugLog(
            $"[UNREGISTER] {gameObject.name}: " +
            $"Intentando quitar Player {player.PlayerId}."
        );

        // Solo el jugador registrado puede liberar esta base.
        if (occupyingPlayer != player)
        {
            DebugLog(
                $"Player {player.PlayerId} no es el jugador registrado " +
                "en esta base. No se modifica el estado."
            );

            return;
        }

        isOccupied = false;
        occupyingPlayer = null;

        Debug.Log(
            $"⬅️ {gameObject.name}: Player {player.PlayerId} " +
            "salió de la base."
        );

        NotifyPuzzlePlayerExited(player);
    }

    // =====================================================
    // NOTIFICAR ENTRADA AL PUZZLE
    // =====================================================

    private void NotifyPuzzlePlayerEntered(PlayerIdentity player)
    {
        if (balloonPuzzleController == null)
        {
            Debug.LogError(
                $"{gameObject.name}: No se puede notificar la entrada " +
                "porque balloonPuzzleController es null."
            );

            return;
        }

        DebugLog(
            $"Notificando al BalloonPuzzleController que " +
            $"Player {player.PlayerId} entró."
        );

        balloonPuzzleController.OnPlayerEnteredBase(
            this,
            player
        );
    }

    // =====================================================
    // NOTIFICAR SALIDA AL PUZZLE
    // =====================================================

    private void NotifyPuzzlePlayerExited(PlayerIdentity player)
    {
        if (balloonPuzzleController == null)
        {
            Debug.LogError(
                $"{gameObject.name}: No se puede notificar la salida " +
                "porque balloonPuzzleController es null."
            );

            return;
        }

        DebugLog(
            $"Notificando al BalloonPuzzleController que " +
            $"Player {player.PlayerId} salió."
        );

        balloonPuzzleController.OnPlayerExitedBase(
            this,
            player
        );
    }

    // =====================================================
    // CONFIGURACIÓN DESDE OTROS SCRIPTS
    // =====================================================

    public void SetRequiredPlayerId(int playerId)
    {
        if (playerId < 1)
        {
            Debug.LogWarning(
                $"{gameObject.name}: El PlayerId debe ser mayor o igual a 1."
            );

            return;
        }

        requiredPlayerId = playerId;

        DebugLog(
            $"La base ahora requiere Player {requiredPlayerId}."
        );
    }

    public void SetBalloonPuzzleController(
        BalloonPuzzleController controller
    )
    {
        balloonPuzzleController = controller;

        DebugLog(
            "BalloonPuzzleController asignado mediante código."
        );
    }

    // =====================================================
    // REINICIAR BASE
    // =====================================================

    public void ResetBase()
    {
        if (occupyingPlayer != null)
        {
            DebugLog(
                $"Reiniciando base. " +
                $"Jugador anterior: P{occupyingPlayer.PlayerId}"
            );
        }
        else
        {
            DebugLog(
                "Reiniciando base. No había jugador registrado."
            );
        }

        isOccupied = false;
        occupyingPlayer = null;

        DebugLog(
            $"Base reiniciada. isOccupied={isOccupied}"
        );
    }

    // =====================================================
    // VALIDACIÓN MANUAL
    // =====================================================

    public bool IsCorrectPlayerOnBase()
    {
        bool result =
            isOccupied &&
            occupyingPlayer != null &&
            occupyingPlayer.PlayerId == requiredPlayerId;

        return result;
    }

    // =====================================================
    // GIZMOS
    // =====================================================

    private void OnDrawGizmos()
    {
        Gizmos.color = isOccupied
            ? Color.green
            : Color.yellow;

        Collider baseCollider =
            GetComponent<Collider>();

        if (baseCollider != null)
        {
            Gizmos.DrawWireCube(
                baseCollider.bounds.center,
                baseCollider.bounds.size
            );
        }
    }

    // =====================================================
    // DEBUG GENERAL
    // =====================================================

    private void DebugLog(string message)
    {
        if (!enableDebugLogs)
        {
            return;
        }

        Debug.Log(
            $"[BalloonBase: {gameObject.name}] {message}"
        );
    }
}