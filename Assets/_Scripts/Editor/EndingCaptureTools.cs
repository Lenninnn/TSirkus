using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// SOLO EDITOR / Play Mode. Material de referencia para producir las 4 cinemáticas finales.
// Fotografía la última prueba real (las cuerdas del monociclo sobre el foso) con copias
// temporales de los modelos de los jugadores: el ganador en pie sobre su cuerda cerca de la
// meta y los otros tres cayendo al vacío. Nada se guarda en GAME ni en los prefabs: las copias,
// la cámara y las luces se crean en Play Mode y se destruyen al terminar.
// Salida: <proyecto>/Capturas_Final/ (fuera de Assets: no entra en el juego).
public static class EndingCaptureTools
{
    private const int Width = 1920;
    private const int Height = 1080;

    // Carriles de las cuerdas (CuerdaP1..P4) y altura del cable, medidos en GAME.
    private static readonly float[] LaneX = { -4.83f, -1.78f, 1.89f, 4.90f };
    private const float RopeY = 0.0f;
    private const float WinnerZ = 67.5f;

    // Cámara cinematográfica común a las 4 versiones.
    private static readonly Vector3 CameraPosition = new Vector3(0f, 1.7f, 56.0f);
    private static readonly Vector3 CameraTarget = new Vector3(0f, -0.7f, 65.5f);
    private const float CameraFov = 52f;

    // Caídas: posiciones fijas en cuadro (izquierda, centro, derecha) para que nunca se solapen:
    // (x, adelanto en z, altura bajo la cuerda, giro de caída, giro lateral, fase de Walk).
    private static readonly (float x, float z, float y, float pitch, float roll, float phase)[] Falls =
    {
        (-2.3f, 61.2f, -0.9f, -55f, 25f, 0.10f),
        (0.1f, 64.0f, -1.6f, 70f, -35f, 0.35f),
        (2.4f, 62.0f, -1.2f, -30f, -60f, 0.62f),
    };

    private static string OutputFolder => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Capturas_Final");


