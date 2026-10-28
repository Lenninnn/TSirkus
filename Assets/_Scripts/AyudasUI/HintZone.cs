using System.Collections.Generic;
using UnityEngine;

// Zona del mapa que muestra una ayuda SOLO al jugador que entra en ella.
// Cada jugador la recibe por separado (y "una vez" es una vez por jugador).
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

    [Header("Contexto (opcional)")]
    [SerializeField] private PuzzleController contextPuzzle;
    [SerializeField] private PuzzleCondition puzzleCondition = PuzzleCondition.Ignore;

    private readonly HashSet<PlayerIdentity> playersInside = new HashSet<PlayerIdentity>();
    private readonly HashSet<PlayerIdentity> requestedFor = new HashSet<PlayerIdentity>();


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

        if (player == null)
            return;

        playersInside.Remove(player);

        // Una ayuda repetible se vuelve a pedir cuando ese jugador reingresa.
        if (hint != null && !hint.showOncePerMatch)
            requestedFor.Remove(player);
    }


    // Se evalúa cada frame mientras haya jugadores dentro, así una zona
    // que espera a que se resuelva un puzzle se dispara en cuanto ocurre.
    private void Update()
    {
        if (playersInside.Count == 0 || !ContextAllows())
            return;

        if (controller == null || hint == null)
        {
            Debug.LogWarning($"HintZone {name}: falta ContextHintController o HintDefinition.");
            enabled = false;
            return;
        }

        playersInside.RemoveWhere(player => player == null || !player.isActiveAndEnabled);

        foreach (PlayerIdentity player in playersInside)
        {
            // requestedFor evita pedirla cada frame; el historial de "ya vista"
            // lo lleva el PlayerHintHUD de cada jugador.
            if (requestedFor.Add(player))
                controller.Request(hint, player);
        }
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
