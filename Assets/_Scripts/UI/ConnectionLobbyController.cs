using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ConnectionLobbyController : MonoBehaviour
{
    [Header("Receptor de conexiones")]
    [SerializeField] private LobbyConnectionReceiver receiver;

    [Header("Estados de jugadores")]
    [SerializeField] private TMP_Text player1Status;
    [SerializeField] private TMP_Text player2Status;
    [SerializeField] private TMP_Text player3Status;
    [SerializeField] private TMP_Text player4Status;

    [Header("Estado general")]
    [SerializeField] private TMP_Text playerCountText;

    [Header("Inicio de partida")]
    [SerializeField] private Button startGameButton;

    private void Update()
    {
        if (receiver == null)
            return;

        UpdatePlayerStatus(
            player1Status,
            receiver.IsPlayerConnected(1)
        );

        UpdatePlayerStatus(
            player2Status,
            receiver.IsPlayerConnected(2)
        );

        UpdatePlayerStatus(
            player3Status,
            receiver.IsPlayerConnected(3)
        );

        UpdatePlayerStatus(
            player4Status,
            receiver.IsPlayerConnected(4)
        );

        int connectedCount =
            receiver.GetConnectedPlayerCount();

        if (playerCountText != null)
        {
            playerCountText.text =
                $"JUGADORES {connectedCount} / 4";
        }

        if (startGameButton != null)
        {
            startGameButton.interactable =
                receiver.AreAllPlayersConnected();
        }
    }

    private void UpdatePlayerStatus(
        TMP_Text text,
        bool connected
    )
    {
        if (text == null)
            return;

        if (connected)
        {
            text.text = "CONECTADO"; // "✓" no existe en LiberationSans: se veía un cuadro vacío.

            text.color = new Color(
                0.35f,
                0.9f,
                0.45f
            );
        }
        else
        {
            text.text = "ESPERANDO...";

            text.color = new Color(
                0.75f,
                0.75f,
                0.75f
            );
        }
    }

    public void StartGame()
    {
        if (receiver == null)
            return;

        if (!receiver.AreAllPlayersConnected())
            return;

        SceneManager.LoadScene("GAME");
    }
}