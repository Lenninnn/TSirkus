using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Crea el HUD de objetivos en GAME y conecta el primer objetivo al final
// del flujo de la puerta (DoorCinematicController.onDoorFlowFinished).
public static class ObjectiveTools
{
    private const string ObjectiveFolder = "Assets/UI/Objectives";
    private const string FirstObjectivePath = ObjectiveFolder + "/Objective_Prueba01.asset";


    [MenuItem("TSirkus/Objetivos/Crear HUD de objetivos y objetivo de la prueba 1")]
    private static void CreateInScene()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("ObjectiveTools: sal de Play Mode antes de configurar la escena.");
            return;
        }

        var scene = EditorSceneManager.GetActiveScene();

        if (scene.name != "GAME")
        {
            Debug.LogError("ObjectiveTools: abre la escena GAME primero.");
            return;
        }

        // Asset del primer objetivo
        ObjectiveDefinition objective = AssetDatabase.LoadAssetAtPath<ObjectiveDefinition>(FirstObjectivePath);

        if (objective == null)
        {
            objective = ScriptableObject.CreateInstance<ObjectiveDefinition>();
            objective.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ObjectiveFolder + "/objective_placeholder_prueba01.png");
            objective.finalMessage = "LA PRIMERA PRUEBA TE ESPERA\nAdéntrate en el circo. Encuéntrala y sobrevive.";
            AssetDatabase.CreateAsset(objective, FirstObjectivePath);
        }

        DoorCinematicController cinematic =
            Object.FindAnyObjectByType<DoorCinematicController>(FindObjectsInactive.Include);

        if (cinematic == null)
        {
            Debug.LogError("ObjectiveTools: no se encontró DoorCinematicController.");
            return;
        }

        ObjectivePromptController controller =
            Object.FindAnyObjectByType<ObjectivePromptController>(FindObjectsInactive.Include) ??
            CreateHud(cinematic);

        // Conectar al final del flujo de la puerta (una sola vez).
        bool alreadyWired = Enumerable.Range(0, cinematic.onDoorFlowFinished.GetPersistentEventCount())
            .Any(i => cinematic.onDoorFlowFinished.GetPersistentTarget(i) == controller &&
                      cinematic.onDoorFlowFinished.GetPersistentMethodName(i) == nameof(ObjectivePromptController.ShowToAll));

        if (!alreadyWired)
        {
            UnityEventTools.AddObjectPersistentListener<ObjectiveDefinition>(
                cinematic.onDoorFlowFinished, controller.ShowToAll, objective);
            EditorUtility.SetDirty(cinematic);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("ObjectiveTools: HUD de objetivos listo y Objective_Prueba01 conectado a onDoorFlowFinished. Escena guardada.");
    }


    private static ObjectivePromptController CreateHud(DoorCinematicController cinematic)
    {
        CanvasGroup cinematicOverlay =
            new SerializedObject(cinematic).FindProperty("cinematicOverlay").objectReferenceValue as CanvasGroup;

        Canvas canvas = cinematicOverlay.transform.parent.GetComponentInParent<Canvas>();

        GameObject hud = new GameObject("ObjectivePromptHUD", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(hud, "Crear HUD de objetivos");
        hud.transform.SetParent(canvas.transform, false);

        // Debajo del overlay de la cinemática.
        hud.transform.SetSiblingIndex(cinematicOverlay.transform.GetSiblingIndex());

        RectTransform hudRect = (RectTransform)hud.transform;
        hudRect.anchorMin = Vector2.zero;
        hudRect.anchorMax = Vector2.one;
        hudRect.offsetMin = Vector2.zero;
        hudRect.offsetMax = Vector2.zero;

        // Plantilla: el controlador la clona en el cuadrante de cada jugador.
        GameObject imageObject = new GameObject("ObjectiveImage", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(hud.transform, false);

        Image image = imageObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;

        ObjectivePromptController controller = hud.AddComponent<ObjectivePromptController>();

        SerializedObject controllerSO = new SerializedObject(controller);
        controllerSO.FindProperty("objectiveImage").objectReferenceValue = image;
        controllerSO.FindProperty("mobileInput").objectReferenceValue =
            Object.FindAnyObjectByType<MobileInputManager>(FindObjectsInactive.Include);
        controllerSO.ApplyModifiedPropertiesWithoutUndo();

        return controller;
    }


    // =====================================================
    // PRUEBA EN PLAY MODE
    // =====================================================

    // Completa el puzzle con los 4 jugadores; cuando aparece el objetivo,
    // P2 pulsa ACCIÓN (solo debe cerrar el suyo) y P1 camina mientras se ve.
    [MenuItem("TSirkus/Objetivos/Probar objetivo tras la puerta (Play Mode)")]
    private static void TestObjectiveAfterDoor()
    {
        FourSwitchPuzzleController puzzle = Object.FindAnyObjectByType<FourSwitchPuzzleController>();

        foreach (PlayerIdentity player in Object.FindObjectsByType<PlayerIdentity>()
                     .Where(p => p.GetComponentInChildren<Camera>(true) != null)
                     .OrderBy(p => p.PlayerId))
        {
            puzzle.RegisterPlayer(player);
        }

        EditorApplication.CallbackFunction wait = null;
        wait = () =>
        {
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.update -= wait;
                return;
            }

            if (!Object.FindObjectsByType<PlayerObjectiveHUD>().Any(h => h.CurrentObjective != null))
                return;

            EditorApplication.update -= wait;

            PlayerIdentity p1 = Object.FindObjectsByType<PlayerIdentity>()
                .First(p => p.PlayerId == 1 && p.GetComponentInChildren<Camera>(true) != null);
            Vector3 start = Vector3.zero;

            PlayerDiagnosticsTools.RunSteps(
                (3.0, () => LogState("Objetivo visible")),
                (0, () =>
                {
                    start = p1.transform.position;
                    PlayerDiagnosticsTools.SetInput(2, "action", true);
                    PlayerDiagnosticsTools.SetInput(1, "movementY", 1f);
                }),
                (0.3, () => PlayerDiagnosticsTools.SetInput(2, "action", false)),
                (0.6, () =>
                {
                    PlayerDiagnosticsTools.SetInput(1, "movementY", 0f);
                    LogState($"Tras ACCIÓN de P2 (P1 caminó {Vector3.Distance(start, p1.transform.position):0.00} m)");
                }),
                (4.5, () => LogState("Pasados ~8,5 s")));
        };

        EditorApplication.update += wait;
    }


    private static void LogState(string label)
    {
        string state = string.Join(" | ", Enumerable.Range(1, 4).Select(id =>
        {
            PlayerObjectiveHUD hud = Object.FindObjectsByType<PlayerObjectiveHUD>().FirstOrDefault(h => h.PlayerId == id);
            return $"P{id}: {(hud != null && hud.CurrentObjective != null ? hud.CurrentObjective.name : "-")}";
        }));

        Debug.Log($"[OBJETIVOS TEST] {label} → {state}");
    }


    [MenuItem("TSirkus/Objetivos/Probar objetivo tras la puerta (Play Mode)", true)]
    private static bool PlayModeOnly()
    {
        return EditorApplication.isPlaying;
    }
}
