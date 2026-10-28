using UnityEngine;

// Audio del payaso SIN tocar su IA: solo lee los getters públicos de ClownAI
// (HasTarget, HasCaptured) y su ciclo de vida (aparece al activarse, desaparece al
// desactivarse). Va en el prefab Payaso. Cues vacíos = silencio.
[RequireComponent(typeof(ClownAI))]
public class ClownAudio : MonoBehaviour
{
    [SerializeField] private SoundCue appearCue;
    [SerializeField] private SoundCue disappearCue;

    [Tooltip("Loop mientras persigue a un jugador (ClownAI.HasTarget).")]
    [SerializeField] private SoundCue chaseLoopCue;

    [Tooltip("Al alcanzar al objetivo (ClownAI.HasCaptured pasa a true).")]
    [SerializeField] private SoundCue captureCue;

    private ClownAI clown;
    private AudioSource chaseSource;
    private bool wasCaptured;


    private void Awake()
    {
        clown = GetComponent<ClownAI>();
    }


    private void OnEnable()
    {
        wasCaptured = false;
        Play(appearCue);
    }


    private void OnDisable()
    {
        Play(disappearCue);

        if (chaseSource != null)
            chaseSource.Stop();
    }


    private void Update()
    {
        if (!wasCaptured && clown.HasCaptured)
        {
            wasCaptured = true;
            Play(captureCue);
        }

        UpdateChaseLoop(clown.HasTarget && !clown.HasCaptured);
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


    private void Play(SoundCue cue)
    {
        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayCueAt(cue, transform.position);
    }
}
