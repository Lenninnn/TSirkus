#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TSirkus.Tickets.Editor
{
    // v1.1: reads imported assets; never changes or reimports the PNG importer.
    // Editor setup for the supplied, already modeled OBJ. No gameplay dependency.
    public static class TSirkusTicketImport
    {
        private const string Folder = "Assets/TSirkus/Ticket3D";
        private const string PrefabPath = Folder + "/Prefabs/PF_TSirkus_Ticket.prefab";
        private const string TexturePath = Folder + "/Textures/TSirkus_Ticket_Print.png";
        private const string ModelPath = Folder + "/Models/TSirkus_Ticket.obj";
        private const float WidthMeters = 0.28f;

        [MenuItem("Tools/TSirkus/Crear ticket 3D", false, 40)]
        public static void CreateTicket()
        {
            if (!CanCreate()) return;
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded) return;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                try { prefab = PreparePrefab(); }
                catch (Exception exception) { Debug.LogException(exception); return; }
            }
            if (prefab == null) return;
            GameObject ticket = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            ticket.name = "Ticket_TSirkus";
            ticket.transform.position = new Vector3(0f, 1f, 0f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(ticket.transform);
            Undo.RegisterCreatedObjectUndo(ticket, "Crear ticket TSirkus");
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = ticket;
            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
            Debug.Log("TSirkus Ticket 3D v1.1: ticket creado en (0, 1, 0). Prefab: " + PrefabPath +
                ". Para recogerlo, anade el componente de recogida de tu juego a la raiz del prefab, donde esta el BoxCollider. " +
                "No se ha modificado ningun jugador ni otro ticket.", ticket);
        }

        [MenuItem("Tools/TSirkus/Crear ticket 3D", true)]
        private static bool CanCreate()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode &&
                !EditorApplication.isCompiling && !EditorApplication.isUpdating &&
                PrefabStageUtility.GetCurrentPrefabStage() == null;
        }

        private static GameObject PreparePrefab()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("Activa URP antes de preparar este material.");
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null) throw new InvalidOperationException("No se encontro " + ModelPath + ". Conserva la estructura de carpetas del paquete.");
            // Import settings belong to the PNG's Inspector. Reimporting here can
            // restart an active refresh; prefab creation only needs to read it.
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (texture == null) throw new InvalidOperationException(
                "TSirkus Ticket 3D v1.1: la textura aun no esta disponible. " +
                "En Project selecciona " + TexturePath +
                ", haz clic derecho > Reimport una vez y espera a que termine. " +
                "Despues ejecuta Tools > TSirkus > Crear ticket 3D. " +
                "Si Reimport vuelve a mostrar un error, revisa el primer error de Console.");

            EnsureFolder(Folder + "/Materials");
            EnsureFolder(Folder + "/Prefabs");
            string materialPath = Folder + "/Materials/M_TSirkus_Ticket_URP.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "M_TSirkus_Ticket_URP" };
                material.SetTexture("_BaseMap", texture);
                material.SetColor("_BaseColor", Color.white);
                material.SetFloat("_Surface", 0f);
                material.SetFloat("_AlphaClip", 0f);
                material.SetFloat("_Cull", (float)CullMode.Back);
                material.SetFloat("_Metallic", 0f);
                material.SetFloat("_Smoothness", 0.07f);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)RenderQueue.Geometry;
                AssetDatabase.CreateAsset(material, materialPath);
            }

            GameObject root = new GameObject("PF_TSirkus_Ticket");
            try
            {
                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                visual.name = "Visual_Ticket";
                Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) throw new InvalidOperationException("El OBJ no contiene una malla renderizable.");
                foreach (Renderer renderer in renderers)
                {
                    int count = Mathf.Max(1, renderer.sharedMaterials.Length);
                    Material[] materials = new Material[count];
                    for (int i = 0; i < count; i++) materials[i] = material;
                    renderer.sharedMaterials = materials;
                    renderer.shadowCastingMode = ShadowCastingMode.On;
                    renderer.receiveShadows = true;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                }
                Bounds bounds = BoundsOf(renderers);
                if (bounds.size.x < 0.00001f) throw new InvalidOperationException("El modelo tiene dimensiones invalidas.");
                // Normalize after Unity's OBJ import, so units stay correct on either platform.
                visual.transform.localScale *= WidthMeters / bounds.size.x;
                bounds = BoundsOf(renderers);
                visual.transform.position -= bounds.center;
                PrefabUtility.RecordPrefabInstancePropertyModifications(visual.transform);
                bounds = BoundsOf(renderers);
                BoxCollider collider = root.AddComponent<BoxCollider>();
                collider.center = root.transform.InverseTransformPoint(bounds.center);
                collider.size = new Vector3(bounds.size.x, bounds.size.y, Mathf.Max(0.02f, bounds.size.z));
                collider.isTrigger = false;
                // The collider is intentionally thicker than the card for first-person raycasts.
                // No Rigidbody: it stays where placed until gameplay code says otherwise.
                bool success;
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out success);
                if (!success || saved == null) throw new InvalidOperationException("No se pudo guardar el prefab del ticket.");
                AssetDatabase.SaveAssets();
                return saved;
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static Bounds BoundsOf(Renderer[] renderers)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
