using UnityEngine;

public class PlayerModelSelector : MonoBehaviour
{
    [Header("Player Models")]
    [SerializeField] private GameObject modelPlayer1;
    [SerializeField] private GameObject modelPlayer2;
    [SerializeField] private GameObject modelPlayer3;
    [SerializeField] private GameObject modelPlayer4;

    private PlayerIdentity playerIdentity;

    private void Awake()
    {
        playerIdentity = GetComponent<PlayerIdentity>();
    }

    private void Start()
    {
        SelectModel();
    }

    private void SelectModel()
    {
        if (playerIdentity == null)
        {
            Debug.LogError(
                $"No se encontró PlayerIdentity en {gameObject.name}"
            );

            return;
        }

        // Desactivar todos los modelos
        if (modelPlayer1 != null)
            modelPlayer1.SetActive(false);

        if (modelPlayer2 != null)
            modelPlayer2.SetActive(false);

        if (modelPlayer3 != null)
            modelPlayer3.SetActive(false);

        if (modelPlayer4 != null)
            modelPlayer4.SetActive(false);

        // Activar el modelo correspondiente
        switch (playerIdentity.PlayerId)
        {
            case 1:
                if (modelPlayer1 != null)
                    modelPlayer1.SetActive(true);
                break;

            case 2:
                if (modelPlayer2 != null)
                    modelPlayer2.SetActive(true);
                break;

            case 3:
                if (modelPlayer3 != null)
                    modelPlayer3.SetActive(true);
                break;

            case 4:
                if (modelPlayer4 != null)
                    modelPlayer4.SetActive(true);
                break;

            default:
                Debug.LogWarning(
                    $"Player ID {playerIdentity.PlayerId} no tiene un modelo asignado."
                );
                break;
        }
    }
}