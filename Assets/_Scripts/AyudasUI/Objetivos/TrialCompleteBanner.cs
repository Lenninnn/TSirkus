using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Cartel "PRUEBA COMPLETADA / CONTINÚA" (y variantes) con el mismo marco de pergamino de los
// avisos y objetivos. Se muestra a la vez en el cuadrante de cada jugador, centrado un poco por
// encima del objetivo, entra y sale con fundido y escala, y no bloquea a nadie.
// Cada evento se muestra una sola vez por partida (clave).
// Uso:
//   - Pruebas sin cinemática: se suscribe solo a los PuzzleController de "trials".
//   - Desde un UnityEvent: ShowCompletedThenObjective(objetivo) (cartel → siguiente objetivo).
//   - Desde código: Show(clave, título, subtítulo, alTerminar).
public class TrialCompleteBanner : MonoBehaviour
{
    [Serializable]
    public class Trial
    {
        public PuzzleController puzzle;
        [Tooltip("Objetivo opcional que aparece después del cartel.")]
        public ObjectiveDefinition nextObjective;
    }

    public static TrialCompleteBanner Instance { get; private set; }

    [Header("Pruebas que muestran el cartel al completarse")]
    [SerializeField] private List<Trial> trials = new List<Trial>();

    [Header("Textos")]
    [SerializeField] private string completedTitle = "PRUEBA COMPLETADA";
    [SerializeField] private string completedSubtitle = "CONTINÚA";

    [Header("Estilo (mismo lenguaje que avisos y objetivos)")]
    [SerializeField] private Sprite frame;
    [SerializeField] private TMP_FontAsset titleFont;
    [SerializeField] private TMP_FontAsset subtitleFont;
    [SerializeField] private Color titleColor = new Color(0.44f, 0.05f, 0.05f);
    [SerializeField] private Color subtitleColor = new Color(0.12f, 0.04f, 0.02f);

    [Tooltip("Zona del cuadrante (0-1) que ocupa el cartel. Encima del objetivo (26-46 %).")]
    [SerializeField] private Vector2 anchorMin = new Vector2(0.18f, 0.5f);
    [SerializeField] private Vector2 anchorMax = new Vector2(0.82f, 0.76f);

    [Header("Tiempos")]
    [Min(0.5f)] [SerializeField] private float visibleSeconds = 2.6f;
    [Min(0.05f)] [SerializeField] private float fadeSeconds = 0.45f;

    [SerializeField] private ObjectivePromptController objectives;
    [SerializeField] private int sortingOrder = 900;

    private readonly HashSet<string> shown = new HashSet<string>();
    private readonly Queue<IEnumerator> pending = new Queue<IEnumerator>();
    private readonly List<(Camera camera, RectTransform root)> panels = new List<(Camera, RectTransform)>();

    private Canvas canvas;
    private CanvasGroup group;
    private bool busy;

    public bool IsShowing => busy;

    public Sprite Frame => frame;


