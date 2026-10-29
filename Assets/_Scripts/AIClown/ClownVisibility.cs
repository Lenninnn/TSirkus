using System.Collections.Generic;
using UnityEngine;

// ¿Una cámara de jugador puede ver REALMENTE al payaso?
// Se comprueban 4 puntos del cuerpo (cabeza, pecho, cintura, pies). Un punto cuenta si:
//   - está delante de la cámara y dentro de su viewport (el rectángulo de su split-screen),
//   - está dentro de la distancia pedida,
//   - y no hay nada entre la cámara y el punto (se ignoran el propio payaso, el jugador dueño
//     de la cámara y los triggers).
// Basta un punto: si se le ve la cabeza asomando detrás de una mesa, se le ve.
// Coste: como máximo 4 raycasts por cámara y comprobación.
public static class ClownVisibility
{
    private static readonly RaycastHit[] hits = new RaycastHit[16];
    private static readonly Vector3[] points = new Vector3[4];

    // Para "visto": un punto pegado al borde de la pantalla todavía no cuenta.
    private const float SeenViewportMargin = 0.03f;


    // distance = distancia al punto visible más cercano (si devuelve true).
    public static bool CanCameraSee(Camera camera, Transform clown, Bounds clownBounds, float maxDistance, out float distance)
    {
        return Check(camera, clown, clownBounds, maxDistance, SeenViewportMargin, out distance);
    }


    // Para elegir dónde aparecer: ¿alguna cámara vería aunque sea un trozo del payaso aquí?
    // (sin margen: aparecer en el borde de la pantalla también está prohibido).
    public static bool IsObservedByAny(IEnumerable<Camera> cameras, Bounds clownBounds, float maxDistance)
    {
        foreach (Camera camera in cameras)
        {
            if (Check(camera, null, clownBounds, maxDistance, 0f, out _))
                return true;
        }

        return false;
    }


    private static bool Check(Camera camera, Transform clown, Bounds bounds, float maxDistance, float margin, out float visibleDistance)
    {
        visibleDistance = float.MaxValue;

        if (camera == null || !camera.isActiveAndEnabled)
            return false;

        Vector3 origin = camera.transform.position;
        Transform viewer = camera.transform.root;

        float bottom = bounds.min.y;
        float height = bounds.size.y;
        points[0] = new Vector3(bounds.center.x, bottom + height * 0.92f, bounds.center.z);
        points[1] = new Vector3(bounds.center.x, bottom + height * 0.70f, bounds.center.z);
        points[2] = new Vector3(bounds.center.x, bottom + height * 0.45f, bounds.center.z);
        points[3] = new Vector3(bounds.center.x, bottom + height * 0.12f, bounds.center.z);

        for (int i = 0; i < points.Length; i++)
        {
            Vector3 toPoint = points[i] - origin;
            float distance = toPoint.magnitude;

            if (distance > maxDistance || distance < 0.01f)
                continue;

            Vector3 viewport = camera.WorldToViewportPoint(points[i]);

            if (viewport.z <= camera.nearClipPlane ||
                viewport.x < margin || viewport.x > 1f - margin ||
                viewport.y < margin || viewport.y > 1f - margin)
            {
                continue;
            }

            if (distance < visibleDistance && IsUnobstructed(origin, toPoint / distance, distance, clown, viewer))
                visibleDistance = distance;
        }

        return visibleDistance < float.MaxValue;
    }


    private static bool IsUnobstructed(Vector3 origin, Vector3 direction, float distance, Transform clown, Transform viewer)
    {
        int count = Physics.RaycastNonAlloc(
            origin, direction, hits, distance - 0.05f, Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Transform hit = hits[i].transform;

            if (hit.root == viewer)
                continue;

            if (clown != null && hit.IsChildOf(clown))
                continue;

            return false;
        }

        return true;
    }
}
