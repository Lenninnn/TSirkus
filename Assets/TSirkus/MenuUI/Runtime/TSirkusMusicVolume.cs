using UnityEngine;
namespace TSirkus.MenuUI
{
    // Optional: add to music AudioSources in gameplay to use the saved menu volume.
    [RequireComponent(typeof(AudioSource))]
    public sealed class TSirkusMusicVolume : MonoBehaviour
    {
        [Range(0, 1)] public float trackGain = 1;
        private void OnEnable() { GetComponent<AudioSource>().volume = trackGain * Mathf.Clamp01(PlayerPrefs.GetFloat("TSirkus.MusicVolume.v1", .55f)); }
    }
}
