using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TSirkus.MainMenuUpgrade
{
    public sealed class TSirkusMainMenuController : MonoBehaviour
    {
        [Header("Destino de Jugar")]
        public string gameScenePath = "Assets/Scenes/GAME.unity";
        [Header("Historia editable")]
        [TextArea(5, 12)] public string story = "";
        [Header("Referencias de interfaz")]
        public CanvasGroup home, settings, history, loading;
        public Button playButton, settingsButton, historyButton, settingsBack, historyBack;
        public Slider musicSlider;
        public Text volumeLabel, storyLabel, statusLabel, loadingLabel;
        public RectTransform progressBar;
        public VideoPlayer backgroundVideo;
        public GameObject volumeSliderRoot;
        public RectTransform volumeFill, volumeHandle;
        public Image volumeHandleGraphic;
        private float musicVolume;

        private void Awake()
        {
            // The scene contains editable slider graphics; this supplies its interaction.
            musicSlider = volumeSliderRoot.GetComponent<Slider>();
            if (!musicSlider) musicSlider = volumeSliderRoot.AddComponent<Slider>();
            musicSlider.fillRect = volumeFill;
            musicSlider.handleRect = volumeHandle;
            musicSlider.targetGraphic = volumeHandleGraphic;
            musicSlider.direction = Slider.Direction.LeftToRight;
            musicSlider.minValue = 0; musicSlider.maxValue = 1;
            musicSlider.wholeNumbers = false;
            var colors = musicSlider.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1, 1, .8f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(.75f, .6f, .4f);
            musicSlider.colors = colors;
#if ENABLE_INPUT_SYSTEM
            var system = FindFirstObjectByType<EventSystem>();
            if (system)
            {
                var module = system.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                if (module && !module.actionsAsset) module.AssignDefaultActions();
            }
#endif
        }
        private bool busy;
        private CanvasGroup current;
        private Coroutine transition, saveVolume;
        
        private const string VolumeKey = "TSirkus.MusicVolume.v1";

        private void Start()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SetPage(home, true); SetPage(settings, false); SetPage(history, false); SetPage(loading, false);
            current = home;
            storyLabel.text = story;
            float volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, .55f));
            musicSlider.SetValueWithoutNotify(volume);
            ApplyVolume(volume);
            if (backgroundVideo)
            {
                backgroundVideo.Stop();
                backgroundVideo.playOnAwake = false;
                backgroundVideo.isLooping = true;
                backgroundVideo.audioOutputMode = VideoAudioOutputMode.Direct;
                backgroundVideo.controlledAudioTrackCount = 1;
                backgroundVideo.EnableAudioTrack(0, true);
                backgroundVideo.prepareCompleted += VideoPrepared;
                backgroundVideo.errorReceived += VideoError;
                backgroundVideo.Prepare();
            }
            playButton.onClick.AddListener(Play);
            settingsButton.onClick.AddListener(OpenSettings);
            historyButton.onClick.AddListener(OpenHistory);
            settingsBack.onClick.AddListener(Back);
            historyBack.onClick.AddListener(Back);
            musicSlider.onValueChanged.AddListener(SetVolume);
            Select(playButton.gameObject);
        }
        private void Update()
        {
            bool cancel = false;
#if ENABLE_INPUT_SYSTEM
            cancel = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
            cancel = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1);
#endif
            if (cancel && !busy && current != home) Back();
        }
        private static void SetPage(CanvasGroup p, bool visible)
        {
            p.alpha = visible ? 1 : 0;
            p.interactable = visible;
            p.blocksRaycasts = visible;
            p.gameObject.SetActive(visible);
        }
        private void Select(GameObject target)
        {
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(target);
        }
        public void OpenSettings() { if (!busy) Show(settings, musicSlider.gameObject); }
        public void OpenHistory() { if (!busy) Show(history, historyBack.gameObject); }
