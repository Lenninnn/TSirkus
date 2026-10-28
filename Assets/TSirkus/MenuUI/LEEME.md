# TSirkus — interfaz principal

Incluye Jugar, Configuraciones e Historia. Fondo de circo abandonado, tipografía marfil, acentos burdeos y oro envejecido, estados de enfoque, transiciones, música de caja musical y sonidos de interfaz.

## Instalar y abrir
1. Importa TSirkus_Interfaz.unitypackage completo mediante Assets > Import Package > Custom Package. Espera a que Unity compile los scripts.
2. Abre Tools > TSirkus > Interfaz > Crear o abrir menu.
3. En Escena de juego elige tu mapa. Se propone Assets/Scenes/Complete map.unity si existe. Pulsa Crear / abrir menú.
4. Se crea y abre Assets/TSirkus/MenuUI/Scenes/TSirkus_Menu.unity. Pulsa Play en Unity.

El paquete incluye el instalador y todos los assets. La escena se genera dentro de tu Unity con sus APIs, no al importar el archivo. Se guarda con objetos y componentes editables en Hierarchy. La escena del circo no se modifica.

El instalador añade el menú al inicio de la Scene List compartida y habilita el destino, conservando las demás escenas. Si el Build Profile activo sobrescribe la lista compartida, incluye TSirkus_Menu y tu mapa en su Scene List; el menú debe ser la primera escena del build.

## Funcionamiento
- Jugar carga la escena seleccionada de forma asíncrona, muestra progreso e impide dobles pulsaciones. Si no está incluida en el build, muestra un mensaje y registra el motivo en Console. Conecta al flujo de jugadores que ya tenga esa escena; no crea salas online ni un sistema de matchmaking.
- Configuraciones contiene únicamente el volumen musical (0–100%, incluido silencio) y Volver. El volumen se oye inmediatamente y se guarda con PlayerPrefs tras 0,3 s sin cambios, al volver y al pausar o salir.
- Historia muestra un texto editable con el contexto de cuatro visitantes, el niño y el Boleto Dorado. El texto es una propuesta y puede cambiarse en TSirkus_Menu > TSirkusMenuController > Story.
- Ratón, toque, navegación mediante flechas y mando a través del EventSystem. Escape o botón B vuelve desde las pantallas secundarias. El instalador elige el Input System activo al compilar.

## Editar apariencia y contenido
Todos los objetos quedan bajo TSirkus_Menu > Canvas_Menu > SafeArea. Las pantallas secundarias están desactivadas inicialmente; actívalas temporalmente para editarlas fuera de Play. Sus referencias están en TSirkusMenuController. No regeneres la escena para cambiar textos: edita los Text y el componente directamente. Al abrir de nuevo el instalador se conserva la escena existente y solo cambia el destino elegido.

La composición está diseñada para pantalla horizontal 16:9, escala por ancho y respeta Screen.safeArea. En vertical queda centrada con mayor espacio superior/inferior; no se incluye un diseño vertical alternativo.

## Música
La música del menú es un loop original sintetizado de 24 segundos. Puedes sustituir el AudioClip en Menu_Music. El slider afecta solo a música, no a voces ni efectos de juego.

Para que la música de tu mapa respete el volumen guardado, agrega TSirkusMusicVolume al GameObject de cada AudioSource musical. Track Gain ajusta la ganancia propia de la pista. El componente lee la preferencia al activarse; no lo agregues a voces o efectos. La música del menú se desvanece al cargar el mapa y su AudioSource se destruye con la escena.

## Requisitos y comprobación
Unity 6, uGUI y URP. Compatible con Input System nuevo o input clásico mediante símbolos de compilación. No usa TextMeshPro ni exige importar TMP Essentials.

Se comprobaron la sintaxis C#, los assets, el audio y la estructura del paquete fuera de Unity. No se ejecutó Unity Editor: falta verificar compilación contra tu versión concreta, navegación, slider, carga de escena y aspecto final en Play Mode.

Prueba rápida: abrir Configuraciones, arrastrar a 0% y comprobar silencio; volver a 70%; detener/iniciar Play y comprobar 70%; abrir Historia y Volver; pulsar Jugar y comprobar que abre tu mapa. Prueba también con el mando o pantalla táctil si forman parte de tu plataforma objetivo.

## Créditos
Fondo creado para este menú con la herramienta integrada de generación de imágenes. Prompt: interior de circo infantil abandonado, composición cinematográfica 16:9, mitad izquierda oscura para texto, carpa roja/beige y caja musical a la derecha, sin texto ni interfaz incorporados.
Música y sonidos sintetizados originalmente para este paquete.
Fuentes DejaVu Sans y DejaVu Serif: licencia incluida en Fonts/LICENSE.txt.
