using UnityEngine;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;

public class MobileInputManager : MonoBehaviour
{
    [Header("Servidor")]
    [SerializeField] private string serverIP = "10.33.13.28";
    [SerializeField] private int serverPort = 9000;

    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;

    private readonly ConcurrentQueue<string> messageQueue =
        new ConcurrentQueue<string>();


    [Serializable]
    private class PlayerInput
    {
        // Movimiento
        public float x;
        public float y;
        public float z;

        // Cámara
        public float lookX;
        public float lookY;

        // Botones
        public bool connected;
        public bool jump;
        public bool action;

        // Soplido
        public float blowIntensity;
    }


    private PlayerInput[] players =
        new PlayerInput[5];


    // =====================================================
    // AWAKE
    // =====================================================

    private void Awake()
    {
        for (int i = 0; i < players.Length; i++)
        {
            players[i] =
                new PlayerInput();
        }

        Debug.Log(
            "🎮 MOBILE INPUT MANAGER"
        );

        ConnectToServer();
    }


    // =====================================================
    // CONEXIÓN
    // =====================================================

    private void ConnectToServer()
    {
        try
        {
            client =
                new TcpClient();

            client.Connect(
                serverIP,
                serverPort
            );

            stream =
                client.GetStream();

            Debug.Log(
                $"✅ CONECTADO AL SERVIDOR TCP → {serverIP}:{serverPort}"
            );

            receiveThread =
                new Thread(
                    ReceiveData
                );

            receiveThread.IsBackground =
                true;

            receiveThread.Start();
        }
        catch (Exception error)
        {
            Debug.LogError(
                $"❌ ERROR CONECTANDO UNITY: {error.Message}"
            );
        }
    }


    // =====================================================
    // RECIBIR DATOS
    // =====================================================

