using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

public class SkrytyeSlovaController : MonoBehaviour
{
    // =============================================================
    // PUZZLE
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
    // PHRASE NETWORK
    // =============================================================

    [Header("Phrase Network")]
    [SerializeField] private PhraseNetworkBridge phraseNetworkBridge;


    // =============================================================
    // PHRASE TIMER
    // =============================================================

    [Header("Phrase Timer")]
    [SerializeField] private float phraseTimeLimit = 60f;


    // =============================================================
    // PUZZLE STATE
    // =============================================================

    [Header("Puzzle State")]
    [SerializeField] private bool searchPhaseActive = false;

    [SerializeField] private bool wordsCompleted = false;

    [SerializeField] private int wordsFound = 0;


    // =============================================================
    // PLAYER WORD PROGRESS
    // =============================================================

    [Header("Player Word Progress")]
    [SerializeField] private int player1WordsFound = 0;

    [SerializeField] private int player2WordsFound = 0;

    [SerializeField] private int player3WordsFound = 0;

    [SerializeField] private int player4WordsFound = 0;


    // =============================================================
    // PHRASE STATE
    // =============================================================

    [Header("Phrase State")]
    [SerializeField] private bool phrasePhaseActive = false;

    [SerializeField] private bool phraseCompleted = false;

    [SerializeField] private bool player1PhraseCorrect = false;

    [SerializeField] private bool player2PhraseCorrect = false;

    [SerializeField] private bool player3PhraseCorrect = false;

    [SerializeField] private bool player4PhraseCorrect = false;


    // =============================================================
    // DEBUG
    // =============================================================

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;


    // =============================================================
    // CONSTANTES
    // =============================================================

    private const int TOTAL_PLAYERS = 4;
    private const int WORDS_PER_PLAYER = 3;
    private const int TOTAL_WORDS = 12;


    // =============================================================
    // FRASE CORRECTA
    // =============================================================

    /*
        FRASE COMPLETA:

        1  CUANDO
        2  LAS
        3  LUCES
        4  SE
        5  APAGUEN
        6  NO
        7  OLVIDES
        8  AL
        9  PAYASO
        10 DETRÁS
        11 DE
        12 TI

        DISTRIBUCIÓN:

        PLAYER 1
        CUANDO
        LAS
        LUCES

        PLAYER 2
        SE
        APAGUEN
        NO

        PLAYER 3
        OLVIDES
        AL
        PAYASO

        PLAYER 4
        DETRÁS
        DE
        TI
    */

    private static readonly string[] CorrectFullPhrase =
    {
        "CUANDO",
        "LAS",
        "LUCES",
        "SE",
        "APAGUEN",
        "NO",
        "OLVIDES",
        "AL",
        "PAYASO",
        "DETRÁS",
        "DE",
        "TI"
    };


    // =============================================================
    // UNITY
    // =============================================================

    private void Awake()
    {
        Debug.Log(
            "====================================================="
        );

        Debug.Log(
            $"🔤 [SKRYTYE SLOVA] Awake -> {gameObject.name}"
        );

        Debug.Log(
            "====================================================="
        );


        if (puzzleController == null)
        {
            puzzleController =
                GetComponent<PuzzleController>();
        }

        if (puzzleController == null)
        {
            Debug.LogError(
                "🔤❌ [SKRYTYE SLOVA] " +
                "No se encontró PuzzleController."
            );
        }


        if (phraseNetworkBridge == null)
        {
            phraseNetworkBridge =
                FindObjectOfType<PhraseNetworkBridge>();
        }


        CleanHiddenWordsList();


        searchPhaseActive = false;
        wordsCompleted = false;
        wordsFound = 0;

        phrasePhaseActive = false;
        phraseCompleted = false;


        ResetPlayerCounters();
        ResetPhrasePlayerStates();


        AssignWordsByListOrder();
    }


