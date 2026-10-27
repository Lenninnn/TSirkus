using System.Linq;
using System.Reflection;
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

        // Plantilla: el controlador la clona dentro del cuadrante de cada jugador
        // y le aplica su tamaño (hintSize, referencia del aviso inicial).
        RectTransform imageRect = (RectTransform)imageObject.transform;
        imageRect.anchorMin = new Vector2(0.5f, 1f);
        imageRect.anchorMax = new Vector2(0.5f, 1f);
        imageRect.pivot = new Vector2(0.5f, 1f);
        imageRect.anchoredPosition = new Vector2(0f, -12f);
        imageRect.sizeDelta = new Vector2(280f, 112f);

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

    // Mueve a un jugador (1-4) a una zona por nombre, o fuera de las zonas (centro de la carpa).
    private static void TeleportPlayer(int playerId, string zoneName)
    {
        PlayerIdentity player = Object.FindObjectsByType<PlayerIdentity>()
            .FirstOrDefault(p =>
                p.PlayerId == playerId &&
                p.TryGetComponent(out CharacterController controller) && controller.enabled &&
                p.TryGetComponent(out PlayerMovement movement) && movement.enabled);

        if (player == null)
        {
            Debug.LogWarning($"ContextHintTools: no se encontró Player {playerId}.");
            return;
        }

        Vector3 target = new Vector3(0f, 0f, -5f);

        if (zoneName != null)
        {
            HintZone zone = Object.FindObjectsByType<HintZone>().FirstOrDefault(z => z.name == zoneName);

            if (zone == null)
            {
                Debug.LogWarning($"ContextHintTools: no existe la zona {zoneName}.");
                return;
            }

            target = zone.transform.position;
        }

        target.y = player.transform.position.y;

        CharacterController body = player.GetComponent<CharacterController>();
        body.enabled = false;
        player.transform.position = target;
        body.enabled = true;

        Debug.Log($"[AYUDAS TEST] Player {playerId} → {zoneName ?? "fuera de las zonas"}");
    }


    // Simula ACCIÓN de un jugador durante 0,3 s (solo editor, por reflexión;
    // MobileInputManager no se modifica).
    private static void PressAction(int playerId)
    {
        MobileInputManager input = Object.FindAnyObjectByType<MobileInputManager>();

        System.Array players = typeof(MobileInputManager)
            .GetField("players", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(input) as System.Array;

        object state = players?.GetValue(playerId);
        FieldInfo action = state?.GetType().GetField("action");

        if (action == null)
        {
            Debug.LogWarning("ContextHintTools: no se pudo simular ACCIÓN.");
            return;
        }

        action.SetValue(state, true);
        double releaseAt = EditorApplication.timeSinceStartup + 0.3;

        EditorApplication.CallbackFunction release = null;
        release = () =>
        {
            if (EditorApplication.timeSinceStartup < releaseAt)
                return;

            action.SetValue(state, false);
            EditorApplication.update -= release;
        };

        EditorApplication.update += release;

        Debug.Log($"[AYUDAS TEST] ACCIÓN de Player {playerId}");
    }


    // Ejecuta pasos con retardo (segundos reales) mientras dura el Play Mode.
    private static void RunSteps(params (double delay, System.Action action)[] steps)
    {
        int index = 0;
        double next = EditorApplication.timeSinceStartup + steps[0].delay;

        EditorApplication.CallbackFunction tick = null;
        tick = () =>
        {
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.update -= tick;
                return;
            }

            while (index < steps.Length && EditorApplication.timeSinceStartup >= next)
            {
                steps[index].action();
                index++;

                if (index < steps.Length)
                    next = EditorApplication.timeSinceStartup + steps[index].delay;
            }

            if (index >= steps.Length)
                EditorApplication.update -= tick;
        };

        EditorApplication.update += tick;
    }


    private static void LogHud(string label)
    {
        string state = string.Join(" | ", Enumerable.Range(1, 4).Select(id =>
        {
            PlayerHintHUD hud = Object.FindObjectsByType<PlayerHintHUD>().FirstOrDefault(h => h.PlayerId == id);
            return $"P{id}: {(hud != null && hud.CurrentHint != null ? hud.CurrentHint.name : "-")}";
        }));

        Debug.Log($"[AYUDAS TEST] {label} → {state}");
    }


    [MenuItem("TSirkus/Ayudas/Probar/Caso 1 - P1 a Interactuar")]
    private static void Case1() => RunSteps(
        (0, () => TeleportPlayer(1, "Zona_Interactuar_Switch1")),
        (1.5, () => LogHud("Caso 1")));

    [MenuItem("TSirkus/Ayudas/Probar/Caso 2 - P3 a Interactuar")]
    private static void Case2() => RunSteps(
        (0, () => TeleportPlayer(3, "Zona_Interactuar_Switch1")),
        (1.5, () => LogHud("Caso 2")));

    [MenuItem("TSirkus/Ayudas/Probar/Caso 3 - P1 sale y vuelve a Interactuar")]
    private static void Case3() => RunSteps(
        (0, () => TeleportPlayer(1, null)),
        (0.6, () => TeleportPlayer(1, "Zona_Interactuar_Switch1")),
        (1.5, () => LogHud("Caso 3 (P1 no debe repetir Hint_Interactuar)")));

    [MenuItem("TSirkus/Ayudas/Probar/Casos 4 y 5 - ACCION de P2 y luego de P1")]
    private static void Case4And5() => RunSteps(
        (0, () => TeleportPlayer(1, "Zona_PuertaCerrada")),
        (1.5, () => LogHud("Antes de ACCIÓN")),
        (0, () => PressAction(2)),
        (1.0, () => LogHud("Caso 4 tras ACCIÓN de P2 (P1 debe seguir)")),
        (0, () => PressAction(1)),
        (1.0, () => LogHud("Caso 5 tras ACCIÓN de P1 (P1 debe cerrarse)")));

    [MenuItem("TSirkus/Ayudas/Probar/Caso 6 - P1 a Cooperacion y P4 a Interactuar")]
    private static void Case6() => RunSteps(
        (0, () => TeleportPlayer(1, "Zona_Cooperacion_Globos")),
        (0, () => TeleportPlayer(4, "Zona_Interactuar_Switch1")),
        (1.5, () => LogHud("Caso 6")));

    [MenuItem("TSirkus/Ayudas/Probar/Pulsar ACCION de P1")]
    private static void P1Action() => PressAction(1);

    [MenuItem("TSirkus/Ayudas/Probar/Pulsar ACCION de P2")]
    private static void P2Action() => PressAction(2);


    [MenuItem("TSirkus/Ayudas/Probar/Caso 1 - P1 a Interactuar", true)]
    [MenuItem("TSirkus/Ayudas/Probar/Caso 2 - P3 a Interactuar", true)]
    [MenuItem("TSirkus/Ayudas/Probar/Caso 3 - P1 sale y vuelve a Interactuar", true)]
    [MenuItem("TSirkus/Ayudas/Probar/Casos 4 y 5 - ACCION de P2 y luego de P1", true)]
    [MenuItem("TSirkus/Ayudas/Probar/Caso 6 - P1 a Cooperacion y P4 a Interactuar", true)]
    [MenuItem("TSirkus/Ayudas/Probar/Pulsar ACCION de P1", true)]
    [MenuItem("TSirkus/Ayudas/Probar/Pulsar ACCION de P2", true)]
    private static bool PlayModeOnly()
    {
        return EditorApplication.isPlaying;
    }
}
