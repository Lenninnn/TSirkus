#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TSirkus.Prototype.Editor
{
    public static class TSirkusLevelValidator
    {
        // Validation envelope, not a player controller configuration.
        private const float BodyHeight = 1.8f;
        private const float BodyRadius = 0.35f;
        private const float FeetClearance = 0.06f;
        private const float SampleStep = 0.25f;

        [MenuItem("Tools/TSirkus/03 Validar nivel seleccionado", false, 20)]
        public static void ValidateSelected()
        {
            GameObject root = FindRoot();
            if (root == null) { Debug.LogWarning("Selecciona TSirkus_Level o uno de sus hijos."); return; }
            ValidateRoot(root);
        }

        private static GameObject FindRoot()
        {
            Transform candidate = Selection.activeTransform;
            while (candidate != null)
            {
                TSirkusMarker marker = candidate.GetComponent<TSirkusMarker>();
                if (marker != null && marker.kind == TSirkusMarkerKind.Level) return candidate.gameObject;
                candidate = candidate.parent;
            }
            foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                TSirkusMarker marker = go.GetComponent<TSirkusMarker>();
                if (marker != null && marker.kind == TSirkusMarkerKind.Level) return go;
            }
            return null;
        }

        public static void ValidateRoot(GameObject root)
        {
            TSirkusMarker level = root.GetComponent<TSirkusMarker>();
            if (level == null) { Debug.LogError("Falta el marcador del nivel.", root); return; }
            if (Vector3.Distance(root.transform.lossyScale, Vector3.one) > 0.001f ||
                Vector3.Dot(root.transform.up, Vector3.up) < 0.999f)
            {
                Debug.LogError("La validacion requiere escala 1,1,1 y suelo horizontal. La raiz puede trasladarse y rotar alrededor de Y.", root);
                return;
            }
            if (!root.activeInHierarchy) { Debug.LogWarning("Activa el nivel antes de validar.", root); return; }
            Physics.SyncTransforms();
            TSirkusMarker[] markers = root.GetComponentsInChildren<TSirkusMarker>(true);
            HashSet<string> ids = new HashSet<string>();
            HashSet<string> issues = new HashSet<string>();
            int spawns = 0, puzzles = 0, routes = 0, samples = 0, blockedSamples = 0, unsupportedSamples = 0;
            foreach (TSirkusMarker marker in markers)
            {
                if (string.IsNullOrWhiteSpace(marker.markerId) || !ids.Add(marker.markerId))
                    issues.Add("ID vacio o repetido: " + marker.name);
                if (marker.kind == TSirkusMarkerKind.Spawn)
                {
                    spawns++;
                    CheckPoint(root.transform, marker.transform.position, marker.name, issues, ref samples, ref blockedSamples, ref unsupportedSamples);
                }
                if (marker.kind == TSirkusMarkerKind.Puzzle) puzzles++;
                if (marker.kind == TSirkusMarkerKind.Route)
                {
                    routes++;
                    if (marker.transform.childCount < 2) issues.Add("Ruta sin suficientes puntos: " + marker.name);
                    for (int i = 1; i < marker.transform.childCount; i++)
                    {
                        Vector3 a = marker.transform.GetChild(i - 1).position;
                        Vector3 b = marker.transform.GetChild(i).position;
                        int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(a, b) / SampleStep));
                        for (int step = 0; step <= steps; step++)
                            CheckPoint(root.transform, Vector3.Lerp(a, b, step / (float)steps), marker.name, issues,
                                ref samples, ref blockedSamples, ref unsupportedSamples);
                    }
                }
                if (marker.kind == TSirkusMarkerKind.Access && marker.markerId.StartsWith("TS_ACCESS_", StringComparison.Ordinal))
                    CheckPoint(root.transform, marker.transform.position, marker.name, issues, ref samples, ref blockedSamples, ref unsupportedSamples);
            }
            if (spawns != 4) issues.Add("Se esperaban 4 spawns; encontrados: " + spawns);
            int expectedPuzzles = level.markerId == "TS_LEVEL_FULL" ? 5 : 0;
            if (puzzles != expectedPuzzles) issues.Add("Zonas de puzzle esperadas: " + expectedPuzzles + "; encontradas: " + puzzles);
            if (routes == 0) issues.Add("Faltan las rutas de validacion.");

            int colliders = 0, triggers = 0, lights = 0, shadowLights = 0;
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            { if (collider.isTrigger) triggers++; else colliders++; }
            foreach (Light light in root.GetComponentsInChildren<Light>(true))
            { lights++; if (light.shadows != LightShadows.None) shadowLights++; }
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) issues.Add("Mesh ausente: " + filter.name);
                else if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(filter.sharedMesh)))
                    issues.Add("Mesh sin asset persistente: " + filter.name);
            }
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material material in renderer.sharedMaterials)
                    if (material == null || material.shader == null || !material.shader.isSupported)
                        issues.Add("Material/shader ausente o no soportado: " + renderer.name);
            }
            string summary = "TSirkus: " + spawns + " spawns, " + puzzles + " zonas puzzle, " + routes + " rutas, " + samples + " muestras. " +
                colliders + " colliders solidos, " + triggers + " triggers, " + lights + " luces (" + shadowLights + " con sombras).";
            if (issues.Count == 0)
                Debug.Log(summary + " VALIDACION GEOMETRICA OK para capsula 1.8 m / radio 0.35 m. Falta comprobar con tus jugadores y medir FPS.", root);
            else
            {
                Debug.LogWarning(summary + " Muestras bloqueadas: " + blockedSamples + "; sin suelo: " + unsupportedSamples + ".", root);
                int shown = 0;
                foreach (string issue in issues)
                { if (shown++ >= 16) break; Debug.LogWarning("TSirkus: " + issue, root); }
                if (issues.Count > 16) Debug.LogWarning("Hay " + (issues.Count - 16) + " incidencias adicionales.", root);
            }
        }

        private static void CheckPoint(Transform root, Vector3 feet, string route, HashSet<string> issues,
            ref int samples, ref int blocked, ref int unsupported)
        {
            samples++;
            Vector3 bottom = feet + Vector3.up * (BodyRadius + FeetClearance);
            Vector3 top = feet + Vector3.up * (BodyHeight - BodyRadius + FeetClearance);
            Collider[] overlaps = Physics.OverlapCapsule(bottom, top, BodyRadius, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            bool blockedHere = false;
            foreach (Collider collider in overlaps)
            {
                if (!collider.transform.IsChildOf(root)) continue;
                issues.Add("Ruta " + route + " intercepta " + collider.name + ". Revisa su collider.");
                blockedHere = true;
            }
            if (blockedHere) blocked++;
            bool supported = false;
            foreach (RaycastHit hit in Physics.RaycastAll(feet + Vector3.up * 0.35f, Vector3.down, 0.65f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.IsChildOf(root) && Vector3.Dot(hit.normal, Vector3.up) > 0.65f)
                { supported = true; break; }
            }
            if (!supported)
            {
                unsupported++;
                issues.Add("Ruta " + route + " sin suelo bajo " + feet.ToString("F2") + ".");
            }
        }
    }
}
#endif
