using UnityEngine;
using UnityEngine.AI;

// Audio del payaso SIN tocar su IA: lee el estado público de ClownAI, el NavMeshAgent y los pies
// del esqueleto animado. Va en el prefab Payaso. Cue vacío = silencio (nunca error).
//
// Evento                         Cue                     Cuándo
// aparece                        appearCue               al crearse (una vez por aparición)
// desaparece                     disappearCue            al desvanecerse de verdad (visto, tiempo, captura);
//                                                        no al cerrar la escena
// empieza a perseguir            chaseStartCue           al recibir objetivo (una vez por objetivo)
// persiguiendo (opcional, loop)  chaseLoopCue            mientras persigue, si el cue tiene clip
// captura                        captureCue              al alcanzar al objetivo (una vez)
// quieto                         idleCue                 cada 5-10 s mientras está quieto, sin bucle
// pisada caminando               footstepCue             cuando un pie toca el suelo caminando
// pisada corriendo               runFootstepCue          cuando un pie toca el suelo corriendo
//
// Todo pasa por GameAudio: volumen por categoría y atenuación por el jugador más cercano
// (split-screen con un solo AudioListener: no hay paneo izq./der.).
[RequireComponent(typeof(ClownAI))]
public class ClownAudio : MonoBehaviour
{
    [Header("Eventos")]
    [SerializeField] private SoundCue appearCue;
    [SerializeField] private SoundCue disappearCue;

    [Tooltip("Una vez al empezar a perseguir a un jugador.")]
    [SerializeField] private SoundCue chaseStartCue;

    [Tooltip("Loop OPCIONAL mientras persigue (ClownAI.HasTarget). Sin clip = no suena.")]
    [SerializeField] private SoundCue chaseLoopCue;

    [Tooltip("Al alcanzar al objetivo (ClownAI.HasCaptured pasa a true).")]
    [SerializeField] private SoundCue captureCue;

    [Header("Quieto")]
    [SerializeField] private SoundCue idleCue;

    [Tooltip("Segundos entre sonidos de respiración mientras está quieto (aleatorio en el rango).")]
    [SerializeField] private Vector2 idleInterval = new Vector2(5f, 10f);

    [Header("Pisadas")]
    [SerializeField] private SoundCue footstepCue;
    [SerializeField] private SoundCue runFootstepCue;

    [Tooltip("A partir de esta velocidad (m/s) las pisadas son de carrera.")]
    [SerializeField] private float runSpeedThreshold = 3f;

    [Tooltip("Velocidad mínima (m/s) para que haya pisadas.")]
    [SerializeField] private float minStepSpeed = 0.3f;

    [Tooltip("El pie pisa al bajar a menos de esto (m) sobre su altura de reposo...")]
    [SerializeField] private float plantMargin = 0.04f;

    [Tooltip("...y vuelve a poder pisar al subir más de esto (m) sobre su altura de reposo.")]
    [SerializeField] private float liftMargin = 0.1f;

    [Header("Depuración")]
    [Tooltip("Escribe en consola cada evento de sonido (aunque el cue no tenga clip).")]
    [SerializeField] private bool logEvents = false;

    private ClownAI clown;
    private NavMeshAgent agent;
    private Animator animator;
    private Transform leftFoot;
    private Transform rightFoot;
    private float restHeight = float.MaxValue;
    private bool leftLifted;
    private bool rightLifted;

    private AudioSource chaseSource;
    private bool wasChasing;
    private bool wasCaptured;
    private float nextIdleSound;

    // Contadores para diagnóstico (cuentan aunque el cue no tenga clip).
    public int AppearCount { get; private set; }
    public int DisappearCount { get; private set; }
    public int ChaseStartCount { get; private set; }
    public int CaptureCount { get; private set; }
    public int IdleCount { get; private set; }
    public int FootstepCount { get; private set; }
    public int RunFootstepCount { get; private set; }


    private void Awake()
    {
        clown = GetComponent<ClownAI>();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        if (animator != null && animator.isHuman)
        {
            leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        }
    }


    private void OnEnable()
    {
        wasCaptured = false;
        wasChasing = false;
        restHeight = float.MaxValue;
        nextIdleSound = Time.time + Random.Range(idleInterval.x, idleInterval.y);
        Fire(appearCue, "aparece");
        AppearCount++;
    }


