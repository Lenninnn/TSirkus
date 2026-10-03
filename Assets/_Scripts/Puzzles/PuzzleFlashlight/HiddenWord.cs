using UnityEngine;
using TMPro;

public class HiddenWord : MonoBehaviour
{
    // =============================================================
    // WORD
    // =============================================================

    [Header("Word")]
    [SerializeField] private string word = "SKRYTYE";


    // =============================================================
    // VISUAL
    // =============================================================

    [Header("Visual")]
    [SerializeField] private TMP_Text wordText;


    // =============================================================
    // STATE
    // =============================================================

    [Header("State")]
    [SerializeField] private bool revealed = false;


    // =============================================================
    // ASSIGNMENT
    // =============================================================

    [Header("Player Assignment")]
    [SerializeField] private int assignedPlayerId = 0;


    // =============================================================
    // REFERENCES
    // =============================================================

    private SkrytyeSlovaController puzzleController;


    // =============================================================
    // PUBLIC PROPERTIES
    // =============================================================

    public string Word =>
        word;

    public bool IsRevealed =>
        revealed;

    public int AssignedPlayerId =>
        assignedPlayerId;


    // =============================================================
    // UNITY
    // =============================================================

    private void Awake()
    {
        // ---------------------------------------------------------
        // BUSCAR TEXTO
        // ---------------------------------------------------------

        if (wordText == null)
        {
            wordText =
                GetComponentInChildren<TMP_Text>();


            if (wordText == null)
            {
                Debug.LogError(
                    $"🔤 [HIDDEN WORD] " +
                    $"No se encontró TMP_Text en " +
                    $"{gameObject.name}."
                );
            }
        }


        // ---------------------------------------------------------
        // BUSCAR CONTROLLER
        // ---------------------------------------------------------

        puzzleController =
            FindObjectOfType<SkrytyeSlovaController>();


        // ---------------------------------------------------------
        // ESTADO INICIAL
        // ---------------------------------------------------------

        revealed =
            false;


        HideWord();


        Debug.Log(
            $"🔤 [HIDDEN WORD] " +
            $"{gameObject.name} preparada. " +
            $"Palabra: {word}"
        );
    }


    // =============================================================
    // ASSIGN PLAYER
    // =============================================================

    public void AssignToPlayer(
        int playerId
    )
    {
        if (
            playerId < 1 ||
            playerId > 4
        )
        {
            Debug.LogWarning(
                $"🔤 [HIDDEN WORD] " +
                $"Player ID inválido para " +
                $"{gameObject.name}: {playerId}"
            );


            return;
        }


        assignedPlayerId =
            playerId;


        Debug.Log(
            $"🔤👤 [HIDDEN WORD] " +
            $"{gameObject.name} -> " +
            $"\"{word}\" asignada a " +
            $"PLAYER {playerId}"
        );
    }


    // =============================================================
    // CLEAR PLAYER ASSIGNMENT
    // =============================================================

    public void ClearPlayerAssignment()
    {
        assignedPlayerId =
            0;


        DebugLog(
            $"🔤 [HIDDEN WORD] " +
            $"{gameObject.name} -> " +
            $"sin Player asignado."
        );
    }


    // =============================================================
    // REVEAL FOR PLAYER
    // =============================================================

    public bool TryRevealForPlayer(
        int playerId
    )
    {
        // ---------------------------------------------------------
        // YA REVELADA
        // ---------------------------------------------------------

        if (revealed)
        {
            return false;
        }


        // ---------------------------------------------------------
        // PLAYER ID
        // ---------------------------------------------------------

        if (
            playerId < 1 ||
            playerId > 4
        )
        {
            Debug.LogWarning(
                $"🔤 [HIDDEN WORD] " +
                $"Player ID inválido intentando " +
                $"revelar {word}: {playerId}"
            );


            return false;
        }


        // ---------------------------------------------------------
        // BUSCAR CONTROLLER
        // ---------------------------------------------------------

        if (puzzleController == null)
        {
            puzzleController =
                FindObjectOfType<SkrytyeSlovaController>();
        }


        if (puzzleController == null)
        {
            Debug.LogWarning(
                $"🔤 [HIDDEN WORD] " +
                $"{gameObject.name} no encontró " +
                $"SkrytyeSlovaController."
            );


            return false;
        }


        // ---------------------------------------------------------
        // VALIDAR
        // ---------------------------------------------------------

        if (
            !puzzleController.CanPlayerRevealWord(
                this,
                playerId
            )
        )
        {
            return false;
        }


        // ---------------------------------------------------------
        // REVELAR
        // ---------------------------------------------------------

        revealed =
            true;


        ShowWord();


        Debug.Log(
            $"🔤✨ [HIDDEN WORD] " +
            $"PALABRA ENCONTRADA -> " +
            $"{word} | PLAYER {playerId}"
        );


        // ---------------------------------------------------------
        // AVISAR CONTROLLER
        // ---------------------------------------------------------

        puzzleController.WordFound(
            this,
            playerId
        );


        return true;
    }


    // =============================================================
    // REVEAL LEGACY / DEBUG
    // =============================================================

    public void Reveal()
    {
        if (revealed)
        {
            return;
        }


        if (
            assignedPlayerId >= 1 &&
            assignedPlayerId <= 4
        )
        {
            TryRevealForPlayer(
                assignedPlayerId
            );


            return;
        }


        // ---------------------------------------------------------
        // REVELACIÓN MANUAL
        // ---------------------------------------------------------

        revealed =
            true;


        ShowWord();


        Debug.Log(
            $"🔤✨ [HIDDEN WORD] " +
            $"PALABRA REVELADA MANUALMENTE -> " +
            $"{word}"
        );
    }


    // =============================================================
    // HIDE
    // =============================================================

    private void HideWord()
    {
        if (wordText != null)
        {
            wordText.enabled =
                false;
        }
    }


    // =============================================================
    // SHOW
    // =============================================================

    private void ShowWord()
    {
        if (wordText != null)
        {
            wordText.enabled =
                true;
        }
    }


    // =============================================================
    // RESET
    // =============================================================

    public void ResetWord()
    {
        revealed =
            false;


        HideWord();


        Debug.Log(
            $"🔤 [HIDDEN WORD] " +
            $"{gameObject.name} reiniciada."
        );
    }


    // =============================================================
    // CONFIGURACIÓN
    // =============================================================

    public void SetWord(
        string newWord
    )
    {
        if (
            string.IsNullOrWhiteSpace(
                newWord
            )
        )
        {
            Debug.LogWarning(
                $"🔤 [HIDDEN WORD] " +
                $"Se intentó asignar una palabra " +
                $"vacía a {gameObject.name}."
            );


            return;
        }


        word =
            newWord.Trim();


        Debug.Log(
            $"🔤 [HIDDEN WORD] " +
            $"{gameObject.name} -> " +
            $"Palabra configurada: {word}"
        );
    }


    // =============================================================
    // DEBUG
    // =============================================================

    private void DebugLog(
        string message
    )
    {
        Debug.Log(
            message
        );
    }


    // =============================================================
    // CONTEXT MENU
    // =============================================================

    [ContextMenu("Reveal Word")]
    private void DebugReveal()
    {
        Reveal();
    }


    [ContextMenu("Reset Word")]
    private void DebugReset()
    {
        ResetWord();
    }
}