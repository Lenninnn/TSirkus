using UnityEngine;

public class ClownSpawnPoint : MonoBehaviour
{
    [Header("Configuración del punto")]
    [Tooltip("Si está activo, este punto puede utilizarse para apariciones.")]
    [SerializeField] private bool canBeUsed = true;

    [Tooltip("Si está activo, el payaso utilizará la orientación del punto.")]
    [SerializeField] private bool faceForward = true;

    [Header("Gizmos")]
    [SerializeField] private float gizmoRadius = 0.5f;

    public bool CanBeUsed => canBeUsed;

    public Vector3 SpawnPosition =>
        transform.position;

    public Quaternion SpawnRotation =>
        transform.rotation;

    public bool FaceForward =>
        faceForward;

    private void OnDrawGizmos()
    {
        Gizmos.color =
            canBeUsed
                ? Color.magenta
                : Color.gray;

        Gizmos.DrawWireSphere(
            transform.position,
            gizmoRadius
        );

        Gizmos.DrawLine(
            transform.position,
            transform.position +
            transform.forward * 1.5f
        );
    }
}