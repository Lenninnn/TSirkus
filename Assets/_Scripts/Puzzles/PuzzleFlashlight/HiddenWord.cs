using UnityEngine;
using TMPro;

public class HiddenWord : MonoBehaviour
{
    [Header("Word")]
    [SerializeField] private string word = "SKRYTYE";

    [Header("Visual")]
    [SerializeField] private TMP_Text wordText;

    [Header("State")]
    [SerializeField] private bool revealed = false;

    private SkrytyeSlovaController puzzleController;

    public string Word => word;
    public bool IsRevealed => revealed;

    private void Awake()
    {
        // ---------------------------------------------------------
        // BUSCAR TEXTO AUTOMÁTICAMENTE
        // ---------------------------------------------------------

        if (wordText == null)
        {
            wordText = GetComponentInChildren<TMP_Text>();

            if (wordText == null)
            {
                Debug.LogError(
                    $"🔤 [HIDDEN WORD] No se encontró un componente " +
                    $"TextMeshPro en {gameObject.name}."
                );
            }
        }

        // ---------------------------------------------------------
        // BUSCAR CONTROLADOR DEL PUZZLE
        // ---------------------------------------------------------

        puzzleController =
            FindObjectOfType<SkrytyeSlovaController>();

        // ---------------------------------------------------------
        // ESTADO INICIAL
        // ---------------------------------------------------------

        revealed = false;

        HideWord();

        Debug.Log(
            $"🔤 [HIDDEN WORD] {gameObject.name} " +
            $"preparada. Palabra: {word}"
        );
    }

    // =============================================================
    // REVEAL
    // =============================================================

    public void Reveal()
    {
        // Si ya fue encontrada, no hacemos nada.
        if (revealed)
            return;

        revealed = true;

        ShowWord();

        Debug.Log(
            $"🔤✨ [HIDDEN WORD] PALABRA ENCONTRADA -> " +
            $"{word}"
        );

        // Avisar al controlador general del puzzle.
        if (puzzleController != null)
        {
            puzzleController.WordFound(this);
        }
        else
        {
            Debug.LogWarning(
                $"🔤 [HIDDEN WORD] {gameObject.name} " +
                $"no encontró un SkrytyeSlovaController."
            );
        }
    }

    // =============================================================
    // HIDE
    // =============================================================

    private void HideWord()
    {
        if (wordText != null)
            wordText.enabled = false;
    }

    // =============================================================
    // SHOW
    // =============================================================

    private void ShowWord()
    {
        if (wordText != null)
            wordText.enabled = true;
    }

    // =============================================================
    // RESET
    // =============================================================

    public void ResetWord()
    {
        revealed = false;

        HideWord();

        Debug.Log(
            $"🔤 [HIDDEN WORD] {gameObject.name} " +
            $"reiniciada."
        );
    }

    // =============================================================
    // CONFIGURACIÓN
    // =============================================================

    public void SetWord(string newWord)
    {
        if (string.IsNullOrWhiteSpace(newWord))
        {
            Debug.LogWarning(
                $"🔤 [HIDDEN WORD] Se intentó asignar " +
                $"una palabra vacía a {gameObject.name}."
            );

            return;
        }

        word = newWord;

        Debug.Log(
            $"🔤 [HIDDEN WORD] {gameObject.name} -> " +
            $"Palabra configurada: {word}"
        );
    }

    // =============================================================
    // DEBUG MANUAL
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