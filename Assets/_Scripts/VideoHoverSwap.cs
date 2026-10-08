using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoHoverSwap : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Pantalla principal")]
    [SerializeField] private RawImage backgroundImage;

    [Header("Render Textures")]
    [SerializeField] private RenderTexture normalTexture;
    [SerializeField] private RenderTexture hoverTexture;

    [Header("Videos")]
    [SerializeField] private VideoPlayer normalVideo;
    [SerializeField] private VideoPlayer hoverVideo;

    private void Start()
    {
        backgroundImage.texture = normalTexture;

        if (hoverVideo != null)
        {
            hoverVideo.isLooping = true;
            hoverVideo.Play();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (backgroundImage == null ||
            hoverTexture == null ||
            hoverVideo == null)
            return;

        // Sincronizar con el momento exacto del video normal
        if (normalVideo != null && normalVideo.isPlaying)
        {
            hoverVideo.time = normalVideo.time;
        }

        backgroundImage.texture = hoverTexture;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (backgroundImage == null || normalTexture == null)
            return;

        backgroundImage.texture = normalTexture;
    }
}