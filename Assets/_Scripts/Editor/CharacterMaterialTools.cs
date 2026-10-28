using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// Herramientas para los materiales de los personajes.
// "Analizar" es de solo lectura: escribe un informe en Console y el layout de UV
// de cada pieza en <proyecto>/Temp/Personaje1_UV (fuera de Assets).
public static class CharacterMaterialTools
{
    private const string Personaje1Fbx = "Assets/Prefabs/Players/Personaje1.fbx";


    [MenuItem("TSirkus/Personajes/Analizar Personaje1 (solo lectura)")]
    private static void AnalyzePersonaje1()
    {
        GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(Personaje1Fbx);
        string uvFolder = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "Personaje1_UV");
        Directory.CreateDirectory(uvFolder);

        StringBuilder log = new StringBuilder($"[PJ1] ===== {Personaje1Fbx}\n");

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            Mesh mesh = renderer is SkinnedMeshRenderer skinned
                ? skinned.sharedMesh
                : renderer.GetComponent<MeshFilter>()?.sharedMesh;

            log.AppendLine($"[PJ1] {renderer.GetType().Name} '{renderer.name}' mesh='{mesh?.name}' submeshes={mesh?.subMeshCount} " +
                           $"verts={mesh?.vertexCount} uv0={mesh != null && mesh.uv.Length > 0} uv1={mesh != null && mesh.uv2.Length > 0}");

            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                Material material = renderer.sharedMaterials[i];
                string color = material != null && material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor").ToString()
                    : material != null && material.HasProperty("_Color") ? material.color.ToString() : "-";

