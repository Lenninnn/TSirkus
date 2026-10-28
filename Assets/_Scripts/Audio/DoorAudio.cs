using UnityEngine;

// Sonidos reutilizables de una puerta. Se llaman desde los eventos de cada puerta
// (p. ej. DoorCinematicController.onDoorFlowFinished → PlayOpen). Cues vacíos = silencio.
public class DoorAudio : MonoBehaviour
{
    [SerializeField] private SoundCue openCue;
    [SerializeField] private SoundCue closeCue;
    [SerializeField] private SoundCue lockedCue;
    [SerializeField] private SoundCue mechanismCue;

    public void PlayOpen() => Play(openCue);
    public void PlayClose() => Play(closeCue);
    public void PlayLocked() => Play(lockedCue);
    public void PlayMechanism() => Play(mechanismCue);

    private void Play(SoundCue cue)
    {
        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayCueAt(cue, transform.position);
    }
}
