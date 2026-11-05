using UnityEngine;
using UnityEngine.EventSystems;

namespace CyberScratch.UI
{
    /// <summary>
    /// Quick press scale so the gold and green action buttons feel tactile.
    /// </summary>
    public class UIButtonPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private Vector3 _baseScale = Vector3.one;

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            transform.localScale = _baseScale * 0.96f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            transform.localScale = _baseScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = _baseScale;
        }
    }
}
