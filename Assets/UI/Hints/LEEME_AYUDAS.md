# Ayudas contextuales (TSirkus)

Sistema: `Assets/_Scripts/AyudasUI/`

- `HintDefinition` (asset): sprite, prioridad, una vez por partida, ocultar por tiempo / con ACCIÓN.
- `ContextHintController` (Canvas/ContextHintHUD): muestra una ayuda a la vez, por prioridad,
  sin bloquear a los jugadores. Espera a que se oculten el aviso inicial, el diálogo de intro y
  la cinemática de la puerta.
- `HintZone` (escena: ContextHints/*): trigger por zona. `playersRequired` (1 = cualquiera,
  4 = todo el grupo) y condición opcional según el estado de un `PuzzleController`.
- `HintEventTrigger`: para pedir una ayuda desde un UnityEvent (`Show()`).

Para añadir una ayuda: Create > TSirkus > Ayudas > Hint, asignar sprite, y crear un GameObject
con BoxCollider + HintZone (o llamar a HintEventTrigger.Show desde un evento).

## Placeholders a reemplazar

Reemplaza el PNG manteniendo el mismo nombre (o cambia el sprite en el asset de `Definitions/`).

| Placeholder | Asset | Mensaje final | Dónde aparece |
|---|---|---|---|
| `hint_placeholder_01` | `Hint_Palancas` | No todo se resuelve solo. / Cada jugador debe encontrar su lugar. | Carpa, zona norte junto a los 4 interruptores (`Zona_Palancas`). Solo antes de abrir la puerta. |
| `hint_placeholder_02` | `Hint_PuertaCerrada` | Si el paso está cerrado, busca cómo abrirlo. / El circo siempre deja pistas. | Carpa, frente a la puerta de la primera prueba (`Zona_PuertaCerrada`). Solo antes de abrirla. |
| `hint_placeholder_03` | `Hint_Interactuar` | Acércate y usa ACCIÓN para interactuar. | Carpa, junto a cada interruptor Switch1–4 (`Zona_Interactuar_SwitchN`). Una vez en total. |
| `hint_placeholder_04` | `Hint_NuevaZona` | Bien hecho. / Sigan avanzando. El circo aún guarda secretos. | Corredor norte, justo detrás de la puerta (`Zona_NuevaZona`). Solo con la prueba superada. |
| `hint_placeholder_05` | `Hint_Cooperacion` | Observen el entorno. / A veces avanzar requiere que todos participen. | Corredor norte, entrada a la sala de globos (`Zona_Cooperacion_Globos`). |

Ya existentes (no forman parte de este sistema):
`aviso-nuevete` (inicio, TutorialImage) y `aviso-explora` (entrada a la carpa, IntroDialogues).
