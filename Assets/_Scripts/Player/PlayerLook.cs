using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerIdentity))]
public class PlayerLook : MonoBehaviour
{
    [Header("Cámara")]
    [Tooltip("Objeto CameraHolder, que debe ser hijo del Player.")]
    [SerializeField] private Transform cameraHolder;

    [Header("Mouse")]
    [SerializeField] private float mouseSensitivity = 0.15f;

    [Header("Celular")]
    [SerializeField] private bool useMobileLook = true;

    [Tooltip("Multiplicador principal de sensibilidad móvil.")]
    [SerializeField] private float mobileSensitivity = 15f;

    [Tooltip("Amplifica aún más el desplazamiento recibido.")]
    [SerializeField] private float mobileLookMultiplier = 3f;

    [Tooltip("Invierte el eje vertical del dedo.")]
    [SerializeField] private bool invertMobileY = false;

    [Header("Límites verticales")]
    [SerializeField] private float minVerticalRotation = -80f;
    [SerializeField] private float maxVerticalRotation = 80f;

    private PlayerIdentity identity;
    private MobileInputManager mobileInput;

    private float verticalRotation;

    private void Awake()
    {
        identity = GetComponent<PlayerIdentity>();

        mobileInput = FindFirstObjectByType<MobileInputManager>();

        if (cameraHolder == null)
        {
            Debug.LogError(
                $"❌ PlayerLook del Player {identity.PlayerId}: " +
                "Camera Holder no está asignado.",
                this
            );
        }

        if (mobileInput == null)
        {
            Debug.LogWarning(
                $"⚠️ Player {identity.PlayerId}: " +
                "No se encontró MobileInputManager.",
                this
            );
        }
    }

    private void Start()
    {
        if (cameraHolder != null)
        {
            verticalRotation = NormalizeAngle(
                cameraHolder.localEulerAngles.x
            );

            verticalRotation = Mathf.Clamp(
                verticalRotation,
                minVerticalRotation,
                maxVerticalRotation
            );

            cameraHolder.localRotation = Quaternion.Euler(
                verticalRotation,
                0f,
                0f
            );
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (cameraHolder == null)
            return;

        bool mobileInputDetected = false;

        if (useMobileLook && mobileInput != null)
        {
            Vector2 lookInput = mobileInput.GetLook(
                identity.PlayerId
            );

            if (lookInput.sqrMagnitude > 0.000001f)
            {
                HandleMobileLook(lookInput);
                mobileInputDetected = true;
            }
        }

        // El mouse solo se procesa si no se recibió entrada móvil.
        if (!mobileInputDetected)
        {
            HandleMouseLook();
        }
    }

    private void HandleMouseLook()
    {
        if (Mouse.current == null)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        if (mouseDelta.sqrMagnitude <= 0.000001f)
            return;

        float horizontalRotation =
            mouseDelta.x * mouseSensitivity;

        float verticalMovement =
            mouseDelta.y * mouseSensitivity;

        // Gira el cuerpo sobre su propio eje.
        transform.Rotate(
            Vector3.up,
            horizontalRotation,
            Space.Self
        );

        verticalRotation -= verticalMovement;

        verticalRotation = Mathf.Clamp(
            verticalRotation,
            minVerticalRotation,
            maxVerticalRotation
        );

        cameraHolder.localRotation = Quaternion.Euler(
            verticalRotation,
            0f,
            0f
        );
    }

    private void HandleMobileLook(Vector2 lookInput)
    {
        /*
         * El multiplicador se aplica directamente al valor recibido
         * por MobileInputManager.
         *
         * Si GetLook devuelve valores muy pequeños, por ejemplo:
         * (0.001, 0.002), este multiplicador los hace perceptibles.
         */

        float sensitivity =
            mobileSensitivity * mobileLookMultiplier;

        float horizontalRotation =
            lookInput.x * sensitivity;

        float verticalMovement =
            lookInput.y * sensitivity;

        if (invertMobileY)
        {
            verticalMovement = -verticalMovement;
        }

        // Rotación horizontal del cuerpo.
        transform.Rotate(
            Vector3.up,
            horizontalRotation,
            Space.Self
        );

        // Rotación vertical únicamente de la cámara.
        verticalRotation += verticalMovement;

        verticalRotation = Mathf.Clamp(
            verticalRotation,
            minVerticalRotation,
            maxVerticalRotation
        );

        cameraHolder.localRotation = Quaternion.Euler(
            verticalRotation,
            0f,
            0f
        );
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;

        return angle;
    }

    private void OnValidate()
    {
        mobileSensitivity = Mathf.Max(
            0.01f,
            mobileSensitivity
        );

        mobileLookMultiplier = Mathf.Max(
            0.01f,
            mobileLookMultiplier
        );

        minVerticalRotation = Mathf.Clamp(
            minVerticalRotation,
            -89f,
            0f
        );

        maxVerticalRotation = Mathf.Clamp(
            maxVerticalRotation,
            0f,
            89f
        );
    }
}