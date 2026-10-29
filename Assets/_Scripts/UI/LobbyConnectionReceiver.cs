using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class LobbyConnectionReceiver : MonoBehaviour
{
    [Header("Servidor")]
    [SerializeField] private string serverIP = "127.0.0.1";
    [SerializeField] private int serverPort = 9000;

    private TcpClient tcpClient;
    private NetworkStream networkStream;
    private Thread connectionThread;

    private volatile bool running;
    private volatile bool serverConnected;

    private readonly ConcurrentQueue<string> messageQueue =
        new ConcurrentQueue<string>();

    private readonly bool[] playersConnected = new bool[5];

    [Serializable]
    private class MessageData
    {
        public string type;
        public int playerId;
    }

    private void Start()
    {
        running = true;

        connectionThread = new Thread(ConnectionLoop);
        connectionThread.IsBackground = true;
        connectionThread.Start();
    }

    private void Update()
    {
        while (messageQueue.TryDequeue(out string message))
        {
            ProcessMessage(message);
        }
    }

    // Antes solo intentaba conectar una vez: si el servidor de teléfonos arrancaba después de
    // abrir esta pantalla, el lobby se quedaba en 0/4 para siempre. Ahora reintenta cada 3 s
    // (en este hilo, sin bloquear Unity). Mensajes sin cambios.
    private void ConnectionLoop()
    {
        while (running)
        {
            ConnectionSession();

            for (int waited = 0; running && waited < 30; waited++)
                Thread.Sleep(100);
        }
    }

    private void ConnectionSession()
    {
        try
        {
            tcpClient = new TcpClient();

            tcpClient.Connect(
                serverIP,
                serverPort
            );

            networkStream = tcpClient.GetStream();
            serverConnected = true;

            byte[] buffer = new byte[8192];
            StringBuilder dataBuffer = new StringBuilder();

            while (
                running &&
                tcpClient != null &&
                tcpClient.Connected
            )
            {
                int bytesRead = networkStream.Read(
                    buffer,
                    0,
                    buffer.Length
                );

                if (bytesRead <= 0)
                    break;

                string incoming = Encoding.UTF8.GetString(
                    buffer,
                    0,
                    bytesRead
                );

                dataBuffer.Append(incoming);

                string currentData = dataBuffer.ToString();
                int newlineIndex;

                while (
                    (newlineIndex = currentData.IndexOf('\n')) >= 0
                )
                {
                    string message = currentData
                        .Substring(0, newlineIndex)
                        .Trim();

                    currentData = currentData.Substring(
                        newlineIndex + 1
                    );

                    if (!string.IsNullOrEmpty(message))
                    {
                        messageQueue.Enqueue(message);
                    }
                }

                dataBuffer.Clear();
                dataBuffer.Append(currentData);
            }
        }
        catch
        {
            // La interfaz mostrará que el servidor no está conectado.
        }
        finally
        {
            serverConnected = false;

            try { tcpClient?.Close(); } catch { }
        }
    }

    private void ProcessMessage(string message)
    {
        try
        {
            MessageData data =
                JsonUtility.FromJson<MessageData>(message);

            if (data == null)
                return;

            if (data.playerId < 1 || data.playerId > 4)
                return;

            if (data.type == "player_connected")
            {
                playersConnected[data.playerId] = true;

                Debug.Log(
                    $"🎪 LOBBY: PLAYER {data.playerId} CONECTADO"
                );

                return;
            }

            if (data.type == "player_disconnected")
            {
                playersConnected[data.playerId] = false;

                Debug.Log(
                    $"🎪 LOBBY: PLAYER {data.playerId} DESCONECTADO"
                );
            }
        }
        catch (Exception error)
        {
            Debug.LogWarning(
                $"Lobby ignoró mensaje: {error.Message}"
            );
        }
    }

    public bool IsPlayerConnected(int playerId)
    {
        if (playerId < 1 || playerId > 4)
            return false;

        return playersConnected[playerId];
    }

    public bool IsServerConnected()
    {
        return serverConnected;
    }

    public int GetConnectedPlayerCount()
    {
        int count = 0;

        for (int i = 1; i <= 4; i++)
        {
            if (playersConnected[i])
                count++;
        }

        return count;
    }

    public bool AreAllPlayersConnected()
    {
        return GetConnectedPlayerCount() == 4;
    }

    private void OnDestroy()
    {
        Disconnect();
    }

    private void OnApplicationQuit()
    {
        Disconnect();
    }

    private void Disconnect()
    {
        running = false;
        serverConnected = false;

        try
        {
            networkStream?.Close();
        }
        catch
        {
        }

        try
        {
            tcpClient?.Close();
        }
        catch
        {
        }
    }
}