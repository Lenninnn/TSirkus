using UnityEngine;
namespace TSirkus.MenuUI
{
    [ExecuteAlways]
    public sealed class TSirkusSafeArea : MonoBehaviour
    {
        private Rect previous;
        private int width, height;
        private void OnEnable() { Apply(); }
        private void Update() { if (previous != Screen.safeArea || width != Screen.width || height != Screen.height) Apply(); }
        private void Apply()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            previous = Screen.safeArea; width = Screen.width; height = Screen.height;
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(previous.xMin / width, previous.yMin / height);
            rect.anchorMax = new Vector2(previous.xMax / width, previous.yMax / height);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
