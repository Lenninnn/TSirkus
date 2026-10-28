using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

// Monta el audio de TSirkus de forma reproducible: carpetas, cues (con el archivo esperado
// documentado), ajustes de importación no destructivos, y conexiones en GAME y prefabs.
// Idempotente: lo que ya existe no se duplica.
public static class AudioSetupTools
{
    private const string Root = "Assets/SFX";
    private const string CueFolder = Root + "/Cues";
    private const string PlayerPrefab = "Assets/Prefabs/Players/Player Variant.prefab";
    private const string ClownPrefab = "Assets/Prefabs/Enemies/Payaso.prefab";

    private static readonly (string folder, string description)[] Folders =
    {
        ("Ambience", "Loops de ambiente (AMB_*): carpa, corredores, zonas de prueba."),
        ("Player", "Sonidos de los jugadores: pasos, captura, rescate."),
        ("Environment", "Puertas, estructuras, mecanismos del mapa."),
        ("Interactions", "Interruptores, palancas, puzzles, tickets, globos."),
        ("Enemies", "Payaso: aparición, persecución, captura, desaparición."),
        ("UI", "Objetivos, confirmaciones, menú. Discretos."),
        ("Stings", "Golpes de tensión globales (peligro, revelaciones)."),
        ("Cinematics", "Solo si una cinemática NO trae audio propio (el video de la puerta ya lo trae)."),
    };

    // Clips existentes → nombre normalizado (MoveAsset conserva el GUID).
    private static readonly (string from, string to)[] ExistingClips =
    {
        (Root + "/sonido-pasos personajes.mp3", Root + "/Player/SFX_Player_Footstep_01.mp3"),
        (Root + "/sonido-horror.mp3", Root + "/Stings/SFX_Sting_Horror_01.mp3"),
    };

    private class Spec
    {
        public string name, file, search, clip;
        public SoundCue.Category category = SoundCue.Category.SFX;
        public bool global;
        public float volume = 0.8f, min = 2f, max = 20f, start, interval = 0.05f, variation = 0.05f;
        public Vector2 pitch = Vector2.one;
    }