public void Back()
{
    Debug.Log("BOTON VOLVER PRESIONADO");

    if (busy) return;

    PlayerPrefs.Save();

    Show(
        home,
        current == history
            ? historyButton.gameObject
            : settingsButton.gameObject
    );
}
        private void Show(CanvasGroup target, GameObject selected)
        {
            if (transition != null) StopCoroutine(transition);
            SetPage(home, false); SetPage(settings, false); SetPage(history, false);
            current = target;
            target.gameObject.SetActive(true); target.alpha = 0;
            target.interactable = true; target.blocksRaycasts = true;
            Select(selected);
            transition = StartCoroutine(FadeIn(target));
        }
        private IEnumerator FadeIn(CanvasGroup target)
        {
            float t = 0;
            while (t < .24f) { t += Time.unscaledDeltaTime; target.alpha = Mathf.SmoothStep(0, 1, t / .24f); yield return null; }
            target.alpha = 1; transition = null;
        }
        private void SetVolume(float value)
        {
            ApplyVolume(value);
            PlayerPrefs.SetFloat(VolumeKey, value);
            if (saveVolume != null) StopCoroutine(saveVolume);
            saveVolume = StartCoroutine(SaveVolumeSoon());
        }
        private IEnumerator SaveVolumeSoon()
        {
            yield return new WaitForSecondsRealtime(.3f);
            PlayerPrefs.Save(); saveVolume = null;
        }
        private void ApplyVolume(float value)
        {
            musicVolume = Mathf.Clamp01(value);
            if (backgroundVideo && backgroundVideo.isPrepared && backgroundVideo.audioTrackCount > 0)
                backgroundVideo.SetDirectAudioVolume(0, musicVolume);
            volumeLabel.text = Mathf.RoundToInt(value * 100) + "%";
        }
        public void Play()
        {
            if (busy) return;
            bool canLoad = !string.IsNullOrWhiteSpace(gameScenePath) && Application.CanStreamedLevelBeLoaded(gameScenePath);
#if UNITY_EDITOR
            canLoad |= !string.IsNullOrWhiteSpace(gameScenePath) && UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(gameScenePath) != null;
#endif
            if (!canLoad)
            {
                statusLabel.text = "No se pudo abrir la partida. Revisa la escena de destino.";
                Debug.LogError("TSirkus: agrega la escena de destino a la Scene List del Build Profile activo: " + gameScenePath, this);
                return;
            }
            PlayerPrefs.Save(); busy = true;
            if (transition != null) StopCoroutine(transition);
            SetPage(home, true); SetPage(settings, false); SetPage(history, false); SetPage(loading, false);
            Select(null);
            StartCoroutine(LoadGame());
        }
        private IEnumerator LoadGame()
        {
            AsyncOperation operation = null;
            try
            {
#if UNITY_EDITOR
                if (!Application.CanStreamedLevelBeLoaded(gameScenePath))
                    operation = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(gameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
                else
                    operation = SceneManager.LoadSceneAsync(gameScenePath, LoadSceneMode.Single);
#else
                operation = SceneManager.LoadSceneAsync(gameScenePath, LoadSceneMode.Single);
#endif
            }
            catch (System.Exception exception) { Debug.LogException(exception, this); }
            if (operation == null)
            {
                busy = false; SetPage(loading, false); SetPage(home, true); current = home;
                statusLabel.text = "No se pudo iniciar. Vuelve a intentarlo.";
                Select(playButton.gameObject); yield break;
            }
            operation.allowSceneActivation = false;
            float startVolume = musicVolume;
            float elapsed = 0;
            while (operation.progress < .9f || elapsed < .65f)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(operation.progress / .9f);
                progressBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 700 * progress);
                loadingLabel.text = "ABRIENDO LA CARPA  ·  " + Mathf.RoundToInt(progress * 100) + "%";
                if (backgroundVideo && backgroundVideo.isPrepared && backgroundVideo.audioTrackCount > 0)
                    backgroundVideo.SetDirectAudioVolume(0, startVolume * (1 - Mathf.Clamp01(elapsed / .65f)));
                yield return null;
            }
            progressBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 700);
            operation.allowSceneActivation = true;
        }
        private void VideoPrepared(VideoPlayer player)
        {
            if (player.audioTrackCount > 0) player.SetDirectAudioVolume(0, musicVolume);
            player.Play();
        }
        private void VideoError(VideoPlayer player, string message)
        {
            statusLabel.text = "El video de fondo no pudo reproducirse. El menú sigue disponible.";
            Debug.LogWarning("TSirkus MainMenu: " + message, this);
        }
        private void OnDestroy()
        {
            if (backgroundVideo)
            {
                backgroundVideo.prepareCompleted -= VideoPrepared;
                backgroundVideo.errorReceived -= VideoError;
            }
        }
        private void OnApplicationPause(bool paused) { if (paused) PlayerPrefs.Save(); }
        private void OnApplicationQuit() { PlayerPrefs.Save(); }
    }
}
