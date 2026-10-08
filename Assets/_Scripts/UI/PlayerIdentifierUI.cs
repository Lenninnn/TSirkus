using UnityEngine;
using UnityEngine.UI;

public class PlayerIdentifierUI : MonoBehaviour
{
    [Header("Identifier Image")]
    [SerializeField] private Image identifierImage;

    [Header("Player Identifier Sprites")]
    [SerializeField] private Sprite player1Sprite;
    [SerializeField] private Sprite player2Sprite;
    [SerializeField] private Sprite player3Sprite;
    [SerializeField] private Sprite player4Sprite;

    private PlayerIdentity playerIdentity;

    private void Awake()
    {
        playerIdentity = GetComponentInParent<PlayerIdentity>();
    }

    public void SetIdentifier()
    {
        if (playerIdentity == null)
        {
            Debug.LogError(
                $"No se encontró PlayerIdentity para {gameObject.name}"
            );
            return;
        }

        if (identifierImage == null)
        {
            Debug.LogError(
                $"No se asignó Identifier Image en {gameObject.name}"
            );
            return;
        }

        // Seleccionar imagen según jugador
        switch (playerIdentity.PlayerId)
        {
            case 1:
                identifierImage.sprite = player1Sprite;
                break;

            case 2:
                identifierImage.sprite = player2Sprite;
                break;

            case 3:
                identifierImage.sprite = player3Sprite;
                break;

            case 4:
                identifierImage.sprite = player4Sprite;
                break;

            default:
                Debug.LogWarning(
                    $"Player ID {playerIdentity.PlayerId} no tiene identificador."
                );
                return;
        }

        // Colocar identificador en su esquina
        ConfigurePosition();
    }

    private void ConfigurePosition()
    {
        RectTransform rect = identifierImage.rectTransform;

        switch (playerIdentity.PlayerId)
        {
            // PLAYER 1 - ARRIBA IZQUIERDA
            case 1:

                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);

                rect.anchoredPosition = new Vector2(30f, -30f);

                break;


            // PLAYER 2 - ARRIBA DERECHA
            case 2:

                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);

                rect.anchoredPosition = new Vector2(-30f, -30f);

                break;


            // PLAYER 3 - ABAJO IZQUIERDA
            case 3:

                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 0f);
                rect.pivot = new Vector2(0f, 0f);

                rect.anchoredPosition = new Vector2(30f, 30f);

                break;


            // PLAYER 4 - ABAJO DERECHA
            case 4:

                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(1f, 0f);

                rect.anchoredPosition = new Vector2(-30f, 30f);

                break;
        }
    }
}