    private void Start()
    {
        if (phraseNetworkBridge == null)
        {
            phraseNetworkBridge =
                FindObjectOfType<PhraseNetworkBridge>();
        }


        if (phraseNetworkBridge != null)
        {
            phraseNetworkBridge.OnPhraseSubmission +=
                HandlePhraseSubmission;

            phraseNetworkBridge.OnPhraseTimeout +=
                HandlePhraseTimeout;
        }
        else
        {
            Debug.LogError(
                "🧩❌ [SKRYTYE SLOVA] " +
                "No se encontró PhraseNetworkBridge."
            );
        }


        if (autoFindFlashlights)
        {
            FindAllFlashlights();
        }


        ValidateFlashlights();
    }


    private void OnDestroy()
    {
        if (phraseNetworkBridge != null)
        {
            phraseNetworkBridge.OnPhraseSubmission -=
                HandlePhraseSubmission;

            phraseNetworkBridge.OnPhraseTimeout -=
                HandlePhraseTimeout;
        }
    }


    // =============================================================
    // ASIGNAR PALABRAS FÍSICAS A PLAYERS
    // =============================================================

    public void AssignWordsByListOrder()
    {
        Debug.Log(
            "====================================================="
        );

        Debug.Log(
            "🔤 [SKRYTYE SLOVA] ASIGNANDO PALABRAS SEGÚN LA FRASE"
        );


        if (hiddenWords.Count != TOTAL_WORDS)
        {
            Debug.LogError(
                $"🔤❌ [SKRYTYE SLOVA] " +
                $"Se esperaban {TOTAL_WORDS} palabras, " +
                $"pero hay {hiddenWords.Count}."
            );
        }


        for (int i = 0; i < hiddenWords.Count; i++)
        {
            HiddenWord word =
                hiddenWords[i];


            if (word == null)
            {
                Debug.LogError(
                    $"🔤❌ [SKRYTYE SLOVA] " +
                    $"Elemento {i} es NULL."
                );

                continue;
            }


            // La palabra decide el jugador (reparto de GetTargetWordsForPlayer, el mismo que se
            // envía a los celulares en la fase de frase). El orden de la lista solo es respaldo.
            int playerId =
                GetPlayerFromWord(word.Word);

            if (playerId == 0)
            {
                playerId =
                    GetPlayerFromIndex(i);

                Debug.LogWarning(
                    $"🔤⚠️ [SKRYTYE SLOVA] " +
                    $"'{word.Word}' no está en el reparto de la frase. " +
                    $"Se asigna por orden de lista -> PLAYER {playerId}"
                );
            }


            word.ClearPlayerAssignment();


            if (
                playerId >= 1 &&
                playerId <= TOTAL_PLAYERS
            )
            {
                word.AssignToPlayer(playerId);


                Debug.Log(
                    $"🔤✅ [ASIGNACIÓN] " +
                    $"Índice {i} | " +
                    $"'{word.Word}' -> PLAYER {playerId}"
                );
            }
        }


        Debug.Log(
            "====================================================="
        );
    }


    // =============================================================
    // PLAYER SEGÚN PALABRA (respaldo: según índice)
    // =============================================================

    private int GetPlayerFromWord(
        string word
    )
    {
        string normalized =
            NormalizeWord(word);

        if (string.IsNullOrEmpty(normalized))
        {
            return 0;
        }

        for (
            int playerId = 1;
            playerId <= TOTAL_PLAYERS;
            playerId++
        )
        {
            foreach (string target in GetTargetWordsForPlayer(playerId))
            {
                if (NormalizeWord(target) == normalized)
                {
                    return playerId;
                }
            }
        }

        return 0;
    }


    private int GetPlayerFromIndex(
        int index
    )
    {
        if (
            index >= 0 &&
            index <= 2
        )
        {
            return 1;
        }


        if (
            index >= 3 &&
            index <= 5
        )
        {
            return 2;
        }


        if (
            index >= 6 &&
            index <= 8
        )
        {
            return 3;
        }


        if (
            index >= 9 &&
            index <= 11
        )
        {
            return 4;
        }


        return 0;
    }


    // =============================================================
    // COMPROBAR SI PLAYER PUEDE REVELAR PALABRA
    // =============================================================

