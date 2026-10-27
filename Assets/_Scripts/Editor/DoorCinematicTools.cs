using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Herramientas de editor para la cinemática de la primera puerta (escena GAME).
public static class DoorCinematicTools
{
    // =====================================================
    // CABLEAR ESCENA
    // =====================================================

    [MenuItem("TSirkus/Puerta 1/Configurar referencias de cinemática")]
    private static void ConfigureScene()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("DoorCinematicTools: sal de Play Mode antes de configurar la escena.");
            return;
        }

        PuzzleDoor door = Object.FindAnyObjectByType<PuzzleDoor>(FindObjectsInactive.Include);
        DoorCinematicController cinematic =
            Object.FindAnyObjectByType<DoorCinematicController>(FindObjectsInactive.Include);

        if (door == null || cinematic == null)
        {
            Debug.LogError("DoorCinematicTools: no se encontró PuzzleDoor o DoorCinematicController en la escena abierta.");
            return;
        }

        // La puerta visible es la instancia del prefab llamada exactamente "DoorVisual".
        // El cubo placeholder 'DoorVisual  ' (con espacios) está inactivo y no se ve.
        Transform doorVisual = door.transform.Cast<Transform>()
            .FirstOrDefault(child => child.name == "DoorVisual");

        if (doorVisual == null)
        {
            Debug.LogError("DoorCinematicTools: no se encontró el hijo 'DoorVisual' de PuzzleDoor.");
            return;
        }

        SerializedObject doorSO = new SerializedObject(door);
        doorSO.FindProperty("doorVisual").objectReferenceValue = doorVisual.gameObject;
        doorSO.ApplyModifiedProperties();

        // Cards de instrucciones que deben quedar ocultas tras la cinemática.
        List<CanvasGroup> groups = new List<CanvasGroup>();

        foreach (TutorialImageFade tutorial in
                 Object.FindObjectsByType<TutorialImageFade>(FindObjectsInactive.Include))
        {
            CanvasGroup group = tutorial.canvasGroup != null
                ? tutorial.canvasGroup
                : tutorial.GetComponent<CanvasGroup>();

            if (group != null)
                groups.Add(group);
        }

        foreach (IntroDialogueSequence intro in
                 Object.FindObjectsByType<IntroDialogueSequence>(FindObjectsInactive.Include))
        {
            Object group = new SerializedObject(intro)
                .FindProperty("dialogCanvasGroup").objectReferenceValue;

            if (group is CanvasGroup canvasGroup)
                groups.Add(canvasGroup);
        }

        SerializedObject cinematicSO = new SerializedObject(cinematic);
        SerializedProperty groupsProp = cinematicSO.FindProperty("instructionCanvasGroups");
        groupsProp.arraySize = groups.Count;

        for (int i = 0; i < groups.Count; i++)
            groupsProp.GetArrayElementAtIndex(i).objectReferenceValue = groups[i];

        cinematicSO.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(door.gameObject.scene);
        EditorSceneManager.SaveScene(door.gameObject.scene);

        Debug.Log(
            $"DoorCinematicTools: doorVisual → '{doorVisual.name}', " +
            $"{groups.Count} cards asignadas ({string.Join(", ", groups.Select(g => g.name))}). Escena guardada."
        );
    }


    // =====================================================
    // SIMULAR LOS 4 JUGADORES (PLAY MODE)
    // =====================================================

    [MenuItem("TSirkus/Puerta 1/Simular 4 jugadores en el puzzle (Play Mode)")]
    private static void SimulatePuzzle()
    {
        FourSwitchPuzzleController puzzle = Object.FindAnyObjectByType<FourSwitchPuzzleController>();

        PlayerIdentity[] players = Object
            .FindObjectsByType<PlayerIdentity>()
            .OrderBy(player => player.PlayerId)
            .ToArray();

        if (puzzle == null || players.Length == 0)
        {
            Debug.LogWarning("DoorCinematicTools: no hay FourSwitchPuzzleController o jugadores en escena.");
            return;
        }

        foreach (PlayerIdentity player in players)
            puzzle.RegisterPlayer(player);
    }


    [MenuItem("TSirkus/Puerta 1/Simular 4 jugadores en el puzzle (Play Mode)", true)]
    private static bool SimulatePuzzleValidate()
    {
        return EditorApplication.isPlaying;
    }
}