    private void ReceiveData()
    {
        byte[] buffer =
            new byte[4096];

        StringBuilder messageBuffer =
            new StringBuilder();

        try
        {
            while (
                client != null &&
                client.Connected
            )
            {
                int bytesRead =
                    stream.Read(
                        buffer,
                        0,
                        buffer.Length
                    );

                if (bytesRead <= 0)
                    break;

                string data =
                    Encoding.UTF8.GetString(
                        buffer,
                        0,
                        bytesRead
                    );

                messageBuffer.Append(
                    data
                );

                string completeData =
                    messageBuffer.ToString();

                string[] messages =
                    completeData.Split(
                        '\n'
                    );

                messageBuffer.Clear();

                for (
                    int i = 0;
                    i < messages.Length - 1;
                    i++
                )
                {
                    if (
                        !string.IsNullOrWhiteSpace(
                            messages[i]
                        )
                    )
                    {
                        messageQueue.Enqueue(
                            messages[i]
                        );
                    }
                }

                if (
                    messages.Length > 0 &&
                    !string.IsNullOrEmpty(
                        messages[messages.Length - 1]
                    )
                )
                {
                    messageBuffer.Append(
                        messages[messages.Length - 1]
                    );
                }
            }
        }
        catch (Exception error)
        {
            Debug.LogError(
                $"❌ ERROR RECIBIENDO TCP: {error.Message}"
            );
        }
    }


    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        while (
            messageQueue.TryDequeue(
                out string message
            )
        )
        {
            ProcessMessage(
                message
            );
        }
    }


    // =====================================================
    // PROCESAR MENSAJES
    // =====================================================

    private void ProcessMessage(
        string message
    )
    {
        try
        {
            MessageData data =
                JsonUtility.FromJson<MessageData>(
                    message
                );

            if (data == null)
                return;


            // =================================================
            // MOVIMIENTO
            // =================================================

            if (
                data.type ==
                "accelerometer"
            )
            {
                int id =
                    data.playerId;

                if (
                    id < 1 ||
                    id > 4
                )
                    return;

                players[id].x =
                    data.x;

                players[id].y =
                    data.y;

                players[id].z =
                    data.z;

                return;
            }


            // =================================================
            // LOOK TÁCTIL
            // =================================================

            if (
                data.type ==
                "look"
            )
            {
                int id =
                    data.playerId;

                if (
                    id < 1 ||
                    id > 4
                )
                    return;

                players[id].lookX +=
                    data.x;

                players[id].lookY +=
                    data.y;

                return;
            }


            // =================================================
            // SOPLIDO
            // =================================================

            if (
                data.type ==
                "blow"
            )
            {
                int id =
                    data.playerId;

                if (
                    id < 1 ||
                    id > 4
                )
                    return;

                players[id].blowIntensity =
                    Mathf.Clamp01(
                        data.intensity
                    );

                return;
            }


            // =================================================
            // PLAYER CONECTADO
            // =================================================

            if (
                data.type ==
                "player_connected"
            )
            {
                int id =
                    data.playerId;

                if (
                    id < 1 ||
                    id > 4
                )
                    return;

                players[id].connected =
                    true;

                players[id].x = 0;
                players[id].y = 0;
                players[id].z = 0;

                players[id].lookX = 0;
                players[id].lookY = 0;

                players[id].jump =
                    false;

                players[id].action =
                    false;

                players[id].blowIntensity =
                    0;

                Debug.Log(
                    $"🟢 PLAYER {id} CONECTADO"
                );

                return;
            }


            // =================================================
            // PLAYER DESCONECTADO
            // =================================================

            if (
                data.type ==
                "player_disconnected"
            )
            {
                int id =
                    data.playerId;

                if (
                    id < 1 ||
                    id > 4
                )
                    return;

                players[id].connected =
                    false;

                players[id].x = 0;
                players[id].y = 0;
                players[id].z = 0;

                players[id].lookX = 0;
                players[id].lookY = 0;

                players[id].jump =
                    false;

                players[id].action =
                    false;

                players[id].blowIntensity =
                    0;

                Debug.Log(
                    $"🔴 PLAYER {id} DESCONECTADO"
                );

                return;
            }


            // =================================================
            // BOTONES
            // =================================================

            if (
                data.type ==
                "button"
            )
            {
                int id =
                    data.playerId;

                if (
                    id < 1 ||
                    id > 4
                )
                    return;


                if (
                    data.button ==
                    "jump"
                )
                {
                    players[id].jump =
                        data.pressed;

                    Debug.Log(
                        $"🦘 SALTO P{id}: {data.pressed}"
                    );
                }


                if (
                    data.button ==
                    "action"
                )
                {
                    players[id].action =
                        data.pressed;

                    Debug.Log(
                        $"⚡ ACCIÓN P{id}: {data.pressed}"
                    );
                }

                return;
            }

        }
        catch (Exception error)
        {
            Debug.LogError(
                $"❌ ERROR PROCESANDO MENSAJE: {error.Message}"
            );
        }
    }


    // =====================================================
    // MOVIMIENTO
    // =====================================================

    public Vector2 GetMovement(
        int playerId
    )
    {
        if (
            playerId < 1 ||
            playerId > 4
        )
            return Vector2.zero;

        return new Vector2(
            players[playerId].x,
            players[playerId].y
        );
    }


    // =====================================================
    // LOOK
    // =====================================================

    public Vector2 GetLook(
        int playerId
    )
    {
        if (
            playerId < 1 ||
            playerId > 4
        )
            return Vector2.zero;

        Vector2 look =
            new Vector2(
                players[playerId].lookX,
                players[playerId].lookY
            );


        // Consumir delta una sola vez

        players[playerId].lookX =
            0f;

        players[playerId].lookY =
            0f;


        return look;
    }


    // =====================================================
    // SOPLIDO
    // =====================================================

    public float GetBlowIntensity(
        int playerId
    )
    {
        if (
            playerId < 1 ||
            playerId > 4
        )
            return 0f;

        return players[playerId].blowIntensity;
    }


    // =====================================================
    // ACELERÓMETRO
    // =====================================================

    public Vector3 GetAccelerometer(
        int playerId
    )
    {
        if (
            playerId < 1 ||
            playerId > 4
        )
            return Vector3.zero;

        return new Vector3(
            players[playerId].x,
            players[playerId].y,
            players[playerId].z
        );
    }


    // =====================================================
    // PLAYER CONECTADO
    // =====================================================

    public bool IsPlayerConnected(
        int playerId
    )
    {
        if (
            playerId < 1 ||
            playerId > 4
        )
            return false;

        return players[playerId].connected;
    }


    // =====================================================
    // SALTO
    // =====================================================

    public bool IsJumpPressed(
        int playerId
    )
    {
        if (
            playerId < 1 ||
            playerId > 4
        )
            return false;

        return players[playerId].jump;
    }


    // =====================================================
    // ACCIÓN
    // =====================================================

    public bool IsActionPressed(
        int playerId
    )
    {
        if (
            playerId < 1 ||
            playerId > 4
        )
            return false;

        return players[playerId].action;
    }


    // =====================================================
    // CONEXIÓN
    // =====================================================

    public bool IsConnected()
    {
        return
            client != null &&
            client.Connected;
    }


    // =====================================================
    // JSON
    // =====================================================

    [Serializable]
    private class MessageData
    {
        public string type;

        public int playerId;

        public float x;
        public float y;
        public float z;

        public float intensity;

        public string button;

        public bool pressed;
    }


    // =====================================================
    // CERRAR
    // =====================================================

    private void OnApplicationQuit()
    {
        try
        {
            if (
                receiveThread != null &&
                receiveThread.IsAlive
            )
            {
                receiveThread.Abort();
            }

            if (
                stream != null
            )
            {
                stream.Close();
            }

            if (
                client != null
            )
            {
                client.Close();
            }
        }
        catch
        {
        }
    }
}