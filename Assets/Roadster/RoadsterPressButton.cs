using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TeslaRoadster
{
    internal sealed class RoadsterPressButton : Button
    {
        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            if (eventData.button == PointerEventData.InputButton.Left && IsActive() && IsInteractable())
                onClick.Invoke();
        }

        public override void OnPointerClick(PointerEventData eventData) { }
    }
}
