using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

// Diagnóstico de los jugadores en Play Mode. "Comparar" es de solo lectura;
// las pruebas mueven jugadores y simulan input del celular solo en el editor.
public static class PlayerDiagnosticsTools
{
    [MenuItem("TSirkus/Diagnostico/Comparar jugadores (Play Mode)")]
    private static void ComparePlayers()
    {
        PlayerIdentity[] identities = Object.FindObjectsByType<PlayerIdentity>(FindObjectsInactive.Include)
            .OrderBy(p => p.PlayerId)
            .ToArray();

        Camera[] playerCameras = identities
            .Select(p => p.GetComponentInChildren<Camera>(true))
            .Where(c => c != null)
            .ToArray();

        foreach (PlayerIdentity player in identities)
        {
            StringBuilder log = new StringBuilder();
            log.AppendLine($"[DIAG] ===== {player.name} | PlayerId={player.PlayerId} | activo={player.gameObject.activeInHierarchy} | pos={player.transform.position}");

            Camera cam = player.GetComponentInChildren<Camera>(true);
            log.AppendLine(cam == null
                ? "[DIAG]   Cámara: ninguna"
                : $"[DIAG]   Cámara: {cam.name} enabled={cam.enabled} rect={cam.rect} cullingMask=0x{cam.cullingMask:X8}");

            // Modelos (PlayerModelSelector)
            PlayerModelSelector selector = player.GetComponent<PlayerModelSelector>();

            if (selector != null)
            {
                SerializedObject so = new SerializedObject(selector);

                for (int i = 1; i <= 4; i++)
                {
                    GameObject model = so.FindProperty($"modelPlayer{i}").objectReferenceValue as GameObject;

                    if (model == null)
                    {
                        log.AppendLine($"[DIAG]   modelPlayer{i}: (sin asignar)");
                        continue;
                    }

                    Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
                    log.Append($"[DIAG]   modelPlayer{i}: {model.name} activeSelf={model.activeSelf} activeInHierarchy={model.activeInHierarchy} " +
                               $"renderers={renderers.Length} enabled={renderers.Count(r => r.enabled)} " +
                               $"capas={string.Join(",", renderers.Select(r => LayerMask.LayerToName(r.gameObject.layer)).Distinct())} " +
                               $"sombrasSolo={renderers.Count(r => r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly)}");

                    if (model.activeInHierarchy && renderers.Length > 0)
                    {
                        Bounds bounds = renderers[0].bounds;
                        foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);

                        log.Append($" bounds={bounds.center}/{bounds.size} isVisible={renderers.Any(r => r.isVisible)}");

                        foreach (Camera other in playerCameras)
                        {
                            if (other == cam) continue;

                            bool inFrustum = GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(other), bounds);
                            bool inMask = renderers.Any(r => (other.cullingMask & (1 << r.gameObject.layer)) != 0);
                            log.Append($" | {other.transform.root.name}: frustum={inFrustum} mask={inMask}");
                        }
                    }

                    log.AppendLine();
                }
            }
            else
            {
                log.AppendLine("[DIAG]   PlayerModelSelector: no");
            }

            // HUD del jugador
            foreach (Canvas canvas in player.GetComponentsInChildren<Canvas>(true))
            {
                log.AppendLine($"[DIAG]   Canvas {canvas.name}: enabled={canvas.enabled} activo={canvas.gameObject.activeInHierarchy} mode={canvas.renderMode} sort={canvas.sortingOrder}");

                foreach (Transform child in canvas.transform)
                {
                    if (!child.name.StartsWith("PlayerIdentificator")) continue;

                    RectTransform rect = (RectTransform)child;
                    log.AppendLine($"[DIAG]     {child.name}: activeSelf={child.gameObject.activeSelf} anchoredPos={rect.anchoredPosition} pantalla={RectTransformUtility.WorldToScreenPoint(null, rect.position)}");
                }
            }

            // Interactor
            PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();

            if (interactor != null)
            {
                SerializedObject so = new SerializedObject(interactor);
                log.AppendLine($"[DIAG]   PlayerInteractor: enabled={interactor.enabled} playerId(serializado)={so.FindProperty("playerId").intValue} " +
                               $"playerIdentity={(so.FindProperty("playerIdentity").objectReferenceValue as PlayerIdentity)?.name ?? "(Awake: GetComponent)"}");
            }

