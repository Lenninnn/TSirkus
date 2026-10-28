using UnityEngine;

// Un sonido del juego: uno o varios clips (variaciones) y cómo reproducirlos.
// Si no tiene clips no suena ni da error: sirve como referencia preparada para un
// archivo que aún no existe (ver expectedFile / searchTerms).
[CreateAssetMenu(menuName = "TSirkus/Audio/Sound Cue", fileName = "SFX_")]
public class SoundCue : ScriptableObject
{
    public enum Category { Ambience, SFX, Player, UI, Cinematic }

    [Header("Clips (se elige uno al azar, sin repetir el anterior)")]
    public AudioClip[] clips;

    [Tooltip("Segundos desde los que empieza el clip (para saltar silencio inicial sin editar el archivo).")]
    [Min(0f)] public float startTime;

    [Header("Mezcla")]
    public Category category = Category.SFX;
    [Range(0f, 1f)] public float volume = 0.8f;
    [Range(0f, 0.5f)] public float volumeVariation = 0.05f;
    public Vector2 pitchRange = new Vector2(1f, 1f);

    [Header("Espacio")]
    [Tooltip("Global: igual para todos (UI, cinemática, stings). Si no, se atenúa por distancia al jugador más cercano.")]
    public bool global;
    [Min(0f)] public float minDistance = 2f;
    [Min(0.1f)] public float maxDistance = 20f;

    [Header("Anti-duplicado")]
    [Tooltip("No vuelve a sonar si se pidió hace menos de estos segundos.")]
    [Min(0f)] public float minInterval = 0.05f;

    [Header("Documentación (archivo pendiente)")]
    [Tooltip("Nombre final esperado del archivo dentro de Assets/SFX.")]
    public string expectedFile;
    [TextArea(2, 4)] public string searchTerms;

    private int lastIndex = -1;

    public bool HasClips => clips != null && clips.Length > 0 && clips[0] != null;

    public AudioClip PickClip()
    {
        if (!HasClips)
            return null;

        if (clips.Length == 1)
            return clips[0];

        int index = Random.Range(0, clips.Length - 1);
        if (index >= lastIndex) index++;
        lastIndex = index;
        return clips[index];
    }

    public float PickVolume() => Mathf.Clamp01(volume * (1f + Random.Range(-volumeVariation, volumeVariation)));
    public float PickPitch() => Random.Range(pitchRange.x, pitchRange.y);
}
