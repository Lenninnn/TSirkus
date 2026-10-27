# Ayudas contextuales (TSirkus)

Las ayudas son **individuales por jugador**: cada uno las ve en su propio cuadrante
del split-screen, con su propio historial y su propio cierre con ACCIÓN.

Sistema: `Assets/_Scripts/AyudasUI/`

- `HintDefinition` (asset): sprite, prioridad, una vez por jugador y partida,
  ocultar por tiempo y/o con ACCIÓN (de ese mismo jugador).
- `ContextHintController` (Canvas/ContextHintHUD): crea un `PlayerHintHUD` por jugador
  clonando la plantilla `HintImage` y hace esperar a todos mientras se ven el aviso
  inicial, el diálogo de intro o la cinemática de la puerta. `hintSize` (280x112)
  usa como referencia el aviso inicial "Muévete y explora el circo".
- `PlayerHintHUD` (se crea en Play Mode: `PlayerHintHUD_P1`..`P4`): anclado al
  `camera.rect` del jugador; cola por prioridad, avisos vistos y ACCIÓN propios.
- `HintZone` (escena: ContextHints/*): envía la ayuda solo al `PlayerIdentity` que
  entra. Condición opcional según el estado de un `PuzzleController`.
- `HintEventTrigger`: `ShowToAll()` o `ShowToPlayer(id)` desde un UnityEvent.

Para añadir una ayuda: Create > TSirkus > Ayudas > Hint, asignar sprite, y crear un
GameObject con BoxCollider + HintZone (o llamar a HintEventTrigger desde un evento).

## Placeholders a reemplazar

Reemplaza el PNG manteniendo el mismo nombre (o cambia el sprite en `Definitions/`).
Todas aplican a cualquier jugador (P1-P4), cada uno por separado, una vez por partida.

| Placeholder | Asset | Texto final (título / subtítulo) | Trigger | Cuándo aparece |
|---|---|---|---|---|
| `hint_placeholder_01` | `Hint_Palancas` | NO TODO SE RESUELVE SOLO / Cada jugador debe encontrar su lugar. | `Zona_Palancas`: carpa, franja norte junto a los 4 interruptores | Al entrar, mientras la primera prueba no esté resuelta |
| `hint_placeholder_02` | `Hint_PuertaCerrada` | SI EL PASO ESTÁ CERRADO / Busca cómo abrirlo. El circo siempre deja pistas. | `Zona_PuertaCerrada`: carpa, frente a la puerta de la primera prueba | Al entrar, mientras la puerta siga cerrada |
| `hint_placeholder_03` | `Hint_Interactuar` | ACÉRCATE Y USA ACCIÓN / para interactuar con lo que encuentres. | `Zona_Interactuar_Switch1..4`: junto a cada interruptor | Al acercarse al primer interruptor (no se repite en los demás) |
| `hint_placeholder_04` | `Hint_NuevaZona` | BIEN HECHO / Sigan avanzando. El circo aún guarda secretos. | `Zona_NuevaZona`: corredor norte, justo detrás de la puerta | Al cruzar la puerta, con la prueba superada y tras la cinemática |
| `hint_placeholder_05` | `Hint_Cooperacion` | OBSERVEN EL ENTORNO / A veces avanzar requiere que todos participen. | `Zona_Cooperacion_Globos`: corredor norte, entrada a la sala de globos | Al entrar a la zona |

Ya existentes (globales, no forman parte de este sistema):
`aviso-nuevete` (inicio, TutorialImage) y `aviso-explora` (entrada a la carpa, IntroDialogues).
