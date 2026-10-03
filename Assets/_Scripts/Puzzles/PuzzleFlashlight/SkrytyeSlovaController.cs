using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
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
    // CONFIGURACIÓN
    // =============================================================

    [Header("Player Configuration")]
    [SerializeField] private int requiredPlayers = 4;

    [SerializeField] private int wordsPerPlayer = 3;


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
    // ESTADO POR PLAYER
    // =============================================================

    private int[] wordsFoundByPlayer =
        new int[5];

    private HashSet<HiddenWord> registeredFoundWords =
        new HashSet<HiddenWord>();


    // =============================================================
    // PUBLIC PROPERTIES
    // =============================================================

    public int WordsFound =>
        wordsFound;

    public int TotalWords =>
        hiddenWords.Count;

    public bool SearchPhaseActive =>
        searchPhaseActive;

    public bool WordsCompleted =>
        wordsCompleted;


    // =============================================================
    // UNITY
    // =============================================================

    private void Awake()
    {
        Debug.Log(
            $"🔤 [SKRYTYE SLOVA] Awake -> {gameObject.name}"
        );


        // ---------------------------------------------------------
        // PUZZLE CONTROLLER
        // ---------------------------------------------------------

        if (puzzleController == null)
        {
            puzzleController =
                GetComponent<PuzzleController>();
        }


        if (puzzleController == null)
        {
            Debug.LogError(
                "🔤 [SKRYTYE SLOVA] " +
                "No se encontró PuzzleController."
            );
        }


        // ---------------------------------------------------------
        // ESTADO
        // ---------------------------------------------------------

        searchPhaseActive =
            false;

        wordsCompleted =
            false;

        wordsFound =
            0;


        registeredFoundWords.Clear();

        ResetPlayerCounters();


        // ---------------------------------------------------------
        // LIMPIAR LISTA
        // ---------------------------------------------------------

        CleanHiddenWordsList();


        Debug.Log(
            $"🔤 [SKRYTYE SLOVA] " +
            $"Palabras configuradas: {hiddenWords.Count}"
        );


        // ---------------------------------------------------------
        // ASIGNAR PALABRAS
        // ---------------------------------------------------------

        AssignWordsToPlayers();
    }


    private void Start()
    {
        Debug.Log(
            $"🔤 [SKRYTYE SLOVA] Start -> {gameObject.name}"
        );


        // ---------------------------------------------------------
        // FLASHLIGHTS
        // ---------------------------------------------------------

        if (autoFindFlashlights)
        {
            FindAllFlashlights();
        }


        DebugLog(
            $"🔤 [SKRYTYE SLOVA] " +
            $"Linternas encontradas: {flashlights.Count}"
        );


        // ---------------------------------------------------------
        // VOLVER A VALIDAR PLAYER IDS
        // ---------------------------------------------------------

        ValidateFlashlightPlayerIds();


        // ---------------------------------------------------------
        // MOSTRAR ASIGNACIÓN
        // ---------------------------------------------------------

        AssignWordsToPlayers();
    }


    // =============================================================
    // ACTIVAR PUZZLE
    // =============================================================

    public void ActivatePuzzle()
    {
        if (searchPhaseActive)
        {
            DebugLog(
                "🔤 [SKRYTYE SLOVA] " +
                "El puzzle ya está activo."
            );

            return;
        }


        searchPhaseActive =
            true;

        wordsCompleted =
            false;

        wordsFound =
            0;


        registeredFoundWords.Clear();

        ResetPlayerCounters();


        Debug.Log(
            "🔤🔦 [SKRYTYE SLOVA] " +
            "PUZZLE ACTIVADO"
        );


        // ---------------------------------------------------------
        // ASIGNAR PALABRAS
        // ---------------------------------------------------------

        AssignWordsToPlayers();


        // ---------------------------------------------------------
        // REINICIAR PALABRAS
        // ---------------------------------------------------------

        ResetAllWords();


        // ---------------------------------------------------------
        // VALIDAR FLASHLIGHTS
        // ---------------------------------------------------------

        ValidateFlashlightPlayerIds();


        // ---------------------------------------------------------
        // ACTIVAR FLASHLIGHTS
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
        searchPhaseActive =
            false;


        Debug.Log(
            "🔤 [SKRYTYE SLOVA] " +
            "FASE DE BÚSQUEDA DESACTIVADA"
        );


        DeactivateAllFlashlights();
    }


    // =============================================================
    // ASIGNAR PALABRAS
    // =============================================================

    private void AssignWordsToPlayers()
    {
        if (hiddenWords.Count == 0)
        {
            Debug.LogWarning(
                "🔤 [SKRYTYE SLOVA] " +
                "No hay palabras para asignar."
            );

            return;
        }


        Debug.Log(
            "🔤👥 [SKRYTYE SLOVA] " +
            "ASIGNANDO PALABRAS A JUGADORES..."
        );


        for (
            int i = 0;
            i < hiddenWords.Count;
            i++
        )
        {
            HiddenWord word =
                hiddenWords[i];


            if (word == null)
            {
                continue;
            }


            int playerId =
                GetPlayerForWord(
                    word.Word
                );


            // -----------------------------------------------------
            // SI NO SE RECONOCE
            // -----------------------------------------------------

            if (
                playerId < 1 ||
                playerId > 4
            )
            {
                // MUY IMPORTANTE:
                // quitamos cualquier Player ID viejo del Inspector.
                // Así una palabra desconocida NO queda perteneciendo
                // accidentalmente al Player 1.

                word.ClearPlayerAssignment();


                Debug.LogError(
                    $"🔤❌ [SKRYTYE SLOVA] " +
                    $"PALABRA NO RECONOCIDA: " +
                    $"'{word.Word}' en {word.name}"
                );


                continue;
            }


            // -----------------------------------------------------
            // ASIGNAR
            // -----------------------------------------------------

            word.AssignToPlayer(
                playerId
            );


            Debug.Log(
                $"🔤✅ [SKRYTYE SLOVA] " +
                $"'{word.Word}' -> PLAYER {playerId}"
            );
        }
    }


    // =============================================================
    // PLAYER DE UNA PALABRA
    // =============================================================

    private int GetPlayerForWord(
        string word
    )
    {
        if (
            string.IsNullOrWhiteSpace(word)
        )
        {
            return 0;
        }


        string normalized =
            NormalizeWord(
                word
            );


        switch (normalized)
        {
            // -----------------------------------------------------
            // PLAYER 1
            // -----------------------------------------------------

            case "CUANDO":
            case "OLVIDES":
            case "DETRAS":
                return 1;


            // -----------------------------------------------------
            // PLAYER 2
            // -----------------------------------------------------

            case "LAS":
            case "AL":
            case "TI":
                return 2;


            // -----------------------------------------------------
            // PLAYER 3
            // -----------------------------------------------------

            case "LUCES":
            case "NO":
            case "DE":
                return 3;


            // -----------------------------------------------------
            // PLAYER 4
            // -----------------------------------------------------

            case "SE":
            case "APAGUEN":
            case "PAYASO":
                return 4;
        }


        return 0;
    }


    // =============================================================
    // NORMALIZAR PALABRA
    // =============================================================

    private string NormalizeWord(
        string value
    )
    {
        string normalized =
            value
                .Trim()
                .ToUpperInvariant()
                .Normalize(
                    NormalizationForm.FormD
                );


        StringBuilder builder =
            new StringBuilder();


        for (
            int i = 0;
            i < normalized.Length;
            i++
        )
        {
            char character =
                normalized[i];


            UnicodeCategory category =
                CharUnicodeInfo.GetUnicodeCategory(
                    character
                );


            if (
                category !=
                UnicodeCategory.NonSpacingMark
            )
            {
                builder.Append(
                    character
                );
            }
        }


        return builder
            .ToString()
            .Normalize(
                NormalizationForm.FormC
            );
    }


    // =============================================================
    // VALIDAR SI PLAYER PUEDE REVELAR
    // =============================================================

    public bool CanPlayerRevealWord(
        HiddenWord word,
        int playerId
    )
    {
        if (
            playerId < 1 ||
            playerId > 4
        )
        {
            return false;
        }


        if (!searchPhaseActive)
        {
            return false;
        }


        if (word == null)
        {
            return false;
        }


        if (!hiddenWords.Contains(word))
        {
            return false;
        }


        if (word.IsRevealed)
        {
            return false;
        }


        // ---------------------------------------------------------
        // PALABRA SIN DUEÑO
        // ---------------------------------------------------------

        if (
            word.AssignedPlayerId < 1 ||
            word.AssignedPlayerId > 4
        )
        {
            DebugLog(
                $"🔤🚫 [SKRYTYE SLOVA] " +
                $"'{word.Word}' NO tiene Player asignado."
            );

            return false;
        }


        // ---------------------------------------------------------
        // PLAYER EQUIVOCADO
        // ---------------------------------------------------------

        if (
            word.AssignedPlayerId !=
            playerId
        )
        {
            DebugLog(
                $"🔤🚫 [SKRYTYE SLOVA] " +
                $"PLAYER {playerId} intentó revelar " +
                $"'{word.Word}', pero pertenece a " +
                $"PLAYER {word.AssignedPlayerId}."
            );

            return false;
        }


        // ---------------------------------------------------------
        // LIMITE POR PLAYER
        // ---------------------------------------------------------

        if (
            wordsFoundByPlayer[playerId] >=
            wordsPerPlayer
        )
        {
            DebugLog(
                $"🔤🚫 [SKRYTYE SLOVA] " +
                $"PLAYER {playerId} ya encontró sus " +
                $"{wordsPerPlayer} palabras."
            );

            return false;
        }


        return true;
    }


    // =============================================================
    // WORD FOUND
    // =============================================================

    public void WordFound(
        HiddenWord word,
        int playerId
    )
    {
        if (!searchPhaseActive)
        {
            return;
        }


        if (word == null)
        {
            return;
        }


        if (!hiddenWords.Contains(word))
        {
            return;
        }


        if (
            playerId < 1 ||
            playerId > 4
        )
        {
            return;
        }


        if (
            registeredFoundWords.Contains(
                word
            )
        )
        {
            return;
        }


        if (
            word.AssignedPlayerId !=
            playerId
        )
        {
            return;
        }


        registeredFoundWords.Add(
            word
        );


        wordsFound++;


        wordsFoundByPlayer[playerId]++;


        Debug.Log(
            $"🔤✨ [SKRYTYE SLOVA] " +
            $"PLAYER {playerId} ENCONTRÓ: " +
            $"{word.Word}"
        );


        Debug.Log(
            $"🔤 [SKRYTYE SLOVA] " +
            $"PLAYER {playerId}: " +
            $"{wordsFoundByPlayer[playerId]}/" +
            $"{wordsPerPlayer}"
        );


        Debug.Log(
            $"🔤 [SKRYTYE SLOVA] " +
            $"PROGRESO TOTAL: " +
            $"{wordsFound}/{hiddenWords.Count}"
        );


        CheckWordsCompleted();
    }


    // =============================================================
    // WORD FOUND LEGACY
    // =============================================================

    public void WordFound(
        HiddenWord word
    )
    {
        if (word == null)
        {
            return;
        }


        WordFound(
            word,
            word.AssignedPlayerId
        );
    }


    // =============================================================
    // CHECK COMPLETADO
    // =============================================================

    private void CheckWordsCompleted()
    {
        if (
            hiddenWords.Count == 0
        )
        {
            return;
        }


        for (
            int playerId = 1;
            playerId <= requiredPlayers;
            playerId++
        )
        {
            if (
                wordsFoundByPlayer[playerId] <
                wordsPerPlayer
            )
            {
                return;
            }
        }


        if (
            wordsFound <
            hiddenWords.Count
        )
        {
            return;
        }


        wordsCompleted =
            true;


        searchPhaseActive =
            false;


        Debug.Log(
            "🔤🎯 [SKRYTYE SLOVA] " +
            "LOS 4 JUGADORES ENCONTRARON " +
            "SUS 3 PALABRAS."
        );


        DeactivateAllFlashlights();


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
    }


    // =============================================================
    // VALIDAR FLASHLIGHT IDS
    // =============================================================

    private void ValidateFlashlightPlayerIds()
    {
        if (
            flashlights == null ||
            flashlights.Count == 0
        )
        {
            return;
        }


        for (
            int i = 0;
            i < flashlights.Count;
            i++
        )
        {
            FlashlightController flashlight =
                flashlights[i];


            if (flashlight == null)
            {
                continue;
            }


            int playerId =
                flashlight.GetPlayerId();


            Debug.Log(
                $"🔦🆔 [SKRYTYE SLOVA] " +
                $"Linterna {i + 1} -> " +
                $"PLAYER {playerId}"
            );
        }
    }


    // =============================================================
    // ACTIVATE FLASHLIGHTS
    // =============================================================

    private void ActivateAllFlashlights()
    {
        if (autoFindFlashlights)
        {
            FindAllFlashlights();
        }


        if (
            flashlights == null ||
            flashlights.Count == 0
        )
        {
            Debug.LogWarning(
                "🔤🔦 [SKRYTYE SLOVA] " +
                "No se encontraron FlashlightController."
            );

            return;
        }


        for (
            int i = 0;
            i < flashlights.Count;
            i++
        )
        {
            FlashlightController flashlight =
                flashlights[i];


            if (flashlight == null)
            {
                continue;
            }


            flashlight.ActivateFlashlight();


            DebugLog(
                $"🔦 [SKRYTYE SLOVA] " +
                $"Linterna {i + 1} ACTIVADA -> " +
                $"PLAYER {flashlight.GetPlayerId()}"
            );
        }
    }


    // =============================================================
    // DEACTIVATE FLASHLIGHTS
    // =============================================================

    private void DeactivateAllFlashlights()
    {
        if (flashlights == null)
        {
            return;
        }


        for (
            int i = 0;
            i < flashlights.Count;
            i++
        )
        {
            FlashlightController flashlight =
                flashlights[i];


            if (flashlight == null)
            {
                continue;
            }


            flashlight.DeactivateFlashlight();
        }
    }


    // =============================================================
    // FIND FLASHLIGHTS
    // =============================================================

    private void FindAllFlashlights()
    {
        FlashlightController[] foundFlashlights =
            FindObjectsOfType<FlashlightController>();


        flashlights.Clear();


        for (
            int i = 0;
            i < foundFlashlights.Length;
            i++
        )
        {
            FlashlightController flashlight =
                foundFlashlights[i];


            if (flashlight == null)
            {
                continue;
            }


            if (
                !flashlights.Contains(
                    flashlight
                )
            )
            {
                flashlights.Add(
                    flashlight
                );
            }
        }


        DebugLog(
            $"🔦 [SKRYTYE SLOVA] " +
            $"Flashlights detectadas: " +
            $"{flashlights.Count}"
        );
    }


    // =============================================================
    // RESET WORDS
    // =============================================================

    private void ResetAllWords()
    {
        for (
            int i = 0;
            i < hiddenWords.Count;
            i++
        )
        {
            HiddenWord word =
                hiddenWords[i];


            if (word == null)
            {
                continue;
            }


            word.ResetWord();
        }


        wordsFound =
            0;

        wordsCompleted =
            false;


        registeredFoundWords.Clear();

        ResetPlayerCounters();
    }


    // =============================================================
    // RESET PLAYER COUNTERS
    // =============================================================

    private void ResetPlayerCounters()
    {
        if (
            wordsFoundByPlayer == null ||
            wordsFoundByPlayer.Length != 5
        )
        {
            wordsFoundByPlayer =
                new int[5];
        }


        for (
            int i = 0;
            i < wordsFoundByPlayer.Length;
            i++
        )
        {
            wordsFoundByPlayer[i] =
                0;
        }
    }


    // =============================================================
    // CLEAN LIST
    // =============================================================

    private void CleanHiddenWordsList()
    {
        for (
            int i = hiddenWords.Count - 1;
            i >= 0;
            i--
        )
        {
            if (
                hiddenWords[i] == null
            )
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
            "🔤 [SKRYTYE SLOVA] " +
            "REINICIANDO PUZZLE"
        );


        searchPhaseActive =
            false;


        wordsCompleted =
            false;


        wordsFound =
            0;


        registeredFoundWords.Clear();


        ResetPlayerCounters();


        AssignWordsToPlayers();

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
                "🔤 [SKRYTYE SLOVA] " +
                "No hay PuzzleController asignado."
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
    // GET FOUND WORDS
    // =============================================================

    public List<HiddenWord> GetFoundWords()
    {
        List<HiddenWord> foundWords =
            new List<HiddenWord>();


        for (
            int i = 0;
            i < hiddenWords.Count;
            i++
        )
        {
            HiddenWord word =
                hiddenWords[i];


            if (word == null)
            {
                continue;
            }


            if (word.IsRevealed)
            {
                foundWords.Add(
                    word
                );
            }
        }


        return foundWords;
    }


    // =============================================================
    // GET WORDS FOUND BY PLAYER
    // =============================================================

    public int GetWordsFoundByPlayer(
        int playerId
    )
    {
        if (
            playerId < 1 ||
            playerId > 4
        )
        {
            return 0;
        }


        return wordsFoundByPlayer[playerId];
    }


    // =============================================================
    // DEBUG
    // =============================================================

    private void DebugLog(
        string message
    )
    {
        if (!debugLogs)
        {
            return;
        }


        Debug.Log(
            message
        );
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


    [ContextMenu("Assign Words To Players")]
    private void DebugAssignWords()
    {
        AssignWordsToPlayers();
    }


    [ContextMenu("Find Flashlights")]
    private void DebugFindFlashlights()
    {
        FindAllFlashlights();
    }


    [ContextMenu("Validate Flashlight IDs")]
    private void DebugValidateFlashlights()
    {
        ValidateFlashlightPlayerIds();
    }
}