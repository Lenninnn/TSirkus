using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Configura el final del juego en GAME (idempotente):
// - Assets/Data/Creditos_TSirkus.asset (nombres y roles editables).
// - Assets/StreamingAssets/Endings/ (aquí van Ending_Player1..4.mp4).
// - Objeto "FinalDelJuego" con GameEndingManager + RaceWinnerDetector y sus referencias.
public static class EndingSetupTools
{
    private const string CreditsPath = "Assets/Data/Creditos_TSirkus.asset";

    [MenuItem("TSirkus/Final/Configurar final en GAME")]
    private static void Setup()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[FINAL] Sal de Play Mode primero.");
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");

        if (!AssetDatabase.IsValidFolder("Assets/StreamingAssets"))
            AssetDatabase.CreateFolder("Assets", "StreamingAssets");

        if (!AssetDatabase.IsValidFolder("Assets/StreamingAssets/Endings"))
            AssetDatabase.CreateFolder("Assets/StreamingAssets", "Endings");

        var credits = AssetDatabase.LoadAssetAtPath<EndingCredits>(CreditsPath);

        if (credits == null)
        {
            credits = ScriptableObject.CreateInstance<EndingCredits>();
            // Integrantes del proyecto, en el orden indicado por el equipo. Sin cargos individuales.
            credits.sections.Add(new EndingCredits.Section
            {
                heading = "INTEGRANTES",
                entries =
                {
                    new EndingCredits.Entry { name = "María José Aricapa", role = "" },
                    new EndingCredits.Entry { name = "Luisa Cruz", role = "" },
                    new EndingCredits.Entry { name = "Lennin Sanchez", role = "" },
                    new EndingCredits.Entry { name = "Juan José Builes", role = "" },
                    new EndingCredits.Entry { name = "Alejandro Avendaño", role = "" }
                }
            });
            AssetDatabase.CreateAsset(credits, CreditsPath);
        }

        GameObject host = Object.FindObjectsByType<GameEndingManager>(FindObjectsInactive.Include)
            .Select(m => m.gameObject).FirstOrDefault() ?? new GameObject("FinalDelJuego");

        GameEndingManager manager = host.GetComponent<GameEndingManager>();
        if (manager == null) manager = host.AddComponent<GameEndingManager>();

        RaceWinnerDetector detector = host.GetComponent<RaceWinnerDetector>();
        if (detector == null) detector = host.AddComponent<RaceWinnerDetector>();

        var so = new SerializedObject(manager);
        so.FindProperty("credits").objectReferenceValue = credits;
        so.FindProperty("titleFont").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Carnevalee Freakshow SDF.asset");
        so.FindProperty("bodyFont").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        SerializedProperty sprites = so.FindProperty("winnerSprites");
        sprites.arraySize = 4;

        for (int i = 0; i < 4; i++)
            sprites.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/UI/Player/player{i + 1}-ui.png");

        so.ApplyModifiedPropertiesWithoutUndo();

        var detectorSo = new SerializedObject(detector);
        detectorSo.FindProperty("raceStartManager").objectReferenceValue = Object.FindAnyObjectByType<RaceStartManager>(FindObjectsInactive.Include);
        detectorSo.FindProperty("endingManager").objectReferenceValue = manager;
        detectorSo.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(host.scene);
        EditorSceneManager.SaveScene(host.scene);
        AssetDatabase.SaveAssets();

        Debug.Log($"[FINAL] Configurado: {host.name} (GameEndingManager + RaceWinnerDetector), créditos en {CreditsPath}, videos en Assets/StreamingAssets/Endings/.");
    }
}
