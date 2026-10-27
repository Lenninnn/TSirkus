using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Locomoción Idle/Walk para Personaje2-4 con el mismo patrón validado en Personaje1
// (CharacterAnimationTools): rig Generic, avatar desde Idle con root mixamorig:Hips,
// Walk in-place si el Hips deriva, controller propio y PlayerAnimationController.
// Los FBX estáticos de Prefabs/Players se conservan intactos.
public static class CharacterLocomotionSetup
{
    private const string PlayerPrefab = "Assets/Prefabs/Players/Player Variant.prefab";

    private static readonly string[] StaticFbx =
    {
        null,
        "Assets/Prefabs/Players/Personaje1.fbx",
        "Assets/Prefabs/Players/Personaje2 1.fbx",
        "Assets/Prefabs/Players/Personaje3.fbx",
        "Assets/Prefabs/Players/Personaje4.fbx",
    };

    private static string Folder(int n) => $"Assets/Art/Characters/Personaje{n}/Animations";
    private static string IdleFbx(int n) => $"{Folder(n)}/Personaje{n} Idle.fbx";
    private static string MovingFbx(int n) => $"{Folder(n)}/Personaje{n} moving.fbx";
    private static string ControllerPath(int n) => $"{Folder(n)}/AC_Personaje{n}.controller";
    private static string InPlacePath(int n) => $"{Folder(n)}/P{n}_Walk_InPlace.anim";


    // =====================================================
    // ANÁLISIS (solo lectura)
    // =====================================================

    [MenuItem("TSirkus/Locomocion/Analizar Personaje2")] private static void Analyze2() => Analyze(2);
    [MenuItem("TSirkus/Locomocion/Analizar Personaje3")] private static void Analyze3() => Analyze(3);
    [MenuItem("TSirkus/Locomocion/Analizar Personaje4")] private static void Analyze4() => Analyze(4);

    private static void Analyze(int n)
    {
        StringBuilder log = new StringBuilder($"[LOCO{n}] ===== Personaje{n}\n");

        foreach (string path in new[] { StaticFbx[n], IdleFbx(n), MovingFbx(n) })
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Renderer[] renderers = asset.GetComponentsInChildren<Renderer>(true);
            Transform hips = asset.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "mixamorig:Hips");