    private static readonly Spec[] Cues =
    {
        // Ya funcionando con los clips actuales
        new Spec { name = "SFX_Player_Footstep", clip = Root + "/Player/SFX_Player_Footstep_01.mp3", category = SoundCue.Category.Player,
                   volume = 0.85f, min = 2f, max = 14f, start = 0.34f, interval = 0f, variation = 0.12f, pitch = new Vector2(0.94f, 1.06f),
                   file = "Player/SFX_Player_Footstep_01..04.wav", search = "footstep wooden floor old boards single step (varias tomas)" },
        new Spec { name = "SFX_Sting_Danger", clip = Root + "/Stings/SFX_Sting_Horror_01.mp3", global = true, volume = 0.7f, interval = 4f,
                   file = "Stings/SFX_Sting_Horror_01.mp3 (ya existe)", search = "horror riser sting short" },

        // Pendientes (referencias vacías)
        new Spec { name = "SFX_Switch_Activate", min = 2f, max = 18f, volume = 0.8f, pitch = new Vector2(0.97f, 1.03f),
                   file = "Interactions/SFX_Switch_Activate_01.wav", search = "old metal lever pull clunk mechanical switch" },
        new Spec { name = "SFX_Switch_Denied", min = 2f, max = 10f, volume = 0.45f, interval = 0.4f,
                   file = "Interactions/SFX_Switch_Denied_01.wav", search = "locked mechanism rattle short metal click denied" },
        new Spec { name = "SFX_Puzzle_Complete", global = true, volume = 0.6f, interval = 2f,
                   file = "Interactions/SFX_Puzzle_Complete_01.wav", search = "heavy mechanism engage clunk gears lock (corto, <1,5 s)" },
        new Spec { name = "SFX_Door_FirstDoor_End", min = 4f, max = 30f, volume = 0.7f, interval = 2f,
                   file = "Environment/SFX_Door_FirstDoor_End_01.wav", search = "heavy wooden door settle thud dust (la cinemática ya trae la apertura)" },
        new Spec { name = "SFX_Door_HeavyOpen", min = 4f, max = 30f, volume = 0.8f, interval = 1f,
                   file = "Environment/SFX_Door_HeavyOpen_01.wav", search = "heavy old wooden metal door creak open horror" },
        new Spec { name = "SFX_Door_Close", min = 4f, max = 30f, volume = 0.8f, interval = 1f,
                   file = "Environment/SFX_Door_Close_01.wav", search = "heavy wooden door slam close old" },
        new Spec { name = "SFX_Door_Locked", min = 2f, max = 12f, volume = 0.6f, interval = 0.5f,
                   file = "Environment/SFX_Door_Locked_01.wav", search = "door handle locked rattle old" },
        new Spec { name = "SFX_Ticket_Pickup", min = 2f, max = 10f, volume = 0.7f, pitch = new Vector2(0.97f, 1.04f),
                   file = "Interactions/SFX_Ticket_Pickup_01.wav", search = "paper ticket pickup carnival tiny music box ding dark" },
        new Spec { name = "SFX_Player_Captured", min = 3f, max = 25f, volume = 0.85f, interval = 1f,
                   file = "Player/SFX_Player_Captured_01.wav", search = "body grabbed struggle gasp horror" },
        new Spec { name = "SFX_Player_Rescued", min = 3f, max = 20f, volume = 0.7f, interval = 1f,
                   file = "Player/SFX_Player_Rescued_01.wav", search = "chains release relief breath" },
        new Spec { name = "SFX_Clown_Appear", min = 3f, max = 25f, volume = 0.85f, interval = 2f,
                   file = "Enemies/SFX_Clown_Appear_01.wav", search = "creepy clown giggle distant horror" },
        new Spec { name = "SFX_Clown_ChaseLoop", min = 3f, max = 20f, volume = 0.6f,
                   file = "Enemies/SFX_Clown_ChaseLoop_01.wav", search = "heavy footsteps running loop + breathing (loop sin cortes)" },
        new Spec { name = "SFX_Clown_Capture", min = 3f, max = 25f, volume = 0.9f, interval = 1f,
                   file = "Enemies/SFX_Clown_Capture_01.wav", search = "monster grab attack hit horror" },
        new Spec { name = "SFX_Clown_Disappear", min = 3f, max = 25f, volume = 0.7f, interval = 1f,
                   file = "Enemies/SFX_Clown_Disappear_01.wav", search = "whoosh vanish dark reverse" },
        new Spec { name = "SFX_UI_Objective", category = SoundCue.Category.UI, global = true, volume = 0.5f, interval = 1f,
                   file = "UI/SFX_UI_Objective_01.wav", search = "subtle dark chime objective music box single note" },
        new Spec { name = "AMB_Circus_Global", category = SoundCue.Category.Ambience, global = true, volume = 0.35f,
                   file = "Ambience/AMB_Circus_Global_01.wav", search = "abandoned circus tent wind canvas creak ambience loop" },
        new Spec { name = "AMB_Tent_Main", category = SoundCue.Category.Ambience, volume = 0.5f,
                   file = "Ambience/AMB_Tent_Main_01.wav", search = "big tent interior wind canvas flapping metal creak loop" },
        new Spec { name = "AMB_Hallway", category = SoundCue.Category.Ambience, volume = 0.45f,
                   file = "Ambience/AMB_Hallway_01.wav", search = "old wooden corridor creaks low drone loop" },
    };


    // =====================================================
    // 1) CARPETAS, CLIPS Y CUES
    // =====================================================

