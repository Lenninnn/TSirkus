using System.Collections.Generic;
using UnityEngine;

public class SkrytyeSlovaController : MonoBehaviour
{
    // =============================================================
    // PUZZLE GENERAL
    // =============================================================

    [Header("Puzzle General")]
    [SerializeField] private PuzzleController puzzleController;

    // =============================================================
    // HIDDEN WORDS
    // =============================================================

    [Header("Hidden Words")]
    [SerializeField] private List<HiddenWord> hiddenWords =
        new List<HiddenWord>();

    // =============================================================
    // FLASHLIGHTS
    // =============================================================

    [Header("Player Flashlights")]
    [SerializeField] private List<FlashlightController> flashlights =
        new List<FlashlightController>();

    [SerializeField] private bool autoFindFlashlights = true;

    // =============================================================
    // PUZZLE STATE
    // =============================================================

    [Header("Puzzle State")]
    [SerializeField] private bool searchPhaseActive = false;

    [SerializeField] private bool wordsCompleted = false;

    [SerializeField] private int wordsFound = 0;

    // =============================================================
    // DEBUG
    // =============================================================

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    // =============================================================
    // PUBLIC PROPERTIES
    // =============================================================

    public int WordsFound => wordsFound;

    public int TotalWords => hiddenWords.Count;

    public bool SearchPhaseActive => searchPhaseActive;

    public bool WordsCompleted => wordsCompleted;

    // =============================================================
    // UNITY
    // =============================================================

    private void Awake()
    {
        Debug.Log(
            $"🔤 [SKRYTYE SLOVA] Awake -> {gameObject.name}"
        );

        // ---------------------------------------------------------
        // BUSCAR PUZZLE CONTROLLER
        // ---------------------------------------------------------

        if (puzzleController == null)
        {
            puzzleController =
                GetComponent<PuzzleController>();
        }

        if (puzzleController == null)
        {
            Debug.LogError(
                "🔤 [SKRYTYE SLOVA] No se encontró " +
                "PuzzleController."
            );
        }

        // ---------------------------------------------------------
        // ESTADO INICIAL
        // ---------------------------------------------------------

        searchPhaseActive = false;
        wordsCompleted = false;
        wordsFound = 0;

        // ---------------------------------------------------------
        // BUSCAR PALABRAS
        // ---------------------------------------------------------

        CleanHiddenWordsList();

        Debug.Log(
            $"🔤 [SKRYTYE SLOVA] Palabras configuradas: " +
            $"{hiddenWords.Count}"
        );
    }

    private void Start()
    {
        Debug.Log(
            $"🔤 [SKRYTYE SLOVA] Start -> {gameObject.name}"
        );

        // ---------------------------------------------------------
        // BUSCAR LINTERNA AUTOMÁTICAMENTE
        // ---------------------------------------------------------

        if (autoFindFlashlights)
        {
            FindAllFlashlights();
        }

        DebugLog(
            $"🔤 [SKRYTYE SLOVA] Linternas encontradas: " +
            $"{flashlights.Count}"
        );
    }

    // =============================================================
    // ACTIVAR PUZZLE
    // =============================================================

    public void ActivatePuzzle()
    {
        if (searchPhaseActive)
        {
            DebugLog(
                "🔤 [SKRYTYE SLOVA] El puzzle ya está activo."
            );

            return;
        }

        searchPhaseActive = true;
        wordsCompleted = false;
        wordsFound = 0;

        Debug.Log(
            "🔤🔦 [SKRYTYE SLOVA] PUZZLE ACTIVADO"
        );

        // ---------------------------------------------------------
        // REINICIAR PALABRAS
        // ---------------------------------------------------------

        ResetAllWords();

        // ---------------------------------------------------------
        // ACTIVAR LINTERNA DE CADA JUGADOR
        // ---------------------------------------------------------

        ActivateAllFlashlights();

        Debug.Log(
            "🔤🔦 [SKRYTYE SLOVA] " +
            "FASE DE BÚSQUEDA INICIADA"
        );
    }

    // =============================================================
    // DESACTIVAR PUZZLE
    // =============================================================

