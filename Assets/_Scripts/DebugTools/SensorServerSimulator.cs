#if UNITY_EDITOR
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

// DEBUG / SOLO EDITOR. No existe en builds (todo el archivo está dentro de #if UNITY_EDITOR).
//
// Simula el servidor de celulares para probar sin teléfonos. Está APAGADO por defecto y el
// interruptor vive en SessionState (se apaga solo al cerrar Unity). Se activa desde
// TSirkus/Debug/Simulador de sensores.
//
// Cuando está activo y empieza Play Mode:
//   1. Abre en 127.0.0.1 dos servidores TCP: sensores (9100) y frase (9101).
//   2. Redirige SOLO EN MEMORIA MobileInputManager y PhraseNetworkBridge a esos puertos
//      (antes de su Start). La escena no se modifica; los puertos reales (9000/9001) no se tocan.
//   3. Envía por el socket exactamente lo que manda el servidor real: una línea JSON por mensaje,
//      terminada en '\n', con los campos de MobileInputManager.MessageData:
//        {"type":"player_connected","playerId":1}
//        {"type":"puzzle_accelerometer","playerId":1,"x":0.00,"y":5.63,"z":8.04}
//      Así se prueba la cadena completa: socket → hilo de recepción → cola → JSON → jugador.
//
// Modelo del teléfono (convención W3C DeviceMotionEvent.accelerationIncludingGravity, m/s²):
// x = derecha de la pantalla, y = arriba de la pantalla, z = sale de la pantalla. En reposo el
// vector apunta hacia arriba del mundo (plano boca arriba = (0, 0, +9.81)).
//   neutralPitch: inclinación de "sostener cómodo" (0 = plano, 90 = vertical).
//   side:  + = borde derecho hacia abajo (girar a la derecha).
//   pitch: + = la parte trasera apunta más arriba (subir la linterna).
// invertAllAxes reproduce los teléfonos que entregan el vector con todos los signos invertidos.
public static class SensorServerSimulator
{
    public const string EnabledKey = "TSirkus.SensorSimulator.Enabled";
    public const int SensorPort = 9100;
    public const int PhrasePort = 9101;
    public const float Gravity = 9.81f;

    public class PhoneState
    {
        public bool connected = true;
        public bool sending = true;
        public float neutralPitch = 35f;
        public float side;
        public float pitch;
        public float noise = 0.04f;
        public bool invertAllAxes;
        public Vector3 linearAcceleration;
        public int sentCount;
    }

    public static bool Enabled
    {
        get => SessionState.GetBool(EnabledKey, false);
        set => SessionState.SetBool(EnabledKey, value);
    }

    public static bool Running => running;
    public static int SendRateHz = 60;
    public static string AccelerometerMessageType = "puzzle_accelerometer";

    // Respuesta automática a phrase_start_request (para cerrar el puzzle sin teléfonos).
    public static bool AutoSubmitPhrase = true;
    public static float AutoSubmitDelay = 1.5f;
    public static string[] PhraseToSubmit =
    {
        "CUANDO", "LAS", "LUCES", "SE", "APAGUEN", "NO",
        "OLVIDES", "AL", "PAYASO", "DETRÁS", "DE", "TI"
    };

    public static readonly List<string> PhraseRequests = new List<string>();

    private static readonly PhoneState[] phones =
    {
        null, new PhoneState(), new PhoneState(), new PhoneState(), new PhoneState()
    };

    private static readonly object stateLock = new object();
    private static volatile bool running;
    private static TcpListener sensorListener;
    private static TcpListener phraseListener;
    private static Thread sensorThread;
    private static Thread phraseThread;
    private static readonly ConcurrentQueue<string> phraseOutgoing = new ConcurrentQueue<string>();
    private static readonly ConcurrentQueue<string> phraseIncoming = new ConcurrentQueue<string>();
    private static readonly System.Random random = new System.Random(1234);

    // =====================================================
    // ESTADO DE CADA TELÉFONO (llamar desde el hilo principal)
    // =====================================================

    public static void SetPose(int playerId, float side, float pitch)
    {
        lock (stateLock)
        {
            phones[playerId].side = side;
            phones[playerId].pitch = pitch;
        }
    }

    public static void Configure(int playerId, Action<PhoneState> change)
    {
        lock (stateLock)
            change(phones[playerId]);
    }

    public static PhoneState Snapshot(int playerId)
    {
        lock (stateLock)
        {
            PhoneState p = phones[playerId];
            return new PhoneState
            {
                connected = p.connected, sending = p.sending, neutralPitch = p.neutralPitch,
                side = p.side, pitch = p.pitch, noise = p.noise, invertAllAxes = p.invertAllAxes,
                linearAcceleration = p.linearAcceleration, sentCount = p.sentCount
            };
        }
    }

