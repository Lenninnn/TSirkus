using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerIdentity))]
public class PlayerLook : MonoBehaviour
{
    [Header("Cámara")]
    [SerializeField] private Transform cameraHolder;

    [Header("Mouse")]
    [SerializeField] private float mouseSensitivity = 0.15f;

    [Header("Celular")]
    [SerializeField] private bool useMobileLook = true;

    [SerializeField] private float mobileSensitivity = 0.12f;

    [Header("Límites")]
    [SerializeField] private float minVerticalRotation = -80f;

    [SerializeField] private float maxVerticalRotation = 80f;


    private PlayerIdentity identity;

    private MobileInputManager mobileInput;

    private float verticalRotation = 0f;


    // =====================================================
    // AWAKE
    // =====================================================

    private void Awake()
    {
        identity =
            GetComponent<PlayerIdentity>();

        mobileInput =
            FindFirstObjectByType<MobileInputManager>();


        Debug.Log(
            $"👀 PlayerLook iniciado → Player {identity.PlayerId}"
        );


        if (
            mobileInput != null
        )
        {
            Debug.Log(
                $"✅ Player {identity.PlayerId} encontró MobileInputManager"
            );
        }
        else
        {
            Debug.LogError(
                $"❌ Player {identity.PlayerId} NO encontró MobileInputManager"
            );
        }
    }


    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible =
            false;
    }


    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        HandleMouseLook();

        HandleMobileLook();
    }


    // =====================================================
    // MOUSE
    // =====================================================

    private void HandleMouseLook()
    {
        if (
            Mouse.current == null
        )
            return;


        Vector2 mouseDelta =
            Mouse.current.delta.ReadValue();


        float mouseX =
            mouseDelta.x *
            mouseSensitivity;


        float mouseY =
            mouseDelta.y *
            mouseSensitivity;


        // ---------------------------------------------
        // IZQUIERDA / DERECHA
        // ---------------------------------------------

        transform.Rotate(
            Vector3.up *
            mouseX
        );


        // ---------------------------------------------
        // ARRIBA / ABAJO
        // ---------------------------------------------

        verticalRotation -=
            mouseY;


        verticalRotation =
            Mathf.Clamp(
                verticalRotation,
                minVerticalRotation,
                maxVerticalRotation
            );


        cameraHolder.localRotation =
            Quaternion.Euler(
                verticalRotation,
                0f,
                0f
            );
    }


    // =====================================================
    // CELULAR
    // =====================================================

    private void HandleMobileLook()
    {
        if (
            !useMobileLook ||
            mobileInput == null
        )
            return;


        Vector2 look =
            mobileInput.GetLook(
                identity.PlayerId
            );


        if (
            look == Vector2.zero
        )
            return;


        // =================================================
        // HORIZONTAL
        // =================================================
        //
        // Dedo a la derecha
        // → mirar a la derecha
        //
        // Dedo a la izquierda
        // → mirar a la izquierda
        //

        float horizontalRotation =
            look.x *
            mobileSensitivity;


        transform.Rotate(
            Vector3.up *
            horizontalRotation
        );


        // =================================================
        // VERTICAL
        // =================================================
        //
        // Dedo hacia arriba
        // → mirar hacia arriba
        //
        // Dedo hacia abajo
        // → mirar hacia abajo
        //

        float verticalMovement =
            look.y *
            mobileSensitivity;


        verticalRotation +=
            verticalMovement;


        verticalRotation =
            Mathf.Clamp(
                verticalRotation,
                minVerticalRotation,
                maxVerticalRotation
            );


        cameraHolder.localRotation =
            Quaternion.Euler(
                verticalRotation,
                0f,
                0f
            );
    }
}