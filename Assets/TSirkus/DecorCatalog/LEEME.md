# Corrección v1.1 — modelos invisibles

Este paquete completo sustituye los archivos del catálogo anterior manteniendo sus GUIDs. Incluye los 29 modelos, prefabs, texturas, materiales y la escena independiente.

Correcciones: rangos de índices LOD0 válidos en las 29 mallas; eliminación de alias YAML; campos vacíos serializados sin el literal null.

Instalación:
1. Guarda tu escena actual. Importa TSirkus_Decoracion_29_Props_CORREGIDO_v1_1.unitypackage usando Assets > Import Package > Custom Package.
2. Importa los archivos modificados. Los archivos idénticos pueden permanecer desactivados en la ventana; es normal. Si aparece Missing Signature, el paquete no está firmado.
3. Abre Assets/TSirkus/DecorCatalog/Scenes/TSirkus_DecorCatalog.unity.
4. Si Unity conserva una vista previa antigua, haz clic derecho sobre Assets/TSirkus/DecorCatalog y selecciona Reimport una vez.

No borres la carpeta ni sus .meta: se conservan los identificadores para mantener las referencias. No se modifica Complete map ni el gameplay. Los prefabs están en DecorCatalog/Prefabs.

Se validaron buffers, índices, rangos LOD, referencias y contenido del paquete fuera del Editor. No se ha ejecutado Unity Editor en este entorno; la importación y Play Mode necesitan comprobación en tu equipo.

--- Información del catálogo original ---

# TSirkus — catálogo independiente de decoración

29 prefabs individuales, una escena de exposición y materiales URP propios. No reemplaza ni modifica el mapa del circo. Escala en metros, pivotes en la base de los objetos. Texturas incluidas: madera envejecida, metal oxidado, lona húmeda, pintura y pelaje.

## Instalación
1. En un proyecto Unity 6 URP, usa Assets > Import Package > Custom Package y selecciona TSirkus_Decoracion_29_Props.unitypackage.
2. Abre Assets/TSirkus/DecorCatalog/Scenes/TSirkus_DecorCatalog.unity. Guarda antes cualquier cambio pendiente en tu escena actual.
3. Arrastra los objetos de Assets/TSirkus/DecorCatalog/Prefabs a las escenas que quieras decorar. Los materiales están en Materials y las mallas en Meshes.

## Contenido
- Tres gradas curvas: pequeñas, medianas y grandes, con madera quemada, estructura metálica y escalones centrales.
- Tres balones: gastado, desinflado y roto; globo marchito y conjunto de globos desinflados.
- Dos estantes, dos mesas, dos cajas musicales y dos monociclos.
- Cuerda enrollada, cuerdas colgantes rotas, barrera de cuerda rota y dos mallas rasgadas.
- Dos cajas de madera, dos barriles, pedestal y cartel de entrada.
- Oso pardo delgado de pie y oso abandonado recostado, sin heridas explícitas.

## Osos
El oso de pie incluye TSirkusAmbientBearBreath, un movimiento ambiental sutil de respiración que funciona en Play. Puede desactivarse en el prefab. Es decoración animada: no incluye IA, locomoción, comportamiento multijugador ni rig de personaje terminado. El oso recostado es estático. Ambos son adaptaciones de una base con anatomía realista; requieren revisión artística adicional para primeros planos de producción.

## Colisiones y rendimiento
Los props usan MeshCollider estático; los osos usan BoxCollider. No añadas un Rigidbody dinámico a un MeshCollider no convexo sin adaptar primero su colisión. No se han añadido componentes de gameplay, triggers ni spawns. Los prefabs no cambian los materiales originales del mapa.

## Fuentes editables
TSirkus_Decoracion.blend contiene las mallas organizadas por colecciones y texturas empaquetadas. Los FBX son exportaciones estáticas individuales. Para Unity, usa preferentemente los prefabs del paquete: ya incluyen las asignaciones de materiales URP. Los nodos de material de Blender pueden requerir reasignación al usar FBX en otra aplicación.

Los scripts Python de Tools/DecorCatalog permiten reconstruir los assets; requieren bpy 4.3, numpy < 2, PyYAML y scipy, además de las fuentes descritas en sus rutas de entrada. No son necesarios para importar el paquete.

## Validación
Se comprobaron fuera de Unity: estructura YAML, GUIDs, referencias locales, buffers de malla, índices, número de prefabs y ausencia de modificaciones en los archivos existentes. Las vistas previas se renderizaron en Blender. No se ejecutó Unity Editor en este entorno: la importación, el aspecto exacto en URP y Play Mode quedan pendientes de verificación en tu proyecto. No hay AssetPostprocessor ni reimportación automática.

## Créditos
Props y adaptaciones creados para TSirkus. Base del oso: qwat385, modelo público Meshy con licencia CC0, descargado el 25-09-2026:
https://www.meshy.ai/3d-models/A-massive-grizzly-brown-bear-quadrupedal-standing-pose-on-four-legs-prominent-shoulder-hump-thick-shaggy-fur-texture-with-varied-dark-brown-and-earthy-tones-muscular-build-rugged-forest-and-mountain-environment-dweller-realistic-snout-and-powerful-claws-game-ready-asset-highfidelity-3D-model-v2-019b6ac9-a891-780f-8b55-c842301d52d2
Licencia: https://creativecommons.org/publicdomain/zero/1.0/
