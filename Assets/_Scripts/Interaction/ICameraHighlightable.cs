using UnityEngine;

public interface ICameraHighlightable
{
    void SetHighlightForCamera(
        Camera camera,
        bool highlighted
    );
}