    [MenuItem("TSirkus/Audio/1. Crear carpetas, cues y ajustes de importación")]
    private static void CreateAssets()
    {
        foreach ((string folder, string description) in Folders)
        {
            string path = $"{Root}/{folder}";

            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(Root, folder);

            string readme = $"{path}/LEEME.txt";

            if (!File.Exists(readme))
                File.WriteAllText(readme, description + "\nNombre: SFX_[Sistema]_[Acción]_[NN] / AMB_[Zona]_[NN]. Ver Assets/SFX/LEEME_AUDIO.md\n");
        }

        if (!AssetDatabase.IsValidFolder(CueFolder))
            AssetDatabase.CreateFolder(Root, "Cues");

        AssetDatabase.Refresh();

        // Normaliza los dos clips existentes (sin tocar su contenido; el GUID se conserva).
        foreach ((string from, string to) in ExistingClips)
        {
            if (File.Exists(from) && !File.Exists(to))
            {
                string error = AssetDatabase.MoveAsset(from, to);
                if (!string.IsNullOrEmpty(error)) Debug.LogError($"[AUDIO] {from}: {error}");
            }
        }

        ConfigureImport(Root + "/Player/SFX_Player_Footstep_01.mp3", forceMono: true);
        ConfigureImport(Root + "/Stings/SFX_Sting_Horror_01.mp3", forceMono: false);

        foreach (Spec spec in Cues)
            CreateCue(spec);

        AssetDatabase.SaveAssets();
        Debug.Log($"[AUDIO] Carpetas, {Cues.Length} cues y ajustes de importación listos en {Root}.");
    }


    // Solo ajustes del importador (.meta): el archivo original no se modifica.
    private static void ConfigureImport(string path, bool forceMono)
    {
        if (!(AssetImporter.GetAtPath(path) is AudioImporter importer))
            return;

        importer.forceToMono = forceMono;
        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad;
        settings.preloadAudioData = true;
        importer.defaultSampleSettings = settings;
        importer.SaveAndReimport();
    }


    private static void CreateCue(Spec spec)
    {
        string path = $"{CueFolder}/{spec.name}.asset";
        SoundCue cue = AssetDatabase.LoadAssetAtPath<SoundCue>(path);
        bool created = cue == null;

        if (created)
            cue = ScriptableObject.CreateInstance<SoundCue>();

        AudioClip clip = spec.clip != null ? AssetDatabase.LoadAssetAtPath<AudioClip>(spec.clip) : null;

        if (clip != null && (cue.clips == null || cue.clips.Length == 0 || cue.clips[0] == null))
            cue.clips = new[] { clip };

        cue.category = spec.category;
        cue.global = spec.global;
        cue.volume = spec.volume;
        cue.volumeVariation = spec.variation;
        cue.pitchRange = spec.pitch;
        cue.minDistance = spec.min;
        cue.maxDistance = spec.max;
        cue.startTime = spec.start;
        cue.minInterval = spec.interval;
        cue.expectedFile = $"Assets/SFX/{spec.file}";
        cue.searchTerms = spec.search;

        if (created) AssetDatabase.CreateAsset(cue, path);
        else EditorUtility.SetDirty(cue);
    }


    private static SoundCue Cue(string name) => AssetDatabase.LoadAssetAtPath<SoundCue>($"{CueFolder}/{name}.asset");


    // =====================================================
    // 2) ESCENA GAME Y PREFABS
    // =====================================================

    [MenuItem("TSirkus/Audio/2. Conectar audio en GAME y prefabs")]
    private static void WireScene()
    {
        var scene = EditorSceneManager.GetActiveScene();

        if (scene.name != "GAME" || EditorApplication.isPlaying)
        {
            Debug.LogError("[AUDIO] Abre GAME y sal de Play Mode.");
            return;
        }

        DoorCinematicController cinematic = Object.FindAnyObjectByType<DoorCinematicController>(FindObjectsInactive.Include);
        CanvasGroup overlay = new SerializedObject(cinematic).FindProperty("cinematicOverlay").objectReferenceValue as CanvasGroup;

        // GameAudio + ambientes (sin clip no crean AudioSource).
        GameObject audioRoot = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "GameAudio")
                               ?? new GameObject("GameAudio");
        GameAudio gameAudio = GetOrAdd<GameAudio>(audioRoot);

