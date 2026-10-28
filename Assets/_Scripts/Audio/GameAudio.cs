using System.Collections.Generic;
using UnityEngine;

// Punto central del audio de juego (escena GAME).
// - Split-screen: Unity solo tiene un AudioListener y los 4 jugadores comparten altavoces,
//   así que los sonidos del mundo se atenúan según el jugador MÁS CERCANO (si alguien está
//   cerca, se oye; si todos están lejos, no). Se reproducen en 2D: el paneo izquierda/derecha
//   no tiene sentido con pantalla dividida.
// - Pool fijo de AudioSources para one-shots (sin crear/destruir en runtime).
// - Volumen por categoría (Ambience, SFX, Player, UI, Cinematic) y "ducking" del ambiente
//   mientras se ven overlays como la cinemática.
public class GameAudio : MonoBehaviour
{
    public static GameAudio Instance { get; private set; }

    [Header("Volumen por categoría")]
    [Range(0f, 1f)] public float ambienceVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 0.9f;
    [Range(0f, 1f)] public float playerVolume = 0.8f;
    [Range(0f, 1f)] public float uiVolume = 0.6f;
    [Range(0f, 1f)] public float cinematicVolume = 0.9f;

    [Header("Pool de one-shots")]
    [SerializeField] private int poolSize = 10;

    [Header("Ducking del ambiente")]
    [Tooltip("Mientras alguno esté visible (alpha > 0), el ambiente baja a ambienceDuck.")]
    [SerializeField] private CanvasGroup[] duckWhileVisible;
    [Range(0f, 1f)] [SerializeField] private float ambienceDuck = 0.25f;

    private readonly List<AudioSource> pool = new List<AudioSource>();
    private readonly Dictionary<SoundCue, float> lastPlayTime = new Dictionary<SoundCue, float>();
    private readonly List<Transform> players = new List<Transform>();
    private float nextPlayerRefresh;
    private int nextSource;

    public float AmbienceDuckFactor { get; private set; } = 1f;


    private void Awake()
    {
        Instance = this;

        for (int i = 0; i < poolSize; i++)
        {
            GameObject child = new GameObject($"OneShot_{i}");
            child.transform.SetParent(transform, false);
            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            pool.Add(source);
        }
    }


    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }


    private void Update()
    {
        float target = 1f;

        if (duckWhileVisible != null)
        {
            foreach (CanvasGroup group in duckWhileVisible)
            {
                if (group != null && group.gameObject.activeInHierarchy && group.alpha > 0.01f)
                    target = ambienceDuck;
            }
        }

        AmbienceDuckFactor = Mathf.MoveTowards(AmbienceDuckFactor, target, Time.unscaledDeltaTime * 2f);
    }


    // =====================================================
    // REPRODUCIR
    // =====================================================

    // Global (UI, stings, mecanismo). Llamable desde UnityEvents.
    public void PlayCue(SoundCue cue) => Play(cue, null);

    // Sonido del mundo en una posición (atenuado por el jugador más cercano).
    public void PlayCueAt(SoundCue cue, Vector3 position) => Play(cue, position);

    private void Play(SoundCue cue, Vector3? position)
    {
        if (cue == null || !cue.HasClips || !PassesInterval(cue))
            return;

        float gain = cue.global || position == null
            ? 1f
            : Attenuation(position.Value, cue.minDistance, cue.maxDistance, null);

        if (gain <= 0.001f)
            return;

        AudioSource source = NextSource();
        source.clip = cue.PickClip();
        source.volume = cue.PickVolume() * CategoryVolume(cue.category) * gain;
        source.pitch = cue.PickPitch();
        source.loop = false;
        source.time = Mathf.Min(cue.startTime, source.clip.length - 0.01f);
        source.Play();
    }


    private bool PassesInterval(SoundCue cue)
    {
        float now = Time.unscaledTime;

        if (lastPlayTime.TryGetValue(cue, out float last) && now - last < cue.minInterval)
            return false;

        lastPlayTime[cue] = now;
        return true;
    }


    // Reutiliza la siguiente fuente libre; si todas suenan, la más antigua.
    private AudioSource NextSource()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            AudioSource candidate = pool[(nextSource + i) % pool.Count];

            if (!candidate.isPlaying)
            {
                nextSource = (nextSource + i + 1) % pool.Count;
                return candidate;
            }
        }

        AudioSource oldest = pool[nextSource];
        nextSource = (nextSource + 1) % pool.Count;
        return oldest;
    }


    public float CategoryVolume(SoundCue.Category category)
    {
        switch (category)
        {
            case SoundCue.Category.Ambience: return ambienceVolume * AmbienceDuckFactor;
            case SoundCue.Category.Player: return playerVolume;
            case SoundCue.Category.UI: return uiVolume;
            case SoundCue.Category.Cinematic: return cinematicVolume;
            default: return sfxVolume;
        }
    }


    // =====================================================
    // ATENUACIÓN SPLIT-SCREEN
    // =====================================================

    // 1 dentro de minDistance; cae de 0 dB a -40 dB hasta maxDistance; 0 más allá.
    // ignore: jugador que no cuenta (p. ej. el que produce sus propios pasos).
    public float Attenuation(Vector3 position, float minDistance, float maxDistance, Transform ignore)
    {
        float distance = NearestPlayerDistance(position, ignore);

        if (distance <= minDistance) return 1f;
        if (distance >= maxDistance) return 0f;

        float t = (distance - minDistance) / Mathf.Max(0.01f, maxDistance - minDistance);
        return Mathf.Pow(10f, -2f * t);
    }


    public float NearestPlayerDistance(Vector3 position, Transform ignore)
    {
        RefreshPlayers();

        float best = float.MaxValue;

        foreach (Transform player in players)
        {
            if (player == null || player == ignore)
                continue;

            best = Mathf.Min(best, Vector3.Distance(position, player.position));
        }

        return best;
    }


    private void RefreshPlayers()
    {
        if (Time.unscaledTime < nextPlayerRefresh && players.Count > 0)
            return;

        nextPlayerRefresh = Time.unscaledTime + 1f;
        players.Clear();

        foreach (PlayerIdentity identity in FindObjectsByType<PlayerIdentity>())
        {
            if (identity.GetComponentInChildren<Camera>(true) != null)
                players.Add(identity.transform);
        }
    }
}
