using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Coordina las ayudas contextuales. Cada jugador tiene su propio PlayerHintHUD
// dentro de su cuadrante (estado, cola, historial y ACCIÓN independientes).
// Aquí solo se crean esos HUD y se decide cuándo deben esperar (intro, cinemática...).
public class ContextHintController : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Contenedor a pantalla completa (ContextHintHUD). Aquí se crea un HUD por jugador.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Tooltip("Plantilla del aviso. Se clona dentro del HUD de cada jugador.")]
    [SerializeField] private Image hintImage;

    [Tooltip("Tamaño del aviso en el cuadrante de cada jugador. Referencia: aviso inicial (~280x100).")]
    [SerializeField] private Vector2 hintSize = new Vector2(280f, 112f);

    [SerializeField] private float topOffset = 12f;

    [Header("Tiempos")]
    [SerializeField] private float fadeDuration = 0.35f;

    [Tooltip("Evita que el mismo toque de ACCIÓN que activó algo cierre la ayuda al instante.")]
    [SerializeField] private float minVisibleBeforeAction = 0.6f;

    [Header("Input")]
    [SerializeField] private MobileInputManager mobileInput;

    [Header("Esperar mientras estén visibles")]
    [Tooltip("Intro, cinemática, tutorial inicial... Las ayudas esperan a que se oculten.")]
    [SerializeField] private CanvasGroup[] waitWhileVisible;

    private readonly Dictionary<int, PlayerHintHUD> huds = new Dictionary<int, PlayerHintHUD>();

    public float FadeDuration => fadeDuration;
    public float MinVisibleBeforeAction => minVisibleBeforeAction;
    public MobileInputManager MobileInput => mobileInput;


    // =====================================================
    // AWAKE
    // =====================================================

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        // El contenedor siempre visible; cada HUD de jugador hace su propio fade.
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (hintImage != null)
            hintImage.gameObject.SetActive(false);

        if (mobileInput == null)
            mobileInput = FindAnyObjectByType<MobileInputManager>();
    }


    // =====================================================
    // PEDIR AYUDA
    // =====================================================

    public void Request(HintDefinition hint, PlayerIdentity player)
    {
        PlayerHintHUD hud = GetHud(player);

        if (hud != null)
            hud.Request(hint);
    }


    public void Request(HintDefinition hint, int playerId)
    {
        Request(hint, FindPlayer(playerId));
    }


    public void RequestForAll(HintDefinition hint)
    {
        foreach (PlayerIdentity player in FindObjectsByType<PlayerIdentity>())
        {
            if (player.GetComponentInChildren<Camera>(true) != null)
                Request(hint, player);
        }
    }


    // =====================================================
    // HUD POR JUGADOR
    // =====================================================

    private PlayerHintHUD GetHud(PlayerIdentity player)
    {
        if (player == null || hintImage == null)
            return null;

        if (huds.TryGetValue(player.PlayerId, out PlayerHintHUD hud) && hud != null)
            return hud;

        // Misma búsqueda de cámara que PlayerStateHUD.
        Camera playerCamera = player.GetComponentInChildren<Camera>(true);

        if (playerCamera == null)
            return null;

        GameObject hudObject = new GameObject($"PlayerHintHUD_P{player.PlayerId}", typeof(RectTransform));
        hudObject.transform.SetParent(transform, false);

        hud = hudObject.AddComponent<PlayerHintHUD>();
        hud.Setup(this, player.PlayerId, playerCamera, hintImage, hintSize, topOffset);

        huds[player.PlayerId] = hud;
        return hud;
    }


    private static PlayerIdentity FindPlayer(int playerId)
    {
        foreach (PlayerIdentity player in FindObjectsByType<PlayerIdentity>())
        {
            if (player.PlayerId == playerId && player.GetComponentInChildren<Camera>(true) != null)
                return player;
        }

        return null;
    }


    // =====================================================
    // ESPERAR A OTRAS PANTALLAS
    // =====================================================

    public bool IsWaiting()
    {
        if (waitWhileVisible == null)
            return false;

        foreach (CanvasGroup group in waitWhileVisible)
        {
            if (group != null && group.gameObject.activeInHierarchy && group.alpha > 0.01f)
                return true;
        }

        return false;
    }
}
