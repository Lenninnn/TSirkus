using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

// Final del juego (una sola vez por partida):
//   gameplay → fundido a negro → cinemática del ganador a pantalla completa (o pantalla
//   provisional si su MP4 aún no existe) → TSIRKUS / EL ESPECTÁCULO HA TERMINADO → créditos
//   → VOLVER AL INICIO (cierra conexiones, limpia lo persistente y carga el menú).
//
// El ganador lo declara RaceWinnerDetector según las reglas de la carrera (DeclareWinner).
//
// Videos: Assets/StreamingAssets/Endings/Ending_Player{N}.mp4. Basta con copiarlos ahí (se
// incluyen tal cual en la build). Si se asigna un VideoClip en endingClips, tiene prioridad.
// Toda la interfaz se construye por código al terminar la partida: no hay objetos de UI en GAME.
public class GameEndingManager : MonoBehaviour
{
    public static GameEndingManager Instance { get; private set; }

    [Header("Videos de los finales")]
    [Tooltip("Opcional: clips asignados a mano (índice 0 = Player1). Vacío = se busca el MP4 en StreamingAssets.")]
    [SerializeField] private VideoClip[] endingClips = new VideoClip[4];

    [Tooltip("Carpeta dentro de StreamingAssets.")]
    [SerializeField] private string streamingFolder = "Endings";

    [Tooltip("{0} = número del jugador ganador.")]
    [SerializeField] private string fileNamePattern = "Ending_Player{0}.mp4";

    [Tooltip("Segundos máximos esperando a que el video esté preparado.")]
    [SerializeField] private float prepareTimeout = 10f;

    [Header("Créditos")]
    [SerializeField] private EndingCredits credits;
    [SerializeField] private TMP_FontAsset titleFont;
    [SerializeField] private TMP_FontAsset bodyFont;

    [Tooltip("Identificador visual de cada jugador (player1-ui ... player4-ui) para la pantalla provisional.")]
    [SerializeField] private Sprite[] winnerSprites = new Sprite[4];

    [Tooltip("Segundos que tarda en subir todo el bloque de créditos.")]
    [SerializeField] private float creditsScrollSeconds = 22f;

    [Header("Ritmo")]
    [SerializeField] private float fadeSeconds = 1.5f;

    [Header("Volver al inicio")]
    [Tooltip("Escena inicial real del proyecto (primera de la Build).")]
    [SerializeField] private string startSceneName = "MainMenu_Mejorado";

    private static readonly Color Blood = new Color(0.62f, 0.04f, 0.05f);
    private static readonly Color Bone = new Color(0.93f, 0.87f, 0.76f);

    private int winner;
    private Canvas canvas;
    private CanvasGroup fade;
    private RawImage videoImage;
    private RectTransform root;
    private VideoPlayer videoPlayer;
    private RenderTexture videoTexture;
    private System.Action primaryAction;
    private float primaryActionArmedAt;

    public bool IsEnding { get; private set; }
    public int Winner => winner;
    public string Stage { get; private set; } = "juego";