    public bool CanPlayerRevealWord(
        HiddenWord word,
        int playerId
    )
    {
        if (!searchPhaseActive)
        {
            return false;
        }


        if (
            playerId < 1 ||
            playerId > TOTAL_PLAYERS
        )
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


        if (
            word.AssignedPlayerId < 1 ||
            word.AssignedPlayerId > TOTAL_PLAYERS
        )
        {
            return false;
        }


        if (
            word.AssignedPlayerId !=
            playerId
        )
        {
            DebugLog(
                $"🔤🚫 [SKRYTYE SLOVA] " +
                $"P{playerId} apuntó a '{word.Word}', " +
                $"pero pertenece a P{word.AssignedPlayerId}."
            );

            return false;
        }


        if (
            GetPlayerWordsFound(playerId) >=
            WORDS_PER_PLAYER
        )
        {
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
            word.AssignedPlayerId !=
            playerId
        )
        {
            Debug.LogError(
                $"🔤❌ [SKRYTYE SLOVA] " +
                $"'{word.Word}' pertenece a " +
                $"P{word.AssignedPlayerId}, " +
                $"pero llegó P{playerId}."
            );

            return;
        }


        if (word.IsRevealed == false)
        {
            DebugLog(
                $"🔤⚠️ [SKRYTYE SLOVA] " +
                $"WordFound recibió '{word.Word}' " +
                $"antes de que aparezca como revelada."
            );
        }


        if (
            GetPlayerWordsFound(playerId) >=
            WORDS_PER_PLAYER
        )
        {
            return;
        }


        wordsFound++;

        AddPlayerWord(playerId);


        Debug.Log(
            $"🔤✨ [SKRYTYE SLOVA] " +
            $"PLAYER {playerId} encontró '{word.Word}'"
        );


        Debug.Log(
            $"🔤📊 [PROGRESO] " +
            $"P1={player1WordsFound}/3 | " +
            $"P2={player2WordsFound}/3 | " +
            $"P3={player3WordsFound}/3 | " +
            $"P4={player4WordsFound}/3"
        );


        CheckWordsCompleted();
    }


    // =============================================================
    // PROGRESO PLAYER
    // =============================================================

    private int GetPlayerWordsFound(
        int playerId
    )
    {
        switch (playerId)
        {
            case 1:
                return player1WordsFound;

            case 2:
                return player2WordsFound;

            case 3:
                return player3WordsFound;

            case 4:
                return player4WordsFound;
        }


        return 0;
    }


    private void AddPlayerWord(
        int playerId
    )
    {
        switch (playerId)
        {
            case 1:
                player1WordsFound++;
                break;

            case 2:
                player2WordsFound++;
                break;

            case 3:
                player3WordsFound++;
                break;

            case 4:
                player4WordsFound++;
                break;
        }
    }


    private void ResetPlayerCounters()
    {
        player1WordsFound = 0;
        player2WordsFound = 0;
        player3WordsFound = 0;
        player4WordsFound = 0;
    }


    // =============================================================
    // CHECK WORDS COMPLETED
    // =============================================================

    private void CheckWordsCompleted()
    {
        if (
            player1WordsFound < WORDS_PER_PLAYER ||
            player2WordsFound < WORDS_PER_PLAYER ||
            player3WordsFound < WORDS_PER_PLAYER ||
            player4WordsFound < WORDS_PER_PLAYER
        )
        {
            return;
        }


        wordsCompleted = true;
        searchPhaseActive = false;


        Debug.Log(
            "====================================================="
        );

        Debug.Log(
            "🔤🎯 [SKRYTYE SLOVA] " +
            "LOS 4 PLAYERS ENCONTRARON SUS 3 PALABRAS"
        );

        Debug.Log(
            "====================================================="
        );


        DeactivateAllFlashlights();


        StartPhrasePhase();
    }


    // =============================================================
    // PHASE DE FRASE
    // =============================================================

