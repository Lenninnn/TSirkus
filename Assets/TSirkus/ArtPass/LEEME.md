# TSirkus — art pass del mapa existente

Escena modificada: `Assets/Scenes/Complete map.unity`.

Base de trabajo: rama `Majo`, commit `19b75d7a72dbc9026c6241e246535c5a63d0939f`, que ya integra los avances de `lennin`. Proyecto: Unity **6000.6.0f1**, URP **17.6.0**, sin cambios de versión ni paquetes. Esta entrega modifica realmente la escena y sus asignaciones; no requiere ejecutar un generador.

## Qué cambió

- Carpa: rojo y beige envejecidos, tejido con manchas, pista con pintura gastada, suelo y tarima con madera marcada y suciedad periférica. Tres bombillas decorativas tienen aspecto apagado; las luces de orientación siguen activas.
- Arquitectura: paredes con grietas finas, humedad, pintura gastada y variaciones de tono. Manchas localizadas superpuestas sobre superficies existentes.
- Pasillos: madera con repetición ajustada a sus dimensiones, paredes sucias y manchas en bordes, conservando los accesos.
- Sala 1: desgaste moderado y luz cálida.
- Sala 2: humedad verdosa, piso con más brillo y decoración incompleta.
- Sala 3: hollín y quemaduras concentrados en una esquina, colores ocres y luz anaranjada.
- Salas futuras: superficies envejecidas y dressing mínimo para mantener el espacio disponible.
- Props existentes: copias de sillas, mesas, cajas y marcos con madera gastada, tela manchada y metal oxidado. Sus dimensiones y colisiones se conservan.
- Iluminación: se reduce la luz direccional de 2 a 0,6; se conservan puntos de luz que permiten identificar la pista, instrucciones y accesos. Ambiente cálido y niebla lineal suave a partir de 24 m.

El desgaste estructural es visual: texturas, relieve ligero y manchas. No se han abierto agujeros en paredes ni roto patas que sigan teniendo colisiones sólidas.

## Organización y edición

Todos los assets nuevos están en `Assets/TSirkus/ArtPass`:

| Carpeta | Contenido |
| --- | --- |
| `Materials_Damaged` | 65 materiales nuevos, incluidas variantes de repetición UV y manchas |
| `Textures` | 4 albedos originales generados y 4 fuentes para relieve importado por Unity |
| `Prefabs/Props_Damaged` | 4 copias de props existentes |
| `Prefabs/Rooms_Damaged` | 5 copias de las salas, enlazadas a los props dañados |
| `Shaders` | Shader URP estático para superponer suciedad, humedad y hollín |

Los nombres de los objetos originales se conservan. Los nombres de archivo de las copias terminan en `_Damaged`. La escena apunta a estas copias; los prefabs y materiales originales no se sobrescriben.

Los elementos añadidos están bajo `TSirkus_ArtPass_Visual`, tanto en el nivel como en cada copia de sala, con contenedores `Wall_Damage_Set`, `Decals_Dirt`, `Burned_Variants` y, donde corresponde, `Props_Damaged`. Son 45 renderers adicionales en total: manchas y pequeños restos de tela en esquinas. No tienen colliders, scripts, etiquetas de gameplay ni contribución a navegación estática; usan la capa Ignore Raycast y no proyectan sombras.

Para ajustar la suciedad, selecciona un material `M_Decal_*` y modifica `Weathering strength`. Para retirar solo el dressing añadido, desactiva sus contenedores `TSirkus_ArtPass_Visual`. Esto no revierte la asignación de materiales ni la iluminación.

## Abrir la entrega

En el checkout de trabajo, abre directamente `Assets/Scenes/Complete map.unity` con la versión de Unity indicada. No ejecutes el constructor de blockout: volvería a generar otro nivel.

Si usas el paquete `.unitypackage`:

1. Utiliza una copia del proyecto basada en el commit de `Majo` indicado arriba. El paquete depende de los meshes, scripts y assets originales de ese proyecto; no es un mapa independiente.
2. Guarda una copia de tu escena si tienes cambios locales posteriores que quieras conservar. La importación sustituye `Complete map.unity` por la escena con el art pass.
3. En Unity, selecciona **Assets > Import Package > Custom Package**, elige el paquete e importa todos los elementos. Los GUID de la escena y las referencias originales se conservan.
4. Espera la importación de texturas y abre `Assets/Scenes/Complete map.unity`.

No hay postprocesadores de importación nuevos, `SaveAndReimport`, scripts de ejecución ni modificaciones al importador del ticket.

## Verificación realizada y revisión pendiente

El validador comparó 3263 registros serializados con la base: preserva 794 transformaciones existentes y 330 componentes de física, scripts, cámaras y UI. Comprueba copias de prefabs, referencias, GUID únicos, texturas 2D y que ningún archivo original distinto de la escena haya cambiado. Resultado en `validation_report.json`.

Para repetirlo, con Python 3 y PyYAML instalados:

```bash
python Tools/ArtPass/validate_art_pass.py
```

**Unity Editor no está disponible en el entorno de preparación.** No se ha ejecutado la importación en Unity, compilado el shader en Metal, renderizado la escena final ni probado Play Mode. La prueba de integridad no garantiza por sí sola el aspecto final o el rendimiento.

Antes de integrar con el equipo, revisar en Unity:

- Consola tras importar: shader sin errores y materiales sin magenta.
- Lobby, pista, escenario, pasillos y cinco salas desde la cámara de jugador.
- Lectura de tickets, periódicos, interruptores, puertas e instrucciones.
- Movimiento, colisiones, spawns y navegación del payaso en Play Mode.
- Pantalla dividida: iluminación y rendimiento con las cuatro vistas activas.

No se han horneado nuevas lightmaps, reflejos ni NavMesh. Si hay datos de iluminación horneada de una versión anterior, revisar su coherencia con los materiales nuevos en el editor.

## Texturas y técnica

Albedos originales generados para este pase: madera con marcas y quemaduras localizadas, lona neutra con moho, pintura/enlucido húmedo y acero con óxido. Las imágenes fuente se incorporan sin retoques y con importación 2D explícita, repetición y mipmaps; resolución importada máxima 1024. Las copias `HeightSource` conservan los mismos píxeles y Unity deriva de su luminancia un normal map suave. Son relieve aproximado, no mapas PBR medidos.

Dirección común usada para generar las cuatro imágenes: textura cuadrada de albedo que ocupa todo el encuadre, vista ortogonal frontal, sin texto, marco, perspectiva, sombras proyectadas ni luz direccional horneada; repetición de bordes solicitada, envejecimiento realista a escala de juego y valores medios legibles con luz cálida tenue. Cada imagen usa su material correspondiente: tablas marrones desgastadas, tela beige grisácea sin estampado, enlucido beige agrietado con humedad, o acero gris con pintura oscura y óxido ocre. La repetición visual final debe revisarse en Unity.

Los materiales principales reutilizan URP/Lit. El damero conserva su textura original y añade detalle superficial. El shader de manchas multiplica suavemente el color ya iluminado y desvanece los bordes; funciona sobre quads finos sin exigir una Decal Renderer Feature. Es estático, sin parpadeos, lógica ni sincronización de red.

Referencias técnicas: [URP Lit](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/lit-shader.html), [importación de normal maps](https://docs.unity3d.com/6000.0/Documentation/Manual/texture-type-normal-map.html), [estructura de shaders URP](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/writing-shaders-urp-basic-unlit-structure.html).
