using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Crea el sistema de ayudas contextuales en la escena GAME y permite probarlo.
// Solo crea lo que falta: no borra ni sobrescribe zonas ya ajustadas a mano.
public static class ContextHintTools
{
    private const string HintFolder = "Assets/UI/Hints";
    private const string DefinitionFolder = "Assets/UI/Hints/Definitions";

    private struct HintSpec
    {
        public string asset, sprite, message;
        public int priority;
        public float seconds;
    }

    private static readonly HintSpec[] Hints =
    {
        new HintSpec { asset = "Hint_Palancas", sprite = "hint_placeholder_01", priority = 20, seconds = 4.5f,
            message = "No todo se resuelve solo.\nCada jugador debe encontrar su lugar." },
        new HintSpec { asset = "Hint_PuertaCerrada", sprite = "hint_placeholder_02", priority = 10, seconds = 4.5f,
            message = "Si el paso está cerrado, busca cómo abrirlo.\nEl circo siempre deja pistas." },
        new HintSpec { asset = "Hint_Interactuar", sprite = "hint_placeholder_03", priority = 30, seconds = 4f,
            message = "Acércate y usa ACCIÓN para interactuar." },
        new HintSpec { asset = "Hint_NuevaZona", sprite = "hint_placeholder_04", priority = 15, seconds = 4.5f,
            message = "Bien hecho.\nSigan avanzando. El circo aún guarda secretos." },
        new HintSpec { asset = "Hint_Cooperacion", sprite = "hint_placeholder_05", priority = 15, seconds = 4.5f,
            message = "Observen el entorno.\nA veces avanzar requiere que todos participen." },
    };


    // =====================================================
    // CREAR SISTEMA EN LA ESCENA
    // =====================================================

