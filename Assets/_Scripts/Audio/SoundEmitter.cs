using UnityEngine;

// Reproduce un SoundCue en la posición de este objeto. Pensado para conectarse desde
// UnityEvents existentes (interruptores, puzzles, puertas...) sin tocar su lógica:
// evento → SoundEmitter.PlayCue(cue).
public class SoundEmitter : MonoBehaviour
{
    public void PlayCue(SoundCue cue)
    {
        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayCueAt(cue, transform.position);
    }
}
