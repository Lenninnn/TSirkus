#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TSirkus.Prototype.Editor
{
    // Run in Edit Mode. All geometry/materials/prefabs are saved as real assets.
    // Existing players, cameras, scene settings and assets are not modified.
    public static class TSirkusLevelBuilder
    {
        internal const string RootName = "TSirkus_Level";
        private const float Radius = 12f;
        private const int Sides = 24;
        private const float WallHeight = 4.2f;
        private const float WallThickness = 0.25f;
        private const float DoorWidth = 3f;
        private const float DoorHeight = 3f;
        private static float LobbyNorth { get { return Radius * Mathf.Cos(Mathf.PI / Sides); } }

        private sealed class RoomSpec
        {
            public readonly string name;
            public readonly int index;
            public readonly Vector3 center;
            public readonly float width, depth;
            public readonly string entry;
            public RoomSpec(string name, int index, Vector3 center, float width, float depth, string entry)
            { this.name = name; this.index = index; this.center = center; this.width = width; this.depth = depth; this.entry = entry; }
        }

        private static readonly RoomSpec[] Rooms =
        {
            new RoomSpec("GameRoom_01", 1, new Vector3(-12f, 0f, 23f), 12f, 14f, "East"),
            new RoomSpec("GameRoom_02", 2, new Vector3( 12f, 0f, 23f), 12f, 14f, "West"),
            new RoomSpec("GameRoom_03", 3, new Vector3(-12f, 0f, 41f), 12f, 14f, "East"),
            new RoomSpec("FutureRoom_04", 4, new Vector3(12f, 0f, 41f), 12f, 14f, "West"),
            new RoomSpec("FutureRoom_05", 5, new Vector3( 0f, 0f, 59f), 14f, 12f, "South")
        };

        private sealed class BuildContext
        {
            public string folder;
            public GameObject root;
            public Transform lobby, corridors, lighting, props, spawns, zones;
            public Material wood, red, beige, dark, wall, checker, teal, purple, metal, glow, rubber;
            public GameObject chair, table, crate, frame;
            public Mesh cone;
            public int assetIndex;
        }

        [MenuItem("Tools/TSirkus/01 Crear lobby base", false, 10)]
        public static void CreateLobby() { Build(false); }

        [MenuItem("Tools/TSirkus/02 Crear mapa completo", false, 11)]
        public static void CreateFullLevel() { Build(true); }

        [MenuItem("Tools/TSirkus/01 Crear lobby base", true)]
        [MenuItem("Tools/TSirkus/02 Crear mapa completo", true)]
        private static bool CanBuild()
        { return !EditorApplication.isPlayingOrWillChangePlaymode && PrefabStageUtility.GetCurrentPrefabStage() == null; }

        private static void Build(bool complete)
        {
            if (!CanBuild()) return;
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded) return;
            foreach (GameObject existing in scene.GetRootGameObjects())
            {
                if (existing.name != RootName) continue;
                Selection.activeGameObject = existing;
                Debug.LogWarning("TSirkus: esta escena ya contiene TSirkus_Level. Para generar otra version, usa otra escena o elimina solo ese contenedor. No se reemplaza tu trabajo.", existing);
                return;
            }
            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset))
            {
                Debug.LogError("TSirkus necesita URP activo en Graphics/Quality. No se han creado objetos.");
                return;
            }
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) { Debug.LogError("No se encontro el shader URP/Lit."); return; }

            BuildContext c = new BuildContext();
            EnsureFolder("Assets/TSirkus/Generated");
            c.folder = AssetDatabase.GenerateUniqueAssetPath("Assets/TSirkus/Generated/Blockout_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            EnsureFolder(c.folder);
            EnsureFolder(c.folder + "/Materials");
            EnsureFolder(c.folder + "/Meshes");
            EnsureFolder(c.folder + "/Prefabs");
            try
            {
                c.root = new GameObject(RootName);
                SceneManager.MoveGameObjectToScene(c.root, scene);
                TSirkusMarker rootMarker = c.root.AddComponent<TSirkusMarker>();
                rootMarker.kind = TSirkusMarkerKind.Level;
                rootMarker.markerId = complete ? "TS_LEVEL_FULL" : "TS_LEVEL_LOBBY";
                rootMarker.notes = "Escala: 1 unidad = 1 m. Y=0 es el suelo. +Z conduce a las salas. Assets: " + c.folder;
                c.lobby = Group("Lobby_Circus", c.root.transform);
                c.corridors = Group("Corridors", c.root.transform);
                c.lighting = Group("Lighting", c.root.transform);
                c.props = Group("Props", c.root.transform);
                c.spawns = Group("SpawnPoints", c.root.transform);
                c.zones = Group("PuzzleZones", c.root.transform);

                MakeMaterials(c, shader);
                MakePrefabs(c);
                BuildLobby(c, complete);
                BuildSpawns(c);
                if (complete)
                {
                    BuildCorridors(c);
                    foreach (RoomSpec room in Rooms) BuildRoom(c, room);
                }
                BuildRoutes(c, complete);
                AssetDatabase.SaveAssets();
                bool saved;
                PrefabUtility.SaveAsPrefabAsset(c.root, c.folder + "/Prefabs/TSirkus_Level.prefab", out saved);
                if (!saved) throw new InvalidOperationException("No se pudo guardar el prefab del nivel.");
                // Register once, after finishing the hierarchy. Undo removes the scene root;
                // generated assets remain available and are never automatically deleted.
                Undo.RegisterCreatedObjectUndo(c.root, "Crear escenario TSirkus");
                EditorSceneManager.MarkSceneDirty(scene);
                Selection.activeGameObject = c.root;
                if (SceneView.lastActiveSceneView != null)
                    SceneView.lastActiveSceneView.LookAt(new Vector3(0f, 0f, complete ? 26f : 0f), Quaternion.Euler(65f, 0f, 0f), complete ? 48f : 20f);
                TSirkusLevelValidator.ValidateRoot(c.root);
                Debug.Log("TSirkus creado. Guarda tu escena (Cmd/Ctrl+S). Prefabs, materiales y meshes: " + c.folder + ". Integra tus jugadores usando SpawnPoints.", c.root);
            }
            catch (Exception e)
            {
                // Only remove this incomplete generation, never objects that predated it.
                if (c.root != null) Object.DestroyImmediate(c.root);
                Debug.LogException(e);
                Debug.LogError("Generacion interrumpida. Los assets parciales se conservan en " + c.folder + " para inspeccion.");
            }
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

        private static Transform Group(string name, Transform parent, Vector3 position = default(Vector3))
        {
            Transform t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = position;
            return t;
        }

        private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Material material, bool solid = true)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!solid) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static GameObject Sphere(string name, Transform parent, Vector3 position, float diameter, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = Vector3.one * diameter;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private static void Beam(string name, Transform parent, Vector3 a, Vector3 b, float thickness, Material material, bool solid = false)
        {
            GameObject beam = Box(name, parent, (a + b) * 0.5f, new Vector3(thickness, thickness, (b - a).magnitude), material, solid);
            beam.transform.localRotation = Quaternion.LookRotation(b - a, Vector3.up);
        }

        private static Material MakeMaterial(BuildContext c, Shader shader, string name, Color color, float smoothness = 0.05f)
        {
            Material material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(material, c.folder + "/Materials/" + name + ".mat");
            return material;
        }

        private static void MakeMaterials(BuildContext c, Shader shader)
        {
            c.wood = MakeMaterial(c, shader, "M_Wood", new Color(0.57f, 0.40f, 0.25f));
            c.red = MakeMaterial(c, shader, "M_Circus_Red", new Color(0.40f, 0.065f, 0.045f));
            c.beige = MakeMaterial(c, shader, "M_Circus_Beige", new Color(0.66f, 0.53f, 0.34f));
            c.dark = MakeMaterial(c, shader, "M_Dark", new Color(0.085f, 0.07f, 0.06f));
            c.wall = MakeMaterial(c, shader, "M_Wall_Aged", new Color(0.40f, 0.41f, 0.34f));
            c.checker = MakeMaterial(c, shader, "M_Checker", Color.white);
            c.teal = MakeMaterial(c, shader, "M_Room_Teal", new Color(0.10f, 0.28f, 0.25f));
            c.purple = MakeMaterial(c, shader, "M_Room_Purple", new Color(0.24f, 0.12f, 0.29f));
            c.metal = MakeMaterial(c, shader, "M_Metal", new Color(0.18f, 0.16f, 0.12f), 0.20f);
            c.rubber = MakeMaterial(c, shader, "M_Future_Floor", new Color(0.25f, 0.28f, 0.29f));
            c.glow = MakeMaterial(c, shader, "M_Bulb", new Color(1f, 0.63f, 0.25f));
            c.glow.EnableKeyword("_EMISSION");
            c.glow.SetColor("_EmissionColor", new Color(1f, 0.45f, 0.12f) * 2f);
            c.red.SetFloat("_Cull", (float)CullMode.Off);
            c.beige.SetFloat("_Cull", (float)CullMode.Off);

            Texture2D wood = new Texture2D(128, 128, TextureFormat.RGBA32, true);
            wood.name = "T_Wood_Procedural";
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
            {
                bool joint = x % 32 < 2 || (y + ((x / 32) % 2) * 64) % 128 < 2;
                float grain = 0.80f + 0.18f * Mathf.PerlinNoise(x * 0.6f, y * 0.035f);
                wood.SetPixel(x, y, joint ? new Color(0.24f, 0.20f, 0.17f) : new Color(grain, grain * 0.95f, grain * 0.87f));
            }
            wood.wrapMode = TextureWrapMode.Repeat;
            wood.Apply();
            AssetDatabase.CreateAsset(wood, c.folder + "/Materials/T_Wood.asset");
            c.wood.SetTexture("_BaseMap", wood);

            Texture2D check = new Texture2D(64, 64, TextureFormat.RGBA32, true);
            check.name = "T_Checker_Procedural";
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                float tone = ((x / 32 + y / 32) % 2 == 0) ? 0.63f : 0.10f;
                if (x % 32 == 0 || y % 32 == 0) tone *= 0.65f;
                check.SetPixel(x, y, new Color(tone, tone * 0.99f, tone * 0.90f));
            }
            check.wrapMode = TextureWrapMode.Repeat;
            check.Apply();
            AssetDatabase.CreateAsset(check, c.folder + "/Materials/T_Checker.asset");
            c.checker.SetTexture("_BaseMap", check);
            c.checker.SetTextureScale("_BaseMap", new Vector2(6f, 7f));
            foreach (Material material in new[] { c.wood, c.red, c.beige, c.dark, c.wall, c.checker, c.teal, c.purple, c.metal, c.rubber, c.glow })
                EditorUtility.SetDirty(material);
        }

        private static GameObject SaveProp(BuildContext c, GameObject source, string filename)
        {
            bool success;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, c.folder + "/Prefabs/" + filename + ".prefab", out success);
            Object.DestroyImmediate(source);
            if (!success || prefab == null) throw new InvalidOperationException("No se guardo " + filename);
            return prefab;
        }

        private static void MakePrefabs(BuildContext c)
        {
            Transform chair = Group("PF_Chair", c.root.transform);
            Box("Seat", chair, new Vector3(0f, 0.46f, 0f), new Vector3(0.56f, 0.08f, 0.58f), c.wood, false);
            Box("Back", chair, new Vector3(0f, 0.79f, 0.24f), new Vector3(0.56f, 0.62f, 0.07f), c.wood, false);
            for (int x = -1; x <= 1; x += 2) for (int z = -1; z <= 1; z += 2)
                Box("Leg_" + x + "_" + z, chair, new Vector3(x * 0.21f, 0.23f, z * 0.22f), new Vector3(0.045f, 0.46f, 0.045f), c.metal, false);
            BoxCollider chairCollider = chair.gameObject.AddComponent<BoxCollider>();
            chairCollider.center = new Vector3(0f, 0.55f, 0f);
            chairCollider.size = new Vector3(0.58f, 1.10f, 0.62f);
            c.chair = SaveProp(c, chair.gameObject, "PF_Chair");

            Transform table = Group("PF_LongTable", c.root.transform);
            Box("Top", table, new Vector3(0f, 0.86f, 0f), new Vector3(1.3f, 0.10f, 4.4f), c.beige, false);
            for (int x = -1; x <= 1; x += 2) for (int z = -1; z <= 1; z += 2)
                Box("Leg_" + x + "_" + z, table, new Vector3(x * 0.49f, 0.41f, z * 1.87f), new Vector3(0.08f, 0.82f, 0.08f), c.metal, false);
            BoxCollider tableCollider = table.gameObject.AddComponent<BoxCollider>();
            tableCollider.center = new Vector3(0f, 0.455f, 0f);
            tableCollider.size = new Vector3(1.3f, 0.91f, 4.4f);
            c.table = SaveProp(c, table.gameObject, "PF_LongTable");

            Transform crate = Group("PF_Crate", c.root.transform);
            Box("Body", crate, new Vector3(0f, 0.5f, 0f), Vector3.one, c.wood);
            Box("Band_A", crate, new Vector3(0f, 0.24f, -0.505f), new Vector3(1.02f, 0.07f, 0.04f), c.metal, false);
            Box("Band_B", crate, new Vector3(0f, 0.77f, -0.505f), new Vector3(1.02f, 0.07f, 0.04f), c.metal, false);
            c.crate = SaveProp(c, crate.gameObject, "PF_Crate");

            Transform frame = Group("PF_AccessFrame_3m", c.root.transform);
            // Posts sit outside the opening: clear width stays exactly 3 m.
            for (int side = -1; side <= 1; side += 2)
                Box("Jamb_" + side, frame, new Vector3(side * 1.57f, 1.5f, 0f), new Vector3(0.14f, 3f, 0.32f), c.red);
            Box("Lintel", frame, new Vector3(0f, 3.10f, 0f), new Vector3(3.28f, 0.20f, 0.32f), c.red);
            c.frame = SaveProp(c, frame.gameObject, "PF_AccessFrame_3m");
            c.cone = MakeCone(c);
        }

        private static Transform Instance(GameObject prefab, Transform parent, string name, Vector3 position, float yaw = 0f)
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return go.transform;
        }

        private struct Gap
        {
            public float center, width;
            public Gap(float center, float width) { this.center = center; this.width = width; }
        }

        private static void WallSpan(Transform parent, string name, Vector3 a, Vector3 b, float height, Material material, params Gap[] gaps)
        {
            Vector3 direction = (b - a).normalized;
            float length = Vector3.Distance(a, b);
            List<Gap> sorted = new List<Gap>(gaps);
            sorted.Sort((x, y) => x.center.CompareTo(y.center));
            float cursor = 0f;
            int index = 0;
            foreach (Gap gap in sorted)
            {
                float start = Mathf.Clamp(gap.center - gap.width * 0.5f, 0f, length);
                float end = Mathf.Clamp(gap.center + gap.width * 0.5f, 0f, length);
                WallPiece(parent, name + "_Solid_" + index, a, direction, cursor, start, 0f, height, material);
                WallPiece(parent, name + "_Header_" + index, a, direction, start, end, DoorHeight, height, material);
                cursor = end;
                index++;
            }
            WallPiece(parent, name + "_Solid_" + index, a, direction, cursor, length, 0f, height, material);
        }

        private static void WallPiece(Transform parent, string name, Vector3 origin, Vector3 direction, float start, float end, float bottom, float top, Material material)
        {
            if (end - start < 0.005f || top <= bottom) return;
            GameObject wall = Box(name, parent, origin + direction * ((start + end) * 0.5f) + Vector3.up * ((bottom + top) * 0.5f),
                new Vector3(end - start, top - bottom, WallThickness), material);
            wall.transform.localRotation = Quaternion.LookRotation(Vector3.Cross(direction, Vector3.up), Vector3.up);
        }

        private static Mesh SaveMesh(BuildContext c, string name, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs)
        {
            Mesh mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, c.folder + "/Meshes/" + (++c.assetIndex).ToString("D3") + "_" + name + ".asset");
            return mesh;
        }

        private static void MeshObject(string name, Transform parent, Mesh mesh, Material material, bool solid)
        {
            GameObject go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (solid) go.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        private static Vector3 Polar(float angle, float radius, float y = 0f)
        { return new Vector3(Mathf.Sin(angle) * radius, y, Mathf.Cos(angle) * radius); }

        private static void BuildLobbyFloor(BuildContext c, Transform parent)
        {
            List<Vector3> v = new List<Vector3> { Vector3.zero, new Vector3(0f, -0.25f, 0f) };
            List<Vector2> uv = new List<Vector2> { Vector2.zero, Vector2.zero };
            List<int> triangles = new List<int>();
            for (int i = 0; i < Sides; i++)
            {
                Vector3 p = Polar((i - 0.5f) * Mathf.PI * 2f / Sides, Radius);
                v.Add(p); v.Add(p + Vector3.down * 0.25f);
                uv.Add(new Vector2(p.x, p.z) / 3f); uv.Add(new Vector2(p.x, p.z) / 3f);
            }
            for (int i = 0; i < Sides; i++)
            {
                int a = 2 + i * 2, b = 2 + ((i + 1) % Sides) * 2;
                triangles.AddRange(new[] { 0, a, b, 1, b + 1, a + 1, a, a + 1, b, b, a + 1, b + 1 });
            }
            MeshObject("Floor_Wood_24m", parent, SaveMesh(c, "LobbyFloor", v, triangles, uv), c.wood, true);
            // A decorative inlay 4 mm above the floor, with no collision seam.
            for (int i = 0; i < Sides; i++)
            {
                float a = (i - 0.5f) * Mathf.PI * 2f / Sides;
                float b = (i + 0.5f) * Mathf.PI * 2f / Sides;
                List<Vector3> diskV = new List<Vector3> { new Vector3(0f, 0.004f, 0f), Polar(a, 5.5f, 0.004f), Polar(b, 5.5f, 0.004f) };
                MeshObject("Ring_Inlay_" + i.ToString("D2"), parent, SaveMesh(c, "RingSector", diskV, new List<int> { 0, 1, 2 }, new List<Vector2> { Vector2.zero, Vector2.right, Vector2.up }), i % 2 == 0 ? c.red : c.beige, false);
            }
        }

        private static void BuildTentRoof(BuildContext c, Transform parent)
        {
            const int radial = 7;
            const int angular = 4;
            for (int stripe = 0; stripe < Sides; stripe++)
            {
                List<Vector3> v = new List<Vector3>();
                List<Vector2> uv = new List<Vector2>();
                List<int> triangles = new List<int>();
                float a0 = (stripe - 0.5f) * Mathf.PI * 2f / Sides;
                float a1 = (stripe + 0.5f) * Mathf.PI * 2f / Sides;
                for (int r = 0; r <= radial; r++) for (int a = 0; a <= angular; a++)
                {
                    float t = r / (float)radial, u = a / (float)angular;
                    float angle = Mathf.Lerp(a0, a1, u);
                    // Follow the polygon edge exactly at the rim, avoiding eave gaps.
                    Vector3 edge = Vector3.Lerp(Polar(a0, Radius), Polar(a1, Radius), u);
                    float y = Mathf.Lerp(8.5f, WallHeight, t) - 0.38f * Mathf.Sin(t * Mathf.PI) - 0.10f * Mathf.Sin(u * Mathf.PI) * Mathf.Sin(t * Mathf.PI);
                    Vector3 point = Vector3.Lerp(Polar(angle, 0.12f), edge, t);
                    point.y = y;
                    v.Add(point); uv.Add(new Vector2(u, t));
                }
                for (int r = 0; r < radial; r++) for (int a = 0; a < angular; a++)
                {
                    int p = r * (angular + 1) + a;
                    int q = p + angular + 1;
                    // Inward/downward winding; URP material renders both sides.
                    triangles.AddRange(new[] { p, p + 1, q, p + 1, q + 1, q });
                }
                MeshObject("Tent_Stripe_" + stripe.ToString("D2"), parent, SaveMesh(c, "TentStripe", v, triangles, uv), stripe % 2 == 0 ? c.red : c.beige, true);
            }
            Sphere("Tent_Apex_Cap", parent, new Vector3(0f, 8.48f, 0f), 0.38f, c.metal);
        }

        private static Mesh MakeCone(BuildContext c)
        {
            const int sides = 10;
            List<Vector3> v = new List<Vector3> { new Vector3(0f, 0.33f, 0f), Vector3.zero };
            List<Vector2> uv = new List<Vector2> { Vector2.up, Vector2.zero };
            List<int> triangles = new List<int>();
            for (int i = 0; i < sides; i++)
            {
                v.Add(Polar(i * Mathf.PI * 2f / sides, 0.12f));
                uv.Add(new Vector2(i / (float)sides, 0f));
            }
            for (int i = 0; i < sides; i++)
            {
                int a = 2 + i, b = 2 + ((i + 1) % sides);
                triangles.AddRange(new[] { 0, a, b, 1, b, a });
            }
            return SaveMesh(c, "PartyHat", v, triangles, uv);
        }

        private static void Label(string name, Transform parent, string text, Vector3 position, float yaw, float characterSize = 0.12f)
        {
            Transform t = Group(name, parent, position);
            t.localRotation = Quaternion.Euler(0f, yaw, 0f);
            TextMesh label = t.gameObject.AddComponent<TextMesh>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text;
            label.fontSize = 64;
            label.characterSize = characterSize;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = new Color(0.94f, 0.80f, 0.54f);
            MeshRenderer renderer = t.GetComponent<MeshRenderer>();
            if (label.font != null) renderer.sharedMaterial = label.font.material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static Transform Marker(Transform parent, string name, TSirkusMarkerKind kind, string id, Vector3 feet, Vector3 size, string notes, bool trigger = false)
        {
            Transform t = Group(name, parent, feet);
            TSirkusMarker marker = t.gameObject.AddComponent<TSirkusMarker>();
            marker.kind = kind; marker.markerId = id; marker.size = size; marker.notes = notes;
            marker.color = kind == TSirkusMarkerKind.Puzzle ? new Color(0.2f, 0.85f, 0.65f) : new Color(0.95f, 0.66f, 0.25f);
            if (trigger)
            {
                BoxCollider collider = t.gameObject.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                collider.center = Vector3.up * (size.y * 0.5f);
                collider.size = size;
            }
            return t;
        }

        private static void Access(BuildContext c, Transform parent, string name, Vector3 position, float yaw, string caption)
        {
            Transform frame = Instance(c.frame, parent, name, position, yaw);
            Label("Sign", frame, caption, new Vector3(0f, 3.48f, -0.23f), 0f, 0.10f);
            Marker(frame, "AccessMarker", TSirkusMarkerKind.Access, "TS_ACCESS_" + name,
                Vector3.zero, new Vector3(3f, 3f, 0.7f), "Paso abierto. Sin hoja de puerta. Preparado para un futuro controlador.");
        }

        private static void LightSource(BuildContext c, Transform parent, string name, Vector3 position, Color color, float intensity, float range, bool spot = false, bool shadows = false)
        {
            Transform t = Group(name, parent, position);
            Light light = t.gameObject.AddComponent<Light>();
            light.type = spot ? LightType.Spot : LightType.Point;
            light.color = color; light.intensity = intensity; light.range = range;
            light.shadows = shadows ? LightShadows.Hard : LightShadows.None;
            light.shadowBias = 0.025f; light.shadowNormalBias = 0.15f;
            if (spot) { light.spotAngle = 110f; t.localRotation = Quaternion.Euler(90f, 0f, 0f); }
            if (t.GetComponent<UniversalAdditionalLightData>() == null)
                t.gameObject.AddComponent<UniversalAdditionalLightData>();
            Sphere("Bulb", t, Vector3.zero, 0.16f, c.glow);
        }

        private static void Curtains(BuildContext c, Transform parent, Vector3 position, float width, float height, float yaw)
        {
            Transform curtains = Group("Curtains", parent, position);
            curtains.localRotation = Quaternion.Euler(0f, yaw, 0f);
            for (int i = 0; i < 24; i++)
                Box("Pleat_" + i.ToString("D2"), curtains,
                    new Vector3(-width * 0.5f + (i + 0.5f) * width / 24f, height * 0.5f, i % 2 == 0 ? 0f : 0.07f),
                    new Vector3(width / 24f + 0.015f, height, 0.11f), i % 2 == 0 ? c.red : c.dark, false);
        }

        private static void BuildLobby(BuildContext c, bool complete)
        {
            Transform architecture = Group("Architecture", c.lobby);
            Transform walls = Group("Walls", architecture);
            Transform roof = Group("Roof_Tent", architecture);
            BuildLobbyFloor(c, architecture);
            BuildTentRoof(c, roof);
            for (int i = 0; i < Sides; i++)
            {
                Vector3 a = Polar((i - 0.5f) * Mathf.PI * 2f / Sides, Radius);
                Vector3 b = Polar((i + 0.5f) * Mathf.PI * 2f / Sides, Radius);
                if (i == 0)
                    WallSpan(walls, "North_Exit", a, b, WallHeight, c.dark, new Gap(Vector3.Distance(a, b) * 0.5f, DoorWidth));
                else
                    WallSpan(walls, "Panel_" + i.ToString("D2"), a, b, WallHeight, i % 3 == 0 ? c.red : c.dark);
                Beam("Eave_" + i.ToString("D2"), walls, a + Vector3.up * 4.13f, b + Vector3.up * 4.13f, 0.14f, c.wood);
                Sphere("StringBulb_" + i.ToString("D2"), roof, Polar(i * Mathf.PI * 2f / Sides, 10.8f, 3.8f), 0.10f, c.glow);
            }
            Access(c, c.lobby, "Lobby_Exit", new Vector3(0f, 0f, LobbyNorth), 180f, "PRUEBAS 01 - 05");
            Transform props = Group("Props_Local", c.lobby);
            Box("Stage", props, new Vector3(-4f, 0.18f, 7f), new Vector3(6f, 0.36f, 2.4f), c.wood);
            Box("Stage_Step", props, new Vector3(-4f, 0.09f, 5.5f), new Vector3(5f, 0.18f, 0.65f), c.wood);
            Curtains(c, props, new Vector3(-4f, 0.36f, 8.15f), 6.4f, 3.3f, 0f);
            Label("Title_TSIRKUS", props, "T S I R K U S", new Vector3(-4f, 3.87f, 7.95f), 0f, 0.22f);
            Label("Briefing_Instructions", props, "REUNANSE EN LA PISTA\nESCUCHEN LAS INSTRUCCIONES", new Vector3(-4f, 2.05f, 7.90f), 0f, 0.085f);
            for (int i = 0; i < 24; i++)
            {
                float degrees = i * 15f;
                if (degrees < 45f || degrees >= 315f || Mathf.Abs(degrees - 180f) < 25f) continue;
                Instance(c.chair, props, "Audience_Chair_" + i.ToString("D2"), Polar(degrees * Mathf.Deg2Rad, 9.3f), degrees);
            }
            Marker(c.zones, "Briefing_Lobby", TSirkusMarkerKind.Briefing, "TS_BRIEFING", new Vector3(-3f, 0f, 2f), new Vector3(5f, 2.4f, 4f), "Reunir al grupo; futura instruccion inicial y desbloqueo de pruebas.", true);
            Marker(c.zones, "Event_Lobby", TSirkusMarkerKind.Event, "TS_EVENT_LOBBY", new Vector3(4f, 0f, 6f), new Vector3(2f, 2.4f, 2f), "Futuro apagon, audio o aparicion. Sin evento conectado.");
            Transform lights = Group("Lobby", c.lighting);
            LightSource(c, lights, "Ring_Key", new Vector3(0f, 6.8f, 0f), new Color(1f, 0.65f, 0.32f), 5f, 18f, true, true);
            LightSource(c, lights, "Stage_Accent", new Vector3(-4f, 3.65f, 6.1f), new Color(1f, 0.43f, 0.18f), 2f, 6f);
            LightSource(c, lights, "Entry_Fill", new Vector3(1f, 3.3f, -5f), new Color(1f, 0.73f, 0.43f), 1.4f, 10f);
            if (!complete)
            {
                // The lobby-only phase has a safe, enclosed vestibule; no open fall-off.
                Transform vestibule = Group("Lobby_TestVestibule", c.corridors);
                float start = LobbyNorth - 0.12f, end = 16f;
                Box("Floor", vestibule, new Vector3(0f, -0.125f, (start + end) * 0.5f), new Vector3(4f, 0.25f, end - start), c.wood);
                Box("Ceiling", vestibule, new Vector3(0f, 3.725f, (start + end) * 0.5f), new Vector3(4.5f, 0.25f, end - start), c.dark);
                for (int side = -1; side <= 1; side += 2)
                    WallSpan(vestibule, "Side_" + side, new Vector3(side * 2.125f, 0f, start), new Vector3(side * 2.125f, 0f, end), 3.6f, c.wall);
                WallSpan(vestibule, "Temporary_End", new Vector3(-2.25f, 0f, end), new Vector3(2.25f, 0f, end), 3.6f, c.dark);
                Label("Test_Limit", vestibule, "FIN DEL BLOQUE LOBBY", new Vector3(0f, 2.1f, 15.82f), 0f);
                LightSource(c, lights, "Vestibule_Light", new Vector3(0f, 3.1f, 14f), new Color(1f, 0.65f, 0.35f), 1.6f, 5f);
            }
        }

        private static void BuildSpawns(BuildContext c)
        {
            Vector3[] positions = { new Vector3(-1.2f, 0.05f, -7f), new Vector3(1.2f, 0.05f, -7f), new Vector3(-1.2f, 0.05f, -5f), new Vector3(1.2f, 0.05f, -5f) };
            for (int i = 0; i < positions.Length; i++)
                Marker(c.spawns, "Spawn_P" + (i + 1).ToString("D2"), TSirkusMarkerKind.Spawn, "TS_SPAWN_" + (i + 1), positions[i], new Vector3(0.7f, 1.8f, 0.7f), "Punto a nivel de pies. Orientacion +Z. Conectar al spawner existente; no instancia jugadores.");
        }

        private static void BuildCorridors(BuildContext c)
        {
            Transform main = Group("MainSpine_4m", c.corridors);
            float start = LobbyNorth - 0.12f, end = 53f;
            Box("Floor", main, new Vector3(0f, -0.125f, (start + end) * 0.5f), new Vector3(4f, 0.25f, end - start), c.wood);
            Box("Ceiling", main, new Vector3(0f, 3.725f, (start + end) * 0.5f), new Vector3(4.5f, 0.25f, end - start), c.dark);
            for (int side = -1; side <= 1; side += 2)
            {
                WallSpan(main, "Side_" + side, new Vector3(side * 2.125f, 0f, start), new Vector3(side * 2.125f, 0f, end), 3.6f, c.wall,
                    new Gap(23f - start, 3f), new Gap(41f - start, 3f));
                foreach (float z in new[] { 23f, 41f })
                {
                    Transform branch = Group("Branch_" + (side < 0 ? "West_" : "East_") + z.ToString("F0"), c.corridors);
                    Box("Floor", branch, new Vector3(side * 4f, -0.125f, z), new Vector3(4f, 0.25f, 3f), c.wood);
                    Box("Ceiling", branch, new Vector3(side * 4f, 3.725f, z), new Vector3(4f, 0.25f, 3.5f), c.dark);
                    float lowX = side < 0 ? -6f : 2f, highX = side < 0 ? -2f : 6f;
                    for (int flank = -1; flank <= 1; flank += 2)
                        WallSpan(branch, "Flank_" + flank, new Vector3(lowX, 0f, z + flank * 1.625f), new Vector3(highX, 0f, z + flank * 1.625f), 3.6f, c.wall);
                }
            }
            // Close shoulders at the junction with the round lobby. Keep the 3 m aperture open.
            WallSpan(main, "Lobby_Shoulder", new Vector3(-2.25f, 0f, LobbyNorth), new Vector3(2.25f, 0f, LobbyNorth), 3.6f, c.dark, new Gap(2.25f, 3f));
            Transform lights = Group("Corridors", c.lighting);
            int n = 0;
            foreach (float z in new[] { 15f, 23f, 32f, 41f, 49.5f })
                LightSource(c, lights, "Practical_" + (++n).ToString("D2"), new Vector3(0f, 3.15f, z), new Color(0.90f, 0.66f, 0.38f), 2.0f, 6.5f);
            Label("Wayfinding_01_02", main, "01  <     >  02", new Vector3(0f, 3.13f, 22.3f), 0f, 0.12f);
            Label("Wayfinding_03_04", main, "03  <     >  04", new Vector3(0f, 3.13f, 40.3f), 0f, 0.12f);
            Marker(c.zones, "Event_Corridor", TSirkusMarkerKind.Event, "TS_EVENT_CORRIDOR", new Vector3(0f, 0f, 32f), new Vector3(3f, 2.4f, 2f), "Punto candidato para audio, apagones o tension entre pruebas.", true);
        }

        private static void BuildRoom(BuildContext c, RoomSpec spec)
        {
            Transform room = Group(spec.name, c.root.transform, spec.center);
            room.SetSiblingIndex(spec.index + 1);
            Transform architecture = Group("Architecture", room);
            Transform walls = Group("Walls", architecture);
            Transform props = Group("Props_Local", room);
            Transform sockets = Group("Sockets", room);
            float w = spec.width, d = spec.depth, h = 4f;
            Box("Floor", architecture, new Vector3(0f, -0.125f, 0f), new Vector3(w, 0.25f, d), spec.index <= 3 ? c.checker : c.rubber);
            Box("Ceiling", architecture, new Vector3(0f, h + 0.125f, 0f), new Vector3(w + 0.5f, 0.25f, d + 0.5f), c.dark);
            string[] edges = { "South", "North", "West", "East" };
            foreach (string edge in edges)
            {
                Vector3 a, b;
                if (edge == "South" || edge == "North")
                {
                    float z = (edge == "South" ? -1f : 1f) * (d * 0.5f + 0.125f);
                    a = new Vector3(-w * 0.5f - 0.25f, 0f, z);
                    b = new Vector3(w * 0.5f + 0.25f, 0f, z);
                }
                else
                {
                    float x = (edge == "West" ? -1f : 1f) * (w * 0.5f + 0.125f);
                    a = new Vector3(x, 0f, -d * 0.5f - 0.25f);
                    b = new Vector3(x, 0f, d * 0.5f + 0.25f);
                }
                if (edge == spec.entry)
                    WallSpan(walls, edge, a, b, h, c.wall, new Gap(Vector3.Distance(a, b) * 0.5f, DoorWidth));
                else WallSpan(walls, edge, a, b, h, c.wall);
            }
            Vector3 entry;
            float yaw;
            if (spec.entry == "East") { entry = new Vector3(w * 0.5f + 0.125f, 0f, 0f); yaw = -90f; }
            else if (spec.entry == "West") { entry = new Vector3(-w * 0.5f - 0.125f, 0f, 0f); yaw = 90f; }
            else { entry = new Vector3(0f, 0f, -d * 0.5f - 0.125f); yaw = 0f; }
            string caption = spec.index <= 3 ? spec.index.ToString("D2") + "  |  SALA DE JUEGO" : spec.index.ToString("D2") + "  |  PROXIMAMENTE";
            Access(c, room, "Door_" + spec.index.ToString("D2"), entry, yaw, caption);
            Marker(sockets, "Socket_Entry", TSirkusMarkerKind.Access, "TS_SOCKET_ENTRY_" + spec.index, entry, new Vector3(0.4f, 0.4f, 0.4f), "Origen para alinear otro modulo a la entrada.").localRotation = Quaternion.Euler(0f, yaw, 0f);
            Marker(sockets, "Socket_Expansion", TSirkusMarkerKind.Expansion, "TS_EXPAND_" + spec.index,
                new Vector3(0f, 0f, d * 0.5f), new Vector3(3f, 3f, 0.4f), "Reserva de conexion. Actualmente hay una pared: abrirla antes de anexar un corredor.");

            // Zone containers follow their room when moved; the top-level PuzzleZones
            // holds lobby and corridor markers. Room IDs stay stable after renaming.
            Transform roomZones = Group("PuzzleZones", room);
            Marker(roomZones, "Puzzle_Main", TSirkusMarkerKind.Puzzle, "TS_PZ_" + spec.index.ToString("D2"), Vector3.zero,
                new Vector3(spec.index <= 3 ? 4f : 6f, 2.4f, spec.index <= 3 ? 2.4f : 6f), "Volumen reservado y transitable. Conectar futura mecanica, sin reglas ni red implementadas.", true);
            Marker(roomZones, "Ticket_Anchor", TSirkusMarkerKind.Ticket, "TS_TICKET_" + spec.index.ToString("D2"),
                new Vector3(4.6f, 0f, -5.3f), new Vector3(0.8f, 1.4f, 0.8f), "Punto de futura recompensa; todavia no entrega tickets.");
            Marker(roomZones, "Event_Anchor", TSirkusMarkerKind.Event, "TS_EVENT_" + spec.index.ToString("D2"),
                new Vector3(-4.6f, 0f, 5.1f), new Vector3(1f, 2f, 1f), "Punto para efecto de terror. No activa eventos.");
            if (spec.index <= 3) DressPartyRoom(c, props, spec.index);
            else
            {
                Label("Future_Label", props, "SALA " + spec.index.ToString("D2") + "\nESPACIO RESERVADO", new Vector3(0f, 2.3f, d * 0.5f - 0.12f), 0f, 0.14f);
                // A color band identifies the empty room without obstructing its center.
                Box("Future_Band", props, new Vector3(0f, 1.05f, d * 0.5f - 0.035f), new Vector3(w, 0.18f, 0.05f), c.teal, false);
            }
            // Parent room lighting locally, under the master Lighting container via
            // a room-level group would break room movement. Use a local Lighting group.
            Transform lights = Group("Lighting_Local", room);
            Color tone = spec.index == 3 ? new Color(0.72f, 0.86f, 0.67f) : new Color(1f, 0.69f, 0.39f);
            LightSource(c, lights, "Room_Key", new Vector3(0f, 3.70f, 0f), tone, 4.4f, 12f, true, spec.index == 3);
            if (spec.index <= 3)
                LightSource(c, lights, "Corner_Accent", new Vector3(-3.5f, 2.6f, -4.5f), new Color(1f, 0.28f, 0.12f), 0.75f, 4.5f);
            // Each complete room becomes an editable, reusable prefab instance.
            bool saved;
            PrefabUtility.SaveAsPrefabAssetAndConnect(room.gameObject, c.folder + "/Prefabs/" + spec.name + ".prefab", InteractionMode.AutomatedAction, out saved);
            if (!saved) throw new InvalidOperationException("No se guardo la sala " + spec.name);
        }

        private static void DressPartyRoom(BuildContext c, Transform props, int variant)
        {
            Material accent = variant == 1 ? c.teal : variant == 2 ? c.purple : c.red;
            Box("Wall_Band_North", props, new Vector3(0f, 1.1f, 6.96f), new Vector3(12f, 0.30f, 0.05f), accent, false);
            Box("Wall_Band_South", props, new Vector3(0f, 1.1f, -6.96f), new Vector3(12f, 0.30f, 0.05f), accent, false);
            float offset = variant == 1 ? -0.6f : variant == 2 ? 1.0f : 0f;
            PartyTable(c, props, "Table_A", new Vector3(-offset, 0f, -3.1f), variant == 1 ? 2 : 3, accent);
            PartyTable(c, props, "Table_B", new Vector3(offset, 0f, 3.1f), variant == 1 ? 2 : 3, accent);
            Label("Party_Banner", props, variant == 1 ? "BIENVENIDOS" : variant == 2 ? "TODOS DEBEN JUGAR" : "SIGUE SONRIENDO",
                new Vector3(0f, 3.20f, 6.88f), 0f, 0.14f);
            Garland(c, props, new Vector3(-5f, 3.35f, -4.8f), new Vector3(5f, 3.35f, -4.8f), accent, "Garland_A");
            Garland(c, props, new Vector3(-5f, 3.35f, 4.8f), new Vector3(5f, 3.35f, 4.8f), c.beige, "Garland_B");
            // Abstract original posters: circles, geometric smiles and a title.
            for (int i = 0; i < 3; i++)
            {
                Transform poster = Group("Poster_" + i, props, new Vector3((i - 1) * 2.7f, 2.15f, 6.88f));
                Box("Board", poster, Vector3.zero, new Vector3(1.5f, 1.6f, 0.045f), i % 2 == 0 ? accent : c.wood, false);
                Sphere("Face", poster, new Vector3(0f, 0.08f, -0.12f), 0.75f, c.beige);
                Sphere("Eye_L", poster, new Vector3(-0.16f, 0.18f, -0.46f), 0.12f, c.dark);
                Sphere("Eye_R", poster, new Vector3(0.16f, 0.18f, -0.46f), 0.12f, c.dark);
                Box("Smile", poster, new Vector3(0f, -0.15f, -0.44f), new Vector3(0.30f, 0.07f, 0.04f), c.red, false);
            }
            if (variant >= 2)
            {
                Instance(c.crate, props, "Crate_A", new Vector3(-4.8f, 0f, -5.6f), -8f);
                Instance(c.crate, props, "Crate_B", new Vector3(-4.7f, 1f, -5.5f), 8f);
                for (int i = 0; i < 3; i++)
                {
                    Vector3 basePoint = new Vector3(4.9f + i * 0.16f, 0.1f, 4.9f);
                    Vector3 topPoint = basePoint + new Vector3((i - 1) * 0.27f, 2.5f + i * 0.18f, 0f);
                    Beam("Balloon_String_" + i, props, basePoint, topPoint, 0.009f, c.beige);
                    Sphere("Balloon_" + i, props, topPoint, 0.43f, i % 2 == 0 ? accent : c.beige);
                }
            }
            if (variant == 3)
            {
                Curtains(c, props, new Vector3(-5.83f, 0f, 0f), 3.8f, 3.55f, 90f);
                Box("Empty_PuppetStage", props, new Vector3(-4.95f, 0.24f, 0f), new Vector3(1.5f, 0.48f, 3.8f), c.wood);
                Transform fallen = Instance(c.chair, props, "Fallen_Chair", new Vector3(4.4f, 0.30f, 5.6f));
                fallen.localRotation = Quaternion.Euler(0f, 24f, 90f);
                Instance(c.crate, props, "Crate_C", new Vector3(-3.65f, 0f, -5.8f), -13f);
            }
        }

        private static void PartyTable(BuildContext c, Transform parent, string name, Vector3 position, int seatsPerSide, Material accent)
        {
            Transform table = Instance(c.table, parent, name, position, 90f);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int seat = 0; seat < seatsPerSide; seat++)
                {
                    float x = seatsPerSide == 2 ? (seat == 0 ? -1.2f : 1.2f) : (seat - 1) * 1.4f;
                    Instance(c.chair, parent, name + "_Chair_" + side + "_" + seat,
                        position + new Vector3(x, 0f, side * 1.03f), side > 0 ? 0f : 180f);
                    Transform hat = Group(name + "_Hat_" + side + "_" + seat, parent, position + new Vector3(x, 0.92f, side * 0.39f));
                    MeshObject("Hat", hat, c.cone, accent, false);
                }
            }
        }

        private static void Garland(BuildContext c, Transform parent, Vector3 a, Vector3 b, Material accent, string name)
        {
            Transform garland = Group(name, parent);
            Vector3 previous = a;
            for (int i = 1; i <= 12; i++)
            {
                float t = i / 12f;
                Vector3 point = Vector3.Lerp(a, b, t) + Vector3.down * (0.22f * Mathf.Sin(t * Mathf.PI));
                Beam("String_" + i, garland, previous, point, 0.015f, c.dark);
                GameObject flag = Box("Flag_" + i, garland, point + Vector3.down * 0.18f, new Vector3(0.28f, 0.34f, 0.025f), i % 2 == 0 ? accent : c.beige, false);
                flag.transform.localRotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? 12f : -12f);
                previous = point;
            }
        }

        private static void Route(Transform parent, string name, params Vector3[] points)
        {
            Transform route = Marker(parent, name, TSirkusMarkerKind.Route, "TS_ROUTE_" + name, Vector3.zero, Vector3.one,
                "Ruta de validacion para capsula de 1.8 m y radio 0.35 m. Actualizar los puntos al mover arquitectura.");
            for (int i = 0; i < points.Length; i++) Group("Point_" + i.ToString("D2"), route, points[i]);
        }

        private static void BuildRoutes(BuildContext c, bool complete)
        {
            Transform routes = Group("ValidationRoutes", c.zones);
            for (int i = 0; i < c.spawns.childCount; i++)
            {
                Vector3 p = c.spawns.GetChild(i).localPosition; p.y = 0f;
                Route(routes, "Spawn_" + (i + 1), p, new Vector3(p.x, 0f, -1f), new Vector3(-3f, 0f, 2f));
            }
            Route(routes, "Briefing_To_Exit", new Vector3(-3f, 0f, 2f), new Vector3(0f, 0f, 3.5f), new Vector3(0f, 0f, 14f));
            if (!complete) return;
            Route(routes, "MainSpine", new Vector3(0f, 0f, 14f), new Vector3(0f, 0f, 59f));
            foreach (RoomSpec room in Rooms)
            {
                if (room.index == 5) continue;
                Route(routes, room.name, new Vector3(0f, 0f, room.center.z), room.center);
            }
        }
    }
}
#endif
