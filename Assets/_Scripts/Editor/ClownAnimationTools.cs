using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// Integra el modelo animado del payaso (Assets/AnimationsClown, Mixamo).
// Idempotente: se puede repetir.
//
// 1. Payaso.fbx y Payaso@Idle/Walk/Run.fbx como Humanoid (cada uno crea su avatar desde su
//    esqueleto). Con Generic no sirven: el modelo tiene "Armature/mixamorig:Hips/..." y los clips
//    "mixamorig:Hips/...", las rutas no coinciden. Humanoid retargetea por hueso.
//    Clips en bucle, rotación y altura de la raíz fijas (sin deriva); el avance XZ va a la raíz y
//    se descarta porque Apply Root Motion = false (lo mueve el NavMeshAgent).
// 2. AC_Payaso.controller: un blend tree 1D por "Speed" (m/s reales) Idle(0) → Walk → Run con
//    umbrales = velocidad natural de cada clip a la escala del modelo (sin patinar). Los clips son
//    "in place" (sin avance de raíz), así que la velocidad natural se mide con el pie apoyado:
//    mientras pisa, el pie retrocede respecto al cuerpo exactamente a la velocidad de la marcha.
// 3. Payaso.prefab rehecho sobre el modelo animado conservando ClownAI / ClownAudio /
//    NavMeshAgent / collider; velocidades del agente = velocidades naturales de Walk y Run.
// 4. Conecta los cues de audio del payaso y la referencia del ClownSpawner en GAME.
public static class ClownAnimationTools
{
    private const string Folder = "Assets/AnimationsClown";
    private const string ModelPath = Folder + "/Payaso.fbx";
    private const string ControllerPath = Folder + "/AC_Payaso.controller";
    private const string PrefabPath = "Assets/Prefabs/Enemies/Payaso.prefab";

    // Altura objetivo si el FBX viene con una escala absurda (fuera de 1.6-3.2 m).
    private const float FallbackHeight = 2.6f;


    // Texturas del payaso (Assets/TexturesClown). El FBX ya dice qué imagen va en cada material
    // (DiffuseColor), pero con rutas de Windows del autor que Unity no encuentra. Se crean
    // materiales URP/Lit con esa misma correspondencia y se registran como remap del FBX
    // (Materials > On Demand Remap): el prefab los recibe sin cambios. Para deshacer, quitar los
    // remaps en el inspector del FBX.
    private const string TextureFolder = "Assets/TexturesClown";
    private const string MaterialFolder = Folder + "/Materials";

    // material del FBX → (textura según el FBX, suavidad).
    private static readonly (string material, string texture, float smoothness)[] TextureMap =
    {
        // Suavidad baja: tela, pelo y piel mates (con más brillo los focos cálidos del circo
        // dejaban reflejos blancos en el pelo y la gola).
        ("Material.013", "WhatsApp Image 2026-09-03 at 12.35.05 PM.jpeg", 0.08f), // cabeza: rostro, pelo, gola
        ("Material.018", "Gemini_Generated_Image_7w1s5k7w1s5k7w1s.jpg", 0.5f),    // ojos (húmedos)
        ("Material.019", "Gemini_Generated_Image_lz5mvnlz5mvnlz5m.jpg", 0.3f),    // boca / dientes
        ("Material.011", "WhatsApp Image 2026-09-03 at 12.52.23 PM.jpeg", 0.05f), // pelo
        ("Material.004", "Gemini_Generated_Image_n0gxnjn0gxnjn0gx.jpg", 0.05f),   // traje
        ("Material.001", "Gemini_Generated_Image_a0n9gta0n9gta0n9.jpg", 0.05f),   // tela sucia
        ("Material.012", "Gemini_Generated_Image_1w0ygj1w0ygj1w0y.jpg", 0.15f),   // pintura roja
        ("Material #26.001", "Gemini_Generated_Image_1w0ygj1w0ygj1w0y.jpg", 0.15f) // pintura roja (nariz)
    };


