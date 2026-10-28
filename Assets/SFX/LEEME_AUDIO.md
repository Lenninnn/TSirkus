# Audio de TSirkus

## Cómo funciona

- **Split-screen**: Unity solo admite un `AudioListener` y los 4 jugadores comparten altavoces.
  Los sonidos del mundo se atenúan según el **jugador más cercano** (si alguien está cerca, se
  oye; si todos están lejos, no). Se reproducen en 2D: el paneo izq./der. no tiene sentido en
  pantalla dividida.
- **`SoundCue`** (`Cues/*.asset`): clips con variación, volumen, pitch, categoría, distancias y
  anti-duplicado. **Un cue sin clip no suena ni da error**: cada uno indica en `expectedFile`
  qué archivo espera. Para activar un sonido: coloca el archivo, arrástralo al campo `clips`.
- **`GameAudio`** (objeto `GameAudio` en GAME): pool fijo de 10 AudioSources, volumen por
  categoría (Ambience 0.5, SFX 0.9, Player 0.8, UI 0.6, Cinematic 0.9) y ducking del ambiente
  durante la cinemática.
- Componentes: `PlayerFootsteps` (visual de cada jugador), `PlayerEventAudio` (raíz del Player:
  captura, rescate, tickets), `SoundEmitter` (interruptores), `DoorAudio` (PuzzleDoor),
  `AmbienceZone` (`GameAudio/Ambience_*`), `ClownAudio` (prefab Payaso).
- Cinemática de la primera puerta: el MP4 **ya trae audio** (mecanismo + impacto ~7,9 s). Se
  enruta por un AudioSource 2D en `DoorCinematicPlayer`. No añadir otros sonidos encima.
- Menús: `TSirkus/Audio/1. Crear carpetas…`, `2. Conectar audio…`, `Probar pasos`,
  `Probar audio de la cinematica`.

## Archivos existentes

| Archivo | Qué es | Uso |
|---|---|---|
| `Player/SFX_Player_Footstep_01.mp3` (antes `sonido-pasos personajes`) | 1 pisada recortada, 0,17 s, golpe a 0,02 s | Pasos de los 4 jugadores |
| `Stings/SFX_Sting_Horror_01.mp3` (antes `sonido-horror`) | Riser/sting estéreo 5 s | `SFX_Sting_Danger`: al capturar a un jugador |

## Archivos que faltan

Formato: WAV 44,1/48 kHz. One-shots 3D en **mono**; ambientes y UI pueden ser estéreo.

### Prioridad alta

| Nombre | Carpeta | Buscar | Duración | Tipo | Uso |
|---|---|---|---|---|---|
| `SFX_Switch_Activate_01.wav` | Interactions | old metal lever pull clunk mechanical switch | 0,3–0,8 s | one-shot mundo, mono | Cada interruptor al activarse |
| `SFX_Ticket_Pickup_01.wav` | Interactions | paper ticket pickup carnival music box ding dark | 0,3–0,8 s | one-shot mundo, mono | Recoger ticket |
| `SFX_UI_Objective_01.wav` | UI | subtle dark chime objective single note music box | 1–2 s | global, estéreo | Aparece "LA PRIMERA PRUEBA TE ESPERA" |
| `SFX_Player_Footstep_02..04.wav` | Player | footstep wooden floor old boards single step | 0,2–0,5 s | mundo, mono, sin silencio inicial | Variaciones de pasos (añadir al cue) |
| `SFX_Clown_Appear_01.wav` | Enemies | creepy clown giggle distant horror | 1–3 s | one-shot mundo, mono | Aparición del payaso |
| `SFX_Player_Captured_01.wav` | Player | body grabbed struggle gasp horror | 1–2 s | one-shot mundo, mono | Jugador capturado |

### Prioridad media

| Nombre | Carpeta | Buscar | Duración | Tipo | Uso |
|---|---|---|---|---|---|
| `AMB_Circus_Global_01.wav` | Ambience | abandoned circus tent wind canvas creak ambience loop | 60–120 s | loop global, estéreo | Fondo de todo el mapa (muy bajo) |
| `AMB_Tent_Main_01.wav` | Ambience | big tent interior wind canvas flapping metal creak loop | 30–90 s | loop zona | Carpa principal |
| `AMB_Hallway_01.wav` | Ambience | old wooden corridor creaks low drone loop | 30–60 s | loop zona | Corredor de inicio |
| `SFX_Clown_ChaseLoop_01.wav` | Enemies | heavy running footsteps loop + breathing | 2–5 s | loop mundo, mono | Payaso persiguiendo |
| `SFX_Clown_Capture_01.wav` | Enemies | monster grab attack hit horror | 0,5–1,5 s | one-shot mundo | Payaso alcanza al jugador |
| `SFX_Player_Rescued_01.wav` | Player | chains release relief breath | 1–2 s | one-shot mundo | Jugador rescatado |
| `SFX_Switch_Denied_01.wav` | Interactions | locked mechanism rattle short metal click | 0,2–0,5 s | one-shot mundo | Interruptor ya usado / bloqueado |
| `SFX_Door_HeavyOpen_01.wav` | Environment | heavy old wooden metal door creak open horror | 2–4 s | one-shot mundo | Otras puertas (no la primera) |

### Prioridad baja

| Nombre | Carpeta | Buscar | Duración | Tipo | Uso |
|---|---|---|---|---|---|
| `SFX_Door_FirstDoor_End_01.wav` | Environment | heavy wooden door settle thud dust | 0,5–1,5 s | one-shot mundo | Opcional: tras la cinemática (el video ya trae la apertura) |
| `SFX_Puzzle_Complete_01.wav` | Interactions | heavy mechanism engage clunk gears lock | <1,5 s | global | Opcional: arranca a la vez que la cinemática |
| `SFX_Door_Close_01.wav` | Environment | heavy wooden door slam close old | 1–2 s | one-shot mundo | Puertas que se cierran |
| `SFX_Door_Locked_01.wav` | Environment | door handle locked rattle old | 0,5–1 s | one-shot mundo | Puerta bloqueada |
| `SFX_Clown_Disappear_01.wav` | Enemies | whoosh vanish dark reverse | 0,5–1,5 s | one-shot mundo | Payaso desaparece |
| `SFX_Balloon_Inflate_Loop_01.wav` | Interactions | balloon inflating air stretch rubber loop | 1–2 s | loop mundo | Globos (requiere hook nuevo) |
| `SFX_Balloon_Full_01.wav` | Interactions | balloon squeak tie rubber | 0,3–0,6 s | one-shot mundo | Globo lleno (requiere hook nuevo) |
| `SFX_Lever_Rescue_01.wav` | Interactions | rusty heavy lever pull | 1–2 s | one-shot mundo | Palanca de rescate (requiere hook nuevo) |
