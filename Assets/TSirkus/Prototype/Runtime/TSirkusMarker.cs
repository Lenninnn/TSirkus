using UnityEngine;

namespace TSirkus.Prototype
{
    public enum TSirkusMarkerKind
    {
        Level, Spawn, Briefing, Puzzle, Ticket, Event, Access, Expansion, Route
    }

    // Metadata and editor gizmos only. This component implements no game rules.
    [DisallowMultipleComponent]
    public sealed class TSirkusMarker : MonoBehaviour
    {
        public TSirkusMarkerKind kind;
        public string markerId;
        public Vector3 size = new Vector3(1f, 2f, 1f);
        public Color color = new Color(0.3f, 0.8f, 0.65f, 1f);
        [TextArea(2, 6)] public string notes;

        private void OnDrawGizmos()
        {
            if (kind == TSirkusMarkerKind.Level) return;
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = color;
            if (kind == TSirkusMarkerKind.Route)
            {
                for (int i = 1; i < transform.childCount; i++)
                    Gizmos.DrawLine(transform.GetChild(i - 1).localPosition,
                        transform.GetChild(i).localPosition);
            }
            else
            {
                Gizmos.DrawWireCube(Vector3.up * size.y * 0.5f, size);
                if (kind == TSirkusMarkerKind.Spawn)
                {
                    Gizmos.DrawLine(Vector3.up * 0.1f, new Vector3(0f, 0.1f, 1.2f));
                    Gizmos.DrawSphere(new Vector3(0f, 0.1f, 1.2f), 0.08f);
                }
            }
            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }
    }
}
