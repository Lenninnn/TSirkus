
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerBalanceVisual : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerInstability playerInstability;
    [SerializeField] private Transform bodyVisual;
    [SerializeField] private Transform playerCamera;
    [SerializeField] private RaceStartManager raceStartManager;

    [Header("Balanceo")]
    [SerializeField, Range(5f, 35f)]
    private float maximumBodyTilt = 20f;

    [SerializeField, Min(0.1f)]
    private float bodySwaySpeed = 5f;

    [SerializeField, Min(0f)]
    private float maximumCameraTilt = 4f;

    [Header("Caída")]
    [SerializeField, Range(45f, 90f)]
    private float fallAngle = 85f;

    [SerializeField, Min(0.1f)]
    private float fallDuration = 0.6f;

    [SerializeField]
    private bool fallForward = false;

    private Quaternion initialBodyRotation;
    private Quaternion initialCameraRotation;
    private Quaternion targetFallRotation;
    private Quaternion targetFallCameraRotation;

    private float fallTimer;
    private bool falling;
    private bool fallCompleted;

    private void Awake()
    {
        if (playerInstability == null)
            playerInstability = GetComponent<PlayerInstability>();

        if (raceStartManager == null)
            raceStartManager =
                FindFirstObjectByType<RaceStartManager>();

        if (playerCamera == null)
        {
            Camera cam = GetComponentInChildren<Camera>();

            if (cam != null)
                playerCamera = cam.transform;
        }

        if (playerInstability == null || bodyVisual == null)
        {
            Debug.LogError(
                $"{name}: asigna PlayerInstability y Body Visual.",
                this
            );

            enabled = false;
            return;
        }

        initialBodyRotation = bodyVisual.localRotation;

        if (playerCamera != null)
            initialCameraRotation = playerCamera.localRotation;

        Vector3 fallEuler = fallForward
            ? new Vector3(fallAngle, 0f, 0f)
            : new Vector3(0f, 0f, fallAngle);

        targetFallRotation =
            initialBodyRotation * Quaternion.Euler(fallEuler);

        Vector3 cameraFallEuler = fallForward
            ? new Vector3(fallAngle, 0f, 0f)
            : new Vector3(-fallAngle * 0.65f, 0f, 0f);

        targetFallCameraRotation =
            initialCameraRotation *
            Quaternion.Euler(cameraFallEuler);
    }

    private void LateUpdate()
    {
        if (playerInstability == null || bodyVisual == null)
            return;

        // La caída ya iniciada continúa hasta terminar.
        if (playerInstability.IsFallen)
        {
            AnimateFall();
            return;
        }

        // Esperar al inicio global de la carrera.
        if (raceStartManager == null ||
            !raceStartManager.RaceStarted)
        {
            ResetVisuals();
            return;
        }

        // Cada jugador se anima únicamente dentro de su puzzle.
        if (!playerInstability.IsParticipating)
        {
            ResetVisuals();
            return;
        }

        AnimateBalance();
    }

    private void AnimateBalance()
    {
        float instability =
            Mathf.Clamp01(playerInstability.Instability);

        float intensity = Mathf.Lerp(0.35f, 1.5f, instability);

        float wave = Mathf.Sin(Time.time * bodySwaySpeed);

        float tilt = wave * maximumBodyTilt * intensity;

        Quaternion targetBody =
            initialBodyRotation *
            Quaternion.Euler(0f, 0f, tilt);

        bodyVisual.localRotation = Quaternion.Slerp(
            bodyVisual.localRotation,
            targetBody,
            12f * Time.deltaTime
        );

        if (playerCamera != null)
        {
            float cameraTilt =
                wave * maximumCameraTilt * intensity;

            Quaternion targetCamera =
                initialCameraRotation *
                Quaternion.Euler(0f, 0f, cameraTilt);

            playerCamera.localRotation = Quaternion.Slerp(
                playerCamera.localRotation,
                targetCamera,
                8f * Time.deltaTime
            );
        }
    }

    private void ResetVisuals()
    {
        bodyVisual.localRotation = Quaternion.Slerp(
            bodyVisual.localRotation,
            initialBodyRotation,
            8f * Time.deltaTime
        );

        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Slerp(
                playerCamera.localRotation,
                initialCameraRotation,
                8f * Time.deltaTime
            );
        }
    }

    private void AnimateFall()
    {
        if (fallCompleted)
            return;

        if (!falling)
        {
            falling = true;
            fallTimer = 0f;
        }

        fallTimer += Time.deltaTime;

        float progress = Mathf.Clamp01(
            fallTimer / fallDuration
        );

        float smoothProgress =
            progress * progress * (3f - 2f * progress);

        bodyVisual.localRotation = Quaternion.Slerp(
            initialBodyRotation,
            targetFallRotation,
            smoothProgress
        );

        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Slerp(
                initialCameraRotation,
                targetFallCameraRotation,
                smoothProgress
            );
        }

        if (progress >= 1f)
        {
            bodyVisual.localRotation = targetFallRotation;

            if (playerCamera != null)
                playerCamera.localRotation = targetFallCameraRotation;

            fallCompleted = true;
        }
    }
}