    private void Awake()
    {
        Instance = this;
    }


    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (videoTexture != null)
            videoTexture.Release();
    }


    // Punto de entrada: una sola vez por partida. Devuelve false si se ignora.
    public bool DeclareWinner(int playerId)
    {
        if (IsEnding)
        {
            Debug.Log($"🏁 [FINAL] Ganador P{playerId} ignorado: el final ya está en curso (P{winner}).");
            return false;
        }

        if (playerId < 1 || playerId > 4)
        {
            Debug.LogWarning($"🏁 [FINAL] Ganador inválido: {playerId}.");
            return false;
        }

        IsEnding = true;
        winner = playerId;
        Debug.Log($"🏁 [FINAL] Gana Player{playerId}. Empieza el final.");
        StartCoroutine(EndingRoutine());
        return true;
    }


    private void Update()
    {
        if (primaryAction == null || Time.unscaledTime < primaryActionArmedAt)
            return;

        if (AnyPlayerPressedAction())
        {
            System.Action action = primaryAction;
            primaryAction = null;
            action();
        }
    }


    // =====================================================
    // FLUJO
    // =====================================================

    private IEnumerator EndingRoutine()
    {
        FreezeGameplay();
        BuildCanvas();

        Stage = "fundido";
        yield return Fade(0f, 1f, fadeSeconds);

        string missing;
        bool played = false;

        if (TryGetVideo(out VideoClip clip, out string url, out missing))
        {
            Stage = "preparando video";
            yield return PlayVideo(clip, url, result => played = result);
        }
        else
        {
            Debug.LogWarning($"🏁 [FINAL] No hay cinemática para Player{winner}: {missing}. Se muestra la pantalla provisional.");
        }

        if (!played)
        {
            Stage = "pantalla provisional";
            yield return WinnerScreen();
        }

        Stage = "créditos";
        yield return Credits();
    }


    private void FreezeGameplay()
    {
        ClownSpawner spawner = FindAnyObjectByType<ClownSpawner>();

        if (spawner != null)
        {
            spawner.DisableAppearances();
            spawner.Vanish("final del juego");
        }

        foreach (PlayerIdentity player in ClownSpawner.ActivePlayers())
        {
            foreach (MonoBehaviour behaviour in player.GetComponents<MonoBehaviour>())
            {
                if (behaviour is PlayerMovement || behaviour is PlayerLook || behaviour is AutomaticPlayerMovement)
                    behaviour.enabled = false;
            }
        }

        if (GameAudio.Instance != null)
            GameAudio.Instance.ForcePauseAmbience = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }


    private bool TryGetVideo(out VideoClip clip, out string url, out string missing)
    {
        clip = endingClips != null && endingClips.Length >= winner ? endingClips[winner - 1] : null;
        url = null;
        missing = null;

        if (clip != null)
            return true;

        string path = Path.Combine(Application.streamingAssetsPath, streamingFolder, string.Format(fileNamePattern, winner));

        if (File.Exists(path))
        {
            url = path;
            return true;
        }

        missing = $"falta {path}";
        return false;
    }


    private IEnumerator PlayVideo(VideoClip clip, string url, System.Action<bool> done)
    {
        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        AudioSource audio = gameObject.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.spatialBlend = 0f;

        videoTexture = new RenderTexture(1920, 1080, 0);
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = videoTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.SetTargetAudioSource(0, audio);
        videoPlayer.EnableAudioTrack(0, true);

        if (clip != null)
        {
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = clip;
        }
        else
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = url;
        }

        bool prepared = false, failed = false, finished = false;
        videoPlayer.prepareCompleted += _ => prepared = true;
        videoPlayer.errorReceived += (_, message) =>
        {
            failed = true;
            Debug.LogWarning($"🏁 [FINAL] Error de video: {message}");
        };
        videoPlayer.loopPointReached += _ => finished = true;
        videoPlayer.Prepare();

        float waited = 0f;

        while (!prepared && !failed && waited < prepareTimeout)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!prepared || failed)
        {
            Debug.LogWarning($"🏁 [FINAL] El video no se pudo preparar ({(failed ? "error" : "tiempo agotado")}).");
            CleanupVideo();
            done(false);
            yield break;
        }

        // Ya está listo: sin pantalla negra intermedia.
        videoImage.texture = videoTexture;
        AspectRatioFitter fitter = videoImage.GetComponent<AspectRatioFitter>();
        fitter.aspectRatio = videoPlayer.width > 0 && videoPlayer.height > 0 ? (float)videoPlayer.width / videoPlayer.height : 16f / 9f;
        videoImage.gameObject.SetActive(true);
        videoPlayer.Play();
        Stage = "video";
        Debug.Log($"🏁 [FINAL] Reproduciendo cinemática de Player{winner} ({videoPlayer.length:F1} s).");

        yield return Fade(1f, 0f, fadeSeconds);

        double limit = videoPlayer.length + 3.0;
        float playing = 0f;

        while (!finished && !failed && playing < limit)
        {
            playing += Time.unscaledDeltaTime;
            yield return null;
        }

        yield return Fade(0f, 1f, fadeSeconds);
        CleanupVideo();
        done(true);
    }


    private void CleanupVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            Destroy(videoPlayer);
        }

        if (videoImage != null)
            videoImage.gameObject.SetActive(false);
    }


    // Sin MP4: ganador sobre fondo oscuro con resplandor rojo, CONTINUAR o 10 s.
    private IEnumerator WinnerScreen()
    {
        RectTransform panel = Panel("PantallaGanador");
        AddGlow(panel, new Vector2(0f, 60f), new Vector2(1500f, 900f), 0.55f);

        Text(panel, "SOBREVIVIENTE", titleFont, 40, Bone * 0.8f, new Vector2(0f, 300f), 8f);

        Sprite sprite = winnerSprites != null && winnerSprites.Length >= winner ? winnerSprites[winner - 1] : null;

        if (sprite != null)
        {
            Image badge = new GameObject("Jugador", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            badge.transform.SetParent(panel, false);
            badge.sprite = sprite;
            badge.preserveAspect = true;
            badge.rectTransform.sizeDelta = new Vector2(720f, 240f);
            badge.rectTransform.anchoredPosition = new Vector2(0f, 130f);
        }
        else
        {
            Text(panel, $"PLAYER {winner}", titleFont, 120, Bone, new Vector2(0f, 130f), 4f);
        }

        Text(panel, $"PLAYER {winner} SE MANTUVO EN PIE", titleFont, 64, Blood, new Vector2(0f, -40f), 4f);
        Text(panel, "LOS DEMÁS CAYERON AL VACÍO", bodyFont, 34, Bone * 0.75f, new Vector2(0f, -120f), 10f);

        bool next = false;
        Button button = MakeButton(panel, "CONTINUAR", new Vector2(0f, -300f), () => next = true);

        yield return Fade(1f, 0f, fadeSeconds);

        float waited = 0f;

        while (!next && waited < 10f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        primaryAction = null;
        yield return Fade(0f, 1f, fadeSeconds);
        Destroy(panel.gameObject);
    }


    private IEnumerator Credits()
    {
        EndingCredits data = credits != null ? credits : ScriptableObject.CreateInstance<EndingCredits>();

        // Portada.
        RectTransform cover = Panel("Portada");
        Image glow = AddGlow(cover, new Vector2(0f, 40f), new Vector2(1700f, 1000f), 0.7f);
        Text(cover, data.title, titleFont, 190, Blood, new Vector2(0f, 70f), 12f);
        Text(cover, data.subtitle, titleFont, 54, Bone, new Vector2(0f, -110f), 8f);

        yield return Fade(1f, 0f, 2.5f);
        yield return Flicker(glow, 4f);
        yield return Fade(0f, 1f, 2f);
        Destroy(cover.gameObject);

        // Créditos que suben.
        RectTransform panel = Panel("Creditos");
        AddGlow(panel, new Vector2(0f, -620f), new Vector2(2200f, 700f), 0.45f);
        RectTransform list = new GameObject("Lista", typeof(RectTransform)).GetComponent<RectTransform>();
        list.SetParent(panel, false);
        list.anchorMin = list.anchorMax = new Vector2(0.5f, 0.5f);
        list.pivot = new Vector2(0.5f, 1f);

        float y = 0f;
        y = Line(list, data.title, titleFont, 110, Blood, y, 30f, 10f);

        foreach (EndingCredits.Section section in data.sections)
        {
            if (section == null || section.entries == null || !section.entries.Exists(e => e != null && !string.IsNullOrWhiteSpace(e.name)))
                continue;

            y -= 70f;
            y = Line(list, section.heading, titleFont, 52, Blood, y, 26f, 8f);

            foreach (EndingCredits.Entry entry in section.entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.name))
                    continue;

                y = Line(list, entry.name, bodyFont, 46, Bone, y, 6f, 2f);

                if (!string.IsNullOrWhiteSpace(entry.role))
                    y = Line(list, entry.role, bodyFont, 30, Bone * 0.65f, y, 22f, 6f);
                else
                    y -= 16f;
            }
        }

        if (!string.IsNullOrWhiteSpace(data.closingLine))
        {
            y -= 80f;
            y = Line(list, data.closingLine, bodyFont, 36, Bone * 0.8f, y, 10f, 4f);
        }

        float height = -y;
        float start = -560f;
        float end = Mathf.Max(start + height + 200f, 520f + height * 0.5f);
        list.anchoredPosition = new Vector2(0f, start);

        yield return Fade(1f, 0f, fadeSeconds);

        bool skip = false;
        primaryAction = () => skip = true;
        primaryActionArmedAt = Time.unscaledTime + 1f;
        float t = 0f;

        while (t < creditsScrollSeconds && !skip)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / creditsScrollSeconds);
            list.anchoredPosition = new Vector2(0f, Mathf.Lerp(start, end, k));
            yield return null;
        }

        // Final: los créditos se apagan y queda VOLVER AL INICIO.
        CanvasGroup listGroup = list.gameObject.AddComponent<CanvasGroup>();

        for (float f = 0f; f < 1f; f += Time.unscaledDeltaTime / 1.2f)
        {
            listGroup.alpha = 1f - f;
            yield return null;
        }

        listGroup.alpha = 0f;
        Text(panel, data.title, titleFont, 120, Blood, new Vector2(0f, 160f), 10f);
        MakeButton(panel, "VOLVER AL INICIO", new Vector2(0f, -140f), ReturnToStart);
        Stage = "volver al inicio";
    }


    // =====================================================
    // VOLVER AL INICIO
    // =====================================================

    public void ReturnToStart()
    {
        primaryAction = null;
        Stage = "cargando inicio";
        Debug.Log("🏁 [FINAL] VOLVER AL INICIO: se cierran conexiones y se carga " + startSceneName);

        if (videoPlayer != null)
            videoPlayer.Stop();

        // MobileInputManager sobrevive a los cambios de escena (DontDestroyOnLoad): sin esto la
        // siguiente partida tendría dos conexiones. Se cierran sus sockets (su propio
        // OnApplicationQuit) y se destruye. PhraseNetworkBridge también tiene un hilo abierto.
        foreach (MobileInputManager input in FindObjectsByType<MobileInputManager>())
        {
            input.SendMessage("OnApplicationQuit", SendMessageOptions.DontRequireReceiver);
            Destroy(input.gameObject);
        }

        foreach (PhraseNetworkBridge bridge in FindObjectsByType<PhraseNetworkBridge>())
            bridge.SendMessage("OnApplicationQuit", SendMessageOptions.DontRequireReceiver);

        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (Application.CanStreamedLevelBeLoaded(startSceneName))
            SceneManager.LoadScene(startSceneName, LoadSceneMode.Single);
        else
            SceneManager.LoadScene(0, LoadSceneMode.Single);
    }


    // =====================================================
    // INTERFAZ (construida por código)
    // =====================================================

    private void BuildCanvas()
    {
        var go = new GameObject("FinalDelJuego_UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(transform, false);
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        root = go.GetComponent<RectTransform>();

        // Fondo negro permanente (tapa las cuatro cámaras).
        Image background = Stretch(new GameObject("Fondo", typeof(RectTransform), typeof(Image)), root).GetComponent<Image>();
        background.color = new Color(0.02f, 0.01f, 0.01f);
        background.raycastTarget = false;

        // Video a pantalla completa, 16:9 dentro de la pantalla.
        var video = new GameObject("Video", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
        Stretch(video, root);
        videoImage = video.GetComponent<RawImage>();
        videoImage.raycastTarget = false;
        AspectRatioFitter fitter = video.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 16f / 9f;
        video.SetActive(false);

        // Cortina de fundido (encima de todo).
        var curtain = new GameObject("Fundido", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        Stretch(curtain, root);
        curtain.GetComponent<Image>().color = Color.black;
        curtain.GetComponent<Image>().raycastTarget = false;
        fade = curtain.GetComponent<CanvasGroup>();
        fade.blocksRaycasts = false;
        fade.alpha = 0f;

        // El fondo negro solo aparece cuando la cortina ya cubre la pantalla.
        background.canvasRenderer.SetAlpha(0f);
        StartCoroutine(ShowBackgroundWhenCovered(background));

        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule)).transform.SetParent(transform, false);
    }


    private IEnumerator ShowBackgroundWhenCovered(Image background)
    {
        while (fade.alpha < 0.99f)
            yield return null;

        background.canvasRenderer.SetAlpha(1f);
    }


    private RectTransform Panel(string name)
    {
        RectTransform panel = Stretch(new GameObject(name, typeof(RectTransform)), root);
        panel.SetSiblingIndex(fade.transform.GetSiblingIndex());
        return panel;
    }


    private static RectTransform Stretch(GameObject go, RectTransform parent)
    {
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }


    private TextMeshProUGUI Text(RectTransform parent, string text, TMP_FontAsset font, float size, Color color, Vector2 position, float spacing)
    {
        var go = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();

        // La tipografía de títulos no trae todas las letras acentuadas (p. ej. "Á"): si falta
        // alguna, ese texto usa la del cuerpo en vez de mostrar huecos.
        if (font != null && font != bodyFont && bodyFont != null && !DrawsAll(font, text))
            font = bodyFont;

        if (font != null)
            label.font = font;

        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.characterSpacing = spacing;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        label.rectTransform.sizeDelta = new Vector2(1800f, size * 1.6f);
        label.rectTransform.anchoredPosition = position;
        return label;
    }


    // La fuente puede declarar un carácter con el glifo vacío: se exige que tenga dibujo.
    private static bool DrawsAll(TMP_FontAsset font, string text)
    {
        font.HasCharacters(text, out uint[] _, false, true);

        foreach (char c in text)
        {
            if (char.IsWhiteSpace(c))
                continue;

            if (!font.characterLookupTable.TryGetValue(c, out TMP_Character character) ||
                character.glyph == null || character.glyph.metrics.width <= 0f)
                return false;
        }

        return true;
    }


    private float Line(RectTransform list, string text, TMP_FontAsset font, float size, Color color, float y, float after, float spacing)
    {
        TextMeshProUGUI label = Text(list, text, font, size, color, Vector2.zero, spacing);
        label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        label.rectTransform.pivot = new Vector2(0.5f, 1f);
        label.rectTransform.anchoredPosition = new Vector2(0f, y);
        return y - size * 1.25f - after;
    }


    // Resplandor rojo difuso (textura radial generada en memoria).
    private Image AddGlow(RectTransform parent, Vector2 position, Vector2 size, float alpha)
    {
        var go = new GameObject("ResplandorRojo", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = GlowSprite();
        image.color = new Color(Blood.r, Blood.g, Blood.b, alpha);
        image.raycastTarget = false;
        image.rectTransform.sizeDelta = size;
        image.rectTransform.anchoredPosition = position;
        return image;
    }


    private static Sprite glowSprite;

    private static Sprite GlowSprite()
    {
        if (glowSprite != null)
            return glowSprite;

        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                float a = Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        texture.Apply();
        glowSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return glowSprite;
    }


    private Button MakeButton(RectTransform parent, string label, Vector2 position, System.Action onClick)
    {
        var go = new GameObject("Boton_" + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(620f, 110f);
        rect.anchoredPosition = position;

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.18f, 0.02f, 0.02f, 0.92f);
        Outline outline = go.GetComponent<Outline>();
        outline.effectColor = new Color(Blood.r, Blood.g, Blood.b, 0.9f);
        outline.effectDistance = new Vector2(3f, -3f);

        Button button = go.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.6f, 0.8f, 0.8f);
        colors.selectedColor = new Color(1.6f, 0.8f, 0.8f);
        colors.pressedColor = new Color(0.7f, 0.5f, 0.5f);
        button.colors = colors;
        button.onClick.AddListener(() =>
        {
            primaryAction = null;
            onClick();
        });

        TextMeshProUGUI text = Text(rect, label, titleFont, 48, Bone, Vector2.zero, 6f);
        text.rectTransform.sizeDelta = rect.sizeDelta;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(go);

        // También responde a ACCIÓN de cualquier celular o a Enter/Espacio.
        primaryAction = onClick;
        primaryActionArmedAt = Time.unscaledTime + 0.75f;
        return button;
    }


    private static bool AnyPlayerPressedAction()
    {
        MobileInputManager input = FindAnyObjectByType<MobileInputManager>();

        if (input != null)
        {
            for (int id = 1; id <= 4; id++)
            {
                if (input.IsActionPressed(id))
                    return true;
            }
        }

        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        return keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame);
    }


    private IEnumerator Fade(float from, float to, float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            fade.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / seconds));
            yield return null;
        }

        fade.alpha = to;
    }


    // Parpadeo irregular del resplandor (luz de circo que falla).
    private IEnumerator Flicker(Image glow, float seconds)
    {
        Color baseColor = glow.color;

        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            float noise = Mathf.PerlinNoise(t * 3f, 0.37f);
            float drop = Random.value < 0.015f ? 0.35f : 1f;
            glow.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * Mathf.Lerp(0.65f, 1.1f, noise) * drop);
            yield return null;
        }

        glow.color = baseColor;
    }
}