    [MenuItem("TSirkus/Payaso/Asignar texturas del payaso")]
    private static void AssignTextures()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[PAYASO TEX] Sal de Play Mode primero.");
            return;
        }

        var log = new StringBuilder("[PAYASO TEX]\n");
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");

        if (lit == null)
        {
            Debug.LogError("[PAYASO TEX] No se encontró URP/Lit.");
            return;
        }

        if (!AssetDatabase.IsValidFolder(MaterialFolder))
            AssetDatabase.CreateFolder(Folder, "Materials");

        var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);

        foreach ((string materialName, string textureName, float smoothness) in TextureMap)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/{textureName}");

            if (texture == null)
            {
                log.AppendLine($"  ✗ {materialName}: falta {textureName}");
                continue;
            }

            string path = $"{MaterialFolder}/Payaso_{materialName.Replace("#", "").Replace(" ", "_")}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material = new Material(lit);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = lit;
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);

            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), materialName), material);
            log.AppendLine($"  {materialName} → {path} (base map {textureName}, suavidad {smoothness})");
        }

        AssetDatabase.SaveAssets();
        importer.SaveAndReimport();

        // Comprobación: qué material y textura usa cada malla del prefab.
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
        {
            log.AppendLine($"  prefab {renderer.name}: " + string.Join(", ", renderer.sharedMaterials.Select(m =>
                m == null ? "NULL" : $"{m.name} [{m.shader.name}] {(m.GetTexture("_BaseMap") != null ? m.GetTexture("_BaseMap").name : "sin textura")}")));
        }

        Debug.Log(log.ToString());
    }


    [MenuItem("TSirkus/Payaso/Configurar animaciones y prefab del payaso")]
    private static void Setup()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[PAYASO ANIM] Sal de Play Mode primero.");
            return;
        }

        var log = new StringBuilder("[PAYASO ANIM]\n");

        // 1. Importación.
        ConfigureModel(log);
        AnimationClip idle = ConfigureClip("Idle", log);
        AnimationClip walk = ConfigureClip("Walk", log);
        AnimationClip run = ConfigureClip("Run", log);

        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);

        if (avatar == null || !avatar.isValid || !avatar.isHuman || idle == null || walk == null || run == null)
        {
            Debug.LogError(log.Append("  ✗ Modelo o clips no válidos: no se continúa.").ToString());
            return;
        }

        // 2. Medidas reales del modelo y de los clips.
        Measure(model, out float height, out float humanScale);
        float scale = height >= 1.6f && height <= 3.2f ? 1f : FallbackHeight / height;
        float walkSpeed = StrideSpeed(model, avatar, walk, log) * scale;
        float runSpeed = StrideSpeed(model, avatar, run, log) * scale;

        log.AppendLine($"  Modelo: altura {height:F2} m, humanScale {humanScale:F3}, escala usada {scale:F3} → {height * scale:F2} m");
        log.AppendLine($"  Velocidad natural: Walk {walkSpeed:F2} m/s, Run {runSpeed:F2} m/s");

        if (walkSpeed < 0.2f || runSpeed <= walkSpeed)
        {
            Debug.LogError(log.Append("  ✗ Velocidades de clip incoherentes: revisa los clips.").ToString());
            return;
        }

        // 3. Animator Controller.
        AnimatorController controller = BuildController(idle, walk, run, walkSpeed, runSpeed, log);

        // 4. Prefab.
        BuildPrefab(model, avatar, controller, scale, height * scale, walkSpeed, runSpeed, log);

        // 5. Audio y escena.
        AudioSetupTools.SetupClownAudio();
        log.AppendLine("  Cues del payaso creados/conectados (AudioSetupTools).");
        ReconnectSpawner(log);

        Debug.Log(log.ToString());
    }


    private static void ConfigureModel(StringBuilder log)
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = false;
        importer.materialSearch = ModelImporterMaterialSearch.RecursiveUp;
        importer.SaveAndReimport();

        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
        log.AppendLine($"  Payaso.fbx: Humanoid, avatar válido={avatar != null && avatar.isValid} humano={avatar != null && avatar.isHuman}");
    }


    private static AnimationClip ConfigureClip(string name, StringBuilder log)
    {
        string path = $"{Folder}/Payaso@{name}.fbx";
        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;

        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;

        foreach (ModelImporterClipAnimation clip in clips)
        {
            clip.name = name;
            clip.loopTime = true;
            clip.lockRootRotation = true;
            clip.keepOriginalOrientation = true;
            clip.lockRootHeightY = true;
            clip.keepOriginalPositionY = true;
            clip.lockRootPositionXZ = false;
        }

        importer.clipAnimations = clips;
        importer.SaveAndReimport();

        AnimationClip result = AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview__"));

        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();

        if (result != null)
        {
            log.AppendLine($"  Payaso@{name}: clip '{result.name}' {result.length:F2} s, {result.frameRate} fps, " +
                           $"loop={result.isLooping}, humano={result.isHumanMotion}, avance medio={result.averageSpeed.magnitude:F3}, " +
                           $"avatar válido={avatar != null && avatar.isValid}");
        }
        else
        {
            log.AppendLine($"  ✗ Payaso@{name}: sin clip");
        }

        return result;
    }


    private static void Measure(GameObject model, out float height, out float humanScale)
    {
        var instance = (GameObject)Object.Instantiate(model);
        instance.hideFlags = HideFlags.HideAndDontSave;

        try
        {
            Bounds bounds = new Bounds(instance.transform.position, Vector3.zero);

            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>())
                bounds.Encapsulate(renderer.bounds);

            height = bounds.size.y;

            Animator animator = instance.GetComponent<Animator>();
            animator.Rebind();
            humanScale = animator.humanScale;
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }


    // Velocidad (unidades del modelo/s) a la que retrocede el pie apoyado durante el ciclo.
    private static float StrideSpeed(GameObject model, Avatar avatar, AnimationClip clip, StringBuilder log)
    {
        var instance = (GameObject)Object.Instantiate(model);
        instance.hideFlags = HideFlags.DontSave;
        bool started = false;

        try
        {
            Animator animator = instance.GetComponent<Animator>();
            animator.avatar = avatar;
            animator.applyRootMotion = false;

            Transform root = instance.transform;
            Transform[] feet =
            {
                animator.GetBoneTransform(HumanBodyBones.LeftFoot),
                animator.GetBoneTransform(HumanBodyBones.RightFoot)
            };

            AnimationMode.StartAnimationMode();
            started = true;

            const int samples = 120;
            float dt = clip.length / samples;
            var positions = new Vector3[2, samples + 1];

            for (int i = 0; i <= samples; i++)
            {
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(instance, clip, i * dt);
                AnimationMode.EndSampling();

                for (int f = 0; f < 2; f++)
                    positions[f, i] = root.InverseTransformPoint(feet[f].position);
            }

            var speeds = new System.Collections.Generic.List<float>();

            for (int f = 0; f < 2; f++)
            {
                float minY = float.MaxValue, maxY = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;

                for (int i = 0; i <= samples; i++)
                {
                    minY = Mathf.Min(minY, positions[f, i].y);
                    maxY = Mathf.Max(maxY, positions[f, i].y);
                    minZ = Mathf.Min(minZ, positions[f, i].z);
                    maxZ = Mathf.Max(maxZ, positions[f, i].z);
                }

                log.AppendLine($"    {clip.name} pie {(f == 0 ? "izq" : "der")}: altura {minY:F2}..{maxY:F2}, avance {minZ:F2}..{maxZ:F2} (unidades del modelo)");

                float contact = minY + (maxY - minY) * 0.15f;

                for (int i = 0; i < samples; i++)
                {
                    if (positions[f, i].y <= contact && positions[f, i + 1].y <= contact)
                        speeds.Add(-(positions[f, i + 1].z - positions[f, i].z) / dt);
                }
            }

            if (speeds.Count == 0)
                return 0f;

            speeds.Sort();
            return speeds[speeds.Count / 2];
        }
        finally
        {
            if (started)
                AnimationMode.StopAnimationMode();

            Object.DestroyImmediate(instance);
        }
    }


    private static AnimatorController BuildController(
        AnimationClip idle, AnimationClip walk, AnimationClip run,
        float walkSpeed, float runSpeed, StringBuilder log)
    {
        AssetDatabase.DeleteAsset(ControllerPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

        AnimatorState state = controller.CreateBlendTreeInController("Locomocion", out BlendTree tree, 0);
        tree.blendType = BlendTreeType.Simple1D;
        tree.blendParameter = "Speed";
        tree.useAutomaticThresholds = false;
        tree.AddChild(idle, 0f);
        tree.AddChild(walk, walkSpeed);
        tree.AddChild(run, runSpeed);

        controller.layers[0].stateMachine.defaultState = state;
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        log.AppendLine($"  AC_Payaso: blend tree Speed → Idle 0 | Walk {walkSpeed:F2} | Run {runSpeed:F2}");
        return controller;
    }


    private static void BuildPrefab(
        GameObject model, Avatar avatar, AnimatorController controller,
        float scale, float height, float walkSpeed, float runSpeed, StringBuilder log)
    {
        // Valores ya ajustados del prefab anterior (ClownAI, ClownAudio) se conservan.
        GameObject old = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        string aiJson = old != null && old.GetComponent<ClownAI>() != null ? EditorJsonUtility.ToJson(old.GetComponent<ClownAI>()) : null;
        string audioJson = old != null && old.GetComponent<ClownAudio>() != null ? EditorJsonUtility.ToJson(old.GetComponent<ClownAudio>()) : null;
        NavMeshAgent oldAgent = old != null ? old.GetComponent<NavMeshAgent>() : null;

        var root = new GameObject("Payaso");

        try
        {
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
            visual.name = "Modelo";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one * scale;

            Animator animator = visual.GetComponent<Animator>();

            if (animator == null)
                animator = visual.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.avatar = avatar;
            animator.applyRootMotion = false;
            // Debe animarse aunque ninguna cámara lo vea: así suenan sus pasos fuera de pantalla.
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var body = root.AddComponent<CapsuleCollider>();
            body.height = height;
            body.radius = 0.35f;
            body.center = new Vector3(0f, height * 0.5f, 0f);

            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = Mathf.Min(height, 2.9f);
            agent.baseOffset = 0f;
            agent.speed = walkSpeed;
            agent.acceleration = oldAgent != null ? oldAgent.acceleration : 8f;
            agent.angularSpeed = 300f;
            agent.stoppingDistance = oldAgent != null ? oldAgent.stoppingDistance : 1f;
            agent.autoBraking = true;

            var ai = root.AddComponent<ClownAI>();
            if (aiJson != null) EditorJsonUtility.FromJsonOverwrite(aiJson, ai);
            var aiSo = new SerializedObject(ai);
            aiSo.FindProperty("navMeshAgent").objectReferenceValue = agent;
            aiSo.FindProperty("animator").objectReferenceValue = animator;
            aiSo.FindProperty("roamSpeed").floatValue = walkSpeed;
            aiSo.FindProperty("chaseSpeed").floatValue = runSpeed;
            aiSo.ApplyModifiedPropertiesWithoutUndo();

            var audio = root.AddComponent<ClownAudio>();
            if (audioJson != null) EditorJsonUtility.FromJsonOverwrite(audioJson, audio);
            var audioSo = new SerializedObject(audio);
            audioSo.FindProperty("runSpeedThreshold").floatValue = (walkSpeed + runSpeed) * 0.5f;
            audioSo.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            log.AppendLine($"  Payaso.prefab: raíz escala 1 (agente {agent.radius} m × {agent.height:F2} m, collider {height:F2} m), " +
                           $"Modelo escala {scale:F3}, Animator AC_Payaso, root motion OFF, roam {walkSpeed:F2} / chase {runSpeed:F2} m/s");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }


    private static void ReconnectSpawner(StringBuilder log)
    {
        var spawner = Object.FindAnyObjectByType<ClownSpawner>(FindObjectsInactive.Include);

        if (spawner == null)
        {
            log.AppendLine("  (GAME no está abierta: ClownSpawner sin revisar)");
            return;
        }

        var so = new SerializedObject(spawner);
        SerializedProperty prefab = so.FindProperty("clownPrefab");
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        if (prefab.objectReferenceValue != asset)
        {
            prefab.objectReferenceValue = asset;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
            EditorSceneManager.SaveScene(spawner.gameObject.scene);
            log.AppendLine("  ClownSpawner.clownPrefab → Payaso.prefab (nuevo) · escena guardada");
        }
        else
        {
            log.AppendLine("  ClownSpawner.clownPrefab ya apuntaba al prefab.");
        }
    }
}