    [MenuItem("TSirkus/Ayudas/Crear ayudas contextuales en GAME")]
    private static void CreateInScene()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("ContextHintTools: sal de Play Mode antes de crear las ayudas.");
            return;
        }

        var scene = EditorSceneManager.GetActiveScene();

        if (scene.name != "GAME")
        {
            Debug.LogError("ContextHintTools: abre la escena GAME primero.");
            return;
        }

        if (!AssetDatabase.IsValidFolder(DefinitionFolder))
            AssetDatabase.CreateFolder(HintFolder, "Definitions");

        var definitions = Hints.ToDictionary(spec => spec.asset, CreateDefinition);

        ContextHintController controller = CreateHud(scene);

        PuzzleController firstPuzzle = Object
            .FindObjectsByType<PuzzleController>(FindObjectsInactive.Include)
            .FirstOrDefault(puzzle => puzzle.name == "Puzzle_Test");

        GameObject root = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "ContextHints");

        if (root == null)
        {
            root = new GameObject("ContextHints");
            Undo.RegisterCreatedObjectUndo(root, "Crear ayudas contextuales");
        }

        // Carpa: zona norte con los 4 interruptores de la primera prueba.
        CreateZone(root, "Zona_Palancas", new Vector3(0f, 1.5f, 8.5f), new Vector3(13f, 3f, 4f),
            definitions["Hint_Palancas"], controller, firstPuzzle, HintZone.PuzzleCondition.OnlyWhileUnsolved);

        // Carpa: frente a la puerta de la primera prueba (PuzzleDoor).
        CreateZone(root, "Zona_PuertaCerrada", new Vector3(0f, 1.5f, 10.6f), new Vector3(3.5f, 3f, 2.4f),
            definitions["Hint_PuertaCerrada"], controller, firstPuzzle, HintZone.PuzzleCondition.OnlyWhileUnsolved);

        // Carpa: junto a cada interruptor (Switch1-4). Comparten la misma ayuda (se ve una vez).
        foreach (string switchName in new[] { "Switch1", "Switch2", "Switch3", "Switch4" })
        {
            GameObject target = scene.GetRootGameObjects().FirstOrDefault(go => go.name == switchName);

            if (target == null)
            {
                Debug.LogWarning($"ContextHintTools: no se encontró {switchName}.");
                continue;
            }

            Vector3 position = target.transform.position;
            position.y = 1.5f;
            position.z -= 1.2f;

            CreateZone(root, $"Zona_Interactuar_{switchName}", position, new Vector3(2.5f, 3f, 2.5f),
                definitions["Hint_Interactuar"], controller, firstPuzzle, HintZone.PuzzleCondition.OnlyWhileUnsolved);
        }

        // Corredor norte, justo detrás de la puerta: solo accesible tras superar la prueba.
        CreateZone(root, "Zona_NuevaZona", new Vector3(0f, 1.5f, 15f), new Vector3(4f, 3f, 3f),
            definitions["Hint_NuevaZona"], controller, firstPuzzle, HintZone.PuzzleCondition.OnlyWhenSolved);

        // Corredor norte: entrada a la sala de globos (GameRoom_01), prueba cooperativa.
        CreateZone(root, "Zona_Cooperacion_Globos", new Vector3(-4.5f, 1.5f, 23f), new Vector3(3f, 3f, 3f),
            definitions["Hint_Cooperacion"], controller, null, HintZone.PuzzleCondition.Ignore);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log($"ContextHintTools: {root.transform.childCount} zonas en ContextHints. Escena guardada.");
    }


    private static HintDefinition CreateDefinition(HintSpec spec)
    {
        string path = $"{DefinitionFolder}/{spec.asset}.asset";
        HintDefinition definition = AssetDatabase.LoadAssetAtPath<HintDefinition>(path);

        if (definition != null)
            return definition;

        definition = ScriptableObject.CreateInstance<HintDefinition>();
        definition.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{HintFolder}/{spec.sprite}.png");
        definition.finalMessage = spec.message;
        definition.priority = spec.priority;
        definition.visibleSeconds = spec.seconds;

        if (definition.sprite == null)
            Debug.LogWarning($"ContextHintTools: no se encontró el sprite {spec.sprite}.");

        AssetDatabase.CreateAsset(definition, path);
        return definition;
    }


    private static ContextHintController CreateHud(UnityEngine.SceneManagement.Scene scene)
    {
        ContextHintController existing =
            Object.FindAnyObjectByType<ContextHintController>(FindObjectsInactive.Include);

        if (existing != null)
            return existing;

        // Mismo Canvas que TutorialHUD / IntroDialogues / DoorCinematicOverlay.
        DoorCinematicController cinematic =
            Object.FindAnyObjectByType<DoorCinematicController>(FindObjectsInactive.Include);

        CanvasGroup cinematicOverlay = cinematic != null
            ? new SerializedObject(cinematic).FindProperty("cinematicOverlay").objectReferenceValue as CanvasGroup
            : null;

        Canvas canvas = cinematicOverlay != null
            ? cinematicOverlay.transform.parent.GetComponentInParent<Canvas>()
            : scene.GetRootGameObjects().Select(go => go.GetComponent<Canvas>()).FirstOrDefault(c => c != null);

        GameObject hud = new GameObject("ContextHintHUD", typeof(RectTransform), typeof(CanvasGroup));
        Undo.RegisterCreatedObjectUndo(hud, "Crear ayudas contextuales");
        hud.transform.SetParent(canvas.transform, false);

        // Debajo del overlay de la cinemática.
        if (cinematicOverlay != null)
            hud.transform.SetSiblingIndex(cinematicOverlay.transform.GetSiblingIndex());

        RectTransform hudRect = (RectTransform)hud.transform;
        hudRect.anchorMin = Vector2.zero;
        hudRect.anchorMax = Vector2.one;
        hudRect.offsetMin = Vector2.zero;
        hudRect.offsetMax = Vector2.zero;

        CanvasGroup group = hud.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        GameObject imageObject = new GameObject("HintImage", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(hud.transform, false);

        // Arriba al centro, como el aviso inicial; proporción de los banners (790x316).
        RectTransform imageRect = (RectTransform)imageObject.transform;
        imageRect.anchorMin = new Vector2(0.5f, 1f);
        imageRect.anchorMax = new Vector2(0.5f, 1f);
        imageRect.pivot = new Vector2(0.5f, 1f);
        imageRect.anchoredPosition = new Vector2(0f, -24f);
        imageRect.sizeDelta = new Vector2(520f, 208f);

        Image image = imageObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;

        ContextHintController controller = hud.AddComponent<ContextHintController>();

        SerializedObject controllerSO = new SerializedObject(controller);
        controllerSO.FindProperty("canvasGroup").objectReferenceValue = group;
        controllerSO.FindProperty("hintImage").objectReferenceValue = image;
        controllerSO.FindProperty("mobileInput").objectReferenceValue =
            Object.FindAnyObjectByType<MobileInputManager>(FindObjectsInactive.Include);

        // Esperar a: aviso inicial, diálogo de intro y cinemática de la puerta.
        var waitFor = Object.FindObjectsByType<TutorialImageFade>(FindObjectsInactive.Include)
            .Select(tutorial => tutorial.canvasGroup)
            .Concat(Object.FindObjectsByType<IntroDialogueSequence>(FindObjectsInactive.Include)
                .Select(intro => new SerializedObject(intro).FindProperty("dialogCanvasGroup").objectReferenceValue as CanvasGroup))
            .Append(cinematicOverlay)
            .Where(cg => cg != null)
            .ToArray();

        SerializedProperty waitProp = controllerSO.FindProperty("waitWhileVisible");
        waitProp.arraySize = waitFor.Length;

        for (int i = 0; i < waitFor.Length; i++)
            waitProp.GetArrayElementAtIndex(i).objectReferenceValue = waitFor[i];

        controllerSO.ApplyModifiedPropertiesWithoutUndo();

        return controller;
    }


    private static void CreateZone(
        GameObject root, string zoneName, Vector3 position, Vector3 size,
        HintDefinition hint, ContextHintController controller,
        PuzzleController puzzle, HintZone.PuzzleCondition condition)
    {
        if (root.transform.Find(zoneName) != null)
            return;

        GameObject zone = new GameObject(zoneName, typeof(BoxCollider), typeof(HintZone));
        zone.transform.SetParent(root.transform, false);
        zone.transform.position = position;

        BoxCollider box = zone.GetComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = size;

        SerializedObject zoneSO = new SerializedObject(zone.GetComponent<HintZone>());
        zoneSO.FindProperty("hint").objectReferenceValue = hint;
        zoneSO.FindProperty("controller").objectReferenceValue = controller;
        zoneSO.FindProperty("contextPuzzle").objectReferenceValue = puzzle;
        zoneSO.FindProperty("puzzleCondition").enumValueIndex = (int)condition;
        zoneSO.ApplyModifiedPropertiesWithoutUndo();
    }


    // =====================================================
    // PRUEBA EN PLAY MODE
    // =====================================================

    private static int nextZone;

    [MenuItem("TSirkus/Ayudas/Probar: llevar Player 1 a la siguiente zona (Play Mode)")]
    private static void TeleportToNextZone()
    {
        HintZone[] zones = Object.FindObjectsByType<HintZone>()
            .OrderBy(zone => zone.transform.GetSiblingIndex())
            .ToArray();

        PlayerIdentity player = Object.FindObjectsByType<PlayerIdentity>()
            .FirstOrDefault(p =>
                p.PlayerId == 1 &&
                p.TryGetComponent(out CharacterController controller) && controller.enabled &&
                p.TryGetComponent(out PlayerMovement movement) && movement.enabled);

        if (zones.Length == 0 || player == null)
        {
            Debug.LogWarning("ContextHintTools: no hay zonas o no se encontró Player 1.");
            return;
        }

        HintZone zone = zones[nextZone % zones.Length];
        nextZone++;

        CharacterController body = player.GetComponent<CharacterController>();
        Vector3 target = zone.transform.position;
        target.y = player.transform.position.y;

        body.enabled = false;
        player.transform.position = target;
        body.enabled = true;

        Debug.Log($"[AYUDAS TEST] Player 1 → {zone.name}");
    }


    [MenuItem("TSirkus/Ayudas/Probar: llevar Player 1 a la siguiente zona (Play Mode)", true)]
    private static bool TeleportToNextZoneValidate()
    {
        return EditorApplication.isPlaying;
    }
}
