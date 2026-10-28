using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TSirkus.MenuUI
{
    public sealed class TSirkusMenuController : MonoBehaviour
    {
        [Header("Destino de Jugar")]
        public string gameScenePath = "Assets/Scenes/Complete map.unity";
        [Header("Historia editable")]
        [TextArea(5, 12)] public string story = "La música dejó de sonar hace años.\nEsta noche, alguien ha vuelto a darle cuerda.\n\nCuatro visitantes cruzan la entrada de un circo abandonado. Entre juegos rotos y risas que no deberían escucharse, un niño les promete mostrarles la salida.\n\nPero cada prueba los acerca a una verdad: el circo no piensa dejarlos ir a todos.\n\nCuatro entran. Solo uno escapa con el Boleto Dorado.";
        [Header("Referencias de interfaz")]
        public CanvasGroup home, settings, history, loading;
        public Button playButton, settingsButton, historyButton, settingsBack, historyBack;
        public Slider musicSlider;
        public Text volumeLabel, storyLabel, statusLabel, loadingLabel;
        public Image progressFill;
        public AudioSource musicSource, interfaceSource;
        public AudioClip hoverClip, clickClip;
        private bool busy;
        private CanvasGroup current;
        private Coroutine transition, saveVolume;
        private float nextHover;
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
            if (musicSource && musicSource.clip) musicSource.Play();
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
        public void HoverSound()
        {
            if (Time.unscaledTime < nextHover || busy) return;
            nextHover = Time.unscaledTime + .08f;
            if (interfaceSource && hoverClip) interfaceSource.PlayOneShot(hoverClip, .16f);
        }
        private void ClickSound()
        {
            if (interfaceSource && clickClip) interfaceSource.PlayOneShot(clickClip, .24f);
        }
        public void OpenSettings() { if (!busy) Show(settings, musicSlider.gameObject); }
        public void OpenHistory() { if (!busy) Show(history, historyBack.gameObject); }
        public void Back()
        {
            if (busy) return;
            PlayerPrefs.Save();
            Show(home, current == history ? historyButton.gameObject : settingsButton.gameObject);
        }
        private void Show(CanvasGroup target, GameObject selected)
        {
            ClickSound();
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
            // This controls music only: voices and gameplay effects are not changed.
            if (musicSource) musicSource.volume = value;
            volumeLabel.text = Mathf.RoundToInt(value * 100) + "%";
        }
        private void Play()
        {
            if (busy) return;
            if (string.IsNullOrWhiteSpace(gameScenePath) || !Application.CanStreamedLevelBeLoaded(gameScenePath))
            {
                statusLabel.text = "No se pudo abrir la partida. Revisa la escena de destino.";
                Debug.LogError("TSirkus: agrega la escena de destino a la Scene List del Build Profile activo: " + gameScenePath, this);
                return;
            }
            ClickSound(); PlayerPrefs.Save(); busy = true;
            if (transition != null) StopCoroutine(transition);
            SetPage(home, false); SetPage(settings, false); SetPage(history, false); SetPage(loading, true);
            Select(null);
            StartCoroutine(LoadGame());
        }
        private IEnumerator LoadGame()
        {
            AsyncOperation operation = null;
            try { operation = SceneManager.LoadSceneAsync(gameScenePath, LoadSceneMode.Single); }
            catch (System.Exception exception) { Debug.LogException(exception, this); }
            if (operation == null)
            {
                busy = false; SetPage(loading, false); SetPage(home, true); current = home;
                statusLabel.text = "No se pudo iniciar. Vuelve a intentarlo.";
                Select(playButton.gameObject); yield break;
            }
            operation.allowSceneActivation = false;
            float startVolume = musicSource ? musicSource.volume : 0;
            float elapsed = 0;
            while (operation.progress < .9f || elapsed < .65f)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(operation.progress / .9f);
                progressFill.fillAmount = progress;
                loadingLabel.text = "ABRIENDO LA CARPA  ·  " + Mathf.RoundToInt(progress * 100) + "%";
                if (musicSource) musicSource.volume = startVolume * (1 - Mathf.Clamp01(elapsed / .65f));
                yield return null;
            }
            progressFill.fillAmount = 1;
            operation.allowSceneActivation = true;
        }
        private void OnApplicationPause(bool paused) { if (paused) PlayerPrefs.Save(); }
        private void OnApplicationQuit() { PlayerPrefs.Save(); }
    }
}
