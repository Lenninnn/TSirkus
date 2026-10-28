using UnityEngine;

// Mensaje de objetivo/misión (abajo al centro de cada cuadrante).
// Se crea como asset y se muestra con ObjectivePromptController.
[CreateAssetMenu(menuName = "TSirkus/Ayudas/Objetivo", fileName = "Objective_")]
public class ObjectiveDefinition : ScriptableObject
{
    [Header("Visual")]
    public Sprite sprite;

    [Tooltip("Texto que debe llevar el arte final. Solo documentación.")]
    [TextArea(2, 4)]
    public string finalMessage;

    [Header("Tiempos")]
    [Min(0.5f)]
    public float visibleSeconds = 5.5f;

    [Min(0.05f)]
    public float fadeSeconds = 0.5f;

    [Header("Cierre")]
    [Tooltip("Permite que ESE jugador lo cierre antes con ACCIÓN.")]
    public bool closeWithAction = true;
}