    private void StartPhrasePhase()
    {
        if (
            phrasePhaseActive ||
            phraseCompleted
        )
        {
            return;
        }


        if (phraseNetworkBridge == null)
        {
            phraseNetworkBridge =
                FindObjectOfType<PhraseNetworkBridge>();
        }


        if (phraseNetworkBridge == null)
        {
            Debug.LogError(
                "🧩❌ [SKRYTYE SLOVA] " +
                "No hay PhraseNetworkBridge."
            );

            return;
        }


        phrasePhaseActive = true;
        phraseCompleted = false;


        ResetPhrasePlayerStates();


        Debug.Log(
            "====================================================="
        );

        Debug.Log(
            "🧩⏱️ [SKRYTYE SLOVA] " +
            "INICIANDO FASE DE FRASE"
        );

        Debug.Log(
            $"🧩⏱️ Tiempo disponible: " +
            $"{phraseTimeLimit:F0} segundos"
        );

        Debug.Log(
            "🧩📝 FRASE CORRECTA:"
        );

        Debug.Log(
            "🧩📝 " +
            "CUANDO LAS LUCES SE APAGUEN " +
            "NO OLVIDES AL PAYASO DETRÁS DE TI"
        );

        Debug.Log(
            "====================================================="
        );


        // ---------------------------------------------------------
        // PLAYER 1
        // ---------------------------------------------------------

        phraseNetworkBridge.StartPhraseForPlayer(
            1,
            phraseTimeLimit,
            "CUANDO",
            "LAS",
            "LUCES"
        );


        // ---------------------------------------------------------
        // PLAYER 2
        // ---------------------------------------------------------

        phraseNetworkBridge.StartPhraseForPlayer(
            2,
            phraseTimeLimit,
            "SE",
            "APAGUEN",
            "NO"
        );


        // ---------------------------------------------------------
        // PLAYER 3
        // ---------------------------------------------------------

        phraseNetworkBridge.StartPhraseForPlayer(
            3,
            phraseTimeLimit,
            "OLVIDES",
            "AL",
            "PAYASO"
        );


        // ---------------------------------------------------------
        // PLAYER 4
        // ---------------------------------------------------------

        phraseNetworkBridge.StartPhraseForPlayer(
            4,
            phraseTimeLimit,
            "DETRÁS",
            "DE",
            "TI"
        );
    }


    // =============================================================
    // RECIBIR FRASE COMPLETA
    // =============================================================

    private void HandlePhraseSubmission(
        int playerId,
        string[] words
    )
    {
        if (!phrasePhaseActive)
        {
            DebugLog(
                "🧩⚠️ [SKRYTYE SLOVA] " +
                "Se recibió phrase_submit, pero " +
                "la fase de frase no está activa."
            );

            return;
        }


        if (
            playerId < 1 ||
            playerId > TOTAL_PLAYERS
        )
        {
            return;
        }


        if (IsPlayerPhraseCorrect(playerId))
        {
            return;
        }


        bool correct =
            IsCorrectFullPhrase(words);


        if (correct)
        {
            SetPlayerPhraseCorrect(
                playerId,
                true
            );


            Debug.Log(
                "====================================================="
            );

            Debug.Log(
                $"🧩✅ [SKRYTYE SLOVA] " +
                $"PLAYER {playerId} FRASE COMPLETA CORRECTA"
            );

            Debug.Log(
                $"🧩✅ FRASE RECIBIDA P{playerId}: " +
                $"{BuildPhraseLog(words)}"
            );

            Debug.Log(
                "====================================================="
            );


            phraseNetworkBridge?.SendPlayerResult(
                playerId,
                true,
                "¡ACERTASTE! La frase es correcta."
            );


            CheckPhraseCompleted();
        }
        else
        {
            Debug.Log(
                "====================================================="
            );

            Debug.Log(
                $"🧩❌ [SKRYTYE SLOVA] " +
                $"PLAYER {playerId} FRASE INCORRECTA"
            );

            Debug.Log(
                $"🧩❌ RECIBIDO P{playerId}: " +
                $"{BuildPhraseLog(words)}"
            );

            Debug.Log(
                $"🧩🎯 ESPERADO: " +
                $"{BuildPhraseLog(CorrectFullPhrase)}"
            );

            Debug.Log(
                "====================================================="
            );


            phraseNetworkBridge?.SendPlayerResult(
                playerId,
                false,
                "VUELVE A INTENTAR. La frase no es correcta."
            );
        }
    }


