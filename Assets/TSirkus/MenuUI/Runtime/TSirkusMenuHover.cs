using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace TSirkus.MenuUI
{
    public sealed class TSirkusMenuHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        public TSirkusMenuController owner;
        public Image accent;
        public Text arrow;
        private bool hovered, selected;
        private float amount;
        public void OnPointerEnter(PointerEventData e) { hovered = true; if (owner) owner.HoverSound(); }
        public void OnPointerExit(PointerEventData e) { hovered = false; }
        public void OnSelect(BaseEventData e) { selected = true; if (owner) owner.HoverSound(); }
        public void OnDeselect(BaseEventData e) { selected = false; }
        private void Update()
        {
            amount = Mathf.MoveTowards(amount, hovered || selected ? 1 : 0, Time.unscaledDeltaTime * 7);
            if (accent) { var c = accent.color; c.a = amount; accent.color = c; }
            if (arrow) { var c = arrow.color; c.a = .25f + .75f * amount; arrow.color = c; }
        }
        private void OnDisable() { hovered = selected = false; amount = 0; }
    }
}
