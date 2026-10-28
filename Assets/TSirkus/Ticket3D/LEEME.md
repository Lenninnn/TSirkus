# Ticket 3D de TSirkus · Unity 6 / URP

El número 0001 se eliminó del diseño. Este paquete contiene un modelo 3D real en OBJ, con UV, frente impreso, reverso de papel envejecido, grosor y perforaciones en la malla.

## Corrección v1.1: bucle de importación de la textura

La versión anterior modificaba el TextureImporter del PNG y llamaba a `SaveAndReimport` al crear el prefab. Tras el error de importación mostrado en Unity 6.4, se eliminó ese paso: la herramienta ahora lee la textura importada y conserva los ajustes de su Inspector. El menú queda deshabilitado mientras Unity importa o compila.

Si ya importaste el modelo, aplica `TSirkus_Ticket3D_Correccion.unitypackage` desde **Assets → Import Package → Custom Package**. Este parche incluye el script actualizado y esta guía. Espera a que termine de compilar.

Para recuperar la importación que falló: abre **Project → Assets → TSirkus → Ticket3D → Textures**, haz clic derecho en **TSirkus_Ticket_Print.png → Reimport** una vez y espera a que Unity termine. Después limpia los mensajes antiguos con **Console → Clear** y ejecuta **Tools → TSirkus → Crear ticket 3D**. Al completarse, Console mostrará **TSirkus Ticket 3D v1.1: ticket creado**.

También puedes sustituir solo `Assets/TSirkus/Ticket3D/Editor/TSirkusTicketImport.cs` por el archivo corregido, conservando su `.meta`. Usa uno de los dos métodos. La herramienta de Editor se ejecuta desde Tools.

Si la reimportación manual vuelve a fallar, conserva el primer error nuevo de Console: podría intervenir otro importador o un problema del Editor que no es visible en la captura. No se ha ejecutado esta corrección dentro de Unity en el entorno de creación.

## Importación recomendada

1. En tu proyecto Unity 6 URP, importa `TSirkus_Ticket3D.unitypackage` desde **Assets → Import Package → Custom Package**. Selecciona todos sus archivos y pulsa Import.
2. Espera a que Unity termine de importar y compilar. Si el proyecto tiene errores de compilación previos, deben resolverse para que aparezcan los nuevos menús.
3. Fuera de Play Mode, abre **Tools → TSirkus → Crear ticket 3D**.
4. Aparecerá `Ticket_TSirkus` en `(0, 1, 0)`, seleccionado en la escena. Pulsa F con el cursor sobre Scene View para enfocarlo si lo necesitas.
5. Guarda la escena. El prefab queda en `Assets/TSirkus/Ticket3D/Prefabs/PF_TSirkus_Ticket.prefab`.

También puedes descomprimir `TSirkus_Ticket3D.zip` y copiar su carpeta `Assets/TSirkus/Ticket3D` dentro de `Assets/TSirkus` de tu proyecto. Después ejecuta el mismo menú. Elige uno de los dos métodos de importación.

El script incluido es una herramienta de Editor: se usa desde Tools y no se arrastra a un GameObject. El OBJ ya está modelado; la herramienta configura el material URP, normaliza la escala, centra el pivote y crea el prefab con su BoxCollider. Al ejecutar de nuevo el menú, instancia el prefab existente y conserva sus modificaciones.

## Conectarlo a la interacción

Abre `PF_TSirkus_Ticket` desde Project y añade el componente de recogida de tu juego a la raíz del prefab, en el mismo GameObject que tiene el BoxCollider. Si tu sistema usa **TicketPickup**, ese es el componente que debes añadir; configura ahí la cantidad que entrega. El número eliminado era decorativo: no controla la cantidad de tickets.

El paquete entrega el objeto y su preparación visual; la recogida, el contador, la desaparición y la sincronización multijugador deben conectarse a los componentes de tu juego. El collider es sólido y permite apuntarle con un raycast. Si tu interacción usa triggers, ajusta el collider según ese sistema. No se añade Rigidbody; el ticket queda donde lo ubiques.