    // =============================================================
    // VALIDAR FRASE COMPLETA
    // =============================================================

    private bool IsCorrectFullPhrase(
        string[] words
    )
    {
        if (
            words == null ||
            words.Length != TOTAL_WORDS
        )
        {
            DebugLog(
                "🧩❌ [VALIDACIÓN] " +
                "La frase no contiene exactamente 12 palabras."
            );

            return false;
        }


        for (
            int i = 0;
            i < TOTAL_WORDS;
            i++
        )
        {
            string received =
                NormalizeWord(words[i]);

            string expected =
                NormalizeWord(
                    CorrectFullPhrase[i]
                );


            if (received != expected)
            {
                DebugLog(
                    $"🧩❌ [VALIDACIÓN] " +
                    $"Posición {i + 1}: " +
                    $"recibido='{received}' " +
                    $"esperado='{expected}'"
                );

                return false;
            }
        }


        return true;
    }


    // =============================================================
    // OBTENER LAS 3 PALABRAS DE CADA PLAYER
    // =============================================================

    private string[] GetTargetWordsForPlayer(
        int playerId
    )
    {
        switch (playerId)
        {
            case 1:

                return new[]
                {
                    "CUANDO",
                    "LAS",
                    "LUCES"
                };


            case 2:

                return new[]
                {
                    "SE",
                    "APAGUEN",
                    "NO"
                };


            case 3:

                return new[]
                {
                    "OLVIDES",
                    "AL",
                    "PAYASO"
                };


            case 4:

                return new[]
                {
                    "DETRÁS",
                    "DE",
                    "TI"
                };
        }


        return null;
    }


    // =============================================================
    // NORMALIZAR PALABRA
    // =============================================================