    public static void ResetPhones()
    {
        lock (stateLock)
        {
            for (int i = 1; i <= 4; i++)
                phones[i] = new PhoneState();
        }
    }

    // Vector que mandaría el teléfono para una postura dada (sin ruido).
    public static Vector3 GravityFor(float neutralPitch, float side, float pitch)
    {
        float theta = (neutralPitch + pitch) * Mathf.Deg2Rad;
        float phi = side * Mathf.Deg2Rad;

        return new Vector3(
            -Mathf.Sin(phi),
            Mathf.Cos(phi) * Mathf.Sin(theta),
            Mathf.Cos(phi) * Mathf.Cos(theta)) * Gravity;
    }

    public static void SendRaw(string jsonLine) => sensorOutgoing.Enqueue(jsonLine);

    public static void SendPhraseSubmit(int playerId, string[] words)
    {
        StringBuilder json = new StringBuilder();
        json.Append("{\"type\":\"phrase_submit\",\"playerId\":").Append(playerId).Append(",\"words\":[");

        for (int i = 0; i < words.Length; i++)
        {
            if (i > 0) json.Append(',');
            json.Append('"').Append(words[i]).Append('"');
        }

        json.Append("]}");
        phraseOutgoing.Enqueue(json.ToString());
    }

    // =====================================================
    // ARRANQUE EN PLAY MODE
    // =====================================================

    private static readonly ConcurrentQueue<string> sensorOutgoing = new ConcurrentQueue<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnPlayModeSceneLoaded()
    {
        if (!Enabled)
            return;

        Start();
        RedirectClients();
        SceneManager.sceneLoaded += (_, __) => RedirectClients();
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.update += PumpMainThread;
    }

