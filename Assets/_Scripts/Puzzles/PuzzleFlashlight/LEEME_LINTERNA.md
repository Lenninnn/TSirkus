# Puzzle de la linterna (Skrytye Slova)

## Flujo

1. Los 4 jugadores cruzan `RoomFlashlight` (`FlashlightPuzzleRoomTrigger`) → `PuzzleGeneral.ActivatePuzzle()`
   y `PuzzleLinterna` (`SkrytyeSlovaController.ActivatePuzzle()`) → se encienden las 4 linternas.
2. Cada linterna (`Flashlightpivot` del prefab `Player`) se calibra con la postura del teléfono en ese momento.
3. Cada jugador busca SUS 3 palabras (repartidas según la frase):
   P1 CUANDO LAS LUCES · P2 SE APAGUEN NO · P3 OLVIDES AL PAYASO · P4 DETRÁS DE TI.
   Una palabra se revela cuando queda ≤ 5° y ≤ 0,5 m del eje del haz, dentro del alcance de la luz,
   sin nada delante, durante 0,25 s. Solo la ve ese jugador (capa privada 28-31 en su cámara).
4. Con las 12 encontradas se apagan las linternas y empieza la fase de frase (`PhraseNetworkBridge`,
   puerto 9001): cada celular recibe sus 3 palabras y envía la frase completa.
5. Las 4 frases correctas → `phrase_all_complete` → `PuzzleGeneral.CompletePuzzle()` (estado Success).
   `PuzzleGeneral` no tiene eventos conectados: hoy no abre ninguna puerta.

## Datos del celular (contrato, `MobileInputManager`, TCP 9000, una línea JSON por mensaje)

```
{"type":"player_connected","playerId":1}
{"type":"puzzle_accelerometer","playerId":1,"x":0.01,"y":5.62,"z":8.04}   (también vale "accelerometer")
```

x, y, z = `accelerationIncludingGravity` en m/s² (ejes del teléfono: x derecha, y arriba, z sale de la
pantalla). Quieto, |a| ≈ 9,81. `FlashlightController` también funciona si llegan en g (|a| ≈ 1) y si el
teléfono entrega todos los signos invertidos. NO funciona con aceleración sin gravedad (|a| ≈ 0): lo avisa
en consola al calibrar.

## Ajustes (en el prefab `Player`, `Flashlightpivot`)

| Campo | Valor | Por qué |
|---|---|---|
| horizontalGain / verticalGain | 1,5 | ±30° de muñeca (rango cómodo) recorren ±45° de haz |
| deadZoneDegrees | 1,5 | ignora el pulso y la deriva en la neutral; continua, sin saltos |
| sensorSmoothTime / aimSmoothTime | 0,06 s | corte ~2,7 Hz: quita el temblor (8-12 Hz); 95 % de un giro en ~0,3 s |
| shakeTolerance | 0,15 | un tirón de 6 m/s² mueve el haz 0,3° (antes 18°) |
| calibrationTime / StillAngle / MaxWait | 0,4 s / 4° / 2 s | neutral = promedio con el teléfono quieto |
| Light | 35 / 20 m / 40° (interior 22°) | antes 1 / 10 m: el haz no se veía |
| Posición del pivote | (0,3; 1,4; 0,4) | a la altura de la mano; antes salía del pie derecho |

Si mañana con un teléfono real el haz va al revés en un eje: `invertHorizontal` / `invertVertical`.

## Probar sin celulares (DEBUG / EDITOR)

`TSirkus/Debug/Simulador de sensores/Activar (EDITOR)` y luego Play. Un servidor local (127.0.0.1:9100 y
:9101) envía el mismo JSON por el mismo socket; la escena no cambia y en builds no existe. Ventana:
`TSirkus/Debug/Simulador de sensores/Abrir ventana (EDITOR)` (inclinación por jugador, ruido, pérdida de
datos, signos invertidos). Se apaga solo al cerrar Unity; apágalo antes de probar con teléfonos.