    private string NormalizeWord(
        string input
    )
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }


        string upper =
            input.Trim().ToUpperInvariant();


        string decomposed =
            upper.Normalize(
                NormalizationForm.FormD
            );


        StringBuilder builder =
            new StringBuilder();


        for (
            int i = 0;
            i < decomposed.Length;
            i++
        )
        {
            char character =
                decomposed[i];


            UnicodeCategory category =
                CharUnicodeInfo.GetUnicodeCategory(
                    character
                );


            if (
                category ==
                UnicodeCategory.NonSpacingMark
            )
            {
                continue;
            }


            if (
                char.IsLetterOrDigit(
                    character
                )
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
    // ESTADO FRASE POR PLAYER
    // =============================================================

    private bool IsPlayerPhraseCorrect(
        int playerId
    )
    {
        switch (playerId)
        {
            case 1:
                return player1PhraseCorrect;

            case 2:
                return player2PhraseCorrect;

            case 3:
                return player3PhraseCorrect;

            case 4:
                return player4PhraseCorrect;
        }


        return false;
    }


    private void SetPlayerPhraseCorrect(
        int playerId,
        bool value
    )
    {
        switch (playerId)
        {
            case 1:
                player1PhraseCorrect = value;
                break;

            case 2:
                player2PhraseCorrect = value;
                break;

            case 3:
                player3PhraseCorrect = value;
                break;

            case 4:
                player4PhraseCorrect = value;
                break;
        }
    }


    private void ResetPhrasePlayerStates()
    {
        player1PhraseCorrect = false;
        player2PhraseCorrect = false;
        player3PhraseCorrect = false;
        player4PhraseCorrect = false;
    }


    // =============================================================
    // CHECK FRASE COMPLETADA
    // =============================================================

    private void CheckPhraseCompleted()
    {
        if (
            !player1PhraseCorrect ||
            !player2PhraseCorrect ||
            !player3PhraseCorrect ||
            !player4PhraseCorrect
        )
        {
            DebugLog(
                $"🧩📊 [FRASE] " +
                $"P1={player1PhraseCorrect} | " +
                $"P2={player2PhraseCorrect} | " +
                $"P3={player3PhraseCorrect} | " +
                $"P4={player4PhraseCorrect}"
            );

            return;
        }


        phraseCompleted = true;
        phrasePhaseActive = false;


        Debug.Log(
            "====================================================="
        );

        Debug.Log(
            "🧩🎯 [SKRYTYE SLOVA] " +
            "LOS 4 PLAYERS COMPLETARON LA FRASE"
        );

        Debug.Log(
            "🧩🎉 FRASE FINAL:"
        );

        Debug.Log(
            "CUANDO LAS LUCES SE APAGUEN " +
            "NO OLVIDES AL PAYASO DETRÁS DE TI"
        );

        Debug.Log(
            "====================================================="
        );


        phraseNetworkBridge?.SendAllComplete();


        CompletePuzzle();
    }


    // =============================================================
    // PHRASE TIMEOUT
    // =============================================================

    private void HandlePhraseTimeout()
    {
        if (!phrasePhaseActive)
        {
            return;
        }


        phrasePhaseActive = false;
        phraseCompleted = false;


        Debug.Log(
            "====================================================="
        );

        Debug.Log(
            "🧩⏰ [SKRYTYE SLOVA] " +
            "SE ACABÓ EL TIEMPO DE LA FRASE"
        );

        Debug.Log(
            "====================================================="
        );


        if (puzzleController != null)
        {
            puzzleController.FailPuzzle();
        }
    }


    // =============================================================
    // FLASHLIGHTS
    // =============================================================

    private void ActivateAllFlashlights()
    {
        if (autoFindFlashlights)
        {
            FindAllFlashlights();
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
        }
    }


    private void DeactivateAllFlashlights()
    {
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


    private void FindAllFlashlights()
    {
        FlashlightController[] found =
            FindObjectsOfType<FlashlightController>();


        flashlights.Clear();


        for (
            int i = 0;
            i < found.Length;
            i++
        )
        {
            if (
                found[i] != null &&
                !flashlights.Contains(
                    found[i]
                )
            )
            {
                flashlights.Add(
                    found[i]
                );
            }
        }
    }


    // =============================================================
    // VALIDAR FLASHLIGHTS
    // =============================================================

    private void ValidateFlashlights()
    {
        bool[] foundPlayers =
            new bool[5];


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
                $"🔦 [SKRYTYE SLOVA] " +
                $"Linterna {i + 1} -> PLAYER {playerId}"
            );


            if (
                playerId >= 1 &&
                playerId <= 4
            )
            {
                foundPlayers[playerId] =
                    true;
            }
        }


        for (
            int playerId = 1;
            playerId <= 4;
            playerId++
        )
        {
            if (!foundPlayers[playerId])
            {
                Debug.LogError(
                    $"🔦❌ [SKRYTYE SLOVA] " +
                    $"Falta linterna para PLAYER {playerId}."
                );
            }
        }
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


        wordsFound = 0;
        wordsCompleted = false;


        ResetPlayerCounters();
    }


    // =============================================================
    // CLEAN HIDDEN WORDS
    // =============================================================

    private void CleanHiddenWordsList()
    {
        for (
            int i = hiddenWords.Count - 1;
            i >= 0;
            i--
        )
        {
            if (hiddenWords[i] == null)
            {
                hiddenWords.RemoveAt(i);
            }
        }
    }


    // =============================================================
    // ACTIVATE PUZZLE
    // =============================================================

    public void ActivatePuzzle()
    {
        if (searchPhaseActive)
        {
            return;
        }


        AssignWordsByListOrder();


        searchPhaseActive = true;
        wordsCompleted = false;
        wordsFound = 0;


        phrasePhaseActive = false;
        phraseCompleted = false;


        ResetPlayerCounters();
        ResetPhrasePlayerStates();


        ResetAllWords();


        ActivateAllFlashlights();


        Debug.Log(
            "====================================================="
        );

        Debug.Log(
            "🔤🔦 [SKRYTYE SLOVA] " +
            "FASE DE BÚSQUEDA INICIADA"
        );

        Debug.Log(
            "🔤📝 12 PALABRAS ASIGNADAS:"
        );

        Debug.Log(
            "P1 -> CUANDO / LAS / LUCES"
        );

        Debug.Log(
            "P2 -> SE / APAGUEN / NO"
        );

        Debug.Log(
            "P3 -> OLVIDES / AL / PAYASO"
        );

        Debug.Log(
            "P4 -> DETRÁS / DE / TI"
        );

        Debug.Log(
            "====================================================="
        );
    }


    // =============================================================
    // DEACTIVATE PUZZLE
    // =============================================================

    public void DeactivatePuzzle()
    {
        searchPhaseActive = false;
        phrasePhaseActive = false;


        DeactivateAllFlashlights();


        Debug.Log(
            "🔤 [SKRYTYE SLOVA] " +
            "PUZZLE DESACTIVADO"
        );
    }


    // =============================================================
    // RESET PUZZLE
    // =============================================================

    public void ResetPuzzle()
    {
        searchPhaseActive = false;
        wordsCompleted = false;
        wordsFound = 0;


        phrasePhaseActive = false;
        phraseCompleted = false;


        ResetPlayerCounters();
        ResetPhrasePlayerStates();


        AssignWordsByListOrder();
        ResetAllWords();
        DeactivateAllFlashlights();


        Debug.Log(
            "🔤 [SKRYTYE SLOVA] " +
            "PUZZLE REINICIADO"
        );
    }


    // =============================================================
    // COMPLETE PUZZLE
    // =============================================================

    public void CompletePuzzle()
    {
        if (!phraseCompleted)
        {
            DebugLog(
                "🔤 [SKRYTYE SLOVA] " +
                "No se puede completar: " +
                "la frase todavía no fue organizada correctamente."
            );

            return;
        }


        if (puzzleController == null)
        {
            Debug.LogError(
                "🔤❌ [SKRYTYE SLOVA] " +
                "No hay PuzzleController asignado."
            );

            return;
        }


        puzzleController.CompletePuzzle();
    }


    // =============================================================
    // GETTERS
    // =============================================================

    public List<HiddenWord> GetFoundWords()
    {
        List<HiddenWord> result =
            new List<HiddenWord>();


        for (
            int i = 0;
            i < hiddenWords.Count;
            i++
        )
        {
            HiddenWord word =
                hiddenWords[i];


            if (
                word != null &&
                word.IsRevealed
            )
            {
                result.Add(word);
            }
        }


        return result;
    }


    public bool AreAllWordsFound()
    {
        return wordsCompleted;
    }


    public bool IsPhrasePhaseActive()
    {
        return phrasePhaseActive;
    }


    public bool IsPhraseCompleted()
    {
        return phraseCompleted;
    }


    // =============================================================
    // BUILD PHRASE LOG
    // =============================================================

    private string BuildPhraseLog(
        string[] words
    )
    {
        if (
            words == null ||
            words.Length == 0
        )
        {
            return "(sin palabras)";
        }


        StringBuilder builder =
            new StringBuilder();


        for (
            int i = 0;
            i < words.Length;
            i++
        )
        {
            if (i > 0)
            {
                builder.Append(" ");
            }


            builder.Append(
                NormalizeWord(words[i])
            );
        }


        return builder.ToString();
    }


    // =============================================================
    // DEBUG LOG
    // =============================================================

    private void DebugLog(
        string message
    )
    {
        if (debugLogs)
        {
            Debug.Log(message);
        }
    }


    // =============================================================
    // CONTEXT MENU
    // =============================================================

    [ContextMenu("Asignar Palabras")]
    private void DebugAssignWords()
    {
        AssignWordsByListOrder();
    }


    [ContextMenu("Validar Linternas")]
    private void DebugValidateFlashlights()
    {
        ValidateFlashlights();
    }


    [ContextMenu("Activar Puzzle")]
    private void DebugActivatePuzzle()
    {
        ActivatePuzzle();
    }


    [ContextMenu("Reset Puzzle")]
    private void DebugResetPuzzle()
    {
        ResetPuzzle();
    }
}