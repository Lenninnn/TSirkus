using System.Collections.Generic;
using UnityEngine;

// Zona del mapa que pide una ayuda cuando entran suficientes jugadores.
// Puede depender del estado de un puzzle (por ejemplo, solo mientras la
// puerta sigue cerrada). No bloquea el movimiento.
[RequireComponent(typeof(Collider))]
public class HintZone : MonoBehaviour
{
    public enum PuzzleCondition
    {
        Ignore,
        OnlyWhileUnsolved,
        OnlyWhenSolved
    }

    [Header("Ayuda")]
    [SerializeField] private HintDefinition hint;
    [SerializeField] private ContextHintController controller;

    [Header("Jugadores")]
    [Tooltip("1 = basta un jugador. 4 = todo el grupo dentro de la zona.")]
    [Range(1, 4)]
    [SerializeField] private int playersRequired = 1;

    [Header("Contexto (opcional)")]
    [SerializeField] private PuzzleController contextPuzzle;
    [SerializeField] private PuzzleCondition puzzleCondition = PuzzleCondition.Ignore;

    private readonly HashSet<PlayerIdentity> playersInside = new HashSet<PlayerIdentity>();
    private bool requested;


    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;

        if (controller == null)
            controller = FindAnyObjectByType<ContextHintController>();
    }


    private void OnTriggerEnter(Collider other)
    {
        PlayerIdentity player = other.GetComponentInParent<PlayerIdentity>();

        if (player != null)
            playersInside.Add(player);
    }


    private void OnTriggerExit(Collider other)
    {
        PlayerIdentity player = other.GetComponentInParent<PlayerIdentity>();

        if (player != null)
            playersInside.Remove(player);

        // Una ayuda repetible se vuelve a pedir cuando el grupo reingresa.
        if (hint != null && !hint.showOncePerMatch && playersInside.Count < playersRequired)
            requested = false;
    }


    // Se evalúa cada frame mientras haya jugadores dentro, así una zona
    // que espera a que se resuelva un puzzle se dispara en cuanto ocurre.
    private void Update()
    {
        if (requested || playersInside.Count == 0)
            return;

        playersInside.RemoveWhere(player => player == null || !player.isActiveAndEnabled);

        if (playersInside.Count < playersRequired || !ContextAllows())
            return;

        if (controller == null || hint == null)
        {
            Debug.LogWarning($"HintZone {name}: falta ContextHintController o HintDefinition.");
            requested = true;
            return;
        }

        controller.Request(hint);
        requested = true;
    }


    private bool ContextAllows()
    {
        if (contextPuzzle == null || puzzleCondition == PuzzleCondition.Ignore)
            return true;

        bool solved = contextPuzzle.CurrentState == PuzzleState.Success;

        return puzzleCondition == PuzzleCondition.OnlyWhenSolved ? solved : !solved;
    }


    private void OnDrawGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>();

        if (box == null)
            return;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(1f, 0.75f, 0.1f, 0.18f);
        Gizmos.DrawCube(box.center, box.size);
        Gizmos.color = new Color(1f, 0.75f, 0.1f, 0.8f);
        Gizmos.DrawWireCube(box.center, box.size);
    }
}