            log.AppendLine($"[LOCO{n}] {System.IO.Path.GetFileName(path)}: skinned={renderers.Count(r => r is SkinnedMeshRenderer)} " +
                           $"mesh={renderers.Count(r => r is MeshRenderer)} huesos={(hips != null ? hips.GetComponentsInChildren<Transform>(true).Length : 0)} " +
                           $"escalaRaíz={asset.transform.localScale}");
        }

        // Materiales embebidos: estático vs Idle (mismo nombre → mismas propiedades?).
        var staticMats = AssetDatabase.LoadAllAssetsAtPath(StaticFbx[n]).OfType<Material>().ToDictionary(m => m.name);
        var idleMats = AssetDatabase.LoadAllAssetsAtPath(IdleFbx(n)).OfType<Material>().ToDictionary(m => m.name);
        int differences = 0;

        foreach (var pair in staticMats)
        {
            if (!idleMats.TryGetValue(pair.Key, out Material idle))
            {
                log.AppendLine($"[LOCO{n}]    material '{pair.Key}' no existe en Idle");
                differences++;
                continue;
            }

            string a = Describe(pair.Value), b = Describe(idle);

            if (a != b)
            {
                log.AppendLine($"[LOCO{n}]    material '{pair.Key}' DIFERENTE: estático[{a}] idle[{b}]");
                differences++;
            }
        }

        log.AppendLine($"[LOCO{n}]    materiales: estático={staticMats.Count} idle={idleMats.Count} diferencias={differences}");

        // Clips: duración, deriva del Hips y cierre del ciclo.
        foreach (string path in new[] { IdleFbx(n), MovingFbx(n) })
        {
            GameObject instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            instance.hideFlags = HideFlags.HideAndDontSave;
            Transform hips = instance.GetComponentsInChildren<Transform>(true).First(t => t.name == "mixamorig:Hips");
            Transform[] bones = hips.GetComponentsInChildren<Transform>();

            foreach (AnimationClip clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")))
            {
                clip.SampleAnimation(instance, 0f);
                Vector3 start = hips.localPosition;
                Quaternion[] startRot = bones.Select(b => b.localRotation).ToArray();

                clip.SampleAnimation(instance, clip.length);
                Vector3 drift = hips.localPosition - start;
                float rotDelta = bones.Select((b, i) => Quaternion.Angle(startRot[i], b.localRotation)).Max();

                log.AppendLine($"[LOCO{n}]    {System.IO.Path.GetFileName(path)} clip '{clip.name}': {clip.length:0.000}s " +
                               $"frames={Mathf.RoundToInt(clip.length * clip.frameRate)} derivaHips=({drift.x:0.000},{drift.y:0.000},{drift.z:0.000}) " +
                               $"rotΔ inicio→fin={rotDelta:0.0}°");
            }

            Object.DestroyImmediate(instance);
        }

        Debug.Log(log.ToString());
    }


    private static string Describe(Material m)
    {
        string color = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor").ToString("F3") : "-";
        string tex = m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null ? m.GetTexture("_BaseMap").name : "-";
        string smooth = m.HasProperty("_Smoothness") ? m.GetFloat("_Smoothness").ToString("F3") : "-";
        return $"{m.shader.name}|{color}|{tex}|{smooth}";
    }


    // =====================================================
    // CONFIGURAR (rig + clips + in-place + controller + visual)
    // =====================================================

    [MenuItem("TSirkus/Locomocion/Configurar Personaje2")] private static void Setup2() => Setup(2);
    [MenuItem("TSirkus/Locomocion/Configurar Personaje3")] private static void Setup3() => Setup(3);
    [MenuItem("TSirkus/Locomocion/Configurar Personaje4")] private static void Setup4() => Setup(4);

    private static void Setup(int n)
    {
        StringBuilder log = new StringBuilder($"[LOCO{n}] ===== Configurar Personaje{n}\n");

        // 1) Idle: modelo visual + Avatar. Sus materiales embebidos se mantienen (iguales al estático).
        ModelImporter idle = (ModelImporter)AssetImporter.GetAtPath(IdleFbx(n));
        string hipsPath = idle.transformPaths.FirstOrDefault(p => p.EndsWith("mixamorig:Hips"));

        if (hipsPath == null)
        {
            Debug.LogError($"[LOCO{n}] Idle no tiene mixamorig:Hips. No se cambia nada.");
            return;
        }

        idle.animationType = ModelImporterAnimationType.Generic;
        idle.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        idle.motionNodeName = hipsPath;
        idle.clipAnimations = BuildClips(idle, $"P{n}_Idle");
        idle.SaveAndReimport();

        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(IdleFbx(n)).OfType<Avatar>().First();

        // 2) moving: solo clip, reutiliza el Avatar de Idle, sin materiales.
        ModelImporter moving = (ModelImporter)AssetImporter.GetAtPath(MovingFbx(n));
        moving.animationType = ModelImporterAnimationType.Generic;
        moving.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
        moving.sourceAvatar = avatar;
        moving.motionNodeName = hipsPath;
        moving.materialImportMode = ModelImporterMaterialImportMode.None;
        moving.clipAnimations = BuildClips(moving, $"P{n}_Walk");
        moving.SaveAndReimport();

        AnimationClip idleClip = AssetDatabase.LoadAllAssetsAtPath(IdleFbx(n)).OfType<AnimationClip>().First(c => c.name == $"P{n}_Idle");
        AnimationClip walkSource = AssetDatabase.LoadAllAssetsAtPath(MovingFbx(n)).OfType<AnimationClip>().First(c => c.name == $"P{n}_Walk");

        log.AppendLine($"[LOCO{n}] avatar='{avatar.name}' válido={avatar.isValid} motionNode='{hipsPath}'");

        // 3) Walk in-place si el Hips deriva en X/Z.
        AnimationClip walkClip = CreateInPlaceIfNeeded(n, walkSource, out float driftXZ, log);
        float walkClipSpeed = driftXZ / walkSource.length;

        // 4) Controller propio.
        CreateOrUpdateController(n, idleClip, walkClip);
        log.AppendLine($"[LOCO{n}] {ControllerPath(n)}: Idle({idleClip.name}) ↔ Walk({walkClip.name})");

        // 5) Visual de PlayerN en el prefab.
        UseRiggedVisual(n, avatar, walkClipSpeed, log);

        Debug.Log(log.ToString());
    }


    // Igual que P1: loop, altura y orientación en la pose, XZ del Hips fuera de la pose.
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


    private static AnimationClip CreateInPlaceIfNeeded(int n, AnimationClip source, out float driftXZ, StringBuilder log)
    {
        var bindings = AnimationUtility.GetCurveBindings(source);
        float Drift(string property)
        {
            var binding = bindings.FirstOrDefault(b => b.path.EndsWith("mixamorig:Hips") && b.propertyName == property);
            AnimationCurve curve = binding.propertyName != null ? AnimationUtility.GetEditorCurve(source, binding) : null;
            return curve != null && curve.length > 1 ? curve.keys[curve.length - 1].value - curve.keys[0].value : 0f;
        }

        float dx = Drift("m_LocalPosition.x"), dz = Drift("m_LocalPosition.z");
        driftXZ = new Vector2(dx, dz).magnitude;
        log.AppendLine($"[LOCO{n}] deriva del Hips en {source.name}: x={dx:0.000} z={dz:0.000} (|XZ|={driftXZ:0.000} en {source.length:0.000}s)");

        if (driftXZ < 0.01f)
        {
            log.AppendLine($"[LOCO{n}] sin deriva: se usa {source.name} directamente");
            return source;
        }

        AnimationClip clip = new AnimationClip { name = $"P{n}_Walk_InPlace", frameRate = source.frameRate };

        foreach (EditorCurveBinding binding in bindings)
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(source, binding);

            bool hipsXZ = binding.path.EndsWith("mixamorig:Hips") &&
                          (binding.propertyName == "m_LocalPosition.x" || binding.propertyName == "m_LocalPosition.z");

            if (hipsXZ && curve.length > 1)
            {
                float slope = (curve.keys[curve.length - 1].value - curve.keys[0].value) / source.length;
                Keyframe[] keys = curve.keys;

                for (int i = 0; i < keys.Length; i++)
                {
                    keys[i].value -= slope * keys[i].time;
                    keys[i].inTangent -= slope;
                    keys[i].outTangent -= slope;
                }

                curve.keys = keys;
            }

            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        // Reutiliza el asset si existe (mismo GUID para el controller).
        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(InPlacePath(n));

        if (existing != null)
        {
            EditorUtility.CopySerialized(clip, existing);
            existing.name = clip.name;
            clip = existing;
        }
        else
        {
            AssetDatabase.CreateAsset(clip, InPlacePath(n));
        }

        AssetDatabase.SaveAssets();
        log.AppendLine($"[LOCO{n}] creado {InPlacePath(n)}");
        return clip;
    }


    // Transiciones de respuesta inmediata: sin Exit Time, duración fija y corta.
    private const float ToWalkDuration = 0.10f;
    private const float ToIdleDuration = 0.08f;
    private const float SpeedThreshold = 0.15f;

    [MenuItem("TSirkus/Locomocion/Ajustar transiciones AC_Personaje1")] private static void Tune1() => TuneTransitions(1);
    [MenuItem("TSirkus/Locomocion/Ajustar transiciones AC_Personaje2")] private static void Tune2() => TuneTransitions(2);
    [MenuItem("TSirkus/Locomocion/Ajustar transiciones AC_Personaje3")] private static void Tune3() => TuneTransitions(3);
    [MenuItem("TSirkus/Locomocion/Ajustar transiciones AC_Personaje4")] private static void Tune4() => TuneTransitions(4);

    private static void TuneTransitions(int n)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath(n));
        StringBuilder log = new StringBuilder($"[LOCO{n}] Transiciones de AC_Personaje{n}:\n");

        foreach (var child in controller.layers[0].stateMachine.states)
        {
            foreach (AnimatorStateTransition transition in child.state.transitions)
            {
                bool toIdle = transition.destinationState.name == "Idle";

                transition.hasExitTime = false;
                transition.hasFixedDuration = true;
                transition.duration = toIdle ? ToIdleDuration : ToWalkDuration;
                transition.offset = 0f;
                transition.conditions = new[]
                {
                    new AnimatorCondition
                    {
                        mode = toIdle ? AnimatorConditionMode.Less : AnimatorConditionMode.Greater,
                        parameter = "Speed",
                        threshold = SpeedThreshold
                    }
                };

                log.AppendLine($"[LOCO{n}]    {child.state.name} → {transition.destinationState.name}: exitTime=off, " +
                               $"{transition.duration:0.00}s fija, Speed {(toIdle ? "<" : ">")} {SpeedThreshold}");
            }
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log(log.ToString());
    }


    // Mismo controller que AC_Personaje1: Idle ↔ Walk por Speed (> 0.15) y WalkSpeedMultiplier.
    private static void CreateOrUpdateController(int n, AnimationClip idleClip, AnimationClip walkClip)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath(n));

        if (controller != null)
        {
            foreach (var child in controller.layers[0].stateMachine.states)
            {
                if (child.state.name == "Idle") child.state.motion = idleClip;
                if (child.state.name == "Walk") child.state.motion = walkClip;
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return;
        }

        controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath(n));
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
        toWalk.duration = ToWalkDuration;
        toWalk.AddCondition(AnimatorConditionMode.Greater, threshold, "Speed");

        var toIdle = walk.AddTransition(idle);
        toIdle.hasExitTime = false;
        toIdle.duration = ToIdleDuration;
        toIdle.AddCondition(AnimatorConditionMode.Less, threshold, "Speed");

        AssetDatabase.SaveAssets();
    }


    // Sustituye solo la instancia visual estática de modelPlayerN. El Player raíz no se toca.
    private static void UseRiggedVisual(int n, Avatar avatar, float walkClipSpeed, StringBuilder log)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefab);

        try
        {
            PlayerModelSelector selector = root.GetComponentInChildren<PlayerModelSelector>(true);
            SerializedObject selectorSO = new SerializedObject(selector);
            SerializedProperty modelProperty = selectorSO.FindProperty($"modelPlayer{n}");
            GameObject oldModel = modelProperty.objectReferenceValue as GameObject;

            string oldSource = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(oldModel));

            if (oldSource != StaticFbx[n])
            {
                log.AppendLine($"[LOCO{n}] modelPlayer{n} no es la instancia de {StaticFbx[n]} (es '{oldSource}'). No se cambia el prefab.");
                return;
            }

            Transform parent = oldModel.transform.parent;
            GameObject newModel = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(IdleFbx(n)), parent);
            newModel.name = oldModel.name;
            newModel.transform.SetSiblingIndex(oldModel.transform.GetSiblingIndex());
            newModel.transform.localPosition = oldModel.transform.localPosition;
            newModel.transform.localRotation = oldModel.transform.localRotation;
            newModel.transform.localScale = oldModel.transform.localScale;
            newModel.SetActive(oldModel.activeSelf);

            if (!newModel.TryGetComponent(out Animator animator))
                animator = newModel.AddComponent<Animator>();

            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath(n));
            animator.avatar = avatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            PlayerAnimationController animationController = newModel.AddComponent<PlayerAnimationController>();
            SerializedObject animationSO = new SerializedObject(animationController);
            animationSO.FindProperty("animator").objectReferenceValue = animator;
            animationSO.FindProperty("walkClipSpeed").floatValue = walkClipSpeed;
            animationSO.ApplyModifiedPropertiesWithoutUndo();

            modelProperty.objectReferenceValue = newModel;
            selectorSO.ApplyModifiedPropertiesWithoutUndo();

            Object.DestroyImmediate(oldModel);

            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab);

            log.AppendLine($"[LOCO{n}] Player {n} usa ahora {IdleFbx(n)} como visual ('{newModel.name}', escala {newModel.transform.localScale}, " +
                           $"activo={newModel.activeSelf}). Skinned={newModel.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length} " +
                           $"walkClipSpeed={walkClipSpeed:0.000} materiales: " +
                           string.Join(", ", newModel.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Select(m => m != null ? m.name : "null").Distinct()));
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }


    // =====================================================
    // PRUEBAS EN PLAY MODE
    // =====================================================

    // El jugador N camina por el centro de la carpa; otro jugador lo mira de lado.
    [MenuItem("TSirkus/Locomocion/Probar P1")] private static void Test1() => TestPlayer(1);
    [MenuItem("TSirkus/Locomocion/Probar P2")] private static void Test2() => TestPlayer(2);
    [MenuItem("TSirkus/Locomocion/Probar P3")] private static void Test3() => TestPlayer(3);
    [MenuItem("TSirkus/Locomocion/Probar P4")] private static void Test4() => TestPlayer(4);

    private static PlayerIdentity[] Players() => Object.FindObjectsByType<PlayerIdentity>()
        .Where(p => p.GetComponentInChildren<Camera>(true) != null).OrderBy(p => p.PlayerId).ToArray();

    private static void Place(PlayerIdentity player, Vector3 position, Vector3 lookAt)
    {
        CharacterController body = player.GetComponent<CharacterController>();
        position.y = player.transform.position.y;

        body.enabled = false;
        player.transform.position = position;
        Vector3 flat = lookAt - position;
        flat.y = 0f;
        player.transform.rotation = Quaternion.LookRotation(flat);
        body.enabled = true;

        Camera cam = player.GetComponentInChildren<Camera>(true);
        Vector3 to = lookAt - cam.transform.position;
        float pitch = -Mathf.Atan2(to.y, new Vector2(to.x, to.z).magnitude) * Mathf.Rad2Deg;

        PlayerLook look = player.GetComponent<PlayerLook>();
        typeof(PlayerLook).GetField("verticalRotation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(look, pitch);
        (new SerializedObject(look).FindProperty("cameraHolder").objectReferenceValue as Transform).localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private static string State(PlayerIdentity player)
    {
        PlayerAnimationController visual = player.GetComponentInChildren<PlayerAnimationController>();

        if (visual == null)
            return $"P{player.PlayerId}: (sin PlayerAnimationController)";

        Animator animator = visual.GetComponent<Animator>();
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        Transform hips = visual.GetComponentsInChildren<Transform>().First(t => t.name == "mixamorig:Hips");
        Vector3 hipsModel = visual.transform.InverseTransformPoint(hips.position);

        return $"P{player.PlayerId}: {(state.IsName("Walk") ? "Walk" : state.IsName("Idle") ? "Idle" : "?")} " +
               $"Speed={animator.GetFloat("Speed"):0.00} ciclo={state.normalizedTime:0.00} hipsXZ=({hipsModel.x:0.00},{hipsModel.z:0.00}) " +
               $"rootMotion={animator.applyRootMotion} visual='{visual.name}'";
    }

    private static void TestPlayer(int n)
    {
        PlayerIdentity[] players = Players();
        PlayerIdentity target = players.First(p => p.PlayerId == n);
        PlayerIdentity observer = players.First(p => p.PlayerId == n % 4 + 1);

        // Carpa: el jugador camina hacia +Z por el centro; el observador mira desde un lado.
        Place(target, new Vector3(0f, 0f, -8f), new Vector3(0f, 1f, 10f));
        Place(observer, new Vector3(7f, 0f, -3f), new Vector3(0f, 1.2f, -4f));

        Vector3 mark = target.transform.position;

        void Log(string label)
        {
            Debug.Log($"[LOCO TEST P{n}] {label} | desplazado={Vector3.Distance(mark, target.transform.position):0.00}m | " +
                      string.Join(" || ", players.Select(State)));
            mark = target.transform.position;
        }

        PlayerDiagnosticsTools.RunSteps(
            (2.0, () => Log("quieto")),
            (0, () => PlayerDiagnosticsTools.SetInput(n, "lookX", 1f)),
            (0.8, () => { PlayerDiagnosticsTools.SetInput(n, "lookX", 0f); Log("gira cámara quieto"); }),
            (0, () => Place(target, target.transform.position, target.transform.position + Vector3.forward * 10f)),
            (0, () => PlayerDiagnosticsTools.SetInput(n, "movementY", 1f)),
            (0.4, () => Log("empieza a caminar")),
            (0.8, () => Log("varios ciclos (a)")),
            (0.8, () => Log("varios ciclos (b)")),
            (0, () => PlayerDiagnosticsTools.SetInput(n, "lookX", 0.6f)),
            (0.5, () => { PlayerDiagnosticsTools.SetInput(n, "lookX", 0f); Log("camina y gira"); }),
            (0, () => PlayerDiagnosticsTools.SetInput(n, "movementY", 0f)),
            (0.5, () => Log("se detiene")),
            (1.5, () => Log("quieto 1,5 s (no se desplaza solo)")));
    }


    // Parada: suelta el movimiento en distintos puntos del ciclo de Walk y mide
    // cuándo se para el Player, cuándo empieza la transición a Idle y cuándo termina.
    [MenuItem("TSirkus/Locomocion/Probar parada de P1")] private static void Stop1() => TestStop(1);
    [MenuItem("TSirkus/Locomocion/Probar parada de P2")] private static void Stop2() => TestStop(2);
    [MenuItem("TSirkus/Locomocion/Probar parada de P3")] private static void Stop3() => TestStop(3);
    [MenuItem("TSirkus/Locomocion/Probar parada de P4")] private static void Stop4() => TestStop(4);

    private static void TestStop(int n)
    {
        PlayerIdentity target = Players().First(p => p.PlayerId == n);
        Animator animator = target.GetComponentInChildren<PlayerAnimationController>().GetComponent<Animator>();
        double[] walkTimes = { 1.0, 1.17, 1.34, 1.51 };

        // Los demás a un lateral de la carpa: el carril central queda libre.
        foreach (PlayerIdentity other in Players().Where(p => p != target))
            Place(other, new Vector3(-8f, 0f, -9f + 2.5f * other.PlayerId), new Vector3(0f, 1f, -5f));
        int trial = 0;

        void StartTrial()
        {
            Place(target, new Vector3(0f, 0f, -9f), new Vector3(0f, 1f, 10f));
            PlayerDiagnosticsTools.RunSteps(
                (0.8, () => PlayerDiagnosticsTools.SetInput(n, "movementY", 1f)),
                (walkTimes[trial], Release));
        }

        void Release()
        {
            PlayerDiagnosticsTools.SetInput(n, "movementY", 0f);

            double t0 = EditorApplication.timeSinceStartup;
            float phase = animator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1f;
            Vector3 last = target.transform.position;
            double stopped = -1, transition = -1, idle = -1;

            EditorApplication.CallbackFunction watch = null;
            watch = () =>
            {
                if (!EditorApplication.isPlaying) { EditorApplication.update -= watch; return; }

                double ms = (EditorApplication.timeSinceStartup - t0) * 1000.0;

                if (stopped < 0 && (target.transform.position - last).sqrMagnitude < 1e-8) stopped = ms;
                last = target.transform.position;

                if (transition < 0 && (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName("Idle")
                                       || animator.GetCurrentAnimatorStateInfo(0).IsName("Idle")))
                    transition = ms;

                if (idle < 0 && !animator.IsInTransition(0) && animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
                    idle = ms;

                if (idle >= 0 || ms > 2000)
                {
                    EditorApplication.update -= watch;
                    Debug.Log($"[PARADA P{n}] ensayo {trial + 1}: soltado en fase {phase:0.00} del ciclo → " +
                              $"Player parado {stopped:0}ms | transición a Idle {transition:0}ms | Idle completo {idle:0}ms");

                    if (++trial < walkTimes.Length)
                        PlayerDiagnosticsTools.RunSteps((0.8, StartTrial));
                }
            };

            EditorApplication.update += watch;
        }

        StartTrial();
    }


    // Dos fases: P1+P3 caminan / P2+P4 quietos, y luego al revés.
    [MenuItem("TSirkus/Locomocion/Probar simultaneo P1-P4")]
    private static void TestSimultaneous()
    {
        PlayerIdentity[] players = Players();
        float[] x = { 0f, -4.5f, -1.5f, 1.5f, 4.5f };

        foreach (PlayerIdentity p in players)
            Place(p, new Vector3(x[p.PlayerId], 0f, -9f), new Vector3(x[p.PlayerId], 1f, 10f));

        void Walk(int id, bool on) => PlayerDiagnosticsTools.SetInput(id, "movementY", on ? 1f : 0f);
        void Log(string label) => Debug.Log($"[LOCO SIM] {label} → " + string.Join(" || ", players.Select(State)));

        PlayerDiagnosticsTools.RunSteps(
            (1.5, () => Log("todos quietos")),
            (0, () => { Walk(1, true); Walk(3, true); }),
            (1.0, () => Log("fase A: P1+P3 caminan, P2+P4 quietos")),
            (0, () => { Walk(1, false); Walk(3, false); }),
            (1.0, () => Log("pausa (todos deben volver a Idle)")),
            (0, () => { Walk(2, true); Walk(4, true); }),
            (1.0, () => Log("fase B: P2+P4 caminan, P1+P3 quietos")),
            (0, () => { Walk(2, false); Walk(4, false); }),
            (1.0, () => Log("final (todos Idle)")));
    }


    [MenuItem("TSirkus/Locomocion/Probar P1", true)]
    [MenuItem("TSirkus/Locomocion/Probar P2", true)]
    [MenuItem("TSirkus/Locomocion/Probar P3", true)]
    [MenuItem("TSirkus/Locomocion/Probar P4", true)]
    [MenuItem("TSirkus/Locomocion/Probar simultaneo P1-P4", true)]
    [MenuItem("TSirkus/Locomocion/Probar parada de P1", true)]
    [MenuItem("TSirkus/Locomocion/Probar parada de P2", true)]
    [MenuItem("TSirkus/Locomocion/Probar parada de P3", true)]
    [MenuItem("TSirkus/Locomocion/Probar parada de P4", true)]
    private static bool PlayModeOnly() => EditorApplication.isPlaying;

    [MenuItem("TSirkus/Locomocion/Configurar Personaje2", true)]
    [MenuItem("TSirkus/Locomocion/Configurar Personaje3", true)]
    [MenuItem("TSirkus/Locomocion/Configurar Personaje4", true)]
    private static bool EditModeOnly() => !EditorApplication.isPlaying;
}
