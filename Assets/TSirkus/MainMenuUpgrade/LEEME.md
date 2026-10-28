# TSirkus — mejora de tu MainMenu

Esta es una copia mejorada de la escena MainMenu que enviaste. Conserva tu logo, tu video, el diseño de boletos y los objetos originales de cámara/Canvas/EventSystem. Tu escena original y el mapa no se sustituyen.

## Abrirla
1. Guarda el trabajo abierto. Importa TSirkus_MainMenu_Mejorado.unitypackage.
2. Abre Assets/TSirkus/MainMenuUpgrade/Scenes/MainMenu_Mejorado.unity.
   También puedes usar Tools > TSirkus > Menú actual > Abrir menú mejorado.
3. Pulsa Play para reproducir el video y probar la interfaz.

Los recursos originales de UI/MainMenu se incluyen con los mismos GUID y contenido. Si Unity los muestra sin casilla, pueden ser recursos idénticos que ya existen. Los archivos nuevos están bajo MainMenuUpgrade. No borres la carpeta del menú anterior.

## Qué incluye
- Tres boletos: JUGAR, CONFIGURACIONES, HISTORIA. Etiquetas editables, estados de selección/pulsación y navegación por teclado/mando a través del EventSystem existente.
- Configuraciones: deslizador continuo 0–100%, aplicado al audio del video original. El volumen se guarda con PlayerPrefs (TSirkus.MusicVolume.v1). No cambia efectos ni voces de la partida.
- Historia: texto propuesto, editable en TSirkus_MenuController > Story. No supone que este texto sea la historia definitiva del juego.
- Volver y Escape / botón B para regresar de las pantallas secundarias.
- Transición de entrada a los paneles y pantalla de carga con progreso al abrir la partida.
- Se desactiva el reproductor duplicado cuyo clip no estaba incluido. El reproductor válido conserva tu video y su audio.
- Fondo oscurecido con moderación, logo proporcionado y tipografía crema/dorada sobre paneles cálidos.

Los paneles Settings, History y Loading quedan inactivos hasta que se abren. Para editarlos fuera de Play, actívalos temporalmente y pon el Alpha de su CanvasGroup a 1; después desactívalos de nuevo. El componente Slider se añade al iniciar sobre los gráficos editables de Music_Slider. Los botones se conectan desde el controlador al iniciar; es normal que el Inspector muestre la lista On Click vacía fuera de Play.

## Conectar Jugar
Destino inicial: Assets/Scenes/Complete map.unity.
Ese mapa no viene en el paquete: se reutiliza el que tienes en tu proyecto.
En Editor puede abrirse directamente si existe en esa ruta.
Si cambió de nombre o ubicación:
1. Abre MainMenu_Mejorado.
2. Tools > TSirkus > Menú actual > Conectar escena de Jugar.
3. Arrastra tu escena de partida al campo y pulsa Conectar y registrar escenas.
4. En File > Build Profiles, revisa la Scene List del perfil activo: el menú debe estar primero y el mapa habilitado. Si el perfil usa su propia lista, actualízala allí también.

Jugar abre tu escena existente. No crea una sala multijugador ni altera el código de conexión de tu juego.

## Dependencias y alcance
Unity 6 URP, Unity UI (UGUI) y el Input System que ya usa tu escena. Se conservan las referencias a los componentes URP y al EventSystem de tu exportación.
No se incluyen ni se sobrescriben scripts de gameplay exportados por dependencia.
Los scripts nuevos tienen namespace TSirkus.MainMenuUpgrade, separado del paquete anterior.
Composición para pantallas horizontales, con Canvas Scaler 1920x1080. No está diseñada para orientación vertical.

## Verificación
Se revisaron la sintaxis C#, la estructura de la escena, sus referencias, los GUID y el contenido del paquete. Se decodificaron imágenes y video. NO se ejecutó el Unity Editor en este entorno; quedan pendientes la compilación/importación y la prueba en Play en tu proyecto.
Prueba allí: abrir Configuraciones; llevar volumen a 0 y a 100%; salir de Play y volver a entrar para comprobar persistencia; abrir Historia y volver; pulsar Jugar para cargar tu mapa. Comprueba también las flechas/Enter o el mando que uses.

La API utilizada para el volumen del video está documentada por Unity:
https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Video.VideoPlayer.SetDirectAudioVolume.html
Fuentes DejaVu incluidas con su licencia en Fonts/LICENSE.txt. El boleto vacío se adaptó de tu gráfico; el logo y el video se conservan sin cambios.
