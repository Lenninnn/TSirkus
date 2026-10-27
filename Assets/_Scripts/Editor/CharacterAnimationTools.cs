using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// Diagnóstico de rig y animaciones de Personaje1 (solo lectura: instancia en memoria y destruye).
public static class CharacterAnimationTools
{
    private const string BaseFbx = "Assets/Prefabs/Players/Personaje1.fbx";
    private const string AnimFolder = "Assets/Art/Characters/Personaje1/Animations";
    private const string IdleFbx = AnimFolder + "/Personaje1 Idle.fbx";
    private const string MovingFbx = AnimFolder + "/Personaje1 moving.fbx";


    [MenuItem("TSirkus/Personajes/Analizar rig y animaciones de Personaje1 (solo lectura)")]
    private static void Analyze()
    {
        StringBuilder log = new StringBuilder("[ANIM] ===== Personaje1: rig y animaciones\n");

        foreach (string path in new[] { BaseFbx, IdleFbx, MovingFbx })
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(path);
            GameObject instance = Object.Instantiate(asset);
            instance.hideFlags = HideFlags.HideAndDontSave;

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);

            Transform hips = instance.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.EndsWith("Hips"));

            log.AppendLine($"[ANIM] {System.IO.Path.GetFileName(path)}: animationType={importer.animationType} avatarSetup={importer.avatarSetup} " +
                           $"rootScale={asset.transform.localScale} skinned={renderers.Count(r => r is SkinnedMeshRenderer)} mesh={renderers.Count(r => r is MeshRenderer)} " +
                           $"altura={bounds.size.y:0.000} centro={bounds.center} huesos={(hips != null ? hips.GetComponentsInChildren<Transform>().Length : 0)} " +
                           $"avatar={AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault()?.name ?? "-"}");

            foreach (Renderer r in renderers.OrderBy(r => r.name))
            {
                Mesh mesh = r is SkinnedMeshRenderer s ? s.sharedMesh : r.GetComponent<MeshFilter>()?.sharedMesh;
                if (mesh == null) continue;

                Vector2[] uv = mesh.uv;
                string uvRange = uv.Length == 0 ? "-" :
                    $"({uv.Min(u => u.x):0.00},{uv.Min(u => u.y):0.00})-({uv.Max(u => u.x):0.00},{uv.Max(u => u.y):0.00})";

                log.AppendLine($"[ANIM]    {r.name}: verts={mesh.vertexCount} subs={mesh.subMeshCount} uv={uvRange} " +
                               $"bones={(r is SkinnedMeshRenderer sk ? sk.bones.Length : 0)} mats={string.Join("/", r.sharedMaterials.Select(m => m != null ? m.name : "null"))}");
            }

            // Clips: duración, desplazamiento del Hips y cierre del ciclo.
            foreach (AnimationClip clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                         .Where(c => !c.name.StartsWith("__preview__")))
            {
                log.Append($"[ANIM]    clip '{clip.name}': {clip.length:0.000}s @{clip.frameRate}fps frames={Mathf.RoundToInt(clip.length * clip.frameRate)} " +
                           $"loopTime={clip.isLooping} curvas={AnimationUtility.GetCurveBindings(clip).Length}");

                if (hips != null)
                {
                    Transform[] bones = hips.GetComponentsInChildren<Transform>();

                    clip.SampleAnimation(instance, 0f);
                    Vector3 hipsStart = hips.localPosition;
                    Quaternion[] start = bones.Select(b => b.localRotation).ToArray();

                    float minY = float.MaxValue, maxY = float.MinValue, maxXZ = 0f;

                    for (int i = 0; i <= 20; i++)
                    {
                        clip.SampleAnimation(instance, clip.length * i / 20f);
                        Vector3 delta = hips.localPosition - hipsStart;
                        maxXZ = Mathf.Max(maxXZ, new Vector2(delta.x, delta.z).magnitude);
                        minY = Mathf.Min(minY, hips.localPosition.y);
                        maxY = Mathf.Max(maxY, hips.localPosition.y);
                    }

                    clip.SampleAnimation(instance, clip.length);
                    Vector3 hipsEnd = hips.localPosition;
                    float maxAngle = bones.Select((b, i) => Quaternion.Angle(start[i], b.localRotation)).Max();
                    string worst = bones.Select((b, i) => (b.name, a: Quaternion.Angle(start[i], b.localRotation))).OrderByDescending(x => x.a).First().name;

                    log.Append($" | hipsXZ máx={maxXZ:0.000} hipsY {minY:0.000}..{maxY:0.000} " +
                               $"inicio→fin: hipsΔ={(hipsEnd - hipsStart).magnitude:0.000} rotΔmáx={maxAngle:0.0}° ({worst})");
                }

                log.AppendLine();
            }

            Object.DestroyImmediate(instance);
        }

        Debug.Log(log.ToString());
    }


    // =====================================================
    // 1) RIG GENERIC + AVATAR + CLIPS
    // =====================================================

    private const string MaterialFolder = "Assets/Art/Characters/Personaje1/Materials";
    private const string ControllerPath = AnimFolder + "/AC_Personaje1.controller";

    // Mismos slots que el FBX estático → mismos materiales URP externos de P1.
    private static readonly (string slot, string material)[] MaterialRemaps =
    {
        ("Material.079", "P1_Piel"), ("Material.081", "P1_Ojo"), ("Material.080", "P1_Ojo_1"),
        ("Material.083", "P1_Ceja"), ("Material.082", "P1_Ceja_1"), ("Material.002", "P1_Camisa"),
        ("Material", "P1_Pantalon"), ("Material.084", "P1_Piernas"), ("Material.086", "P1_Zapatos"),
        ("Material.003", "P1_Gorra"), ("school subjects colors.001", "P1_Morral_Oscuro"),
        ("school subjects colors", "P1_Morral_Rojo"), ("school subjects colors.002", "P1_Morral_Detalles"),
    };


    [MenuItem("TSirkus/Personajes/1. Configurar rig Generic y clips Idle-Walk de Personaje1")]
    private static void ConfigureRigAndClips()
    {
        // Idle: modelo visual (malla + rig) y dueño del Avatar.
        ModelImporter idle = (ModelImporter)AssetImporter.GetAtPath(IdleFbx);
        string hipsPath = idle.transformPaths.FirstOrDefault(p => p.EndsWith("mixamorig:Hips"));

        idle.animationType = ModelImporterAnimationType.Generic;
        idle.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        idle.motionNodeName = hipsPath;
        idle.clipAnimations = BuildClips(idle, "P1_Idle");

        foreach ((string slot, string material) in MaterialRemaps)
        {
            idle.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), slot),
                AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/{material}.mat"));
        }

        idle.SaveAndReimport();

        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(IdleFbx).OfType<Avatar>().First();

        // moving: solo aporta el clip; reutiliza el Avatar de Idle y no importa materiales.
        ModelImporter moving = (ModelImporter)AssetImporter.GetAtPath(MovingFbx);
        moving.animationType = ModelImporterAnimationType.Generic;
        moving.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
        moving.sourceAvatar = avatar;
        moving.motionNodeName = hipsPath;
        moving.materialImportMode = ModelImporterMaterialImportMode.None;
        moving.clipAnimations = BuildClips(moving, "P1_Walk");
        moving.SaveAndReimport();

        StringBuilder log = new StringBuilder($"[ANIM] Rig Generic configurado. motionNode='{hipsPath}' avatar='{avatar.name}' válido={avatar.isValid}\n");

        foreach (string path in new[] { IdleFbx, MovingFbx })
        {
            foreach (AnimationClip clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")))
            {
                log.AppendLine($"[ANIM]    {clip.name}: {clip.length:0.000}s loopTime={clip.isLooping} motionCurves={clip.hasMotionCurves} " +
                               $"rootCurves={clip.hasRootCurves} averageSpeed={clip.averageSpeed}");
            }
        }

        Debug.Log(log.ToString());
    }


    // Clip en loop. XZ del Hips NO se hornea en la pose: pasa a root motion y,
    // con Apply Root Motion desactivado, se descarta (ciclo in-place).
    // Altura y orientación sí se quedan en la pose.
    private static ModelImporterClipAnimation[] BuildClips(ModelImporter importer, string clipName)
    {
        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;

        foreach (ModelImporterClipAnimation clip in clips)
        {
            clip.name = clipName;
            clip.loopTime = true;
            clip.lockRootRotation = true;
            clip.keepOriginalOrientation = true;
            clip.lockRootHeightY = true;
            clip.keepOriginalPositionY = true;
            clip.lockRootPositionXZ = false;
            clip.keepOriginalPositionXZ = true;
        }

        return clips;
    }


    // =====================================================
    // 1b) WALK IN-PLACE (copia del clip, el FBX no se toca)
    // =====================================================

    private const string WalkInPlacePath = AnimFolder + "/P1_Walk_InPlace.anim";

    // En Generic con Apply Root Motion desactivado, el avance del Hips se queda en la pose
    // (el modelo se adelanta y salta atrás en cada ciclo). Esta copia resta solo la deriva
    // lineal X/Z del Hips en el ciclo: conserva el balanceo y deja el paso en el sitio.
    [MenuItem("TSirkus/Personajes/1b. Crear P1_Walk_InPlace")]
    private static void CreateWalkInPlace()
    {
        AnimationClip source = AssetDatabase.LoadAllAssetsAtPath(MovingFbx).OfType<AnimationClip>().First(c => c.name == "P1_Walk");

        AnimationClip clip = new AnimationClip { name = "P1_Walk_InPlace", frameRate = source.frameRate };
        StringBuilder log = new StringBuilder("[ANIM] P1_Walk_InPlace:\n");

        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(source))
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(source, binding);

            bool hipsXZ = binding.path.EndsWith("mixamorig:Hips") &&
                          (binding.propertyName == "m_LocalPosition.x" || binding.propertyName == "m_LocalPosition.z");

            if (hipsXZ && curve.length > 1)
            {
                float drift = curve.keys[curve.length - 1].value - curve.keys[0].value;
                float slope = drift / source.length;

                Keyframe[] keys = curve.keys;

                for (int i = 0; i < keys.Length; i++)
                {
                    keys[i].value -= slope * keys[i].time;
                    keys[i].inTangent -= slope;
                    keys[i].outTangent -= slope;
                }

                curve.keys = keys;
                log.AppendLine($"[ANIM]    {binding.propertyName}: deriva eliminada {drift:0.000}");
            }

            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(WalkInPlacePath) != null)
            AssetDatabase.DeleteAsset(WalkInPlacePath);

        AssetDatabase.CreateAsset(clip, WalkInPlacePath);
        AssetDatabase.SaveAssets();

        Debug.Log(log.ToString());
    }


    // =====================================================
    // 2) ANIMATOR CONTROLLER
    // =====================================================

    [MenuItem("TSirkus/Personajes/2. Crear AC_Personaje1 (Idle-Walk)")]
    private static void CreateController()
    {
        AnimationClip idleClip = AssetDatabase.LoadAllAssetsAtPath(IdleFbx).OfType<AnimationClip>().First(c => c.name == "P1_Idle");
        AnimationClip walkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(WalkInPlacePath);

        // Si ya existe (y el prefab lo referencia), solo se actualizan los clips: mismo GUID.
        var existing = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(ControllerPath);

        if (existing != null)
        {
            foreach (var child in existing.layers[0].stateMachine.states)
            {
                if (child.state.name == "Idle") child.state.motion = idleClip;
                if (child.state.name == "Walk") child.state.motion = walkClip;
            }

            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ANIM] {ControllerPath} actualizado: Idle({idleClip.name}) ↔ Walk({walkClip.name})");
            return;
        }

        var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter(new AnimatorControllerParameter
        {
            name = "WalkSpeedMultiplier",
            type = AnimatorControllerParameterType.Float,
            defaultFloat = 1f
        });

        var machine = controller.layers[0].stateMachine;

        var idle = machine.AddState("Idle");
        idle.motion = idleClip;

        var walk = machine.AddState("Walk");
        walk.motion = walkClip;
        walk.speedParameter = "WalkSpeedMultiplier";
        walk.speedParameterActive = true;

        machine.defaultState = idle;

        const float threshold = 0.15f;

        var toWalk = idle.AddTransition(walk);
        toWalk.hasExitTime = false;
        toWalk.duration = 0.12f;
        toWalk.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, threshold, "Speed");

        var toIdle = walk.AddTransition(idle);
        toIdle.hasExitTime = false;
        toIdle.duration = 0.15f;
        toIdle.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, threshold, "Speed");

        AssetDatabase.SaveAssets();
        Debug.Log($"[ANIM] {ControllerPath}: Idle({idleClip.name}) ↔ Walk({walkClip.name}), Speed > {threshold}");
    }


    // =====================================================
    // PRUEBA EN PLAY MODE
    // =====================================================

    // P2 mira a P1 desde 9 m por delante. P1: quieto → gira cámara → camina → camina girando → se detiene.
    [MenuItem("TSirkus/Personajes/Probar locomocion de P1 (Play Mode)")]
    private static void TestLocomotion()
    {
        PlayerIdentity[] players = Object.FindObjectsByType<PlayerIdentity>()
            .Where(p => p.GetComponentInChildren<Camera>(true) != null).ToArray();
        PlayerIdentity p1 = players.First(p => p.PlayerId == 1);
        PlayerIdentity p2 = players.First(p => p.PlayerId == 2);

        PlayerAnimationController visual = p1.GetComponentInChildren<PlayerAnimationController>();
        Animator animator = visual.GetComponent<Animator>();
        Transform hips = visual.GetComponentsInChildren<Transform>().First(t => t.name == "mixamorig:Hips");

        // P1 mirando a lo largo del corredor (+Z) y P2 9 m por delante, mirando a su pecho.
        p1.transform.rotation = Quaternion.identity;

        Vector3 p2Position = p1.transform.position + Vector3.forward * 9f;
        p2Position.y = p2.transform.position.y;

        CharacterController p2Body = p2.GetComponent<CharacterController>();
        p2Body.enabled = false;
        p2.transform.position = p2Position;
        p2.transform.rotation = Quaternion.LookRotation(Vector3.back);
        p2Body.enabled = true;

        Camera p2Camera = p2.GetComponentInChildren<Camera>(true);
        Vector3 toChest = p1.transform.position + Vector3.up * 1.2f - p2Camera.transform.position;
        float pitch = -Mathf.Atan2(toChest.y, new Vector2(toChest.x, toChest.z).magnitude) * Mathf.Rad2Deg;
        PlayerLook p2Look = p2.GetComponent<PlayerLook>();
        typeof(PlayerLook).GetField("verticalRotation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(p2Look, pitch);
        (new SerializedObject(p2Look).FindProperty("cameraHolder").objectReferenceValue as Transform).localRotation = Quaternion.Euler(pitch, 0f, 0f);

        Debug.Log($"[LOCO] P1 en {p1.transform.position} | P2 en {p2.transform.position} mirando a P1");

        Vector3 mark = p1.transform.position;

        void Log(string label)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            string name = state.IsName("Walk") ? "Walk" : state.IsName("Idle") ? "Idle" : "?";
            Vector3 hipsModel = visual.transform.InverseTransformPoint(hips.position);

            Debug.Log($"[LOCO] {label} → estado={name} Speed={animator.GetFloat("Speed"):0.00} " +
                      $"mult={animator.GetFloat("WalkSpeedMultiplier"):0.00} ciclo={state.normalizedTime:0.00} " +
                      $"P1 desplazado={Vector3.Distance(mark, p1.transform.position):0.00}m hipsXZ(modelo)=({hipsModel.x:0.00},{hipsModel.z:0.00}) " +
                      $"applyRootMotion={animator.applyRootMotion}");

            mark = p1.transform.position;
        }

        PlayerDiagnosticsTools.RunSteps(
            (2.5, () => Log("1 quieto")),
            (0, () => PlayerDiagnosticsTools.SetInput(1, "lookX", 1f)),
            (1.0, () => { PlayerDiagnosticsTools.SetInput(1, "lookX", 0f); Log("5 gira cámara quieto"); }),
            (0, () => PlayerDiagnosticsTools.SetInput(1, "movementY", 1f)),
            (0.35, () => Log("2 empieza a caminar")),
            (0.6, () => Log("3 sigue caminando (a)")),
            (0.6, () => Log("3 sigue caminando (b)")),
            (0, () => PlayerDiagnosticsTools.SetInput(1, "lookX", 0.6f)),
            (0.6, () => { PlayerDiagnosticsTools.SetInput(1, "lookX", 0f); Log("6 camina y gira"); }),
            (0, () => PlayerDiagnosticsTools.SetInput(1, "movementY", 0f)),
            (0.5, () => Log("4 se detiene")),
            (1.5, () => Log("7 quieto 1,5 s (no debe desplazarse)")));
    }


    [MenuItem("TSirkus/Personajes/Probar locomocion de P1 (Play Mode)", true)]
    private static bool PlayModeOnly()
    {
        return EditorApplication.isPlaying;
    }


    // =====================================================
    // 3) VISUAL DE PLAYER 1 EN EL PREFAB
    // =====================================================

    private const string PlayerPrefab = "Assets/Prefabs/Players/Player Variant.prefab";

    // Sustituye solo la instancia visual estática de modelPlayer1 por la versión con rig.
    // El Player raíz y su lógica no se tocan. Personaje1.fbx (estático) se conserva como asset.
    [MenuItem("TSirkus/Personajes/3. Usar visual con rig en Player 1")]
    private static void UseRiggedVisual()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefab);

        try
        {
            PlayerModelSelector selector = root.GetComponentInChildren<PlayerModelSelector>(true);
            SerializedObject selectorSO = new SerializedObject(selector);
            SerializedProperty modelProperty = selectorSO.FindProperty("modelPlayer1");
            GameObject oldModel = modelProperty.objectReferenceValue as GameObject;

            string oldSource = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(oldModel));

            if (oldSource != BaseFbx)
            {
                Debug.LogError($"[ANIM] modelPlayer1 no es la instancia de {BaseFbx} (es '{oldSource}'). No se cambia nada.");
                return;
            }

            Transform parent = oldModel.transform.parent;
            GameObject newModel = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(IdleFbx), parent);
            newModel.name = "Personaje1";
            newModel.transform.SetSiblingIndex(oldModel.transform.GetSiblingIndex());
            newModel.transform.localPosition = oldModel.transform.localPosition;
            newModel.transform.localRotation = oldModel.transform.localRotation;
            newModel.transform.localScale = oldModel.transform.localScale;
            newModel.SetActive(oldModel.activeSelf);

            if (!newModel.TryGetComponent(out Animator animator))
                animator = newModel.AddComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            animator.avatar = AssetDatabase.LoadAllAssetsAtPath(IdleFbx).OfType<Avatar>().First();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            PlayerAnimationController animationController = newModel.AddComponent<PlayerAnimationController>();
            SerializedObject animationSO = new SerializedObject(animationController);
            animationSO.FindProperty("animator").objectReferenceValue = animator;
            animationSO.ApplyModifiedPropertiesWithoutUndo();

            modelProperty.objectReferenceValue = newModel;
            selectorSO.ApplyModifiedPropertiesWithoutUndo();

            Object.DestroyImmediate(oldModel);

            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab);

            Debug.Log($"[ANIM] Player 1 usa ahora el visual con rig ({IdleFbx}) bajo '{parent.name}', escala {newModel.transform.localScale}. " +
                      $"Renderers: {newModel.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length} skinned, " +
                      $"materiales: {string.Join(", ", newModel.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Select(m => m != null ? m.name : "null").Distinct())}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
