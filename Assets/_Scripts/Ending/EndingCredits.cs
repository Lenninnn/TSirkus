using System;
using System.Collections.Generic;
using UnityEngine;

// Textos de la pantalla final. Se editan en el asset Assets/Data/Creditos_TSirkus.asset.
// Una sección sin nombres no se muestra; un nombre sin rol se muestra solo.
[CreateAssetMenu(menuName = "TSirkus/Créditos del final", fileName = "Creditos_TSirkus")]
public class EndingCredits : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string name;

        [Tooltip("Vacío = se muestra solo el nombre.")]
        public string role;
    }

    [Serializable]
    public class Section
    {
        public string heading;
        public List<Entry> entries = new List<Entry>();
    }

    [Header("Portada")]
    public string title = "TSIRKUS";
    public string subtitle = "EL ESPECTÁCULO HA TERMINADO";

    [Header("Créditos")]
    public List<Section> sections = new List<Section>();

    [Tooltip("Línea final opcional (vacía = no se muestra).")]
    [TextArea(1, 3)]
    public string closingLine;
}
