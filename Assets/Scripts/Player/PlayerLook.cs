using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private float sensitivity = 0.15f;

    private float verticalRotation = 0f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float mouseX = mouseDelta.x * sensitivity;
        float mouseY = mouseDelta.y * sensitivity;

        // Girar el cuerpo izquierda/derecha
        transform.Rotate(Vector3.up * mouseX);

        // Mirar arriba/abajo
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -80f, 80f);

        cameraHolder.localRotation =
            Quaternion.Euler(verticalRotation, 0f, 0f);
    }
}