using UnityEditor;
using UnityEngine;

// DEBUG / SOLO EDITOR. Ventana para mover "teléfonos" simulados P1-P4.
// El simulador está apagado por defecto y se apaga solo al cerrar Unity.
// Ver SensorServerSimulator para el contrato de datos que reproduce.
public class SensorSimulatorWindow : EditorWindow
{
    private const string EnableMenu = "TSirkus/Debug/Simulador de sensores/Activar (EDITOR)";

    [MenuItem("TSirkus/Debug/Simulador de sensores/Abrir ventana (EDITOR)")]
    private static void Open() => GetWindow<SensorSimulatorWindow>("Simulador sensores");

    [MenuItem(EnableMenu)]
    private static void Toggle()
    {
        SensorServerSimulator.Enabled = !SensorServerSimulator.Enabled;
        Debug.LogWarning(SensorServerSimulator.Enabled
            ? "[SENSOR SIM] ACTIVADO: el próximo Play Mode usará teléfonos simulados en 127.0.0.1 (no los reales)."
            : "[SENSOR SIM] Desactivado: Play Mode usa el servidor real.");
    }

    [MenuItem(EnableMenu, true)]
    private static bool ToggleValidate()
    {
        Menu.SetChecked(EnableMenu, SensorServerSimulator.Enabled);
        return !EditorApplication.isPlaying;
    }

    private void OnInspectorUpdate() => Repaint();

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "DEBUG/EDITOR. Reemplaza a los celulares en Play Mode por un servidor local con el mismo " +
            "JSON. Se activa antes de entrar a Play Mode y se apaga solo al cerrar Unity.",
            MessageType.Warning);

        using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            SensorServerSimulator.Enabled = EditorGUILayout.ToggleLeft("Activado (próximo Play Mode)", SensorServerSimulator.Enabled);

        EditorGUILayout.LabelField("Servidor", SensorServerSimulator.Running ? "corriendo" : "detenido");
        SensorServerSimulator.SendRateHz = EditorGUILayout.IntSlider("Frecuencia (Hz)", SensorServerSimulator.SendRateHz, 5, 120);
        SensorServerSimulator.AutoSubmitPhrase = EditorGUILayout.ToggleLeft("Responder la frase automáticamente", SensorServerSimulator.AutoSubmitPhrase);

        for (int id = 1; id <= 4; id++)
        {
            SensorServerSimulator.PhoneState phone = SensorServerSimulator.Snapshot(id);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"P{id}  (mensajes enviados: {phone.sentCount})", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            bool connected = EditorGUILayout.ToggleLeft("Conectado", phone.connected);
            bool sending = EditorGUILayout.ToggleLeft("Enviando datos (apagar = pérdida de datos)", phone.sending);
            float neutral = EditorGUILayout.Slider("Postura neutra (0 plano, 90 vertical)", phone.neutralPitch, 0f, 90f);
            float side = EditorGUILayout.Slider("Izquierda (-) / derecha (+)", phone.side, -60f, 60f);
            float pitch = EditorGUILayout.Slider("Abajo (-) / arriba (+)", phone.pitch, -45f, 45f);
            float noise = EditorGUILayout.Slider("Ruido (m/s²)", phone.noise, 0f, 0.5f);
            bool invert = EditorGUILayout.ToggleLeft("Signos invertidos (algunos teléfonos)", phone.invertAllAxes);

            if (EditorGUI.EndChangeCheck())
            {
                SensorServerSimulator.Configure(id, p =>
                {
                    p.connected = connected;
                    p.sending = sending;
                    p.neutralPitch = neutral;
                    p.side = side;
                    p.pitch = pitch;
                    p.noise = noise;
                    p.invertAllAxes = invert;
                });
            }

            if (GUILayout.Button("Neutral"))
                SensorServerSimulator.SetPose(id, 0f, 0f);
        }
    }
}
