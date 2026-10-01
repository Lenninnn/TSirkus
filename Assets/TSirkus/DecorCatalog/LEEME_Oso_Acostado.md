# Material del oso acostado

Importa TSirkus_Oso_Acostado_Material.unitypackage en Unity 6 URP. Incluye textura de pelaje pardo ceniza, material con shader URP mate y el prefab del oso acostado con la asignación aplicada. Incluye su malla corregida; no requiere importar de nuevo todo el catálogo.

Prefab: Assets/TSirkus/DecorCatalog/Prefabs/Osos/PF_Oso_Pardo_Abandonado_Muerto.prefab
Material: Assets/TSirkus/DecorCatalog/Materials/M_Bear_Abandoned.mat
Textura: Assets/TSirkus/DecorCatalog/Textures/T_Bear_Abandoned_Albedo.png

Conserva el GUID del prefab del oso acostado: actualiza sus instancias, salvo que tengan un material sobrescrito. En ese caso selecciona el hijo Visual y asigna M_Bear_Abandoned al primer elemento de Mesh Renderer > Materials.

No cambia el material compartido del oso de pie. No cambia geometría, colisión ni gameplay. La apariencia de pelo procede de la textura; no añade pelo volumétrico ni rig.

El atlas anterior estaba truncado. Se creó una nueva variante con la herramienta integrada de imágenes, usando como referencia la parte recuperable del atlas original. Prompt: conservar posiciones de las islas UV y rasgos faciales, pelaje pardo ceniza apelmazado con polvo y barro seco, completar las áreas negras con pelaje, color difuso sin iluminación direccional, sin sangre ni heridas.

El shader mantiene las UV faciales originales de la parte recuperada. Para las zonas sin información usa una región de pelaje sin rasgos del atlas generado, y añade una capa leve de polvo. La imagen generada no debe asignarse sola al Base Map; usa el material incluido. Pueden quedar costuras visibles en la transición de las áreas reconstruidas. Revisar el resultado sobre el modelo en Unity. Textura y dependencias comprobadas fuera del Editor; no se ha ejecutado Unity en este entorno.

Base original del oso: qwat385 / Meshy, CC0. Créditos completos en LEEME.md del catálogo.
