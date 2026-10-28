using UnityEngine;

// Pasos de UN jugador, sincronizados con su estado Walk del Animator (que a su vez depende
// del desplazamiento real del Player). No lee input: funciona igual con celular o teclado.
// Va en la raíz del visual con rig, junto a Animator y PlayerAnimationController.
public class PlayerFootsteps : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Animator animator;

    [Header("Sonido")]
    [Tooltip("Cue de pasos. Más adelante puede elegirse por superficie.")]
    [SerializeField] private SoundCue footstepCue;

    [Tooltip("Fases del ciclo de Walk (0-1) en las que un pie toca el suelo. Se calculan del clip.")]
    [SerializeField] private float[] stepPhases = { 0f, 0.5f };

    [Header("Split-screen")]
    [Tooltip("Nivel de los pasos propios. Sube hasta 1 si otro jugador está cerca.")]
    [Range(0f, 1f)] [SerializeField] private float ownLevel = 0.6f;

    private static readonly int WalkState = Animator.StringToHash("Walk");

    private AudioSource source;
    private Transform playerRoot;
    private float lastPhase = -1f;

    // Solo lectura, para diagnóstico.
    public int StepCount { get; private set; }
    public float LastStepVolume { get; private set; }
    public bool IsSounding => source != null && source.isPlaying;


    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        CharacterController body = GetComponentInParent<CharacterController>();
        playerRoot = body != null ? body.transform : transform.parent;

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
    }


    private void OnDisable()
    {
        StopSteps();
    }


    private void Update()
    {
        if (animator == null || footstepCue == null || !footstepCue.HasClips)
            return;

        if (!IsWalking(out float phase))
        {
            StopSteps();
            return;
        }

        // Al entrar en Walk no suena de inmediato: espera el siguiente apoyo del pie.
        if (lastPhase >= 0f)
        {
            foreach (float step in stepPhases)
            {
                if (Crossed(lastPhase, phase, step))
                {
                    PlayStep();
                    break;
                }
            }
        }

        lastPhase = phase;
    }


    // Walk activo y sin transición hacia otro estado (al parar, la transición a Idle corta ya).
    private bool IsWalking(out float phase)
    {
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        phase = state.normalizedTime % 1f;

        if (state.shortNameHash != WalkState)
            return false;

        if (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).shortNameHash != WalkState)
            return false;

        return true;
    }


    private static bool Crossed(float from, float to, float mark)
    {
        return from <= to ? from < mark && mark <= to : mark > from || mark <= to;
    }


    private void PlayStep()
    {
        AudioClip clip = footstepCue.PickClip();
        float level = ownLevel;

        if (GameAudio.Instance != null)
        {
            float others = GameAudio.Instance.Attenuation(transform.position,
                footstepCue.minDistance, footstepCue.maxDistance, playerRoot);
            level = Mathf.Max(ownLevel, others) * GameAudio.Instance.CategoryVolume(footstepCue.category);
        }

        source.Stop();
        source.clip = clip;
        source.volume = footstepCue.PickVolume() * level;
        source.pitch = footstepCue.PickPitch();
        source.time = Mathf.Min(footstepCue.startTime, clip.length - 0.01f);
        source.Play();

        StepCount++;
        LastStepVolume = source.volume;
    }


    private void StopSteps()
    {
        lastPhase = -1f;

        if (source != null && source.isPlaying)
            source.Stop();
    }
}
