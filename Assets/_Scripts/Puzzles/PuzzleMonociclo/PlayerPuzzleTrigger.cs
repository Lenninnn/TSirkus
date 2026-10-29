
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerPuzzleTrigger : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private bool logEvents = true;

    private void OnTriggerEnter(Collider other)
    {
        PlayerIdentity identity =
            other.GetComponentInParent<PlayerIdentity>();

        if (identity == null)
            return;

        PlayerInstability instability =
            identity.GetComponent<PlayerInstability>();

        if (instability == null)
        {
            Debug.LogWarning(
                $"{identity.name}: no tiene PlayerInstability.",
                identity
            );

            return;
        }

        instability.SetPuzzleParticipation(true);

        if (logEvents)
        {
            Debug.Log(
                $"{identity.name} entró en el puzzle.",
                identity
            );
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerIdentity identity =
            other.GetComponentInParent<PlayerIdentity>();

        if (identity == null)
            return;

        PlayerInstability instability =
            identity.GetComponent<PlayerInstability>();

        if (instability == null)
            return;

        instability.SetPuzzleParticipation(false);

        if (logEvents)
        {
            Debug.Log(
                $"{identity.name} salió del puzzle.",
                identity
            );
        }
    }
}
