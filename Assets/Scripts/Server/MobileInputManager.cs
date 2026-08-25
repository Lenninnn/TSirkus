using UnityEngine;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;


public class MobileInputManager : MonoBehaviour
{

    // =====================================================
    // CONFIGURACIÓN
    // =====================================================

    [Header("Servidor")]

    [SerializeField]
    private string serverIP = "10.33.42.88";

    [SerializeField]
    private int serverPort = 9000;


    // =====================================================
    // TCP
    // =====================================================

    private TcpClient client;

    private NetworkStream stream;

    private Thread receiveThread;

    private bool connected = false;


    // =====================================================
    // MENSAJES RECIBIDOS
    // =====================================================

    private ConcurrentQueue<string> messageQueue =
        new ConcurrentQueue<string>();


    // =====================================================
    // DATOS DE LOS 4 JUGADORES
    // =====================================================

    [Serializable]
    public class PlayerInput
    {

        public float x;

        public float y;

        public float z;

    }


    private PlayerInput[] players =
        new PlayerInput[5];


    // =====================================================
    // START
    // =====================================================

    void Start()
    {

        Debug.Log(
            "===================================="
        );

        Debug.Log(
            "🎮 MOBILE INPUT MANAGER"
        );

        Debug.Log(
            "===================================="
        );


        // Crear datos para jugadores 1-4

        for (
            int i = 1;
            i <= 4;
            i++
        )
        {

            players[i] =
                new PlayerInput();

        }


        ConnectToServer();

    }


    // =====================================================
    // CONECTAR A NODE.JS
    // =====================================================

    void ConnectToServer()
    {

        try
        {

            Debug.Log(
                $"🔌 Conectando a {serverIP}:{serverPort}"
            );


            client =
                new TcpClient();


            client.Connect(
                serverIP,
                serverPort
            );


            stream =
                client.GetStream();


            connected =
                true;


            Debug.Log(
                "✅ UNITY CONECTADO AL SERVIDOR TCP"
            );


            // Crear hilo para recibir datos

            receiveThread =
                new Thread(
                    ReceiveData
                );


            receiveThread.IsBackground =
                true;


            receiveThread.Start();

        }

        catch (Exception e)
        {

            Debug.LogError(
                "❌ ERROR CONECTANDO UNITY: " +
                e.Message
            );

        }

    }


    // =====================================================
    // RECIBIR DATOS
    // =====================================================

    void ReceiveData()
    {

        byte[] buffer =
            new byte[4096];


        StringBuilder messageBuffer =
            new StringBuilder();


        try
        {

            while (
                connected &&
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


                if (
                    bytesRead <= 0
                )
                {

                    break;

                }


                string data =
                    Encoding.UTF8.GetString(
                        buffer,
                        0,
                        bytesRead
                    );


                messageBuffer.Append(
                    data
                );


                // =====================================
                // SEPARAR MENSAJES
                // =====================================

                string current =
                    messageBuffer.ToString();


                string[] messages =
                    current.Split(
                        '\n'
                    );


                // Guardar último fragmento

                messageBuffer =
                    new StringBuilder(
                        messages[
                            messages.Length - 1
                        ]
                    );


                // Procesar mensajes completos

                for (
                    int i = 0;
                    i < messages.Length - 1;
                    i++
                )
                {

                    string message =
                        messages[i].Trim();


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

            }

        }

        catch (
            Exception e
        )
        {

            if (
                connected
            )
            {

                Debug.LogError(
                    "❌ Error recibiendo TCP: " +
                    e.Message
                );

            }

        }

    }


    // =====================================================
    // UPDATE
    // =====================================================

    void Update()
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
    // PROCESAR MENSAJE
    // =====================================================

    void ProcessMessage(
        string message
    )
    {

        try
        {

            AccelerometerMessage data =
                JsonUtility.FromJson
                <AccelerometerMessage>(
                    message
                );


            if (
                data.type ==
                "accelerometer"
            )
            {

                int playerId =
                    data.playerId;


                if (
                    playerId >= 1 &&
                    playerId <= 4
                )
                {

                    players[
                        playerId
                    ].x =
                        data.x;


                    players[
                        playerId
                    ].y =
                        data.y;


                    players[
                        playerId
                    ].z =
                        data.z;


                    Debug.Log(

                        $"🎮 P{playerId} | " +
                        $"X: {data.x:F2} | " +
                        $"Y: {data.y:F2} | " +
                        $"Z: {data.z:F2}"

                    );

                }

            }

        }

        catch (
            Exception e
        )
        {

            Debug.LogError(
                "❌ Error procesando JSON: " +
                e.Message
            );

        }

    }


    // =====================================================
    // ESTRUCTURA DEL ACELERÓMETRO
    // =====================================================

    [Serializable]
    private class AccelerometerMessage
    {

        public string type;

        public int playerId;

        public float x;

        public float y;

        public float z;

    }


    // =====================================================
    // OBTENER DATOS DEL JUGADOR
    // =====================================================

    public Vector3 GetAccelerometer(
        int playerId
    )
    {

        if (
            playerId >= 1 &&
            playerId <= 4
        )
        {

            return new Vector3(

                players[playerId].x,

                players[playerId].y,

                players[playerId].z

            );

        }


        return Vector3.zero;

    }


    // =====================================================
    // ESTADO DE CONEXIÓN
    // =====================================================

    public bool IsConnected()
    {

        return connected;

    }


    // =====================================================
    // CERRAR CONEXIÓN
    // =====================================================

    void OnApplicationQuit()
    {

        connected =
            false;


        try
        {

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
            // Ignorar errores al cerrar
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
            // Ignorar errores al cerrar
        }

    }

}