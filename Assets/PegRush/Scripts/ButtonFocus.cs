using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PegRush
{
    public sealed class ButtonFocus : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        private Image _ring;
        private Button _button;
        private bool _selected;
        private bool _hovered;

        public void Wire(Image ring, Button button)
        {
            _ring = ring;
            _button = button;
            Refresh();
        }
        public void OnPointerEnter(PointerEventData data)
        {
            _hovered = true;
            Refresh();
        }
        public void OnPointerExit(PointerEventData data)
        {
            _hovered = false;
            Refresh();
        }
        public void OnSelect(BaseEventData data)
        {
            _selected = true;
            Refresh();
        }
        public void OnDeselect(BaseEventData data)
        {
            _selected = false;
            Refresh();
        }
        private void Refresh()
        {
            if (_ring != null) _ring.enabled = (_hovered || _selected) && _button.IsInteractable();
        }
        private void OnDisable()
        {
            _selected = false;
            _hovered = false;
            Refresh();
        }
        private void LateUpdate()
        {
            Refresh();
        }
    }
}
