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

        // Objeto que onPuzzleCompleted activa; se apaga al abrir la puerta.
        GameObject successObject = door.gameObject.scene.GetRootGameObjects()
            .FirstOrDefault(root => root.name == "SuccessObject");

        if (successObject == null)
            Debug.LogWarning("DoorCinematicTools: no se encontró 'SuccessObject' en la raíz de la escena.");

        SerializedObject cinematicSO = new SerializedObject(cinematic);
        cinematicSO.FindProperty("successObject").objectReferenceValue = successObject;
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


    // =====================================================
    // VERIFICAR CORREDOR (PLAY MODE, TRAS LA CINEMÁTICA)
    // =====================================================

    [MenuItem("TSirkus/Puerta 1/Verificar corredor libre (Play Mode)")]
    private static void VerifyCorridor()
    {
        PuzzleDoor door = Object.FindAnyObjectByType<PuzzleDoor>(FindObjectsInactive.Include);
        DoorCinematicController cinematic =
            Object.FindAnyObjectByType<DoorCinematicController>(FindObjectsInactive.Include);

        GameObject doorVisual = new SerializedObject(door)
            .FindProperty("doorVisual").objectReferenceValue as GameObject;
        GameObject successObject = new SerializedObject(cinematic)
            .FindProperty("successObject").objectReferenceValue as GameObject;

        Debug.Log(
            $"[VERIFICAR] doorVisual activo: {(doorVisual != null && doorVisual.activeInHierarchy)} | " +
            $"SuccessObject activo: {(successObject != null && successObject.activeInHierarchy)}"
        );

        foreach (PlayerIdentity player in Object.FindObjectsByType<PlayerIdentity>().OrderBy(p => p.PlayerId))
        {
            bool control =
                IsEnabled(player.GetComponent<PlayerMovement>()) &&
                IsEnabled(player.GetComponent<PlayerLook>()) &&
                IsEnabled(player.GetComponent<PlayerInteractor>());

            // CapsuleCast desde el jugador hasta 3 m más allá de la puerta.
            CharacterController body = player.GetComponent<CharacterController>();
            Vector3 center = body != null ? player.transform.TransformPoint(body.center) : player.transform.position + Vector3.up;
            float radius = body != null ? body.radius : 0.4f;
            float half = body != null ? Mathf.Max(0f, body.height * 0.5f - radius) : 0.5f;

            Vector3 target = door.transform.position;
            target.y = center.y;
            Vector3 toDoor = target - center;
            Vector3 direction = toDoor.normalized;
            float distance = toDoor.magnitude + 3f;

            string blockers = string.Join(", ", Physics
                .CapsuleCastAll(center + Vector3.up * half, center - Vector3.up * half, radius * 0.9f,
                    direction, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                .Where(hit => !hit.collider.transform.IsChildOf(player.transform))
                .OrderBy(hit => hit.distance)
                .Select(hit => $"{hit.collider.name}@{hit.distance:0.0}m"));

            Debug.Log(
                $"[VERIFICAR] Player {player.PlayerId} | control: {control} | " +
                $"obstáculos hasta pasar la puerta ({distance:0.0}m): {(blockers.Length == 0 ? "ninguno" : blockers)}"
            );
        }
    }


    private static bool IsEnabled(Behaviour component)
    {
        return component != null && component.enabled;
    }


    [MenuItem("TSirkus/Puerta 1/Verificar corredor libre (Play Mode)", true)]
    private static bool VerifyCorridorValidate()
    {
        return EditorApplication.isPlaying;
    }


    [MenuItem("TSirkus/Puerta 1/Simular 4 jugadores en el puzzle (Play Mode)", true)]
    private static bool SimulatePuzzleValidate()
    {
        return EditorApplication.isPlaying;
    }
}