Puedes duplicar las instancias del prefab una vez configurado. Prueba primero una recogida antes de colocar muchas copias.

## Características del objeto

| Propiedad | Valor |
| --- | --- |
| Ancho del prefab preparado | 28 cm |
| Alto aproximado | 10,1 cm |
| Grosor de la cartulina | 0,8 mm |
| Forma | Leve curvatura; borde irregular y muescas |
| Perforaciones internas | 14 agujeros reales |
| Triángulos | 5.688 |
| Material | Un material URP/Lit opaco |
| Frente | Diseño TSIRKUS sin número |
| Reverso | Papel sin impresión, obtenido mediante otra zona de UV |
| Pivote | Centrado por la herramienta de Unity |

El volumen del BoxCollider tiene 2 cm de profundidad para facilitar la selección en primera persona. Esto no cambia el grosor visible de la cartulina. El collider aproxima el cuerpo del ticket y cubre también sus pequeñas perforaciones; no representa cada detalle del borde.

El material usa iluminación de la escena. Coloca el ticket cerca de una luz o comprueba el material en una escena iluminada si aparece muy oscuro. Mantiene los colores del diseño y una superficie mate.

La versión 1.1 conserva la configuración de importación que ya tenga el PNG. Para ajustar su calidad puedes seleccionar la textura en Project: **Texture Type: Default**, **sRGB: activado**, **Max Size: 2048**, **Wrap Mode: Clamp** y **Filter Mode: Trilinear**. Aplica estos cambios desde el Inspector si los necesitas; no son un paso obligatorio para crear el prefab.

## Archivos y edición

- `Models/TSirkus_Ticket.obj`: malla triangulada con normales y UV. Se puede abrir en Unity o importar en Blender.
- `Models/TSirkus_Ticket.mtl`: material de intercambio para visores de OBJ.
- `Textures/TSirkus_Ticket_Print.png`: textura de impresión que usa el modelo.
- `Editor/TSirkusTicketImport.cs`: preparación del material, collider y prefab en Unity.
- `Docs/Ticket_3D_Frente.png` y `Docs/Ticket_3D_Reverso.png` en el ZIP: renders técnicos del modelo exportado con fondo transparente.
- `Docs/Verificacion_Modelo.json`: medidas y comprobaciones de la malla.

La textura de impresión conserva un margen de cuadrícula de la edición generativa. Ese margen queda fuera de la geometría visible: el borde y los agujeros están recortados en la malla. La textura está preparada para el objeto 3D; para una imagen aislada, las vistas renderizadas incluidas tienen transparencia real.

El reverso usa una zona de papel vacía de la textura. Así conserva el color del frente y no muestra la tipografía invertida. Para crear después un reverso ilustrado, se puede sustituir esa isla de UV y añadir una textura específica.

## Verificación y límites

Se comprobaron índices, UV, medidas, normales, cierre de la superficie y orientación consistente de las caras. La malla es una superficie cerrada con 14 agujeros pasantes. Las vistas incluidas se renderizaron a partir de esa misma geometría y textura; no son una ilustración de un modelo distinto.

El entorno de creación no dispone de Unity ni de un compilador C#. La importación del `.unitypackage`, la compilación de la herramienta y la integración con la recogida deben probarse en tu proyecto. El OBJ, la textura y el código también están disponibles dentro del ZIP.

Unity admite OBJ como formato estándar de modelos: [referencia de formatos de Unity 6](https://docs.unity3d.com/6000.0/Documentation/Manual/3D-formats.html). La herramienta usa el [shader Lit de URP](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/lit-shader.html) y las APIs nativas de prefabs.

Unity documenta los reinicios de importación durante el [refresco de AssetDatabase](https://docs.unity3d.com/6000.0/Documentation/Manual/AssetDatabaseRefreshing.html) y el comportamiento de [SaveAndReimport](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AssetImporter.SaveAndReimport.html). La captura confirma el bucle y la textura afectada; identificar todas las causas dentro de tu proyecto requiere probar allí.
