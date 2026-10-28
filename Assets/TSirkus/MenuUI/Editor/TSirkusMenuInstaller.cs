using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace TSirkus.MenuUI.Editor
{
    public sealed class TSirkusMenuInstaller : EditorWindow
    {
        private const string Folder = "Assets/TSirkus/MenuUI";
        private const string ScenePath = Folder + "/Scenes/TSirkus_Menu.unity";
        private SceneAsset gameScene;
        private static readonly Color Cream = new Color(.91f, .85f, .72f);
        private static readonly Color Gold = new Color(.66f, .48f, .26f);
        private static readonly Color Muted = new Color(.64f, .60f, .53f);
        private Font bodyFont, titleFont;
        private TSirkusMenuController controller;

        [MenuItem("Tools/TSirkus/Interfaz/Crear o abrir menu")]
        public static void OpenInstaller() { GetWindow<TSirkusMenuInstaller>("TSirkus · Interfaz").Show(); }
        private void OnEnable() { gameScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Complete map.unity"); }
        private void OnGUI()
        {
            GUILayout.Space(12);
            GUILayout.Label("TSIRKUS · MENÚ PRINCIPAL", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Crea una escena independiente con Jugar, Configuraciones e Historia. Elige la escena que abrirá Jugar.", MessageType.Info);
            gameScene = (SceneAsset)EditorGUILayout.ObjectField("Escena de juego", gameScene, typeof(SceneAsset), false);
            GUILayout.Space(8);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || !gameScene))
                if (GUILayout.Button("Crear / abrir menú", GUILayout.Height(38))) CreateOrOpen();
            GUILayout.Space(8);
            EditorGUILayout.LabelField("Ubicación", ScenePath, EditorStyles.wordWrappedLabel);
            EditorGUILayout.HelpBox("Se añade el menú al inicio de la Scene List compartida y se habilita el mapa elegido. Si tu Build Profile usa una lista propia, añade allí ambas escenas también.", MessageType.None);
        }
        private void CreateOrOpen()
        {
            string targetPath = AssetDatabase.GetAssetPath(gameScene);
            if (targetPath == ScenePath) { EditorUtility.DisplayDialog("TSirkus", "El destino de Jugar debe ser tu mapa, no el menú.", "Entendido"); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath))
            {
                Scene scene = EditorSceneManager.OpenScene(ScenePath);
                var menu = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TSirkusMenuController>(true)).FirstOrDefault();
                if (menu) { Undo.RecordObject(menu, "Cambiar destino de Jugar"); menu.gameScenePath = targetPath; EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
                RegisterScenes(targetPath);
                return;
            }
            bodyFont = AssetDatabase.LoadAssetAtPath<Font>(Folder + "/Fonts/DejaVuSans.ttf");
            titleFont = AssetDatabase.LoadAssetAtPath<Font>(Folder + "/Fonts/DejaVuSerif.ttf");
            var backdrop = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Art/Circus_Menu.png");
            var music = AssetDatabase.LoadAssetAtPath<AudioClip>(Folder + "/Audio/Circus_MusicBox.wav");
            if (!bodyFont || !titleFont || !backdrop || !music) { EditorUtility.DisplayDialog("TSirkus", "Faltan assets del menú. Importa el paquete completo y espera a que Unity termine.", "Entendido"); return; }
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("TSirkus_Menu");
            controller = root.AddComponent<TSirkusMenuController>();
            controller.gameScenePath = targetPath;
            var cameraGO = new GameObject("Menu_Camera", typeof(Camera), typeof(AudioListener));
            cameraGO.transform.SetParent(root.transform);
            var camera = cameraGO.GetComponent<Camera>();camera.clearFlags = CameraClearFlags.SolidColor;camera.backgroundColor = Color.black;camera.cullingMask = 0;cameraGO.tag = "MainCamera";
            var eventGO = new GameObject("EventSystem", typeof(EventSystem)); eventGO.transform.SetParent(root.transform);
#if ENABLE_INPUT_SYSTEM
            eventGO.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#else
            eventGO.AddComponent<StandaloneInputModule>();
#endif
            var canvasGO = new GameObject("Canvas_Menu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(root.transform, false);
            canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution = new Vector2(1600,900);scaler.matchWidthOrHeight = 0;
            RectTransform canvas = canvasGO.GetComponent<RectTransform>();
            var background = Full("Circus_Background", canvas);var raw = background.gameObject.AddComponent<RawImage>();raw.texture = backdrop;raw.raycastTarget = false;
            var aspect = background.gameObject.AddComponent<AspectRatioFitter>();aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;aspect.aspectRatio = (float)backdrop.width / backdrop.height;
            var shade = Full("Atmosphere_Shade", canvas).gameObject.AddComponent<Image>();shade.color = new Color(0,0,0,.23f);shade.raycastTarget = false;
            var safe = Full("SafeArea",canvas);safe.gameObject.AddComponent<TSirkusSafeArea>();
            BuildHome(safe);BuildSettings(safe);BuildHistory(safe);BuildLoading(safe);
            controller.musicSource = Audio("Menu_Music", root.transform, music, .55f, true);
            controller.interfaceSource = Audio("UI_Sounds", root.transform, null, 1, false);
            controller.hoverClip = AssetDatabase.LoadAssetAtPath<AudioClip>(Folder + "/Audio/Hover.wav");
            controller.clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>(Folder + "/Audio/Click.wav");
            Hide(controller.settings);Hide(controller.history);Hide(controller.loading);
            if (!AssetDatabase.IsValidFolder(Folder + "/Scenes")) AssetDatabase.CreateFolder(Folder,"Scenes");
            if (!EditorSceneManager.SaveScene(newScene, ScenePath)) { Debug.LogError("TSirkus: no se pudo guardar la escena."); return; }
            RegisterScenes(targetPath);
            Selection.activeGameObject = controller.gameObject;
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath));
            Debug.Log("TSirkus: menú creado en " + ScenePath + ". Pulsa Play para probar. Jugar abre " + targetPath);
        }
        private static void RegisterScenes(string target)
        {
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath,true) };
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.path == ScenePath) continue;
                scenes.Add(new EditorBuildSettingsScene(scene.path, scene.path == target || scene.enabled));
            }
            if (!scenes.Any(s => s.path == target)) scenes.Add(new EditorBuildSettingsScene(target,true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
        private static AudioSource Audio(string name, Transform parent, AudioClip clip, float volume, bool loop)
        {
            var go = new GameObject(name);go.transform.SetParent(parent,false);var a = go.AddComponent<AudioSource>();a.clip = clip;a.volume = volume;a.loop = loop;a.playOnAwake = false;a.spatialBlend = 0;return a;
        }
        private static void Hide(CanvasGroup group) { group.alpha = 0;group.interactable = false;group.blocksRaycasts = false;group.gameObject.SetActive(false); }
        private static RectTransform Full(string name, Transform parent)
        {
            var r = new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin = Vector2.zero;r.anchorMax = Vector2.one;r.offsetMin = r.offsetMax = Vector2.zero;return r;
        }
        private static RectTransform Box(string name, Transform parent, float x, float y, float w, float h)
        {
            var r = new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin = r.anchorMax = new Vector2(0,1);r.pivot = new Vector2(0,1);r.anchoredPosition = new Vector2(x,-y);r.sizeDelta = new Vector2(w,h);return r;
        }
        private CanvasGroup Page(string name, Transform parent)
        {
            var page = Full(name,parent);return page.gameObject.AddComponent<CanvasGroup>();
        }
        private RectTransform Column(Transform page)
        {
            var r = Box("Content",page,0,0,690,770);r.anchorMin = r.anchorMax = new Vector2(0,.5f);r.pivot = new Vector2(0,.5f);r.anchoredPosition = new Vector2(105,0);return r;
        }
        private Text Label(string name, Transform parent, string value, float x, float y, float w, float h, int size, Color color, bool serif = false)
        {
            var text = Box(name,parent,x,y,w,h).gameObject.AddComponent<Text>();text.font = serif ? titleFont : bodyFont;text.text = value;text.fontSize = size;text.color = color;text.alignment = TextAnchor.MiddleLeft;text.raycastTarget = false;text.horizontalOverflow = HorizontalWrapMode.Wrap;text.verticalOverflow = VerticalWrapMode.Truncate;return text;
        }
        private static Image Block(string name, Transform parent, float x, float y, float w, float h, Color color)
        {
            var image = Box(name,parent,x,y,w,h).gameObject.AddComponent<Image>();image.color = color;image.raycastTarget = false;return image;
        }
        private Button Button(Transform parent, string name, string text, float y, bool primary = false, float width = 530)
        {
            var box = Box(name,parent,0,y,width,76);var bg = box.gameObject.AddComponent<Image>();bg.color = primary ? new Color(.30f,.095f,.075f,.96f) : new Color(.05f,.04f,.035f,.68f);
            var button = box.gameObject.AddComponent<Button>();button.targetGraphic = bg;
            var colors = button.colors;colors.normalColor = Color.white;colors.highlightedColor = new Color(1.35f,1.25f,1.13f);colors.selectedColor = new Color(1.30f,1.20f,1.08f);colors.pressedColor = new Color(.72f,.65f,.57f);colors.fadeDuration = .16f;button.colors = colors;
            Label("Label",box,text,28,0,width-90,76,24,Cream);
            var arrow = Label("Arrow",box,"›",width-52,0,40,72,35,Gold);
            Block("BottomRule",box,0,75,width,1,new Color(Gold.r,Gold.g,Gold.b,.45f));
            var accent = Block("FocusAccent",box,0,0,3,76,new Color(Gold.r,Gold.g,Gold.b,0));
            var hover = box.gameObject.AddComponent<TSirkusMenuHover>();hover.owner = controller;hover.accent = accent;hover.arrow = arrow;
            return button;
        }
        private void BuildHome(Transform parent)
        {
            controller.home = Page("01_Home",parent);var c = Column(controller.home.transform);
            Label("Eyebrow",c,"B I E N V E N I D O   A L   E S P E C T Á C U L O",0,25,650,30,14,Gold);
            var title = Label("Title",c,"TSIRKUS",-7,68,695,145,116,Cream,true);var shadow = title.gameObject.AddComponent<Shadow>();shadow.effectColor = new Color(.19f,.025f,.02f,.9f);shadow.effectDistance = new Vector2(3,-4);
            Block("TitleRule",c,0,231,94,2,Gold);
            Label("Tagline",c,"CUATRO ENTRAN. SOLO UNO SALE.",0,257,600,34,18,Muted);
            controller.playButton = Button(c,"Button_Play","JUGAR",342,true);
            controller.settingsButton = Button(c,"Button_Settings","CONFIGURACIONES",434);
            controller.historyButton = Button(c,"Button_History","HISTORIA",526);
            controller.statusLabel = Label("Status",c,"",0,623,630,65,16,new Color(.92f,.57f,.39f));
            Label("Footer",c,"EL ESPECTÁCULO AÚN NO TERMINA.",0,716,630,26,12,Muted);
        }
        private void PageHeading(Transform c,string section,string title,string subtitle)
        {
            Block("PanelBackdrop",c,-35,-5,750,785,new Color(.025f,.02f,.018f,.91f));
            Label("Section",c,section,0,34,630,25,14,Gold);
            Label("Title",c,title,0,80,675,95,53,Cream,true);
            Block("Rule",c,0,199,100,2,Gold);
            Label("Subtitle",c,subtitle,0,223,640,60,18,Muted);
        }
        private void BuildSettings(Transform parent)
        {
            controller.settings = Page("02_Settings",parent);var c = Column(controller.settings.transform);
            PageHeading(c,"TSIRKUS  /  CONFIGURACIONES","Ajusta el espectáculo","La música también sabe guardar silencio.");
            Label("MusicLabel",c,"VOLUMEN DE MÚSICA",0,327,500,35,21,Cream);
            controller.volumeLabel = Label("VolumeValue",c,"55%",520,325,110,40,27,Gold);controller.volumeLabel.alignment = TextAnchor.MiddleRight;
            var sliderRect = Box("MusicVolume",c,0,395,630,70);var slider = sliderRect.gameObject.AddComponent<Slider>();slider.minValue = 0;slider.maxValue = 1;slider.value = .55f;
            var track = Block("Track",sliderRect,12,31,606,6,new Color(.18f,.15f,.11f));
            var fillArea = Full("FillArea",sliderRect);fillArea.offsetMin = new Vector2(12,31);fillArea.offsetMax = new Vector2(-12,-33);
            var fillRect = Full("Fill",fillArea);var fill = fillRect.gameObject.AddComponent<Image>();fill.color = Gold;
            var handleArea = Full("HandleArea",sliderRect);handleArea.offsetMin = new Vector2(12,0);handleArea.offsetMax = new Vector2(-12,0);
            var handle = Box("Handle",handleArea,0,0,22,34);handle.pivot = new Vector2(.5f,.5f);handle.anchorMin = new Vector2(.55f,0);handle.anchorMax = new Vector2(.55f,1);handle.sizeDelta = new Vector2(22,-36);handle.anchoredPosition = Vector2.zero;var handleImage = handle.gameObject.AddComponent<Image>();handleImage.color = Cream;
            slider.fillRect = fillRect;slider.handleRect = handle;slider.targetGraphic = handleImage;slider.direction = Slider.Direction.LeftToRight;
            var hit = sliderRect.gameObject.AddComponent<Image>();hit.color = new Color(0,0,0,0);hit.raycastTarget = true;
            controller.musicSlider = slider;
            Label("Minimum",c,"SILENCIO",0,482,240,28,13,Muted);
            var maximum = Label("Maximum",c,"100%",440,482,190,28,13,Muted);maximum.alignment = TextAnchor.MiddleRight;
            Label("SavedNote",c,"Tu preferencia se guarda automáticamente.",0,548,630,32,17,Muted);
            controller.settingsBack = Button(c,"Button_Back","VOLVER",651,false,260);
        }
        private void BuildHistory(Transform parent)
        {
            controller.history = Page("03_History",parent);var c = Column(controller.history.transform);
            PageHeading(c,"TSIRKUS  /  HISTORIA","La última función","Algunas puertas nunca debieron abrirse.");
            controller.storyLabel = Label("Story",c,controller.story,0,311,650,307,21,Cream);controller.storyLabel.alignment = TextAnchor.UpperLeft;controller.storyLabel.resizeTextForBestFit = true;controller.storyLabel.resizeTextMinSize = 17;controller.storyLabel.resizeTextMaxSize = 21;controller.storyLabel.lineSpacing = 1.18f;
            controller.historyBack = Button(c,"Button_Back","VOLVER",651,false,260);
        }
        private void BuildLoading(Transform parent)
        {
            controller.loading = Page("04_Loading",parent);var c = Column(controller.loading.transform);
            PageHeading(c,"TSIRKUS","Toma tu lugar.","La función está por comenzar.");
            controller.loadingLabel = Label("ProgressLabel",c,"ABRIENDO LA CARPA",0,374,650,40,18,Gold);
            Block("ProgressTrack",c,0,444,630,4,new Color(.18f,.15f,.11f));
            controller.progressFill = Block("ProgressFill",c,0,444,630,4,Gold);controller.progressFill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");controller.progressFill.type = Image.Type.Filled;controller.progressFill.fillMethod = Image.FillMethod.Horizontal;controller.progressFill.fillOrigin = 0;controller.progressFill.fillAmount = 0;
        }
    }
}
