using UnityEngine;
using UnityEngine.InputSystem;

public class MobileSensorTest : MonoBehaviour
{
    private float timer;

    private void Start()
    {
        if (Accelerometer.current != null)
        {
            InputSystem.EnableDevice(Accelerometer.current);
            Debug.Log("Acelerómetro detectado");
        }
        else
        {
            Debug.LogWarning("No se detectó acelerómetro");
        }

        if (UnityEngine.InputSystem.Gyroscope.current != null)
        {
            InputSystem.EnableDevice(
                UnityEngine.InputSystem.Gyroscope.current
            );

            Debug.Log("Giroscopio detectado");
        }
        else
        {
            Debug.LogWarning("No se detectó giroscopio");
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer < 0.25f)
            return;

        timer = 0f;

        if (Accelerometer.current != null)
        {
            Vector3 acceleration =
                Accelerometer.current.acceleration.ReadValue();

            Debug.Log(
                $"ACCEL X: {acceleration.x:F2} " +
                $"Y: {acceleration.y:F2} " +
                $"Z: {acceleration.z:F2}"
            );
        }
    }
}