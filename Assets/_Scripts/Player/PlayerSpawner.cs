using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private GameObject playerPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Game Settings")]
    [SerializeField, Range(2, 4)] private int playerCount = 4;

    private void Start()
    {
        SpawnPlayers();
    }

    private void SpawnPlayers()
    {
        int amountToSpawn =
            Mathf.Min(playerCount, spawnPoints.Length);

        for (int i = 0; i < amountToSpawn; i++)
        {
            GameObject newPlayer = Instantiate(
                playerPrefab,
                spawnPoints[i].position,
                spawnPoints[i].rotation
            );

            newPlayer.name = "Player_" + (i + 1);

            // -------------------------
            // ASIGNAR PLAYER ID
            // -------------------------

            PlayerIdentity identity =
                newPlayer.GetComponent<PlayerIdentity>();

            if (identity != null)
            {
                identity.SetPlayerId(i + 1);
            }

            // -------------------------
            // CONFIGURAR CÁMARA
            // -------------------------

            Camera playerCamera =
                newPlayer.GetComponentInChildren<Camera>();

            if (playerCamera != null)
            {
                ConfigureCamera(
                    playerCamera,
                    i,
                    amountToSpawn
                );

                ConfigureHighlightLayer(
                    playerCamera,
                    i + 1
                );
            }

            // -------------------------
            // AUDIO LISTENER
            // -------------------------

            AudioListener listener =
                newPlayer.GetComponentInChildren<AudioListener>();

            if (listener != null)
            {
                // Solo Player 1 tendrá AudioListener.
                listener.enabled = (i == 0);
            }
        }
    }

    private void ConfigureCamera(
        Camera cam,
        int playerIndex,
        int totalPlayers)
    {
        if (totalPlayers == 2)
        {
            if (playerIndex == 0)
            {
                cam.rect = new Rect(
                    0f, 0f,
                    0.5f, 1f
                );
            }
            else
            {
                cam.rect = new Rect(
                    0.5f, 0f,
                    0.5f, 1f
                );
            }
        }

        else if (totalPlayers == 3)
        {
            if (playerIndex == 0)
            {
                cam.rect = new Rect(
                    0f, 0.5f,
                    0.5f, 0.5f
                );
            }
            else if (playerIndex == 1)
            {
                cam.rect = new Rect(
                    0.5f, 0.5f,
                    0.5f, 0.5f
                );
            }
            else
            {
                cam.rect = new Rect(
                    0.25f, 0f,
                    0.5f, 0.5f
                );
            }
        }

        else if (totalPlayers == 4)
        {
            switch (playerIndex)
            {
                case 0:
                    cam.rect = new Rect(
                        0f, 0.5f,
                        0.5f, 0.5f
                    );
                    break;

                case 1:
                    cam.rect = new Rect(
                        0.5f, 0.5f,
                        0.5f, 0.5f
                    );
                    break;

                case 2:
                    cam.rect = new Rect(
                        0f, 0f,
                        0.5f, 0.5f
                    );
                    break;

                case 3:
                    cam.rect = new Rect(
                        0.5f, 0f,
                        0.5f, 0.5f
                    );
                    break;
            }
        }
    }

    private void ConfigureHighlightLayer(
        Camera cam,
        int playerId)
    {
        int layerP1 = LayerMask.NameToLayer("HighlightP1");
        int layerP2 = LayerMask.NameToLayer("HighlightP2");
        int layerP3 = LayerMask.NameToLayer("HighlightP3");
        int layerP4 = LayerMask.NameToLayer("HighlightP4");

        if (
            layerP1 == -1 ||
            layerP2 == -1 ||
            layerP3 == -1 ||
            layerP4 == -1
        )
        {
            Debug.LogWarning(
                "Faltan una o más layers HighlightP1-P4."
            );

            return;
        }

        // Primero quitar todas las capas de highlight
        // de esta cámara.
        cam.cullingMask &= ~(1 << layerP1);
        cam.cullingMask &= ~(1 << layerP2);
        cam.cullingMask &= ~(1 << layerP3);
        cam.cullingMask &= ~(1 << layerP4);

        // Después activar solamente la correspondiente
        // a este jugador.
        switch (playerId)
        {
            case 1:
                cam.cullingMask |= (1 << layerP1);
                break;

            case 2:
                cam.cullingMask |= (1 << layerP2);
                break;

            case 3:
                cam.cullingMask |= (1 << layerP3);
                break;

            case 4:
                cam.cullingMask |= (1 << layerP4);
                break;
        }
    }
}