                log.AppendLine($"[PJ1]    slot {i}: '{material?.name}' shader={material?.shader.name} color={color} " +
                               $"mainTex={(material != null && material.mainTexture != null ? material.mainTexture.name : "-")}");
            }

            if (mesh == null || mesh.uv.Length == 0)
                continue;

            Vector2[] uv = mesh.uv;

            for (int sub = 0; sub < mesh.subMeshCount; sub++)
            {
                int[] triangles = mesh.GetTriangles(sub);
                Vector2[] used = triangles.Select(t => uv[t]).ToArray();
                Vector2 min = new Vector2(used.Min(u => u.x), used.Min(u => u.y));
                Vector2 max = new Vector2(used.Max(u => u.x), used.Max(u => u.y));
                log.AppendLine($"[PJ1]    submesh {sub}: tris={triangles.Length / 3} uvMin={min} uvMax={max}");

                File.WriteAllBytes(Path.Combine(uvFolder, $"{renderer.name}_sub{sub}.png"), DrawUv(uv, triangles));
            }
        }

        log.AppendLine($"[PJ1] UV en: {uvFolder}");
        Debug.Log(log.ToString());
    }


    // =====================================================
    // MATERIALES URP EXTERNOS PARA PERSONAJE1
    // =====================================================

    private const string Personaje1Folder = "Assets/Art/Characters/Personaje1";

    // Roughness medida dentro de las islas UV (constante en todo el mapa):
    // Ropa_Roughness = 0.737, Morral_Rougness = 0.906 → Smoothness = 1 - Roughness.
    private const float ClothSmoothness = 1f - 0.737f;
    private const float BackpackSmoothness = 1f - 0.906f;

    private struct SlotSpec
    {
        public string source, target, baseMap;
        public float? smoothness;
    }

    // Slot embebido del FBX → material externo. Los atlas Ropa_* / Morral* NO se asignan:
    // se hornearon sobre otro unwrap y las UV de este FBX no coinciden.
    private static readonly SlotSpec[] Slots =
    {
        new SlotSpec { source = "Material.079", target = "P1_Piel", baseMap = "Skin_Color.jpg" },
        new SlotSpec { source = "Material.081", target = "P1_Ojo", baseMap = "Ojos.png" },
        new SlotSpec { source = "Material.080", target = "P1_Ojo_1", baseMap = "Ojos.png" },
        new SlotSpec { source = "Material.083", target = "P1_Ceja" },
        new SlotSpec { source = "Material.082", target = "P1_Ceja_1" },
        new SlotSpec { source = "Material.002", target = "P1_Camisa", smoothness = ClothSmoothness },
        new SlotSpec { source = "Material", target = "P1_Pantalon", smoothness = ClothSmoothness },
        new SlotSpec { source = "Material.084", target = "P1_Piernas" },
        new SlotSpec { source = "Material.086", target = "P1_Zapatos", smoothness = ClothSmoothness },
        new SlotSpec { source = "Material.003", target = "P1_Gorra", smoothness = ClothSmoothness },
        new SlotSpec { source = "school subjects colors.001", target = "P1_Morral_Oscuro", smoothness = BackpackSmoothness },
        new SlotSpec { source = "school subjects colors", target = "P1_Morral_Rojo", smoothness = BackpackSmoothness },
        new SlotSpec { source = "school subjects colors.002", target = "P1_Morral_Detalles" },
    };


    [MenuItem("TSirkus/Personajes/Configurar materiales URP de Personaje1")]
    private static void SetupPersonaje1Materials()
    {
        string materialFolder = Personaje1Folder + "/Materials";

        if (!AssetDatabase.IsValidFolder(materialFolder))
            AssetDatabase.CreateFolder(Personaje1Folder, "Materials");

        ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(Personaje1Fbx);

        // Materiales embebidos actuales (sin remapeo) para copiar sus parámetros.
        var embedded = AssetDatabase.LoadAllAssetsAtPath(Personaje1Fbx)
            .OfType<Material>()
            .ToDictionary(m => m.name);

        StringBuilder log = new StringBuilder("[PJ1] ===== Materiales URP de Personaje1\n");

        foreach (SlotSpec slot in Slots)
        {
            string path = $"{materialFolder}/{slot.target}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                if (!embedded.TryGetValue(slot.source, out Material source))
                {
                    Debug.LogWarning($"[PJ1] No existe el slot embebido '{slot.source}'.");
                    continue;
                }

                material = new Material(source) { name = slot.target };
                AssetDatabase.CreateAsset(material, path);
            }

            if (slot.baseMap != null)
            {
                material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{Personaje1Folder}/Textures/{slot.baseMap}"));
                material.SetColor("_BaseColor", Color.white);
            }

            if (slot.smoothness.HasValue)
                material.SetFloat("_Smoothness", slot.smoothness.Value);

            EditorUtility.SetDirty(material);

            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), slot.source), material);

            log.AppendLine($"[PJ1] {slot.source} → {slot.target} | baseMap={slot.baseMap ?? "-"} color={material.GetColor("_BaseColor")} " +
                           $"smoothness={material.GetFloat("_Smoothness"):0.000} metallic={material.GetFloat("_Metallic"):0.00}");
        }

        AssetDatabase.SaveAssets();
        importer.SaveAndReimport();

        Debug.Log(log.ToString());
    }


    // =====================================================
    // PRUEBA: VER A PLAYER 1 DESDE OTRAS CÁMARAS
    // =====================================================

    // P2 de frente a P1 y P4 de lado, ambos mirando a su pecho (solo editor).
    [MenuItem("TSirkus/Personajes/Mirar a Player 1 desde P2 y P4 (Play Mode)")]
    private static void LookAtPlayer1()
    {
        PlayerIdentity[] players = Object.FindObjectsByType<PlayerIdentity>()
            .Where(p => p.GetComponentInChildren<Camera>(true) != null)
            .ToArray();

        PlayerIdentity p1 = players.FirstOrDefault(p => p.PlayerId == 1);

        if (p1 == null)
            return;

        Vector3 chest = p1.transform.position + Vector3.up * 1.3f;
        PlaceLooking(players.FirstOrDefault(p => p.PlayerId == 2), p1.transform.position + p1.transform.forward * 1.8f, chest);
        PlaceLooking(players.FirstOrDefault(p => p.PlayerId == 4), p1.transform.position + p1.transform.right * 1.8f, chest);
    }


    private static void PlaceLooking(PlayerIdentity player, Vector3 position, Vector3 target)
    {
        if (player == null)
            return;

        CharacterController body = player.GetComponent<CharacterController>();
        position.y = player.transform.position.y;

        body.enabled = false;
        player.transform.position = position;
        body.enabled = true;

        Camera cam = player.GetComponentInChildren<Camera>(true);
        Vector3 toTarget = target - cam.transform.position;
        float yaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
        float pitch = -Mathf.Atan2(toTarget.y, new Vector2(toTarget.x, toTarget.z).magnitude) * Mathf.Rad2Deg;

        player.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        PlayerLook look = player.GetComponent<PlayerLook>();

        if (look != null)
        {
            typeof(PlayerLook).GetField("verticalRotation",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(look, pitch);

            Transform holder = new SerializedObject(look).FindProperty("cameraHolder").objectReferenceValue as Transform;

            if (holder != null)
                holder.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }


    [MenuItem("TSirkus/Personajes/Mirar a Player 1 desde P2 y P4 (Play Mode)", true)]
    private static bool PlayModeOnly()
    {
        return EditorApplication.isPlaying;
    }


    // Dibuja las aristas de los triángulos en UV [0,1] sobre 512x512 (fondo transparente).
    private static byte[] DrawUv(Vector2[] uv, int[] triangles)
    {
        const int size = 512;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.SetPixels32(Enumerable.Repeat(new Color32(0, 0, 0, 0), size * size).ToArray());

        for (int i = 0; i < triangles.Length; i += 3)
        {
            for (int e = 0; e < 3; e++)
            {
                Vector2 a = uv[triangles[i + e]];
                Vector2 b = uv[triangles[i + (e + 1) % 3]];
                int steps = Mathf.CeilToInt(Vector2.Distance(a, b) * size) + 1;

                for (int s = 0; s <= steps; s++)
                {
                    Vector2 p = Vector2.Lerp(a, b, s / (float)steps);
                    float x = p.x - Mathf.Floor(p.x);
                    float y = p.y - Mathf.Floor(p.y);
                    texture.SetPixel((int)(x * (size - 1)), (int)(y * (size - 1)), new Color32(0, 255, 255, 255));
                }
            }
        }

        texture.Apply();
        byte[] png = texture.EncodeToPNG();
        Object.DestroyImmediate(texture);
        return png;
    }
}