    private static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.ExitingPlayMode)
            return;

        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.update -= PumpMainThread;
        Stop();
    }

    // Cambia IP/puerto en memoria antes de que MobileInputManager y PhraseNetworkBridge conecten
    // (AfterSceneLoad corre después de Awake y antes de Start).
    private static void RedirectClients()
    {
        foreach (MobileInputManager manager in UnityEngine.Object.FindObjectsByType<MobileInputManager>(FindObjectsInactive.Include))
            Redirect(manager, SensorPort);

        foreach (PhraseNetworkBridge bridge in UnityEngine.Object.FindObjectsByType<PhraseNetworkBridge>(FindObjectsInactive.Include))
            Redirect(bridge, PhrasePort);
    }

    private static void Redirect(object target, int port)
    {
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        target.GetType().GetField("serverIP", flags)?.SetValue(target, "127.0.0.1");
        target.GetType().GetField("serverPort", flags)?.SetValue(target, port);
        Debug.LogWarning($"[SENSOR SIM] DEBUG/EDITOR: {target.GetType().Name} redirigido a 127.0.0.1:{port} (solo en memoria).");
    }

    public static void Start()
    {
        if (running)
            return;

        running = true;
        sensorListener = Listen(SensorPort);
        phraseListener = Listen(PhrasePort);
        sensorThread = new Thread(SensorLoop) { IsBackground = true, Name = "SensorSim" };
        phraseThread = new Thread(PhraseLoop) { IsBackground = true, Name = "PhraseSim" };
        sensorThread.Start();
        phraseThread.Start();
        Debug.LogWarning($"[SENSOR SIM] Servidor simulado activo en 127.0.0.1:{SensorPort} (sensores) y :{PhrasePort} (frase).");
    }

    public static void Stop()
    {
        running = false;
        try { sensorListener?.Stop(); } catch { }
        try { phraseListener?.Stop(); } catch { }
        sensorListener = null;
        phraseListener = null;
        Debug.Log("[SENSOR SIM] Servidor simulado detenido.");
    }

    private static TcpListener Listen(int port)
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, port);
        listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        listener.Start();
        return listener;
    }

    // =====================================================
    // SERVIDOR DE SENSORES (hilo propio, ritmo fijo como un teléfono)
    // =====================================================

    private static void SensorLoop()
    {
        while (running)
        {
            TcpClient client = null;

            try
            {
                client = sensorListener.AcceptTcpClient();
                client.NoDelay = true;
                NetworkStream stream = client.GetStream();
                bool[] announced = new bool[5];
                Stopwatch clock = Stopwatch.StartNew();
                long next = 0;

                while (running && client.Connected)
                {
                    StringBuilder batch = new StringBuilder();

                    while (sensorOutgoing.TryDequeue(out string raw))
                        batch.Append(raw).Append('\n');

                    lock (stateLock)
                    {
                        for (int id = 1; id <= 4; id++)
                        {
                            PhoneState phone = phones[id];

                            if (phone.connected != announced[id])
                            {
                                announced[id] = phone.connected;
                                batch.Append("{\"type\":\"")
                                    .Append(phone.connected ? "player_connected" : "player_disconnected")
                                    .Append("\",\"playerId\":").Append(id).Append("}\n");
                            }

                            if (!phone.connected || !phone.sending)
                                continue;

                            Vector3 a = GravityFor(phone.neutralPitch, phone.side, phone.pitch) + phone.linearAcceleration;
                            a += new Vector3(Noise(phone.noise), Noise(phone.noise), Noise(phone.noise));

                            if (phone.invertAllAxes)
                                a = -a;

                            batch.Append("{\"type\":\"").Append(AccelerometerMessageType)
                                .Append("\",\"playerId\":").Append(id)
                                .Append(",\"x\":").Append(a.x.ToString("F4", CultureInfo.InvariantCulture))
                                .Append(",\"y\":").Append(a.y.ToString("F4", CultureInfo.InvariantCulture))
                                .Append(",\"z\":").Append(a.z.ToString("F4", CultureInfo.InvariantCulture))
                                .Append("}\n");
                            phone.sentCount++;
                        }
                    }

                    byte[] bytes = Encoding.UTF8.GetBytes(batch.ToString());
                    stream.Write(bytes, 0, bytes.Length);

                    next += 1000 / Mathf.Max(1, SendRateHz);
                    int wait = (int)(next - clock.ElapsedMilliseconds);
                    if (wait > 0)
                        Thread.Sleep(wait);
                }
            }
            catch (Exception error)
            {
                if (running)
                    Debug.Log($"[SENSOR SIM] Cliente de sensores desconectado: {error.Message}");
            }
            finally
            {
                client?.Close();
            }
        }
    }

    // Gaussiana (Box-Muller) con desviación sigma.
    private static float Noise(float sigma)
    {
        if (sigma <= 0f)
            return 0f;

        double u1 = 1.0 - random.NextDouble();
        double u2 = random.NextDouble();
        return (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2)) * sigma;
    }

    // =====================================================
    // SERVIDOR DE FRASE
    // =====================================================

    private static void PhraseLoop()
    {
        while (running)
        {
            TcpClient client = null;

            try
            {
                client = phraseListener.AcceptTcpClient();
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[8192];
                StringBuilder pending = new StringBuilder();

                while (running && client.Connected)
                {
                    while (phraseOutgoing.TryDequeue(out string json))
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(json + "\n");
                        stream.Write(bytes, 0, bytes.Length);
                    }

                    if (!stream.DataAvailable)
                    {
                        Thread.Sleep(20);
                        continue;
                    }

                    int read = stream.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                        break;

                    pending.Append(Encoding.UTF8.GetString(buffer, 0, read));
                    string text = pending.ToString();
                    int newline;

                    while ((newline = text.IndexOf('\n')) >= 0)
                    {
                        phraseIncoming.Enqueue(text.Substring(0, newline).Trim());
                        text = text.Substring(newline + 1);
                    }

                    pending.Clear().Append(text);
                }
            }
            catch (Exception error)
            {
                if (running)
                    Debug.Log($"[SENSOR SIM] Cliente de frase desconectado: {error.Message}");
            }
            finally
            {
                client?.Close();
            }
        }
    }

    private static readonly List<(double time, int playerId)> scheduledSubmits = new List<(double, int)>();

    // Hilo principal: registra lo que Unity manda al servidor de frase y responde.
    private static void PumpMainThread()
    {
        while (phraseIncoming.TryDequeue(out string line))
        {
            if (string.IsNullOrEmpty(line))
                continue;

            PhraseRequests.Add(line);
            Debug.Log($"[SENSOR SIM] Unity → servidor de frase: {line}");

            if (AutoSubmitPhrase && line.Contains("\"phrase_start_request\""))
            {
                PhraseProbe probe = JsonUtility.FromJson<PhraseProbe>(line);
                scheduledSubmits.Add((EditorApplication.timeSinceStartup + AutoSubmitDelay + probe.playerId * 0.25, probe.playerId));
            }
        }

        for (int i = scheduledSubmits.Count - 1; i >= 0; i--)
        {
            if (EditorApplication.timeSinceStartup < scheduledSubmits[i].time)
                continue;

            int id = scheduledSubmits[i].playerId;
            scheduledSubmits.RemoveAt(i);
            SendPhraseSubmit(id, PhraseToSubmit);
            Debug.Log($"[SENSOR SIM] Servidor de frase → Unity: phrase_submit P{id}");
        }
    }

    [Serializable]
    private class PhraseProbe
    {
        public string type;
        public int playerId;
    }
}
#endif
