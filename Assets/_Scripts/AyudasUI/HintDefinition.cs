using UnityEngine;

// Datos de una ayuda contextual. Se crea como asset y se reutiliza
// desde cualquier HintZone o HintEventTrigger.
[CreateAssetMenu(menuName = "TSirkus/Ayudas/Hint", fileName = "Hint_")]
public class HintDefinition : ScriptableObject
{
    [Header("Visual")]
    public Sprite sprite;

    [Tooltip("Texto que debe llevar el arte final. Solo documentación.")]
    [TextArea(2, 4)]
    public string finalMessage;

    [Header("Comportamiento")]
    [Tooltip("Si dos ayudas coinciden, se muestra primero la de mayor prioridad.")]
    public int priority = 0;

    [Tooltip("Mostrar solo una vez por jugador en cada partida.")]
    public bool showOncePerMatch = true;

    [Tooltip("Ocultar automáticamente después de visibleSeconds.")]
    public bool hideAfterTime = true;

    [Min(0.5f)]
    public float visibleSeconds = 4f;

    [Tooltip("Ocultar cuando ESE jugador pulsa ACCIÓN en su celular.")]
    public bool hideWithAction = true;
}
