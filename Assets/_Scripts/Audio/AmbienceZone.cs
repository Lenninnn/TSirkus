using UnityEngine;

// Loop de ambiente. Global: suena igual para todos. Zona: su volumen depende de la
// distancia del jugador más cercano a la zona (fuera de 'fadeDistance' se calla).
// - Loop continuo con dos fuentes: antes del final arranca otra copia y se cruzan, así
//   los fundidos propios del archivo no dejan un bajón en cada vuelta.
// - Pausa mientras GameAudio.AmbiencePaused (cinemática) y se reanuda donde iba, con fade-in.
// Solo crea AudioSources si el cue tiene clip: un cue vacío no cuesta nada.
public class AmbienceZone : MonoBehaviour
{
    [SerializeField] private SoundCue loopCue;

    [Tooltip("Global: sin distancia. Si no, zona centrada en este objeto.")]
    [SerializeField] private bool global;

    [Tooltip("Radio en el que el ambiente suena al 100 %.")]
    [Min(0f)] [SerializeField] private float innerRadius = 8f;

    [Tooltip("Distancia extra a lo largo de la cual se desvanece.")]
    [Min(0.1f)] [SerializeField] private float fadeDistance = 6f;

    [Header("Loop")]
    [Tooltip("Segundos de cruce entre el final de una vuelta y el inicio de la siguiente.")]
    [Min(0f)] [SerializeField] private float crossfadeSeconds = 4f;

    [Tooltip("Las vueltas siguientes empiezan aquí (salta el fade-in del archivo).")]
    [Min(0f)] [SerializeField] private float loopStartSeconds = 1f;

    [Header("Inicio / pausa")]
    [Min(0.05f)] [SerializeField] private float startFadeSeconds = 2f;
    [Min(0.05f)] [SerializeField] private float pauseFadeSeconds = 0.4f;
    [Min(0.05f)] [SerializeField] private float resumeFadeSeconds = 3f;

    private readonly AudioSource[] sources = new AudioSource[2];
    private int active;
    private bool crossfading;
    private bool sourcesPaused;
    private float zoneGain;
    private float pauseGain;
    private float fadeSpeed;

    public bool IsPlayingAudio => sources[active] != null && sources[active].isPlaying;
    public float CurrentVolume => sources[active] != null ? sources[active].volume : 0f;


    private void Start()
    {
        if (loopCue == null || !loopCue.HasClips)
            return;

        AudioClip clip = loopCue.PickClip();

        for (int i = 0; i < sources.Length; i++)
        {
            sources[i] = gameObject.AddComponent<AudioSource>();
            sources[i].playOnAwake = false;
            sources[i].spatialBlend = 0f;
            sources[i].loop = false;
            sources[i].clip = clip;
            sources[i].volume = 0f;
        }

        sources[active].Play();
        fadeSpeed = 1f / startFadeSeconds;
    }


    private void Update()
    {
        if (sources[0] == null || GameAudio.Instance == null)
            return;

        UpdatePause();

        if (sourcesPaused)
            return;

        UpdateLoop();

        float target = global
            ? 1f
            : GameAudio.Instance.Attenuation(transform.position, innerRadius, innerRadius + fadeDistance, null);

        // Fundido suave para que entrar/salir de la zona no se note como un corte.
        zoneGain = Mathf.MoveTowards(zoneGain, target, Time.unscaledDeltaTime * 0.5f);
        ApplyVolumes();
    }


    // Baja rápido al pausar, se detiene conservando la posición, y al reanudar sube despacio.
    private void UpdatePause()
    {
        bool paused = GameAudio.Instance.AmbiencePaused;

        if (paused) fadeSpeed = 1f / pauseFadeSeconds;
        else if (sourcesPaused) fadeSpeed = 1f / resumeFadeSeconds;

        pauseGain = Mathf.MoveTowards(pauseGain, paused ? 0f : 1f, Time.unscaledDeltaTime * fadeSpeed);

        if (paused && pauseGain <= 0f && !sourcesPaused)
        {
            foreach (AudioSource source in sources)
            {
                source.volume = 0f;
                source.Pause();
            }
            sourcesPaused = true;
        }
        else if (!paused && sourcesPaused)
        {
            foreach (AudioSource source in sources) source.UnPause();
            sourcesPaused = false;
        }
    }


    private void UpdateLoop()
    {
        AudioSource current = sources[active];
        AudioSource next = sources[1 - active];
        float length = current.clip.length;
        float crossfadeStart = Mathf.Max(0f, length - crossfadeSeconds);

        if (!crossfading && current.time >= crossfadeStart)
        {
            next.time = Mathf.Min(loopStartSeconds, length * 0.5f);
            next.Play();
            crossfading = true;
        }

        if (crossfading && !current.isPlaying)
        {
            active = 1 - active;
            crossfading = false;
        }
    }


    private void ApplyVolumes()
    {
        float baseVolume = loopCue.volume * GameAudio.Instance.CategoryVolume(loopCue.category) * zoneGain * pauseGain;
        AudioSource current = sources[active];

        float mix = 0f;

        if (crossfading && crossfadeSeconds > 0f)
            mix = Mathf.Clamp01((current.time - (current.clip.length - crossfadeSeconds)) / crossfadeSeconds);

        current.volume = baseVolume * (1f - mix);
        sources[1 - active].volume = crossfading ? baseVolume * mix : 0f;
    }


    private void OnDrawGizmosSelected()
    {
        if (global)
            return;

        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, innerRadius);
        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, innerRadius + fadeDistance);
    }
}
