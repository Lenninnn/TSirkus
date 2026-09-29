using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class MobileInputManager : MonoBehaviour
{
    [Header("Servidor")]
    [SerializeField] private string serverIP = "127.0.0.1";

    [SerializeField] private int serverPort = 9000;


    [Serializable]
    public class PlayerInput
    {
        // =================================================
        // MOVIMIENTO / JOYSTICK
        // =================================================

        public float movementX;

        public float movementY;

        public float movementZ;


        // =================================================
        // ACELERÓMETRO FÍSICO
        // =================================================

        public float accelerometerX;

        public float accelerometerY;

        public float accelerometerZ;


        // =================================================
        // ORIENTACIÓN
        // =================================================

        public float alpha;

        public float beta;

        public float gamma;


        // =================================================
        // LOOK
        // =================================================

        public float lookX;

        public float lookY;


        // =================================================
        // ESTADOS
        // =================================================

        public bool connected;

        public bool jump;

        public bool action;


        // =================================================
        // MICRÓFONO
        // =================================================

        public float blowIntensity;
    }


    private readonly PlayerInput[] players =
        new PlayerInput[5];


    // =====================================================
    // TCP
    // =====================================================

    private TcpClient tcpClient;

    private NetworkStream networkStream;

    private Thread receiveThread;

    private volatile bool running;


    // =====================================================
    // COLA
    // =====================================================

    private readonly ConcurrentQueue<string>
        messageQueue =
            new ConcurrentQueue<string>();


    // =====================================================
    // UNITY
    // =====================================================

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);


        for (
            int i = 0;
            i < players.Length;
            i++
        )
        {
            players[i] =
                new PlayerInput();
        }
    }


    private void Start()
    {
        ConnectToServer();
    }


    private void Update()
    {
        ProcessMessages();
    }


    // =====================================================
    // CONECTAR
    // =====================================================

    private void ConnectToServer()
    {
        try
        {
            Debug.Log(
                $"📡 Conectando a Unity TCP: {serverIP}:{serverPort}"
            );


            tcpClient =
                new TcpClient();


            tcpClient.Connect(
                serverIP,
                serverPort
            );


            networkStream =
                tcpClient.GetStream();


            running =
                true;


            receiveThread =
                new Thread(
                    ReceiveLoop
                );


            receiveThread.IsBackground =
                true;


            receiveThread.Start();


            Debug.Log(
                "✅ MobileInputManager conectado al servidor"
            );
        }
        catch (Exception error)
        {
            Debug.LogError(
                $"❌ Error conectando MobileInputManager: {error.Message}"
            );
        }
    }


    // =====================================================
    // RECIBIR TCP
    // =====================================================

    private void ReceiveLoop()
    {
        byte[] buffer =
            new byte[8192];


        StringBuilder dataBuffer =
            new StringBuilder();


        while (
            running
        )
        {
            try
            {
                if (
                    networkStream == null ||
                    tcpClient == null ||
                    !tcpClient.Connected
                )
                {
                    break;
                }


                int bytesRead =
                    networkStream.Read(
                        buffer,
                        0,
                        buffer.Length
                    );


                if (
                    bytesRead <= 0
                )
                {
                    break;
                }


                string incoming =
                    Encoding.UTF8.GetString(
                        buffer,
                        0,
                        bytesRead
                    );


                dataBuffer.Append(
                    incoming
                );


                string currentData =
                    dataBuffer.ToString();


                int newlineIndex;


                while (
                    (
                        newlineIndex =
                            currentData.IndexOf('\n')
                    ) >= 0
                )
                {
                    string message =
                        currentData
                            .Substring(
                                0,
                                newlineIndex
                            )
                            .Trim();


                    currentData =
                        currentData.Substring(
                            newlineIndex + 1
                        );


                    if (
                        !string.IsNullOrEmpty(
                            message
                        )
                    )
                    {
                        messageQueue.Enqueue(
                            message
                        );
                    }
                }


                dataBuffer.Clear();


                dataBuffer.Append(
                    currentData
                );
            }
            catch (Exception error)
            {
                if (
                    running
                )
                {
                    Debug.LogError(
                        $"❌ Error recibiendo TCP: {error.Message}"
                    );
                }


                break;
            }
        }
    }


    // =====================================================
    // PROCESAR MENSAJES
    // =====================================================

    private void ProcessMessages()
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
    // PROCESAR JSON
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


            if (
                data == null
            )
            {
                return;
            }


            int playerId =
                data.playerId;


            if (
                playerId < 1 ||
                playerId > 4
            )
            {
                return;
            }


            PlayerInput player =
                players[playerId];


            // =================================================
            // MOVIMIENTO / JOYSTICK
            // =================================================

            if (
                data.type ==
                "movement"
            )
            {
                player.movementX =
                    data.x;


                player.movementY =
                    data.y;


                player.movementZ =
                    data.z;


                return;
            }


            // =================================================
            // ACELERÓMETRO FÍSICO
            // =================================================

            if (
                data.type ==
                "accelerometer"
            )
            {
                player.accelerometerX =
                    data.x;


                player.accelerometerY =
                    data.y;


                player.accelerometerZ =
                    data.z;


                Debug.Log(
                    $"📱 P{playerId} ACCELEROMETER FÍSICO → " +
                    $"X:{data.x:F2} | " +
                    $"Y:{data.y:F2} | " +
                    $"Z:{data.z:F2}"
                );


                return;
            }


            // =================================================
            // ORIENTACIÓN
            // =================================================

            if (
                data.type ==
                "orientation"
            )
            {
                player.alpha =
                    data.alpha;


                player.beta =
                    data.beta;


                player.gamma =
                    data.gamma;


                return;
            }


            // =================================================
            // LOOK
            // =================================================

            if (
                data.type ==
                "look"
            )
            {
                player.lookX +=
                    data.x;


                player.lookY +=
                    data.y;


                return;
            }


            // =================================================
            // BLOW
            // =================================================

            if (
                data.type ==
                "blow"
            )
            {
                player.blowIntensity =
                    data.intensity;


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
                ResetPlayerInput(
                    player
                );


                player.connected =
                    true;


                Debug.Log(
                    $"📱 PLAYER {playerId} CONECTADO"
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
                ResetPlayerInput(
                    player
                );


                player.connected =
                    false;


                Debug.Log(
                    $"📱 PLAYER {playerId} DESCONECTADO"
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
                if (
                    data.button ==
                    "jump"
                )
                {
                    player.jump =
                        data.pressed;
                }


                if (
                    data.button ==
                    "action"
                )
                {
                    player.action =
                        data.pressed;
                }


                return;
            }
        }
        catch (Exception error)
        {
            Debug.LogError(
                $"❌ Error procesando JSON: {error.Message}\n{message}"
            );
        }
    }


    // =====================================================
    // RESET PLAYER
    // =====================================================

    private void ResetPlayerInput(
        PlayerInput player
    )
    {
        // Movimiento

        player.movementX =
            0f;

        player.movementY =
            0f;

        player.movementZ =
            0f;


        // Acelerómetro físico

        player.accelerometerX =
            0f;

        player.accelerometerY =
            0f;

        player.accelerometerZ =
            0f;


        // Orientación

        player.alpha =
            0f;

        player.beta =
            0f;

        player.gamma =
            0f;


        // Look

        player.lookX =
            0f;

        player.lookY =
            0f;


        // Botones

        player.jump =
            false;

        player.action =
            false;


        // Micrófono

        player.blowIntensity =
            0f;
    }


    // =====================================================
    // GET MOVEMENT
    // =====================================================

    public Vector2 GetMovement(
        int playerId
    )
    {
        if (
            !IsValidPlayer(
                playerId
            )
        )
        {
            return Vector2.zero;
        }


        return new Vector2(
            players[playerId].movementX,
            players[playerId].movementY
        );
    }


    // =====================================================
    // GET LOOK
    // =====================================================

    public Vector2 GetLook(
        int playerId
    )
    {
        if (
            !IsValidPlayer(
                playerId
            )
        )
        {
            return Vector2.zero;
        }


        Vector2 look =
            new Vector2(
                players[playerId].lookX,
                players[playerId].lookY
            );


        players[playerId].lookX =
            0f;


        players[playerId].lookY =
            0f;


        return look;
    }


    // =====================================================
    // GET ORIENTATION
    // =====================================================

    public Vector3 GetOrientation(
        int playerId
    )
    {
        if (
            !IsValidPlayer(
                playerId
            )
        )
        {
            return Vector3.zero;
        }


        return new Vector3(
            players[playerId].alpha,
            players[playerId].beta,
            players[playerId].gamma
        );
    }


    // =====================================================
    // GET ACCELEROMETER
    // =====================================================

    public Vector3 GetAccelerometer(
        int playerId
    )
    {
        if (
            !IsValidPlayer(
                playerId
            )
        )
        {
            return Vector3.zero;
        }


        return new Vector3(
            players[playerId].accelerometerX,
            players[playerId].accelerometerY,
            players[playerId].accelerometerZ
        );
    }


    // =====================================================
    // GET BLOW
    // =====================================================

    public float GetBlowIntensity(
        int playerId
    )
    {
        if (
            !IsValidPlayer(
                playerId
            )
        )
        {
            return 0f;
        }


        return players[playerId]
            .blowIntensity;
    }


    // =====================================================
    // PLAYER CONNECTED
    // =====================================================

    public bool IsPlayerConnected(
        int playerId
    )
    {
        if (
            !IsValidPlayer(
                playerId
            )
        )
        {
            return false;
        }


        return players[playerId]
            .connected;
    }


    // =====================================================
    // JUMP
    // =====================================================

    public bool IsJumpPressed(
        int playerId
    )
    {
        if (
            !IsValidPlayer(
                playerId
            )
        )
        {
            return false;
        }


        return players[playerId]
            .jump;
    }


    // =====================================================
    // ACTION
    // =====================================================

    public bool IsActionPressed(
        int playerId
    )
    {
        if (
            !IsValidPlayer(
                playerId
            )
        )
        {
            return false;
        }


        return players[playerId]
            .action;
    }


    // =====================================================
    // CONEXIÓN GENERAL
    // =====================================================

    public bool IsConnected()
    {
        return (
            tcpClient != null &&
            tcpClient.Connected
        );
    }


    // =====================================================
    // VALIDAR PLAYER
    // =====================================================

    private bool IsValidPlayer(
        int playerId
    )
    {
        return (
            playerId >= 1 &&
            playerId <= 4
        );
    }


    // =====================================================
    // JSON
    // =====================================================

    [Serializable]
    private class MessageData
    {
        public string type;

        public int playerId;

        public string source;

        public float x;

        public float y;

        public float z;

        public float alpha;

        public float beta;

        public float gamma;

        public float intensity;

        public string button;

        public bool pressed;
    }


    // =====================================================
    // QUIT
    // =====================================================

    private void OnApplicationQuit()
    {
        running =
            false;


        try
        {
            if (
                networkStream != null
            )
            {
                networkStream.Close();
            }
        }
        catch
        {
        }


        try
        {
            if (
                tcpClient != null
            )
            {
                tcpClient.Close();
            }
        }
        catch
        {
        }


        try
        {
            if (
                receiveThread != null &&
                receiveThread.IsAlive
            )
            {
                receiveThread.Abort();
            }
        }
        catch
        {
        }
    }
}