        SerializedObject audioSO = new SerializedObject(gameAudio);
        SerializedProperty duck = audioSO.FindProperty("duckWhileVisible");
        duck.arraySize = 1;
        duck.GetArrayElementAtIndex(0).objectReferenceValue = overlay;
        audioSO.ApplyModifiedPropertiesWithoutUndo();

        Ambience(audioRoot, "Ambience_Global", "AMB_Circus_Global", true, Vector3.zero, 0f, 1f);
        Ambience(audioRoot, "Ambience_Carpa", "AMB_Tent_Main", false, new Vector3(0f, 1.5f, 0f), 10f, 6f);
        Ambience(audioRoot, "Ambience_Corredor", "AMB_Hallway", false, new Vector3(0f, 1.5f, -23f), 5f, 6f);

        // Cinemática de la puerta: el MP4 YA trae audio; se enruta por un AudioSource 2D
        // (antes estaba en 'None', es decir, silenciado). No se añade ningún otro sonido encima.
        VideoPlayer video = new SerializedObject(cinematic).FindProperty("videoPlayer").objectReferenceValue as VideoPlayer;
        AudioSource videoSource = GetOrAdd<AudioSource>(video.gameObject);
        videoSource.playOnAwake = false;
        videoSource.spatialBlend = 0f;
        videoSource.volume = 0.85f;
        video.audioOutputMode = VideoAudioOutputMode.AudioSource;
        video.controlledAudioTrackCount = 1;
        video.EnableAudioTrack(0, true);
        video.SetTargetAudioSource(0, videoSource);
        EditorUtility.SetDirty(video);

        // Interruptores de la primera prueba: sonido 3D en cada uno.
        foreach (PuzzleSwitch puzzleSwitch in Object.FindObjectsByType<PuzzleSwitch>(FindObjectsInactive.Exclude))
        {
            SoundEmitter emitter = GetOrAdd<SoundEmitter>(puzzleSwitch.gameObject);
            AddCueListener(puzzleSwitch.onActivated, emitter.PlayCue, Cue("SFX_Switch_Activate"), puzzleSwitch);
            AddCueListener(puzzleSwitch.onRejected, emitter.PlayCue, Cue("SFX_Switch_Denied"), puzzleSwitch);
        }

        // Puzzle completado (global: arranca a la vez que la cinemática a pantalla completa).
        PuzzleController firstPuzzle = Object.FindObjectsByType<PuzzleController>(FindObjectsInactive.Include)
            .First(p => p.name == "Puzzle_Test");
        AddCueListener(firstPuzzle.onPuzzleCompleted, gameAudio.PlayCue, Cue("SFX_Puzzle_Complete"), firstPuzzle);

        // Puerta de la primera prueba: final de apertura al terminar el flujo + objetivo UI.
        PuzzleDoor door = Object.FindAnyObjectByType<PuzzleDoor>(FindObjectsInactive.Include);
        DoorAudio doorAudio = GetOrAdd<DoorAudio>(door.gameObject);
        SerializedObject doorSO = new SerializedObject(doorAudio);
        doorSO.FindProperty("openCue").objectReferenceValue = Cue("SFX_Door_FirstDoor_End");
        doorSO.FindProperty("closeCue").objectReferenceValue = Cue("SFX_Door_Close");
        doorSO.FindProperty("lockedCue").objectReferenceValue = Cue("SFX_Door_Locked");
        doorSO.ApplyModifiedPropertiesWithoutUndo();

        if (!HasListener(cinematic.onDoorFlowFinished, doorAudio, nameof(DoorAudio.PlayOpen)))
            UnityEventTools.AddVoidPersistentListener(cinematic.onDoorFlowFinished, doorAudio.PlayOpen);

