using UnityEngine;
using UnityEngine.EventSystems;

namespace FlapDash
{
    public sealed class ButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        [SerializeField] private VisualEffectsSettings settings;
        private bool highlighted;
        private bool pressed;
        private float submitPulse;
        private void Update()
        {
            submitPulse = Mathf.MoveTowards(submitPulse, 0f, Time.unscaledDeltaTime * 8f);
            float size = pressed ? 0.96f : highlighted ? 1.035f : 1f;
            size = Mathf.Lerp(1f, size - submitPulse * 0.04f, settings.Intensity);
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * size, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
        }
        private void OnDisable()
        {
            pressed = highlighted = false;
            submitPulse = 0f;
            transform.localScale = Vector3.one;
        }
        public void OnPointerEnter(PointerEventData eventData) => highlighted = true;
        public void OnPointerExit(PointerEventData eventData) { highlighted = false; pressed = false; }
        public void OnPointerDown(PointerEventData eventData) => pressed = true;
        public void OnPointerUp(PointerEventData eventData) => pressed = false;
        public void OnSelect(BaseEventData eventData) => highlighted = true;
        public void OnDeselect(BaseEventData eventData) => highlighted = false;
        public void OnSubmit(BaseEventData eventData) => submitPulse = 1f;
    }
}
