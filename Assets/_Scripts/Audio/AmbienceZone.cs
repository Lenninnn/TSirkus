using UnityEngine;

// Loop de ambiente. Global: suena igual para todos. Zona: su volumen depende de la
// distancia del jugador más cercano a la zona (fuera de 'fadeDistance' se calla).
// Solo crea su AudioSource si el cue tiene clip: un cue vacío no cuesta nada.
public class AmbienceZone : MonoBehaviour
{
    [SerializeField] private SoundCue loopCue;

    [Tooltip("Global: sin distancia. Si no, zona centrada en este objeto.")]
    [SerializeField] private bool global;

    [Tooltip("Radio en el que el ambiente suena al 100 %.")]
    [Min(0f)] [SerializeField] private float innerRadius = 8f;

    [Tooltip("Distancia extra a lo largo de la cual se desvanece.")]
    [Min(0.1f)] [SerializeField] private float fadeDistance = 6f;

    private AudioSource source;
    private float currentGain;


    private void Start()
    {
        if (loopCue == null || !loopCue.HasClips)
            return;

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.loop = true;
        source.clip = loopCue.PickClip();
        source.volume = 0f;
        source.time = Random.Range(0f, source.clip.length);
        source.Play();
    }


    private void Update()
    {
        if (source == null || GameAudio.Instance == null)
            return;

        float target = global
            ? 1f
            : GameAudio.Instance.Attenuation(transform.position, innerRadius, innerRadius + fadeDistance, null);

        // Fundido suave para que entrar/salir de la zona no se note como un corte.
        currentGain = Mathf.MoveTowards(currentGain, target, Time.unscaledDeltaTime * 0.5f);
        source.volume = loopCue.volume * currentGain * GameAudio.Instance.CategoryVolume(loopCue.category);
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