            Debug.Log(log.ToString());
        }

        Debug.Log($"[DIAG] Pantalla={Screen.width}x{Screen.height}");
    }


    // =====================================================
    // PRUEBA: ACCIÓN POR JUGADOR EN LOS INTERRUPTORES
    // =====================================================

    [MenuItem("TSirkus/Diagnostico/Probar ACCION por jugador en interruptores (Play Mode)")]
    private static void TestActionPerSwitch()
    {
        var steps = new System.Collections.Generic.List<(double, System.Action)>();

        // 1) Los cuatro frente a su interruptor a la vez: con el bug, ACCIÓN de P1 los registraba a todos.
        for (int id = 1; id <= 4; id++)
        {
            int playerId = id;
            steps.Add((0.2, () => PlaceInFrontOfSwitch(playerId, $"Switch{playerId}")));
        }

        steps.Add((1.0, () => LogState("Todos frente a su interruptor")));

        // 2) ACCIÓN de cada jugador, de uno en uno.
        for (int id = 1; id <= 4; id++)
        {
            int playerId = id;
            steps.Add((1.2, () => SetInput(playerId, "action", true)));
            steps.Add((0.3, () => SetInput(playerId, "action", false)));
            steps.Add((0.8, () => LogState($"Tras ACCIÓN de P{playerId}")));
        }

        RunSteps(steps.ToArray());
    }


    [MenuItem("TSirkus/Diagnostico/Probar movimiento y mirada de P1 (Play Mode)")]
    private static void TestMoveAndLookP1()
    {
        Vector3[] startPositions = null;
        Quaternion[] startRotations = null;
        PlayerIdentity[] players = null;

        RunSteps(
            (0, () =>
            {
                players = RuntimePlayers();
                startPositions = players.Select(p => p.transform.position).ToArray();
                startRotations = players.Select(p => p.transform.rotation).ToArray();
                SetInput(1, "movementY", 1f);
            }),
            (0.8, () =>
            {
                SetInput(1, "movementY", 0f);
                SetInput(1, "lookX", 1f);
            }),
            (0.4, () =>
            {
                SetInput(1, "lookX", 0f);

                for (int i = 0; i < players.Length; i++)
                {
                    Debug.Log(
                        $"[DIAG] {players[i].name}: desplazamiento={Vector3.Distance(startPositions[i], players[i].transform.position):0.00}m " +
                        $"giro={Quaternion.Angle(startRotations[i], players[i].transform.rotation):0.0}°");
                }
            }));
    }


    private static PlayerIdentity[] RuntimePlayers()
    {
        return Object.FindObjectsByType<PlayerIdentity>()
            .Where(p => p.GetComponentInChildren<Camera>(true) != null)
            .OrderBy(p => p.PlayerId)
            .ToArray();
    }


    // Coloca al jugador 1,6 m al sur del interruptor y apunta su cámara al centro del mismo.
    private static void PlaceInFrontOfSwitch(int playerId, string switchName)
    {
        PlayerIdentity player = RuntimePlayers().FirstOrDefault(p => p.PlayerId == playerId);
        GameObject target = GameObject.Find(switchName);

        if (player == null || target == null)
        {
            Debug.LogWarning($"[DIAG] No se encontró Player {playerId} o {switchName}.");
            return;
        }

        Collider targetCollider = target.GetComponentInChildren<Collider>();
        Vector3 aim = targetCollider != null ? targetCollider.bounds.center : target.transform.position;

        CharacterController body = player.GetComponent<CharacterController>();
        Vector3 stand = new Vector3(aim.x, player.transform.position.y, aim.z - 1.6f);

        body.enabled = false;
        player.transform.position = stand;
        body.enabled = true;

        Camera cam = player.GetComponentInChildren<Camera>(true);
        Vector3 toTarget = aim - cam.transform.position;
        float yaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
        float pitch = -Mathf.Atan2(toTarget.y, new Vector2(toTarget.x, toTarget.z).magnitude) * Mathf.Rad2Deg;

        player.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        // PlayerLook reaplica su pitch interno cada frame: se ajusta también ese valor.
        PlayerLook look = player.GetComponent<PlayerLook>();

        if (look != null)
        {
            typeof(PlayerLook).GetField("verticalRotation", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(look, pitch);

            Transform holder = new SerializedObject(look).FindProperty("cameraHolder").objectReferenceValue as Transform;

            if (holder != null)
                holder.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        Debug.Log($"[DIAG] {player.name} frente a {switchName}");
    }


    // Simula input del celular (solo editor, por reflexión; MobileInputManager no se modifica).
    private static void SetInput(int playerId, string field, object value)
    {
        MobileInputManager input = Object.FindAnyObjectByType<MobileInputManager>();

        System.Array states = typeof(MobileInputManager)
            .GetField("players", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(input) as System.Array;

        object state = states?.GetValue(playerId);
        state?.GetType().GetField(field)?.SetValue(state, value);

        if (field == "action" && (bool)value)
            Debug.Log($"[DIAG] ACCIÓN de Player {playerId}");
    }


    private static void LogState(string label)
    {
        string hints = string.Join(" | ", Enumerable.Range(1, 4).Select(id =>
        {
            PlayerHintHUD hud = Object.FindObjectsByType<PlayerHintHUD>().FirstOrDefault(h => h.PlayerId == id);
            return $"P{id}: {(hud != null && hud.CurrentHint != null ? hud.CurrentHint.name : "-")}";
        }));

        Debug.Log($"[DIAG] {label} → ayudas: {hints}");
    }


    private static void RunSteps(params (double delay, System.Action action)[] steps)
    {
        int index = 0;
        double next = EditorApplication.timeSinceStartup + steps[0].delay;

        EditorApplication.CallbackFunction tick = null;
        tick = () =>
        {
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.update -= tick;
                return;
            }

            while (index < steps.Length && EditorApplication.timeSinceStartup >= next)
            {
                steps[index].action();
                index++;

                if (index < steps.Length)
                    next = EditorApplication.timeSinceStartup + steps[index].delay;
            }

            if (index >= steps.Length)
                EditorApplication.update -= tick;
        };

        EditorApplication.update += tick;
    }


    [MenuItem("TSirkus/Diagnostico/Comparar jugadores (Play Mode)", true)]
    [MenuItem("TSirkus/Diagnostico/Probar ACCION por jugador en interruptores (Play Mode)", true)]
    [MenuItem("TSirkus/Diagnostico/Probar movimiento y mirada de P1 (Play Mode)", true)]
    private static bool PlayModeOnly()
    {
        return EditorApplication.isPlaying;
    }
}