    public void DeactivatePuzzle()
    {
        searchPhaseActive = false;

        Debug.Log(
            "🔤 [SKRYTYE SLOVA] FASE DE BÚSQUEDA DESACTIVADA"
        );

        DeactivateAllFlashlights();
    }

    // =============================================================
    // WORD FOUND
    // =============================================================

    public void WordFound(HiddenWord word)
    {
        if (!searchPhaseActive)
        {
            DebugLog(
                "🔤 [SKRYTYE SLOVA] Se encontró una palabra " +
                "pero la fase de búsqueda no está activa."
            );

            return;
        }

        if (word == null)
        {
            Debug.LogWarning(
                "🔤 [SKRYTYE SLOVA] WordFound recibió una " +
                "palabra NULL."
            );

            return;
        }

        // ---------------------------------------------------------
        // EVITAR CONTAR DOS VECES LA MISMA PALABRA
        // ---------------------------------------------------------

        if (!hiddenWords.Contains(word))
        {
            DebugLog(
                $"🔤 [SKRYTYE SLOVA] La palabra {word.name} " +
                "no pertenece a este puzzle."
            );

            return;
        }

        // ---------------------------------------------------------
        // CONTAR PALABRA
        // ---------------------------------------------------------

        wordsFound++;

        Debug.Log(
            $"🔤✨ [SKRYTYE SLOVA] PALABRA ENCONTRADA: " +
            $"{word.Word}"
        );

        Debug.Log(
            $"🔤 [SKRYTYE SLOVA] Progreso: " +
            $"{wordsFound}/{hiddenWords.Count}"
        );

        // ---------------------------------------------------------
        // COMPROBAR SI TERMINARON TODAS
        // ---------------------------------------------------------

        CheckWordsCompleted();
    }

    // =============================================================
    // CHECK WORDS COMPLETED
    // =============================================================

    private void CheckWordsCompleted()
    {
        if (hiddenWords.Count == 0)
        {
            Debug.LogWarning(
                "🔤 [SKRYTYE SLOVA] No hay palabras configuradas."
            );

            return;
        }

        if (wordsFound < hiddenWords.Count)
            return;

        wordsCompleted = true;

        Debug.Log(
            "🔤🎯 [SKRYTYE SLOVA] " +
            "TODAS LAS PALABRAS FUERON ENCONTRADAS"
        );

        // ---------------------------------------------------------
        // DETENER BÚSQUEDA
        // ---------------------------------------------------------

        searchPhaseActive = false;

        // ---------------------------------------------------------
        // APAGAR LINTERNA
        // ---------------------------------------------------------

        DeactivateAllFlashlights();

        // ---------------------------------------------------------
        // SIGUIENTE FASE
        // ---------------------------------------------------------

        StartPhrasePhase();
    }

    // =============================================================
    // PHRASE PHASE
    // =============================================================

    private void StartPhrasePhase()
    {
        Debug.Log(
            "🔤🧩 [SKRYTYE SLOVA] " +
            "FASE DE FRASE PREPARADA"
        );

        /*
         * AQUÍ agregaremos posteriormente:
         *
         * - UI de palabras encontradas
         * - selección de palabras
         * - orden de palabras
         * - botón confirmar
         * - comprobación de frase correcta
         *
         * Todavía NO completamos el puzzle aquí.
         */
    }

    // =============================================================
    // ACTIVATE ALL FLASHLIGHTS
    // =============================================================

    private void ActivateAllFlashlights()
    {
        if (autoFindFlashlights)
        {
            FindAllFlashlights();
        }

        if (flashlights.Count == 0)
        {
            Debug.LogWarning(
                "🔤🔦 [SKRYTYE SLOVA] " +
                "No se encontraron FlashlightController."
            );

            return;
        }

        for (int i = 0; i < flashlights.Count; i++)
        {
            FlashlightController flashlight =
                flashlights[i];

            if (flashlight == null)
                continue;

            flashlight.ActivateFlashlight();

            DebugLog(
                $"🔦 [SKRYTYE SLOVA] " +
                $"Linterna {i + 1} ACTIVADA."
            );
        }
    }

    // =============================================================
    // DEACTIVATE ALL FLASHLIGHTS
    // =============================================================

