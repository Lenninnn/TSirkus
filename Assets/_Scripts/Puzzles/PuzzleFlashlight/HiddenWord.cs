using UnityEngine;
using TMPro;
using System.Collections.Generic;

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
    // PLAYER ASSIGNMENT
    // =============================================================

    [Header("Player Assignment")]
    [SerializeField] private int assignedPlayerId = 0;


    // =============================================================
    // PRIVATE VISUAL
    // =============================================================

    [Header("Private Visual")]
    [SerializeField] private bool usePrivatePlayerView = true;

    [Tooltip("Capas privadas usadas para aislar las palabras de P1-P4. Usa capas de usuario, por ejemplo 28-31.")]
    [SerializeField] private int player1PrivateLayer = 28;

    [SerializeField] private int player2PrivateLayer = 29;

    [SerializeField] private int player3PrivateLayer = 30;

    [SerializeField] private int player4PrivateLayer = 31;

    [SerializeField] private float privateTextOffsetY = 0f;


    // =============================================================
    // PUZZLE
    // =============================================================

    private SkrytyeSlovaController puzzleController;


    // =============================================================
    // PRIVATE VIEW STATE
    // =============================================================

    private readonly Dictionary<int, PrivateWordView> privateViews =
        new Dictionary<int, PrivateWordView>();


    // =============================================================
    // PUBLIC
    // =============================================================

    public string Word => word;

    public bool IsRevealed => revealed;

    public int AssignedPlayerId => assignedPlayerId;


    // =============================================================
    // UNITY
    // =============================================================

    private void Awake()
    {
        if (wordText == null)
        {
            wordText = GetComponentInChildren<TMP_Text>();

            if (wordText == null)
            {
                Debug.LogError(
                    $"🔤❌ [HIDDEN WORD] {gameObject.name} no tiene TMP_Text."
                );
            }
        }

        puzzleController = FindObjectOfType<SkrytyeSlovaController>();

        revealed = false;

        HideWord();

        Debug.Log(
            $"🔤 [HIDDEN WORD] {gameObject.name} -> " +
            $"Palabra: '{word}' | Player inicial: {assignedPlayerId}"
        );
    }


    // =============================================================
    // ASIGNAR PLAYER
    // =============================================================

    public void AssignToPlayer(int playerId)
    {
        if (playerId < 1 || playerId > 4)
        {
            Debug.LogError(
                $"🔤❌ [HIDDEN WORD] Player ID inválido: {playerId}"
            );
            return;
        }

        assignedPlayerId = playerId;

        Debug.Log(
            $"🔤✅ [HIDDEN WORD] {gameObject.name} | " +
            $"'{word}' -> PLAYER {assignedPlayerId}"
        );
    }


    // =============================================================
    // LIMPIAR PLAYER
    // =============================================================

    public void ClearPlayerAssignment()
    {
        assignedPlayerId = 0;

        Debug.Log(
            $"🔤⚪ [HIDDEN WORD] {gameObject.name} | " +
            $"'{word}' -> PLAYER 0"
        );
    }


    // =============================================================
    // REVEAL
    // =============================================================

    public bool TryRevealForPlayer(int playerId)
    {
        return TryRevealForPlayer(playerId, null);
    }


    // =============================================================
    // REVEAL + PLAYER CAMERA
    // =============================================================

    public bool TryRevealForPlayer(int playerId, Camera playerCamera)
    {
        if (revealed)
        {
            return false;
        }

        if (playerId < 1 || playerId > 4)
        {
            return false;
        }

        if (puzzleController == null)
        {
            puzzleController = FindObjectOfType<SkrytyeSlovaController>();
        }

        if (puzzleController == null)
        {
            Debug.LogError(
                "🔤❌ [HIDDEN WORD] No se encontró SkrytyeSlovaController."
            );
            return false;
        }

        if (!puzzleController.CanPlayerRevealWord(this, playerId))
        {
            return false;
        }

        revealed = true;

        if (usePrivatePlayerView)
        {
            if (playerCamera == null)
            {
                playerCamera = FindPlayerCamera(playerId);
            }

            if (playerCamera != null)
            {
                CreatePrivatePlayerView(playerId, playerCamera);
            }
            else
            {
                Debug.LogError(
                    $"🔤❌ [HIDDEN WORD] '{word}' fue revelada por P{playerId}, " +
                    "pero no se encontró su cámara."
                );
            }
        }
        else
        {
            ShowWord();
        }

        Debug.Log(
            $"🔤✨ [HIDDEN WORD] '{word}' REVELADA POR PLAYER {playerId}"
        );

        puzzleController.WordFound(this, playerId);

        return true;
    }


    // =============================================================
    // REVEAL MANUAL
    // =============================================================

    public void Reveal()
    {
        if (revealed)
        {
            return;
        }

        if (assignedPlayerId < 1 || assignedPlayerId > 4)
        {
            Debug.LogWarning(
                $"🔤⚠️ [HIDDEN WORD] '{word}' no tiene Player asignado."
            );
            return;
        }

        TryRevealForPlayer(assignedPlayerId);
    }


    // =============================================================
    // HIDE WORD
    // =============================================================

    private void HideWord()
    {
        if (wordText != null)
        {
            wordText.enabled = false;
        }
    }


    // =============================================================
    // SHOW WORD
    // =============================================================

    private void ShowWord()
    {
        if (wordText != null)
        {
            wordText.enabled = true;
        }
    }


    // =============================================================
    // FIND PLAYER CAMERA
    // =============================================================

    private Camera FindPlayerCamera(int playerId)
    {
        PlayerIdentity[] identities =
            FindObjectsOfType<PlayerIdentity>(true);

        for (int i = 0; i < identities.Length; i++)
        {
            PlayerIdentity identity = identities[i];

            if (identity == null || identity.PlayerId != playerId)
            {
                continue;
            }

            Camera[] cameras =
                identity.GetComponentsInChildren<Camera>(true);

            for (int c = 0; c < cameras.Length; c++)
            {
                if (cameras[c] != null)
                {
                    return cameras[c];
                }
            }
        }

        Camera[] allCameras = FindObjectsOfType<Camera>(true);

        for (int i = 0; i < allCameras.Length; i++)
        {
            Camera candidate = allCameras[i];

            if (candidate == null)
            {
                continue;
            }

            PlayerIdentity identity =
                candidate.GetComponentInParent<PlayerIdentity>();

            if (identity != null && identity.PlayerId == playerId)
            {
                return candidate;
            }
        }

        return null;
    }


    // =============================================================
    // PRIVATE VIEW
    // =============================================================

    private void CreatePrivatePlayerView(int playerId, Camera playerCamera)
    {
        if (playerCamera == null)
        {
            return;
        }

        if (privateViews.ContainsKey(playerId) && privateViews[playerId] != null)
        {
            privateViews[playerId].Refresh();
            return;
        }

        int privateLayer = GetPrivateLayer(playerId);

        if (!IsValidLayer(privateLayer))
        {
            Debug.LogError(
                $"🔤❌ [HIDDEN WORD] Capa privada inválida para P{playerId}: " +
                privateLayer
            );
            return;
        }

        ConfigurePrivateLayerForCamera(playerCamera, privateLayer);

        PrivateWordView view = null;

        TextMeshPro worldText =
            wordText as TextMeshPro;

        TextMeshProUGUI uiText =
            wordText as TextMeshProUGUI;

        if (worldText != null)
        {
            view = CreateWorldTextClone(
                playerId,
                playerCamera,
                privateLayer,
                worldText
            );
        }
        else if (uiText != null)
        {
            view = CreateWorldSpaceUiClone(
                playerId,
                playerCamera,
                privateLayer,
                uiText
            );
        }
        else
        {
            Debug.LogError(
                $"🔤❌ [HIDDEN WORD] '{word}' usa un tipo de TMP_Text " +
                "no soportado para la vista privada."
            );
        }

        if (view != null)
        {
            privateViews[playerId] = view;

            Debug.Log(
                $"🔤👁️ [HIDDEN WORD] Vista privada WORLD creada | " +
                $"Palabra: '{word}' | P{playerId} | " +
                $"Cámara: {playerCamera.name} | " +
                $"Layer: {privateLayer}"
            );
        }
    }


    // =============================================================
    // WORLD TEXT CLONE
    // =============================================================

    private PrivateWordView CreateWorldTextClone(
        int playerId,
        Camera playerCamera,
        int privateLayer,
        TextMeshPro source
    )
    {
        if (source == null)
        {
            return null;
        }

        GameObject cloneObject = new GameObject(
            $"HiddenWordPrivate_P{playerId}_{gameObject.name}"
        );

        cloneObject.transform.SetPositionAndRotation(
            source.transform.position,
            source.transform.rotation
        );

        cloneObject.transform.localScale =
            source.transform.lossyScale;

        cloneObject.layer = privateLayer;

        TextMeshPro cloneText =
            cloneObject.AddComponent<TextMeshPro>();

        CopyTextAppearance(cloneText);

        cloneText.text = word;
        cloneText.enabled = true;

        // Mantener exactamente la escala/posición visual del original.
        cloneText.transform.SetPositionAndRotation(
            source.transform.position,
            source.transform.rotation
        );

        cloneText.transform.localScale =
            source.transform.lossyScale;

        cloneText.transform.localPosition +=
            source.transform.TransformVector(Vector3.zero);

        SetLayerRecursively(cloneObject, privateLayer);

        return cloneObject.AddComponent<PrivateWordView>()
            .Initialize(
                this,
                playerCamera,
                source.transform,
                cloneObject.transform,
                cloneText
            );
    }


    // =============================================================
    // WORLD SPACE UI CLONE
    // =============================================================

    private PrivateWordView CreateWorldSpaceUiClone(
        int playerId,
        Camera playerCamera,
        int privateLayer,
        TextMeshProUGUI source
    )
    {
        if (source == null)
        {
            return null;
        }

        RectTransform sourceRect =
            source.rectTransform;

        GameObject canvasObject = new GameObject(
            $"HiddenWordPrivateCanvas_P{playerId}_{gameObject.name}"
        );

        canvasObject.transform.SetPositionAndRotation(
            sourceRect.position,
            sourceRect.rotation
        );

        Vector3 sourceLossyScale =
            sourceRect.lossyScale;

        float uniformScale =
            Mathf.Max(
                0.00001f,
                (Mathf.Abs(sourceLossyScale.x) +
                 Mathf.Abs(sourceLossyScale.y) +
                 Mathf.Abs(sourceLossyScale.z)) / 3f
            );

        canvasObject.transform.localScale =
            Vector3.one * uniformScale;

        SetLayerRecursively(canvasObject, privateLayer);

        Canvas canvas =
            canvasObject.AddComponent<Canvas>();

        canvas.renderMode =
            RenderMode.WorldSpace;

        canvas.worldCamera =
            playerCamera;

        canvas.overrideSorting =
            true;

        canvas.sortingOrder =
            5000;

        RectTransform canvasRect =
            canvas.GetComponent<RectTransform>();

        canvasRect.sizeDelta =
            sourceRect.rect.size;

        canvasRect.pivot =
            new Vector2(0.5f, 0.5f);

        TextMeshProUGUI cloneText = null;

        GameObject textObject = new GameObject(
            $"PrivateWordText_{gameObject.name}"
        );

        textObject.transform.SetParent(
            canvasObject.transform,
            false
        );

        SetLayerRecursively(textObject, privateLayer);

        cloneText =
            textObject.AddComponent<TextMeshProUGUI>();

        CopyTextAppearance(cloneText);

        cloneText.text = word;
        cloneText.enabled = true;
        cloneText.raycastTarget = false;

        RectTransform cloneRect =
            cloneText.rectTransform;

        cloneRect.anchorMin =
            new Vector2(0.5f, 0.5f);

        cloneRect.anchorMax =
            new Vector2(0.5f, 0.5f);

        cloneRect.pivot =
            sourceRect.pivot;

        cloneRect.sizeDelta =
            sourceRect.rect.size;

        cloneRect.anchoredPosition =
            Vector2.zero;

        cloneRect.localRotation =
            Quaternion.identity;

        cloneRect.localScale =
            Vector3.one;

        return canvasObject.AddComponent<PrivateWordView>()
            .Initialize(
                this,
                playerCamera,
                source.transform,
                canvasObject.transform,
                cloneText
            );
    }


    // =============================================================
    // LAYER CONFIGURATION
    // =============================================================

    private void ConfigurePrivateLayerForCamera(
        Camera targetCamera,
        int privateLayer
    )
    {
        int layerMask =
            1 << privateLayer;

        Camera[] cameras =
            FindObjectsOfType<Camera>(true);

        for (int i = 0; i < cameras.Length; i++)
        {
            Camera camera = cameras[i];

            if (camera == null)
            {
                continue;
            }

            if (camera == targetCamera)
            {
                camera.cullingMask |= layerMask;
            }
            else
            {
                camera.cullingMask &= ~layerMask;
            }
        }
    }


    private int GetPrivateLayer(int playerId)
    {
        switch (playerId)
        {
            case 1:
                return player1PrivateLayer;
            case 2:
                return player2PrivateLayer;
            case 3:
                return player3PrivateLayer;
            case 4:
                return player4PrivateLayer;
            default:
                return -1;
        }
    }


    private bool IsValidLayer(int layer)
    {
        return layer >= 0 && layer < 32;
    }


    private void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null)
        {
            return;
        }

        target.layer = layer;

        for (int i = 0; i < target.transform.childCount; i++)
        {
            SetLayerRecursively(
                target.transform.GetChild(i).gameObject,
                layer
            );
        }
    }


    // =============================================================
    // COPY TEXT APPEARANCE
    // =============================================================

    private void CopyTextAppearance(TMP_Text target)
    {
        if (target == null || wordText == null)
        {
            return;
        }

        target.font =
            wordText.font;

        target.fontSharedMaterial =
            wordText.fontSharedMaterial;

        target.fontSize =
            wordText.fontSize;

        target.fontStyle =
            wordText.fontStyle;

        target.alignment =
            wordText.alignment;

        Color visibleColor =
            wordText.color;

        if (visibleColor.a <= 0.01f)
        {
            visibleColor.a = 1f;
        }

        target.color =
            visibleColor;

        target.textWrappingMode =
            TextWrappingModes.NoWrap;

        target.overflowMode =
            TextOverflowModes.Overflow;

        target.characterSpacing =
            wordText.characterSpacing;

        target.wordSpacing =
            wordText.wordSpacing;

        target.lineSpacing =
            wordText.lineSpacing;

        target.outlineWidth =
            wordText.outlineWidth;

        target.outlineColor =
            wordText.outlineColor;

        Color visibleFaceColor =
            wordText.faceColor;

        if (visibleFaceColor.a <= 0.01f)
        {
            visibleFaceColor.a = 1f;
        }

        target.faceColor =
            visibleFaceColor;
    }


    // =============================================================
    // RESET
    // =============================================================

    public void ResetWord()
    {
        revealed = false;

        HideWord();

        DestroyPrivateViews();

        Debug.Log(
            $"🔤 [HIDDEN WORD] '{word}' reiniciada."
        );
    }


    // =============================================================
    // DESTROY PRIVATE VIEWS
    // =============================================================

    private void DestroyPrivateViews()
    {
        foreach (
            KeyValuePair<int, PrivateWordView> pair
            in privateViews
        )
        {
            if (pair.Value != null)
            {
                Destroy(pair.Value.gameObject);
            }
        }

        privateViews.Clear();
    }


    // =============================================================
    // SET WORD
    // =============================================================

    public void SetWord(string newWord)
    {
        if (string.IsNullOrWhiteSpace(newWord))
        {
            Debug.LogWarning(
                "🔤⚠️ [HIDDEN WORD] No se puede asignar una palabra vacía."
            );
            return;
        }

        word = newWord.Trim();

        Debug.Log(
            $"🔤 [HIDDEN WORD] {gameObject.name} -> '{word}'"
        );
    }


    // =============================================================
    // PRIVATE WORD VIEW
    // =============================================================

    private sealed class PrivateWordView : MonoBehaviour
    {
        private HiddenWord owner;
        private Camera targetCamera;
        private Transform source;
        private Transform privateTransform;
        private TMP_Text privateText;

        public PrivateWordView Initialize(
            HiddenWord owner,
            Camera targetCamera,
            Transform source,
            Transform privateTransform,
            TMP_Text privateText
        )
        {
            this.owner = owner;
            this.targetCamera = targetCamera;
            this.source = source;
            this.privateTransform = privateTransform;
            this.privateText = privateText;

            if (privateText != null)
            {
                privateText.enabled = true;
            }

            return this;
        }

        public void Refresh()
        {
            SyncToSource();
        }

        private void LateUpdate()
        {
            if (owner == null)
            {
                Destroy(gameObject);
                return;
            }

            if (targetCamera == null || privateTransform == null)
            {
                return;
            }

            SyncToSource();
        }

        private void SyncToSource()
        {
            if (source == null || privateTransform == null)
            {
                return;
            }

            privateTransform.SetPositionAndRotation(
                source.position,
                source.rotation
            );

            // Para TMP 3D conservamos exactamente el tamaño visual del original.
            if (source.lossyScale.sqrMagnitude > 0.000001f)
            {
                privateTransform.localScale =
                    source.lossyScale;
            }

            if (privateText != null)
            {
                privateText.text = owner.word;
                privateText.enabled = true;
            }
        }
    }
}