    private void OnDisable()
    {
        // Solo una desaparición real (no al salir de Play Mode o cambiar de escena).
        if (clown != null && clown.IsVanishing)
        {
            Fire(disappearCue, "desaparece");
            DisappearCount++;
        }

        if (chaseSource != null)
            chaseSource.Stop();
    }


    private void Update()
    {
        if (clown.IsVanishing)
            return;

        if (!wasCaptured && clown.HasCaptured)
        {
            wasCaptured = true;
            Fire(captureCue, "captura");
            CaptureCount++;
        }

        bool chasing = clown.HasTarget && !clown.HasCaptured;

        if (chasing && !wasChasing)
        {
            Fire(chaseStartCue, "empieza a perseguir");
            ChaseStartCount++;
        }

        wasChasing = chasing;
        UpdateChaseLoop(chasing);

        float speed = agent != null && agent.enabled ? agent.velocity.magnitude : 0f;

        UpdateIdle(speed);
        UpdateFootsteps(speed);
    }


    private void UpdateIdle(float speed)
    {
        if (speed > 0.1f || clown.HasCaptured)
        {
            nextIdleSound = Mathf.Max(nextIdleSound, Time.time + idleInterval.x * 0.5f);
            return;
        }

        if (Time.time < nextIdleSound)
            return;

        nextIdleSound = Time.time + Random.Range(idleInterval.x, idleInterval.y);
        Fire(idleCue, "respira (quieto)");
        IdleCount++;
    }


    // Un pie "pisa" cuando baja hasta su altura de reposo después de haberse levantado
    // (histéresis: un temblor del pie no cuenta como dos pisadas).
    private void UpdateFootsteps(float speed)
    {
        if (leftFoot == null && animator != null && animator.isHuman)
        {
            leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        }

        if (leftFoot == null || rightFoot == null)
            return;

        float ground = transform.position.y;
        float left = leftFoot.position.y - ground;
        float right = rightFoot.position.y - ground;

        // Altura del tobillo con el pie apoyado (la más baja vista). Se ignoran lecturas bajo el
        // suelo (el frame en que el agente se recoloca con Warp).
        float lowest = Mathf.Min(left, right);

        if (lowest > -0.02f)
            restHeight = Mathf.Min(restHeight, lowest);

        if (restHeight == float.MaxValue)
            return;

        bool moving = speed >= minStepSpeed;
        UpdateFoot(left, ref leftLifted, moving, speed);
        UpdateFoot(right, ref rightLifted, moving, speed);
    }


    private void UpdateFoot(float height, ref bool lifted, bool moving, float speed)
    {
        if (height > restHeight + liftMargin)
        {
            lifted = true;
            return;
        }

        if (lifted && height < restHeight + plantMargin)
        {
            lifted = false;

            if (moving)
                Step(speed);
        }
    }


    private void Step(float speed)
    {
        if (speed >= runSpeedThreshold)
        {
            Fire(runFootstepCue, null);
            RunFootstepCount++;
        }
        else
        {
            Fire(footstepCue, null);
            FootstepCount++;
        }
    }


    private void UpdateChaseLoop(bool chasing)
    {
        if (chaseLoopCue == null || !chaseLoopCue.HasClips || GameAudio.Instance == null)
            return;

        if (chaseSource == null)
        {
            chaseSource = gameObject.AddComponent<AudioSource>();
            chaseSource.playOnAwake = false;
            chaseSource.spatialBlend = 0f;
            chaseSource.loop = true;
            chaseSource.clip = chaseLoopCue.PickClip();
        }

        if (!chasing)
        {
            if (chaseSource.isPlaying) chaseSource.Stop();
            return;
        }

        if (!chaseSource.isPlaying)
            chaseSource.Play();

        chaseSource.volume = chaseLoopCue.volume
                             * GameAudio.Instance.CategoryVolume(chaseLoopCue.category)
                             * GameAudio.Instance.Attenuation(transform.position, chaseLoopCue.minDistance, chaseLoopCue.maxDistance, null);
    }


    private void Fire(SoundCue cue, string label)
    {
        if (logEvents && label != null)
        {
            Debug.Log($"🔊🤡 {label} ({(cue != null ? cue.name : "sin cue")}{(cue != null && !cue.HasClips ? ", sin clip todavía" : "")})");
        }

        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayCueAt(cue, transform.position);
    }
}
