using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TSirkus.MainMenuUpgrade;

namespace TSirkus.MainMenuUpgrade.Editor
{
    public sealed class TSirkusMainMenuSetup : EditorWindow
    {
        private const string MenuPath = "Assets/TSirkus/MainMenuUpgrade/Scenes/MainMenu_Mejorado.unity";
        private SceneAsset destination;
        [MenuItem("Tools/TSirkus/Menú actual/Abrir menú mejorado")]
        private static void OpenMenu()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(MenuPath);
        }
        [MenuItem("Tools/TSirkus/Menú actual/Conectar escena de Jugar")]
        private static void ShowWindow() { GetWindow<TSirkusMainMenuSetup>("TSirkus · Jugar"); }
        private void OnEnable() { destination = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Complete map.unity"); }
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Destino del botón Jugar", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Abre MainMenu_Mejorado, selecciona la escena de partida y aplica. No se modifica el mapa ni sus scripts.", MessageType.Info);
            destination = (SceneAsset)EditorGUILayout.ObjectField("Escena de partida", destination, typeof(SceneAsset), false);
            using (new EditorGUI.DisabledScope(!destination || EditorApplication.isPlaying))
                if (GUILayout.Button("Conectar y registrar escenas")) Apply();
        }
        private void Apply()
        {
            var controller = Object.FindFirstObjectByType<TSirkusMainMenuController>();
            if (!controller || controller.gameObject.scene.path != MenuPath)
            { EditorUtility.DisplayDialog("TSirkus", "Abre primero la escena MainMenu_Mejorado desde Tools > TSirkus > Menú actual.", "Entendido"); return; }
            string path = AssetDatabase.GetAssetPath(destination);
            if (path == MenuPath) { EditorUtility.DisplayDialog("TSirkus", "Elige la escena del juego, no la del menú.", "Entendido"); return; }
            Undo.RecordObject(controller, "Conectar Jugar TSirkus");
            controller.gameScenePath = path;
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            EditorSceneManager.SaveScene(controller.gameObject.scene);
            var scenes = new List<EditorBuildSettingsScene>();
            scenes.Add(new EditorBuildSettingsScene(MenuPath, true));
            foreach (var scene in EditorBuildSettings.scenes)
                if (scene.path != MenuPath && scene.path != path) scenes.Add(scene);
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            EditorUtility.DisplayDialog("TSirkus", "Jugar está conectado. En Build Profiles, comprueba también la Scene List si el perfil activo tiene una lista propia.", "Listo");
        }
    }
}
