using UnityEngine;

public class PuzzleDoor : MonoBehaviour
{
    [Header("Puerta")]
    [Tooltip("Objeto visual de la puerta que será desactivado al abrir.")]
    [SerializeField] private GameObject doorVisual;

    [Header("Payaso")]
    [Tooltip("Objeto que contiene el ClownSpawner.")]
    [SerializeField] private ClownSpawner clownSpawner;

    [Tooltip("Si está activo, abrir la puerta dispara la primera aparición del payaso.")]
    [SerializeField] private bool triggerFirstClownAppearance = true;

    private bool doorAlreadyOpened;

    // =========================================================
    // ABRIR PUERTA
    // =========================================================

    public void OpenDoor()
    {
        if (doorAlreadyOpened)
        {
            Debug.Log(
                $"{gameObject.name}: La puerta ya estaba abierta."
            );

            return;
        }

        doorAlreadyOpened = true;

        if (doorVisual != null)
        {
            doorVisual.SetActive(false);
        }

        Debug.Log(
            $"{gameObject.name}: PUERTA ABIERTA"
        );

        // -----------------------------------------------------
        // ACTIVAR PRIMERA APARICIÓN
        // -----------------------------------------------------

        if (triggerFirstClownAppearance)
        {
            if (clownSpawner != null)
            {
                clownSpawner.TriggerFirstAppearance();

                Debug.Log(
                    $"{gameObject.name}: Primera aparición del payaso activada."
                );
            }
            else
            {
                Debug.LogWarning(
                    $"{gameObject.name}: No hay ClownSpawner asignado."
                );
            }
        }
    }

    // =========================================================
    // CERRAR PUERTA
    // =========================================================

    public void CloseDoor()
    {
        doorAlreadyOpened = false;

        if (doorVisual != null)
        {
            doorVisual.SetActive(true);
        }

        Debug.Log(
            $"{gameObject.name}: PUERTA CERRADA"
        );
    }
}