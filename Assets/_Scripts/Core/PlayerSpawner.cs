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
        int amountToSpawn = Mathf.Min(playerCount, spawnPoints.Length);

        for (int i = 0; i < amountToSpawn; i++)
        {
            GameObject newPlayer = Instantiate(
                playerPrefab,
                spawnPoints[i].position,
                spawnPoints[i].rotation
            );

            // Asignar ID
            PlayerIdentity identity =
                newPlayer.GetComponent<PlayerIdentity>();

            if (identity != null)
            {
                identity.SetPlayerId(i + 1);
            }

            // Buscar cámara del jugador
            Camera playerCamera =
                newPlayer.GetComponentInChildren<Camera>();

            if (playerCamera != null)
            {
                ConfigureCamera(
                    playerCamera,
                    i,
                    amountToSpawn
                );
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
}