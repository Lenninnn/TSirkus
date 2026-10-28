using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Muestra mensajes de objetivo/misión en el cuadrante de cada jugador
// (abajo al centro, encima del HUD de corrupción). No bloquea a nadie.
// Uso: ShowToAll(objetivo) desde un UnityEvent, o ShowToPlayer(objetivo, id) desde código.
public class ObjectivePromptController : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Plantilla del cuadro. Se clona dentro del cuadrante de cada jugador.")]
    [SerializeField] private Image objectiveImage;

    [Tooltip("Zona del cuadrante (0-1) que ocupa el cuadro. Por encima del HUD de corrupción (~4-25 % inferior).")]
    [SerializeField] private Vector2 anchorMin = new Vector2(0.2f, 0.26f);
    [SerializeField] private Vector2 anchorMax = new Vector2(0.8f, 0.46f);

    [Header("Input")]
    [SerializeField] private MobileInputManager mobileInput;

    private readonly Dictionary<int, PlayerObjectiveHUD> huds = new Dictionary<int, PlayerObjectiveHUD>();

    public MobileInputManager MobileInput => mobileInput;


    private void Awake()
    {
        if (objectiveImage != null)
            objectiveImage.gameObject.SetActive(false);

        if (mobileInput == null)
            mobileInput = FindAnyObjectByType<MobileInputManager>();
    }


    // =====================================================
    // MOSTRAR
    // =====================================================

    public void ShowToAll(ObjectiveDefinition objective)
    {
        foreach (PlayerIdentity player in FindObjectsByType<PlayerIdentity>())
        {
            if (player.GetComponentInChildren<Camera>(true) != null)
                Show(objective, player);
        }
    }


    public void ShowToPlayer(ObjectiveDefinition objective, int playerId)
    {
        foreach (PlayerIdentity player in FindObjectsByType<PlayerIdentity>())
        {
            if (player.PlayerId == playerId && player.GetComponentInChildren<Camera>(true) != null)
            {
                Show(objective, player);
                return;
            }
        }
    }


    public void Show(ObjectiveDefinition objective, PlayerIdentity player)
    {
        PlayerObjectiveHUD hud = GetHud(player);

        if (hud != null)
            hud.Show(objective);
    }


    // =====================================================
    // HUD POR JUGADOR
    // =====================================================

    private PlayerObjectiveHUD GetHud(PlayerIdentity player)
    {
        if (player == null || objectiveImage == null)
            return null;

        if (huds.TryGetValue(player.PlayerId, out PlayerObjectiveHUD hud) && hud != null)
            return hud;

        Camera playerCamera = player.GetComponentInChildren<Camera>(true);

        if (playerCamera == null)
            return null;

        GameObject hudObject = new GameObject($"PlayerObjectiveHUD_P{player.PlayerId}", typeof(RectTransform));
        hudObject.transform.SetParent(transform, false);

        hud = hudObject.AddComponent<PlayerObjectiveHUD>();
        hud.Setup(this, player.PlayerId, playerCamera, objectiveImage, anchorMin, anchorMax);

        huds[player.PlayerId] = hud;
        return hud;
    }
}