        AddCueListener(cinematic.onDoorFlowFinished, gameAudio.PlayCue, Cue("SFX_UI_Objective"), cinematic);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        WirePlayerPrefab();
        WireClownPrefab();

        Debug.Log("[AUDIO] GAME y prefabs conectados. Escena guardada.");
    }


    private static void Ambience(GameObject parent, string name, string cue, bool global, Vector3 position, float radius, float fade)
    {
        Transform existing = parent.transform.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name);
        go.transform.SetParent(parent.transform, true);
        go.transform.position = position;

        AmbienceZone zone = GetOrAdd<AmbienceZone>(go);
        SerializedObject so = new SerializedObject(zone);
        so.FindProperty("loopCue").objectReferenceValue = Cue(cue);
        so.FindProperty("global").boolValue = global;
        so.FindProperty("innerRadius").floatValue = radius;
        so.FindProperty("fadeDistance").floatValue = fade;
        so.ApplyModifiedPropertiesWithoutUndo();
    }


    private static void AddCueListener(UnityEvent unityEvent, UnityAction<SoundCue> action, SoundCue cue, Object owner)
    {
        if (cue == null || HasListener(unityEvent, action.Target as Object, action.Method.Name))
            return;

        UnityEventTools.AddObjectPersistentListener(unityEvent, action, cue);
        EditorUtility.SetDirty(owner);
    }


    private static bool HasListener(UnityEventBase unityEvent, Object target, string method)
    {
        for (int i = 0; i < unityEvent.GetPersistentEventCount(); i++)
        {
            if (unityEvent.GetPersistentTarget(i) == target && unityEvent.GetPersistentMethodName(i) == method)
                return true;
        }

        return false;
    }


    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        return go.TryGetComponent(out T component) ? component : go.AddComponent<T>();
    }


    // Pasos en cada visual con rig (fases de apoyo calculadas del clip Walk) + eventos del jugador.
    private static void WirePlayerPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefab);

        try
        {
            PlayerEventAudio events = GetOrAdd<PlayerEventAudio>(root);
            SerializedObject eventsSO = new SerializedObject(events);
            eventsSO.FindProperty("capturedCue").objectReferenceValue = Cue("SFX_Player_Captured");
            eventsSO.FindProperty("capturedStingCue").objectReferenceValue = Cue("SFX_Sting_Danger");
            eventsSO.FindProperty("rescuedCue").objectReferenceValue = Cue("SFX_Player_Rescued");
            eventsSO.FindProperty("ticketPickupCue").objectReferenceValue = Cue("SFX_Ticket_Pickup");
            eventsSO.ApplyModifiedPropertiesWithoutUndo();

            foreach (PlayerAnimationController visual in root.GetComponentsInChildren<PlayerAnimationController>(true))
            {
                Animator animator = visual.GetComponent<Animator>();
                PlayerFootsteps footsteps = GetOrAdd<PlayerFootsteps>(visual.gameObject);
                float[] phases = FootContactPhases(animator);

                SerializedObject so = new SerializedObject(footsteps);
                so.FindProperty("animator").objectReferenceValue = animator;
                so.FindProperty("footstepCue").objectReferenceValue = Cue("SFX_Player_Footstep");
                SerializedProperty phasesProp = so.FindProperty("stepPhases");
                phasesProp.arraySize = phases.Length;
                for (int i = 0; i < phases.Length; i++) phasesProp.GetArrayElementAtIndex(i).floatValue = phases[i];
                so.ApplyModifiedPropertiesWithoutUndo();

                Debug.Log($"[AUDIO] Pasos en '{visual.name}': apoyos en fases {string.Join(", ", phases.Select(p => p.ToString("0.00")))}");
            }

            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }


    // Momento del ciclo Walk en que cada pie está más bajo (apoyo).
    private static float[] FootContactPhases(Animator animator)
    {
        var controller = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
        AnimationClip walk = controller.layers[0].stateMachine.states
            .First(s => s.state.name == "Walk").state.motion as AnimationClip;

        GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(animator.gameObject) ?? animator.gameObject;
        GameObject instance = Object.Instantiate(source);
        instance.hideFlags = HideFlags.HideAndDontSave;

        try
        {
            Transform left = instance.GetComponentsInChildren<Transform>().First(t => t.name == "mixamorig:LeftFoot");
            Transform right = instance.GetComponentsInChildren<Transform>().First(t => t.name == "mixamorig:RightFoot");

            float Lowest(Transform foot)
            {
                float best = 0f, minY = float.MaxValue;

                for (int i = 0; i < 60; i++)
                {
                    float t = i / 60f;
                    walk.SampleAnimation(instance, t * walk.length);

                    if (foot.position.y < minY)
                    {
                        minY = foot.position.y;
                        best = t;
                    }
                }

                return best;
            }

            // El punto más bajo del pie se contamina con el balanceo de la cadera; el ciclo es
            // simétrico, así que el otro apoyo va medio ciclo después del pie izquierdo.
            float leftContact = Lowest(left);
            return new[] { leftContact, (leftContact + 0.5f) % 1f }.OrderBy(p => p).ToArray();
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }


    private static void WireClownPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(ClownPrefab);

        try
        {
            ClownAudio clown = GetOrAdd<ClownAudio>(root);
            SerializedObject so = new SerializedObject(clown);
            so.FindProperty("appearCue").objectReferenceValue = Cue("SFX_Clown_Appear");
            so.FindProperty("disappearCue").objectReferenceValue = Cue("SFX_Clown_Disappear");
            so.FindProperty("chaseLoopCue").objectReferenceValue = Cue("SFX_Clown_ChaseLoop");
            so.FindProperty("captureCue").objectReferenceValue = Cue("SFX_Clown_Capture");
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, ClownPrefab);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }


    // =====================================================
    // PRUEBA EN PLAY MODE
    // =====================================================

    // Cada jugador camina solo, luego dos a la vez, y P1 pasa junto a P4 (atenuación).
    [MenuItem("TSirkus/Audio/Probar pasos (Play Mode)")]
    private static void TestFootsteps()
    {
        PlayerIdentity[] players = Object.FindObjectsByType<PlayerIdentity>()
            .Where(p => p.GetComponentInChildren<Camera>(true) != null).OrderBy(p => p.PlayerId).ToArray();
        PlayerFootsteps Steps(int id) => players.First(p => p.PlayerId == id).GetComponentInChildren<PlayerFootsteps>();

        // Esquinas de la carpa, lejos entre sí, mirando hacia dentro.
        Vector3[] spots = { default, new Vector3(-6f, 0f, -7f), new Vector3(6f, 0f, -7f), new Vector3(-6f, 0f, 2f), new Vector3(6f, 0f, 2f) };

        void PlaceAll()
        {
            foreach (PlayerIdentity p in players)
                Teleport(p, spots[p.PlayerId], p.PlayerId <= 2 ? Vector3.forward : Vector3.back);
        }

        int[] counts = new int[5];
        void Mark() { for (int id = 1; id <= 4; id++) counts[id] = Steps(id).StepCount; }

        void Log(string label) => Debug.Log($"[AUDIO TEST] {label} → " + string.Join(" | ", Enumerable.Range(1, 4).Select(id =>
            $"P{id}: +{Steps(id).StepCount - counts[id]} pasos sonando={Steps(id).IsSounding} vol={Steps(id).LastStepVolume:0.00}")) +
            $" | AudioSources en escena={Object.FindObjectsByType<AudioSource>().Length}");

        void Walk(int id, bool on) => PlayerDiagnosticsTools.SetInput(id, "movementY", on ? 1f : 0f);

        PlayerDiagnosticsTools.RunSteps(
            (0, PlaceAll),
            (1.0, () => { Mark(); Log("todos quietos 1 s"); Mark(); }),
            (0, () => Walk(1, true)),
            (2.0, () => { Walk(1, false); Log("P1 caminó 2 s solo"); }),
            (0.05, () => { Log("P1 recién detenido (no debe sonar)"); Mark(); }),
            (0.6, PlaceAll),
            (0, () => Walk(2, true)),
            (2.0, () => { Walk(2, false); Log("P2 caminó 2 s solo"); Mark(); }),
            (0.6, PlaceAll),
            (0, () => { Walk(3, true); Walk(4, true); }),
            (2.0, () => { Walk(3, false); Walk(4, false); Log("P3 y P4 caminaron a la vez"); Mark(); }),
            (0.6, () => { PlaceAll(); Teleport(players.First(p => p.PlayerId == 4), spots[1] + new Vector3(1.5f, 0f, 1f), Vector3.forward); }),
            (0.5, () => Walk(1, true)),
            (1.5, () => { Walk(1, false); Log("P1 caminó con P4 al lado (volumen debe subir)"); }));
    }

    // Completa la primera prueba y mide la señal que realmente sale del video y del listener.
    [MenuItem("TSirkus/Audio/Probar audio de la cinematica (Play Mode)")]
    private static void TestCinematicAudio()
    {
        DoorCinematicController cinematic = Object.FindAnyObjectByType<DoorCinematicController>();
        VideoPlayer video = new SerializedObject(cinematic).FindProperty("videoPlayer").objectReferenceValue as VideoPlayer;
        AudioSource videoSource = video.GetTargetAudioSource(0);
        FourSwitchPuzzleController puzzle = Object.FindAnyObjectByType<FourSwitchPuzzleController>();

        float Rms(System.Action<float[]> read)
        {
            float[] data = new float[512];
            read(data);
            return Mathf.Sqrt(data.Select(v => v * v).Average());
        }

        void Log(string label) => Debug.Log(
            $"[AUDIO TEST] {label} → video.isPlaying={video.isPlaying} salida={video.audioOutputMode} " +
            $"fuente.isPlaying={(videoSource != null && videoSource.isPlaying)} " +
            $"RMS fuente={(videoSource != null ? Rms(d => videoSource.GetOutputData(d, 0)) : 0f):0.0000} " +
            $"RMS listener={Rms(d => AudioListener.GetOutputData(d, 0)):0.0000} " +
            $"listeners activos={Object.FindObjectsByType<AudioListener>().Count(l => l.isActiveAndEnabled)} " +
            $"MuteAudio(Game view)={EditorUtility.audioMasterMute}");

        foreach (PlayerIdentity player in Object.FindObjectsByType<PlayerIdentity>()
                     .Where(p => p.GetComponentInChildren<Camera>(true) != null).OrderBy(p => p.PlayerId))
            puzzle.RegisterPlayer(player);

        PlayerDiagnosticsTools.RunSteps(
            (2.0, () => Log("cinemática +2 s")),
            (2.0, () => Log("cinemática +4 s")),
            (2.0, () => Log("cinemática +6 s")),
            (6.0, () => Log("después de la cinemática")));
    }

    [MenuItem("TSirkus/Audio/Probar audio de la cinematica (Play Mode)", true)]
    private static bool PlayModeOnly2() => EditorApplication.isPlaying;

    private static void Teleport(PlayerIdentity player, Vector3 position, Vector3 forward)
    {
        CharacterController body = player.GetComponent<CharacterController>();
        position.y = player.transform.position.y;
        body.enabled = false;
        player.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward));
        body.enabled = true;
    }

    [MenuItem("TSirkus/Audio/Probar pasos (Play Mode)", true)]
    private static bool PlayModeOnly() => EditorApplication.isPlaying;
}
