using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// Configura el payaso en GAME (idempotente, se puede repetir si cambia el nivel):
// 1. Conecta PuzzleDoor → ClownSpawner (sin eso la primera aparición nunca ocurría).
// 2. Ajusta el ritmo de ClownSpawner (valores documentados abajo).
// 3. Hornea el NavMesh del circo desde los colliders sólidos del nivel (sin triggers ni la
//    puerta de la primera prueba, que se abre antes de que exista el payaso) y lo guarda en
//    Assets/Scenes/GAME/NavMesh_Circo.asset, cargado por un NavMeshSurface "NavMesh_Circo".
public static class ClownSetupTools
{
    private const string NavMeshAssetPath = "Assets/Scenes/GAME/NavMesh_Circo.asset";

    [MenuItem("TSirkus/Payaso/Configurar payaso en GAME (NavMesh + referencias)")]
    private static void Setup()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[PAYASO] Sal de Play Mode primero.");
            return;
        }

        var scene = EditorSceneManager.GetActiveScene();
        var log = new StringBuilder("[PAYASO] Configuración\n");

        ClownSpawner spawner = Object.FindAnyObjectByType<ClownSpawner>(FindObjectsInactive.Include);

        if (spawner == null)
        {
            Debug.LogError("[PAYASO] No hay ClownSpawner en la escena abierta.");
            return;
        }

        // 1. Puerta → spawner.
        foreach (PuzzleDoor door in Object.FindObjectsByType<PuzzleDoor>(FindObjectsInactive.Include))
        {
            var so = new SerializedObject(door);
            SerializedProperty property = so.FindProperty("clownSpawner");

            if (property.objectReferenceValue == null)
            {
                property.objectReferenceValue = spawner;
                so.ApplyModifiedPropertiesWithoutUndo();
                log.AppendLine($"  {door.name}.clownSpawner → {spawner.name}");
            }
        }

        // 2. Ritmo:
        //    aparece a 7-30 m (cerca 7-11 / media 11-18 / lejos 18-30),
        //    ronda 25-55 s si nadie lo ve, pausa de 15-40 s antes de volver.
        var spawnerSo = new SerializedObject(spawner);
        SetFloat(spawnerSo, "maxSpawnDistanceFromPlayers", 30f, log);
        SetFloat(spawnerSo, "minimumTimeBetweenAppearances", 15f, log);
        SetFloat(spawnerSo, "maximumTimeBetweenAppearances", 40f, log);
        SetFloat(spawnerSo, "minimumVisibleDuration", 25f, log);
        SetFloat(spawnerSo, "maximumVisibleDuration", 55f, log);
        spawnerSo.ApplyModifiedPropertiesWithoutUndo();

        // 3. NavMesh.
        NavMeshData data = BakeNavMesh(log);

        if (data != null)
        {
            NavMeshSurface surface = Object.FindObjectsByType<NavMeshSurface>(FindObjectsInactive.Include)
                .FirstOrDefault(s => s.name == "NavMesh_Circo");

            if (surface == null)
            {
                var go = new GameObject("NavMesh_Circo");
                surface = go.AddComponent<NavMeshSurface>();
                surface.collectObjects = CollectObjects.All;
                surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                log.AppendLine("  Creado NavMesh_Circo (NavMeshSurface).");
            }

            surface.RemoveData();
            surface.navMeshData = data;
            surface.AddData();
            EditorUtility.SetDirty(surface);

            Verify(spawner, log);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        log.AppendLine("  Escena guardada.");
        Debug.Log(log.ToString());
    }


    private static void SetFloat(SerializedObject so, string name, float value, StringBuilder log)
    {
        SerializedProperty property = so.FindProperty(name);

        if (!Mathf.Approximately(property.floatValue, value))
        {
            log.AppendLine($"  ClownSpawner.{name}: {property.floatValue} → {value}");
            property.floatValue = value;
        }
    }


    private static NavMeshData BakeNavMesh(StringBuilder log)
    {
        var sources = new List<NavMeshBuildSource>();

        NavMeshBuilder.CollectSources(
            (Transform)null, ~0, NavMeshCollectGeometry.PhysicsColliders, 0, false,
            new List<NavMeshBuildMarkup>(), false, sources);

        // Colliders de la puerta de la primera prueba (el payaso solo existe con ella abierta).
        var doorVisuals = new HashSet<GameObject>();

        foreach (PuzzleDoor door in Object.FindObjectsByType<PuzzleDoor>(FindObjectsInactive.Include))
        {
            var visual = new SerializedObject(door).FindProperty("doorVisual").objectReferenceValue as GameObject;

            if (visual != null)
                doorVisuals.Add(visual);
        }

        int before = sources.Count;
        int triggers = 0, doors = 0;

        sources.RemoveAll(source =>
        {
            if (source.component is Collider collider)
            {
                if (collider.isTrigger)
                {
                    triggers++;
                    return true;
                }

                if (doorVisuals.Any(d => collider.transform.IsChildOf(d.transform)))
                {
                    doors++;
                    return true;
                }
            }

            return false;
        });

        if (sources.Count == 0)
        {
            Debug.LogError("[PAYASO] No hay geometría para el NavMesh.");
            return null;
        }

        Bounds bounds = new Bounds();
        bool first = true;

        foreach (NavMeshBuildSource source in sources)
        {
            if (source.component is Collider collider)
            {
                if (first) { bounds = collider.bounds; first = false; }
                else bounds.Encapsulate(collider.bounds);
            }
        }

        bounds.Expand(2f);

        NavMeshBuildSettings settings = NavMesh.GetSettingsByID(0);
        NavMeshData data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);

        if (data == null)
        {
            Debug.LogError("[PAYASO] Falló el horneado del NavMesh.");
            return null;
        }

        data.name = "NavMesh_Circo";

        if (!AssetDatabase.IsValidFolder("Assets/Scenes/GAME"))
            AssetDatabase.CreateFolder("Assets/Scenes", "GAME");

        AssetDatabase.DeleteAsset(NavMeshAssetPath);
        AssetDatabase.CreateAsset(data, NavMeshAssetPath);
        AssetDatabase.SaveAssets();

        log.AppendLine($"  NavMesh: {sources.Count} colliders (de {before}; fuera {triggers} triggers y {doors} de la puerta), " +
                       $"agente {NavMesh.GetSettingsNameFromID(0)} radio {settings.agentRadius} alto {settings.agentHeight}, bounds {bounds.size}.");

        return data;
    }


    private static void Verify(ClownSpawner spawner, StringBuilder log)
    {
        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
        float area = 0f;

        for (int i = 0; i < triangulation.indices.Length; i += 3)
        {
            Vector3 a = triangulation.vertices[triangulation.indices[i]];
            Vector3 b = triangulation.vertices[triangulation.indices[i + 1]];
            Vector3 c = triangulation.vertices[triangulation.indices[i + 2]];
            area += Vector3.Cross(b - a, c - a).magnitude * 0.5f;
        }

        log.AppendLine($"  Triangulación: {triangulation.vertices.Length} vértices, {area:F0} m² caminables.");

        Transform[] playerSpawns = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude)
            .Where(t => t.parent != null && t.parent.name == "SpawnPoints").ToArray();

        Vector3 start = playerSpawns.Length > 0 ? playerSpawns[0].position : Vector3.zero;
        NavMesh.SamplePosition(start, out NavMeshHit startHit, 3f, NavMesh.AllAreas);
        var path = new NavMeshPath();

        foreach (ClownSpawnPoint point in Object.FindObjectsByType<ClownSpawnPoint>(FindObjectsInactive.Include))
        {
            bool onMesh = NavMesh.SamplePosition(point.SpawnPosition, out NavMeshHit hit, 2f, NavMesh.AllAreas);
            string reach = "-";

            if (onMesh && startHit.hit)
            {
                NavMesh.CalculatePath(hit.position, startHit.position, NavMesh.AllAreas, path);
                reach = path.status.ToString();
            }

            log.AppendLine($"  {point.name} {point.SpawnPosition}: NavMesh={(onMesh ? $"sí (Δ{Vector3.Distance(hit.position, point.SpawnPosition):F2} m)" : "NO")} camino hasta inicio de jugadores={reach}");
        }
    }
}