    private void DeactivateAllFlashlights()
    {
        for (int i = 0; i < flashlights.Count; i++)
        {
            FlashlightController flashlight =
                flashlights[i];

            if (flashlight == null)
                continue;

            flashlight.DeactivateFlashlight();

            DebugLog(
                $"🔦 [SKRYTYE SLOVA] " +
                $"Linterna {i + 1} DESACTIVADA."
            );
        }
    }

    // =============================================================
    // FIND ALL FLASHLIGHTS
    // =============================================================

    private void FindAllFlashlights()
    {
        FlashlightController[] foundFlashlights =
            FindObjectsOfType<FlashlightController>();

        flashlights.Clear();

        for (int i = 0; i < foundFlashlights.Length; i++)
        {
            FlashlightController flashlight =
                foundFlashlights[i];

            if (flashlight == null)
                continue;

            if (!flashlights.Contains(flashlight))
            {
                flashlights.Add(flashlight);
            }
        }

        DebugLog(
            $"🔦 [SKRYTYE SLOVA] " +
            $"Flashlights detectadas: {flashlights.Count}"
        );
    }

    // =============================================================
    // RESET WORDS
    // =============================================================

    private void ResetAllWords()
    {
        for (int i = 0; i < hiddenWords.Count; i++)
        {
            HiddenWord word = hiddenWords[i];

            if (word == null)
                continue;

            word.ResetWord();
        }

        wordsFound = 0;
        wordsCompleted = false;

        DebugLog(
            "🔤 [SKRYTYE SLOVA] Todas las palabras reiniciadas."
        );
    }

    // =============================================================
    // CLEAN LIST
    // =============================================================

    private void CleanHiddenWordsList()
    {
        for (int i = hiddenWords.Count - 1; i >= 0; i--)
        {
            if (hiddenWords[i] == null)
            {
                hiddenWords.RemoveAt(i);
            }
        }
    }

    // =============================================================
    // RESET PUZZLE
    // =============================================================

    public void ResetPuzzle()
    {
        Debug.Log(
            "🔤 [SKRYTYE SLOVA] REINICIANDO PUZZLE"
        );

        searchPhaseActive = false;
        wordsCompleted = false;
        wordsFound = 0;

        ResetAllWords();

        DeactivateAllFlashlights();
    }

    // =============================================================
    // COMPLETE PUZZLE
    // =============================================================

    public void CompletePuzzle()
    {
        if (!wordsCompleted)
        {
            DebugLog(
                "🔤 [SKRYTYE SLOVA] " +
                "No se puede completar todavía."
            );

            return;
        }

        if (puzzleController == null)
        {
            Debug.LogError(
                "🔤 [SKRYTYE SLOVA] No hay PuzzleController " +
                "asignado."
            );

            return;
        }

        Debug.Log(
            "🔤🎉 [SKRYTYE SLOVA] " +
            "PUZZLE COMPLETADO"
        );

        puzzleController.CompletePuzzle();
    }

    // =============================================================
    // PUBLIC GETTERS
    // =============================================================

    public List<HiddenWord> GetFoundWords()
    {
        List<HiddenWord> foundWords =
            new List<HiddenWord>();

        for (int i = 0; i < hiddenWords.Count; i++)
        {
            HiddenWord word = hiddenWords[i];

            if (word == null)
                continue;

            if (word.IsRevealed)
            {
                foundWords.Add(word);
            }
        }

        return foundWords;
    }

    public bool AreAllWordsFound()
    {
        return wordsCompleted;
    }

    // =============================================================
    // DEBUG
    // =============================================================

    private void DebugLog(string message)
    {
        if (!debugLogs)
            return;

        Debug.Log(message);
    }

    // =============================================================
    // CONTEXT MENU
    // =============================================================

    [ContextMenu("Activate Puzzle")]
    private void DebugActivatePuzzle()
    {
        ActivatePuzzle();
    }

    [ContextMenu("Deactivate Puzzle")]
    private void DebugDeactivatePuzzle()
    {
        DeactivatePuzzle();
    }

    [ContextMenu("Reset Puzzle")]
    private void DebugResetPuzzle()
    {
        ResetPuzzle();
    }

    [ContextMenu("Find Flashlights")]
    private void DebugFindFlashlights()
    {
        FindAllFlashlights();
    }
}