    [MenuItem("TSirkus/Final/Capturar referencias para las cinemáticas (Play Mode)")]
    private static void CaptureAll()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("[CAPTURAS] Entra en Play Mode (GAME cargado con los 4 jugadores).");
            return;
        }

        List<PlayerIdentity> players = ClownSpawner.ActivePlayers().OrderBy(p => p.PlayerId).ToList();

        if (players.Count < 4)
        {
            Debug.LogWarning($"[CAPTURAS] Hay {players.Count} jugadores; se necesitan 4.");
            return;
        }

        Directory.CreateDirectory(OutputFolder);
        Directory.CreateDirectory(Path.Combine(OutputFolder, "Referencias_Personajes"));

        ClownSpawner spawner = Object.FindAnyObjectByType<ClownSpawner>();
        if (spawner != null) { spawner.DisableAppearances(); spawner.Vanish("capturas"); }

        var hidden = new List<Renderer>();
        foreach (PlayerIdentity player in players)
        {
            foreach (Renderer renderer in player.GetComponentsInChildren<Renderer>())
            {
                if (renderer.enabled) { renderer.enabled = false; hidden.Add(renderer); }
            }
        }

        var temporary = new List<Object>();

        try
        {
            Camera template = players[0].GetComponentInChildren<Camera>(true);
            Camera camera = MakeCamera(template, temporary);
            BuildLights(temporary);

            Shoot(camera, "Final_Escenario_Limpio.png");

            for (int winner = 1; winner <= 4; winner++)
            {
                var poses = new List<GameObject>();
                int fall = 0;

                foreach (PlayerIdentity player in players)
                {
                    GameObject copy = CopyModel(player);
                    poses.Add(copy);
                    int id = player.PlayerId;

                    if (id == winner)
                    {
                        Pose(copy, "Idle", 0.3f);
                        copy.transform.SetPositionAndRotation(new Vector3(LaneX[id - 1], RopeY, WinnerZ),
                            Quaternion.LookRotation(CameraPosition - new Vector3(LaneX[id - 1], 0f, WinnerZ), Vector3.up) * Quaternion.Euler(0f, 0f, 0f));
                        FlattenYaw(copy.transform);
                    }
                    else
                    {
                        var f = Falls[fall++];
                        Pose(copy, "Walk", f.phase);
                        copy.transform.position = new Vector3(f.x, RopeY + f.y, f.z);
                        copy.transform.rotation = Quaternion.LookRotation(CameraPosition - copy.transform.position) *
                                                  Quaternion.Euler(f.pitch, 0f, f.roll);
                    }
                }

                Shoot(camera, $"Final_Player{winner}_Ganador.png");

                foreach (GameObject pose in poses)
                    Object.DestroyImmediate(pose);
            }

            // Referencias por personaje: frente y perfil, misma luz.
            Camera reference = MakeCamera(template, temporary);
            reference.fieldOfView = 40f;
            var frontLight = new GameObject("Captura_Frontal_TEMP");
            temporary.Add(frontLight);
            Light front = frontLight.AddComponent<Light>();
            front.type = LightType.Point;
            front.color = new Color(1f, 0.92f, 0.85f);
            front.intensity = 18f;
            front.range = 12f;

            foreach (PlayerIdentity player in players)
            {
                GameObject copy = CopyModel(player);
                Pose(copy, "Idle", 0.3f);
                Vector3 spot = new Vector3(0f, 0f, 54.6f);
                copy.transform.SetPositionAndRotation(spot, Quaternion.LookRotation(Vector3.back));

                foreach ((string side, float yaw) in new[] { ("frente", 0f), ("perfil", 90f), ("espalda", 180f) })
                {
                    Vector3 offset = Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0f, -4.8f);
                    reference.transform.position = spot + offset + Vector3.up * 1.15f;
                    frontLight.transform.position = spot + offset * 0.6f + Vector3.up * 2.2f;
                    reference.transform.LookAt(spot + Vector3.up * 0.95f);
                    Shoot(reference, Path.Combine("Referencias_Personajes", $"Personaje_P{player.PlayerId}_{side}.png"));
                }

                Object.DestroyImmediate(copy);
            }
        }
        finally
        {
            foreach (Object o in temporary)
                if (o != null) Object.DestroyImmediate(o);

            foreach (Renderer renderer in hidden)
                if (renderer != null) renderer.enabled = true;
        }

        Debug.Log($"[CAPTURAS] Guardadas en {OutputFolder}");
        EditorUtility.RevealInFinder(OutputFolder);
    }


    private static Camera MakeCamera(Camera template, List<Object> temporary)
    {
        var go = new GameObject("CapturaCam_TEMP");
        temporary.Add(go);
        Camera camera = go.AddComponent<Camera>();

        if (template != null)
            camera.CopyFrom(template);

        camera.rect = new Rect(0f, 0f, 1f, 1f);
        camera.cullingMask = ~0;
        camera.fieldOfView = CameraFov;
        camera.nearClipPlane = 0.05f;
        camera.enabled = false;

        UniversalAdditionalCameraData data = go.GetComponent<UniversalAdditionalCameraData>();
        if (data == null) data = go.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = true;
        data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

        go.transform.position = CameraPosition;
        go.transform.LookAt(CameraTarget);
        return camera;
    }


    // Luz cálida y roja de circo, igual en todas las versiones.
    private static void BuildLights(List<Object> temporary)
    {
        Light Make(string name, LightType type, Vector3 position, Vector3 target, Color color, float intensity, float range, float angle)
        {
            var go = new GameObject(name);
            temporary.Add(go);
            Light light = go.AddComponent<Light>();
            light.type = type;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.spotAngle = angle;
            light.innerSpotAngle = angle * 0.5f;
            light.shadows = LightShadows.Soft;
            go.transform.position = position;
            go.transform.LookAt(target);
            return light;
        }

        // Foco cenital cálido sobre la meta (el ganador queda iluminado).
        Make("Captura_Foco_TEMP", LightType.Spot, new Vector3(0f, 6.5f, 68f), new Vector3(0f, 0f, WinnerZ), new Color(1f, 0.8f, 0.62f), 160f, 18f, 80f);
        // Resplandor rojo desde el foso (los que caen).
        Make("Captura_Foso_TEMP", LightType.Point, new Vector3(0f, -3.2f, 62f), new Vector3(0f, 0f, 62f), new Color(0.9f, 0.1f, 0.06f), 90f, 12f, 0f);
        // Contraluz rojo desde el fondo.
        Make("Captura_Contra_TEMP", LightType.Spot, new Vector3(0f, 3f, 74f), CameraPosition, new Color(0.8f, 0.12f, 0.08f), 60f, 24f, 60f);
        // Relleno suave desde la cámara.
        Make("Captura_Relleno_TEMP", LightType.Point, CameraPosition + new Vector3(0.8f, 0.6f, 1.2f), CameraTarget, new Color(0.8f, 0.75f, 0.85f), 25f, 30f, 0f);
    }


    // Copia visual del jugador (modelo con rig) sin sus scripts.
    private static GameObject CopyModel(PlayerIdentity player)
    {
        Animator source = player.GetComponentsInChildren<Animator>().FirstOrDefault(a => a.gameObject.activeInHierarchy && a.GetComponentInChildren<SkinnedMeshRenderer>() != null);
        GameObject sourceObject = source.gameObject;

        sourceObject.SetActive(false);
        GameObject copy = Object.Instantiate(sourceObject);
        sourceObject.SetActive(true);

        foreach (MonoBehaviour behaviour in copy.GetComponentsInChildren<MonoBehaviour>(true))
            Object.DestroyImmediate(behaviour);

        foreach (AudioSource audio in copy.GetComponentsInChildren<AudioSource>(true))
            Object.DestroyImmediate(audio);

        copy.name = $"Captura_P{player.PlayerId}_TEMP";
        copy.transform.SetParent(null);
        copy.transform.localScale = sourceObject.transform.lossyScale;
        copy.SetActive(true);

        foreach (Renderer renderer in copy.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = true;

        return copy;
    }


    // Congela una pose de un estado del Animator del jugador.
    private static void Pose(GameObject copy, string state, float normalizedTime)
    {
        Animator animator = copy.GetComponent<Animator>();

        if (animator == null)
            return;

        animator.applyRootMotion = false;
        animator.Rebind();
        animator.Play(state, 0, normalizedTime);
        animator.Update(0f);
        animator.speed = 0f;
    }


    private static void FlattenYaw(Transform t)
    {
        Vector3 forward = t.forward;
        forward.y = 0f;
        t.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
    }


    private static void Shoot(Camera camera, string relativePath)
    {
        var rt = new RenderTexture(Width, Height, 24) { antiAliasing = 4 };
        camera.targetTexture = rt;
        camera.Render();
        RenderTexture.active = rt;
        var texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
        texture.Apply();
        File.WriteAllBytes(Path.Combine(OutputFolder, relativePath), texture.EncodeToPNG());
        RenderTexture.active = null;
        camera.targetTexture = null;
        Object.DestroyImmediate(texture);
        rt.Release();
        Object.DestroyImmediate(rt);
        Debug.Log($"[CAPTURAS] {relativePath}");
    }
}