    private void Awake()
    {
        Instance = this;

        if (objectives == null)
            objectives = FindAnyObjectByType<ObjectivePromptController>();

        foreach (Trial trial in trials)
        {
            if (trial == null || trial.puzzle == null)
                continue;

            Trial captured = trial;
            trial.puzzle.onPuzzleCompleted.AddListener(() =>
                Show("prueba:" + captured.puzzle.name, completedTitle, completedSubtitle,
                    () => ShowObjective(captured.nextObjective)));
        }
    }


    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

    }


    // =====================================================
    // API
    // =====================================================

    // Para la primera puerta: tras su cinemática, cartel y después el objetivo siguiente.
    public void ShowCompletedThenObjective(ObjectiveDefinition objective)
    {
        Show("prueba:puerta", completedTitle, completedSubtitle, () => ShowObjective(objective));
    }


    public bool Show(string key, string title, string subtitle, Action onFinished = null)
    {
        if (!string.IsNullOrEmpty(key) && !shown.Add(key))
        {
            Debug.Log($"🏷️ CARTEL ya mostrado, se ignora: {key}");
            return false;
        }

        Debug.Log($"🏷️ CARTEL: {title} / {subtitle} ({key})");
        pending.Enqueue(ShowRoutine(title, subtitle, onFinished));

        if (!busy)
            StartCoroutine(RunQueue());

        return true;
    }


    // =====================================================
    // SECUENCIA
    // =====================================================

    private IEnumerator RunQueue()
    {
        busy = true;

        while (pending.Count > 0)
            yield return pending.Dequeue();

        busy = false;
    }


    private IEnumerator ShowRoutine(string title, string subtitle, Action onFinished)
    {
        Build(title, subtitle);

        yield return Animate(0f, 1f, 0.92f, 1f);

        for (float t = 0f; t < visibleSeconds; t += Time.unscaledDeltaTime)
            yield return null;

        yield return Animate(1f, 0f, 1f, 1.04f);

        Clear();
        onFinished?.Invoke();
    }


    private IEnumerator Animate(float fromAlpha, float toAlpha, float fromScale, float toScale)
    {
        for (float t = 0f; t < fadeSeconds; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / fadeSeconds);
            Apply(Mathf.Lerp(fromAlpha, toAlpha, k), Mathf.Lerp(fromScale, toScale, k));
            yield return null;
        }

        Apply(toAlpha, toScale);
    }


    private void Apply(float alpha, float scale)
    {
        if (group != null)
            group.alpha = alpha;

        foreach ((Camera _, RectTransform root) in panels)
        {
            if (root != null && root.childCount > 0)
                root.GetChild(0).localScale = Vector3.one * scale;
        }
    }


    private void ShowObjective(ObjectiveDefinition objective)
    {
        if (objective != null && objectives != null)
            objectives.ShowToAll(objective);
    }


    // =====================================================
    // UI (un cartel por cuadrante, anclado al viewport de cada cámara)
    // =====================================================

    private void Build(string title, string subtitle)
    {
        EnsureCanvas();
        Clear();

        foreach (PlayerIdentity player in ClownSpawner.ActivePlayers())
        {
            Camera playerCamera = player.GetComponentInChildren<Camera>();

            if (playerCamera == null)
                continue;

            var root = new GameObject($"Cartel_P{player.PlayerId}", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(canvas.transform, false);
            root.offsetMin = root.offsetMax = Vector2.zero;
            FitToCamera(root, playerCamera);

            var plaque = new GameObject("Marco", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            plaque.SetParent(root, false);
            plaque.anchorMin = anchorMin;
            plaque.anchorMax = anchorMax;
            plaque.offsetMin = plaque.offsetMax = Vector2.zero;

            Image image = plaque.GetComponent<Image>();
            image.sprite = frame;
            image.preserveAspect = true;
            image.raycastTarget = false;

            // Zona de texto del marco (790x316): pergamino entre x 108-684 e y 96-232.
            var textArea = new GameObject("Texto", typeof(RectTransform), typeof(AspectRatioFitter)).GetComponent<RectTransform>();
            textArea.SetParent(plaque, false);
            textArea.anchorMin = textArea.anchorMax = new Vector2(0.5f, 0.5f);
            AspectRatioFitter fitter = textArea.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 790f / 316f;

            AddLabel(textArea, title, titleFont, titleColor, new Vector2(0.17f, 0.455f), new Vector2(0.83f, 0.655f));
            AddLabel(textArea, subtitle, subtitleFont, subtitleColor, new Vector2(0.3f, 0.27f), new Vector2(0.7f, 0.4f));
            AddRule(textArea, new Vector2(0.317f, 0.41f), new Vector2(0.683f, 0.416f));

            panels.Add((playerCamera, root));
        }

        Apply(0f, 0.92f);
    }


    private void AddLabel(RectTransform parent, string text, TMP_FontAsset font, Color color, Vector2 min, Vector2 max)
    {
        var rect = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = rect.GetComponent<TextMeshProUGUI>();

        // Si la fuente de títulos no dibuja alguna letra (p. ej. "Ú"), se usa la otra.
        label.font = font != null && DrawsAll(font, text) ? font : (subtitleFont != null ? subtitleFont : font);
        label.text = text;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true;
        label.fontSizeMin = 8f;
        label.fontSizeMax = 200f;
        label.characterSpacing = 4f;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        label.fontStyle = FontStyles.Bold;
    }


    private static void AddRule(RectTransform parent, Vector2 min, Vector2 max)
    {
        var rect = new GameObject("Linea", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        Image line = rect.GetComponent<Image>();
        line.color = new Color(0.44f, 0.05f, 0.05f, 0.9f);
        line.raycastTarget = false;
    }


    // La tipografía de títulos (Carnevalee) no trae mayúsculas acentuadas ni Ñ (sus glifos están vacíos).
    // Se decide sin pedir glifos a la fuente: forzar glifos en atlas dinámicos durante la partida
    // estropeaba otros caracteres (la "Ú" se veía como "ú" y la "Á" perdía la tilde).
    private const string MissingInTitleFont = "ÁÉÍÓÚÜÑáéíóúüñ¡¿";

    private static bool DrawsAll(TMP_FontAsset font, string text)
    {
        if (font == null || string.IsNullOrEmpty(text))
            return true;

        if (!font.name.StartsWith("Carnevalee"))
            return true;

        foreach (char c in text)
        {
            if (MissingInTitleFont.IndexOf(c) >= 0)
                return false;
        }

        return true;
    }


    private void EnsureCanvas()
    {
        if (canvas != null)
            return;

        var go = new GameObject("CartelesPrueba_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        go.transform.SetParent(transform, false);

        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        group = go.GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        group.alpha = 0f;
    }


    private void Update()
    {
        foreach ((Camera playerCamera, RectTransform root) in panels)
            FitToCamera(root, playerCamera);
    }


    // Mismo criterio que PlayerObjectiveHUD: anclar al viewport de la cámara del jugador.
    private static void FitToCamera(RectTransform root, Camera playerCamera)
    {
        if (root == null || playerCamera == null)
            return;

        Rect viewport = playerCamera.rect;
        root.anchorMin = viewport.min;
        root.anchorMax = viewport.max;
    }


    private void Clear()
    {
        foreach ((Camera _, RectTransform root) in panels)
        {
            if (root != null)
                Destroy(root.gameObject);
        }

        panels.Clear();
    }
}
