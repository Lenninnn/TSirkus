using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerIdentity))]
public class PlayerMovement : MonoBehaviour
{

    // =====================================================
    // MOVIMIENTO
    // =====================================================

    [Header("Movimiento")]

    [SerializeField]
    private float moveSpeed = 4f;

    [SerializeField]
    private float gravity = -20f;


    // =====================================================
    // SALTO
    // =====================================================

    [Header("Salto")]

    [SerializeField]
    private float jumpHeight = 1.5f;


    // =====================================================
    // TECLADO
    // =====================================================

    [Header("Teclado")]

    [SerializeField]
    private bool useKeyboardInput = true;


    // =====================================================
    // CELULAR
    // =====================================================

    [Header("Celular")]

    [SerializeField]
    private bool useMobileInput = true;

    [SerializeField]
    private MobileInputManager mobileInput;


    // =====================================================
    // COMPONENTES
    // =====================================================

    private CharacterController controller;

    private PlayerIdentity identity;


    // =====================================================
    // FÍSICA
    // =====================================================

    private float verticalVelocity;


    // =====================================================
    // ESTADO DEL SALTO
    // =====================================================

    private bool previousJumpState = false;


    // =====================================================
    // AWAKE
    // =====================================================

    private void Awake()
    {
        controller =
            GetComponent<CharacterController>();

        identity =
            GetComponent<PlayerIdentity>();

        mobileInput =
            FindFirstObjectByType<MobileInputManager>();

        Debug.Log(
            $"🎮 PlayerMovement iniciado → Player {identity.PlayerId}"
        );

        if (mobileInput != null)
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
    // UPDATE
    // =====================================================

    private void Update()
    {

        HandleMovement();

        HandleJump();

    }


    // =====================================================
    // MOVIMIENTO
    // =====================================================

    private void HandleMovement()
    {

        Vector2 input =
            Vector2.zero;


        // =================================================
        // TECLADO
        // =================================================

        if (
            useKeyboardInput
        )
        {

            input +=
                GetKeyboardInput();

        }


        // =================================================
        // CELULAR
        // =================================================

        if (
            useMobileInput &&
            mobileInput != null
        )
        {

            Vector2 mobileMovement =
                mobileInput.GetMovement(
                    identity.PlayerId
                );


            // =============================================
            // DEBUG DEL CELULAR
            // =============================================

            if (
                mobileMovement.magnitude > 0.05f
            )
            {

                Debug.Log(
                    $"📱 Player {identity.PlayerId} → " +
                    $"Joystick X: {mobileMovement.x:F2} | " +
                    $"Y: {mobileMovement.y:F2}"
                );

            }


            input +=
                mobileMovement;

        }


        // =================================================
        // LIMITAR INPUT
        // =================================================

        input =
            Vector2.ClampMagnitude(
                input,
                1f
            );


        // =================================================
        // MOVIMIENTO RELATIVO AL PLAYER
        // =================================================

        Vector3 move =
            transform.right *
            input.x
            +
            transform.forward *
            input.y;


        // =================================================
        // GRAVEDAD
        // =================================================

        if (
            controller.isGrounded &&
            verticalVelocity < 0f
        )
        {

            verticalVelocity =
                -2f;

        }


        verticalVelocity +=
            gravity *
            Time.deltaTime;


        // =================================================
        // VELOCIDAD
        // =================================================

        move *=
            moveSpeed;


        move.y =
            verticalVelocity;


        // =================================================
        // MOVER PLAYER
        // =================================================

        controller.Move(
            move *
            Time.deltaTime
        );

    }


    // =====================================================
    // SALTO
    // =====================================================

    private void HandleJump()
    {

        bool jumpPressed =
            false;


        // =================================================
        // CELULAR
        // =================================================

        if (
            useMobileInput &&
            mobileInput != null
        )
        {

            jumpPressed =
                mobileInput.IsJumpPressed(
                    identity.PlayerId
                );


            // =============================================
            // DEBUG DEL BOTÓN
            // =============================================

            if (
                jumpPressed != previousJumpState
            )
            {

                Debug.Log(
                    $"🦘 Player {identity.PlayerId} → " +
                    $"Botón SALTAR: {jumpPressed}"
                );

            }

        }


        // =================================================
        // DETECTAR PULSACIÓN
        // =================================================

        bool jumpDown =
            jumpPressed &&
            !previousJumpState;


        // =================================================
        // SALTAR
        // =================================================

        if (
            jumpDown &&
            controller.isGrounded
        )
        {

            verticalVelocity =
                Mathf.Sqrt(
                    jumpHeight *
                    -2f *
                    gravity
                );


            Debug.Log(
                $"🦘 Player {identity.PlayerId} SALTÓ | " +
                $"Fuerza: {verticalVelocity:F2}"
            );

        }


        // =================================================
        // GUARDAR ESTADO
        // =================================================

        previousJumpState =
            jumpPressed;

    }


    // =====================================================
    // TECLADO
    // =====================================================

    private Vector2 GetKeyboardInput()
    {

        if (
            Keyboard.current == null
        )
        {

            return Vector2.zero;

        }


        Vector2 input =
            Vector2.zero;


        if (
            Keyboard.current.wKey.isPressed
        )
        {

            input.y += 1f;

        }


        if (
            Keyboard.current.sKey.isPressed
        )
        {

            input.y -= 1f;

        }


        if (
            Keyboard.current.dKey.isPressed
        )
        {

            input.x += 1f;

        }


        if (
            Keyboard.current.aKey.isPressed
        )
        {

            input.x -= 1f;

        }


        return Vector2.ClampMagnitude(
            input,
            1f
        );

    }

}