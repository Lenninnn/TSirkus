using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class PhraseNetworkBridge : MonoBehaviour
{
    [Header("Server")]
    [SerializeField] private string serverIP = "127.0.0.1";
    [SerializeField] private int serverPort = 9001;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private TcpClient tcpClient;
    private NetworkStream networkStream;
    private Thread receiveThread;
    private volatile bool running;

    private readonly ConcurrentQueue<string> incomingMessages =
        new ConcurrentQueue<string>();

    private readonly ConcurrentQueue<string> pendingOutgoing =
        new ConcurrentQueue<string>();

    public event Action<int, string[]> OnPhraseSubmission;
    public event Action OnPhraseTimeout;

    private void Start()
    {
        ConnectToServer();
    }

    private void Update()
    {
        FlushOutgoingMessages();
        ProcessIncomingMessages();
    }

    private void ConnectToServer()
    {
        try
        {
            if (running)
                return;

            DebugLog($"🧩📡 Conectando PhraseNetworkBridge a {serverIP}:{serverPort}");

            tcpClient = new TcpClient();
            tcpClient.Connect(serverIP, serverPort);
            networkStream = tcpClient.GetStream();
            running = true;

            receiveThread = new Thread(ReceiveLoop);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            DebugLog("🧩✅ PhraseNetworkBridge conectado al servidor.");
        }
        catch (Exception error)
        {
            Debug.LogError(
                $"🧩❌ Error conectando PhraseNetworkBridge: {error.Message}"
            );
        }
    }

    private void ReceiveLoop()
    {
        byte[] buffer = new byte[8192];
        StringBuilder dataBuffer = new StringBuilder();

        while (running)
        {
            try
            {
                if (networkStream == null || tcpClient == null || !tcpClient.Connected)
                    break;

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
                    string line = currentData
                        .Substring(0, newlineIndex)
                        .Trim();

                    currentData = currentData.Substring(
                        newlineIndex + 1
                    );

                    if (!string.IsNullOrEmpty(line))
                    {
                        incomingMessages.Enqueue(line);
                    }
                }

                dataBuffer.Clear();
                dataBuffer.Append(currentData);
            }
            catch (Exception error)
            {
                if (running)
                {
                    Debug.LogError(
                        $"🧩❌ Error recibiendo PhraseNetworkBridge: {error.Message}"
                    );
                }

                break;
            }
        }
    }

    private void ProcessIncomingMessages()
    {
        while (incomingMessages.TryDequeue(out string message))
        {
            ProcessIncomingMessage(message);
        }
    }

    private void ProcessIncomingMessage(string message)
    {
        try
        {
            PhraseMessage data =
                JsonUtility.FromJson<PhraseMessage>(message);

            if (data == null || string.IsNullOrWhiteSpace(data.type))
                return;

            if (data.type == "phrase_submit")
            {
                if (data.playerId < 1 || data.playerId > 4)
                    return;

                string[] submittedWords = data.words;

                if (submittedWords == null || submittedWords.Length == 0)
                {
                    submittedWords = data.BuildWordsArray();
                }

                DebugLog(
                    $"🧩📨 Phrase submit P{data.playerId}: " +
                    $"{submittedWords.Length} palabras"
                );

                OnPhraseSubmission?.Invoke(
                    data.playerId,
                    submittedWords
                );

                return;
            }

            if (data.type == "phrase_timeout")
            {
                DebugLog("🧩⏰ El servidor informó TIMEOUT de la frase.");
                OnPhraseTimeout?.Invoke();
                return;
            }
        }
        catch (Exception error)
        {
            Debug.LogError(
                $"🧩❌ Error procesando mensaje de frase: {error.Message}"
            );
        }
    }

    public void StartPhraseForPlayer(
        int playerId,
        float duration,
        string word1,
        string word2,
        string word3
    )
    {
        if (playerId < 1 || playerId > 4)
        {
            Debug.LogError(
                $"🧩❌ Player inválido en StartPhraseForPlayer: {playerId}"
            );

            return;
        }

        PhraseMessage data = new PhraseMessage
        {
            type = "phrase_start_request",
            playerId = playerId,
            duration = duration,
            word1 = word1,
            word2 = word2,
            word3 = word3
        };

        SendJson(data);

        DebugLog(
            $"🧩➡️ Phrase start solicitado para P{playerId}: " +
            $"[{word1}] [{word2}] [{word3}]"
        );
    }

    public void SendPlayerResult(
        int playerId,
        bool correct,
        string message
    )
    {
        PhraseMessage data = new PhraseMessage
        {
            type = "phrase_player_result",
            playerId = playerId,
            correct = correct,
            message = message
        };

        SendJson(data);
    }

    public void SendAllComplete()
    {
        PhraseMessage data = new PhraseMessage
        {
            type = "phrase_all_complete"
        };

        SendJson(data);
    }

    public void SendForceTimeout()
    {
        PhraseMessage data = new PhraseMessage
        {
            type = "phrase_force_timeout"
        };

        SendJson(data);
    }

    private void SendJson(PhraseMessage data)
    {
        string json =
            JsonUtility.ToJson(data);

        pendingOutgoing.Enqueue(json);
    }

    private void FlushOutgoingMessages()
    {
        if (!running || networkStream == null || tcpClient == null || !tcpClient.Connected)
            return;

        while (pendingOutgoing.TryDequeue(out string json))
        {
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(json + "\n");
                networkStream.Write(bytes, 0, bytes.Length);
                networkStream.Flush();

                DebugLog($"🧩➡️ SERVER: {json}");
            }
            catch (Exception error)
            {
                Debug.LogError(
                    $"🧩❌ Error enviando mensaje de frase: {error.Message}"
                );

                pendingOutgoing.Enqueue(json);
                return;
            }
        }
    }

    private void DebugLog(string message)
    {
        if (debugLogs)
        {
            Debug.Log(message);
        }
    }

    [Serializable]
    private class PhraseMessage
    {
        public string type;
        public int playerId;
        public float duration;
        public string word1;
        public string word2;
        public string word3;
        public string word4;
        public string word5;
        public string word6;
        public string word7;
        public string word8;
        public string word9;
        public string word10;
        public string word11;
        public string word12;
        public string[] words;
        public bool correct;
        public string message;

        public string[] BuildWordsArray()
        {
            return new[]
            {
                word1,
                word2,
                word3,
                word4,
                word5,
                word6,
                word7,
                word8,
                word9,
                word10,
                word11,
                word12
            };
        }
    }

    private void OnApplicationQuit()
    {
        running = false;

        try
        {
            if (networkStream != null)
                networkStream.Close();
        }
        catch
        {
        }

        try
        {
            if (tcpClient != null)
                tcpClient.Close();
        }
        catch
        {
        }

        try
        {
            if (receiveThread != null && receiveThread.IsAlive)
                receiveThread.Abort();
        }
        catch
        {
        }
    }
}
