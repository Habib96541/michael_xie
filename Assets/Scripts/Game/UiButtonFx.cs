using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NeonDash
{
    /// <summary>Hover and press feedback: buttons swell slightly and brighten when touched.</summary>
    public sealed class UiButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
                                     IPointerDownHandler, IPointerUpHandler
    {
        public float HoverScale = 1.05f;

        Image image;
        Color baseColor;
        bool hovering;

        void Awake()
        {
            image = GetComponent<Image>();
            if (image != null) baseColor = image.color;
        }

        public void OnPointerEnter(PointerEventData eventData) { hovering = true; Animate(hovering ? HoverScale : 1f); }
        public void OnPointerExit(PointerEventData eventData) { hovering = false; Animate(1f); }
        public void OnPointerDown(PointerEventData eventData) { Animate(0.96f); }
        public void OnPointerUp(PointerEventData eventData) { Animate(hovering ? HoverScale : 1f); }

        void OnDisable()
        {
            transform.localScale = Vector3.one;
            hovering = false;
            if (image != null) image.color = baseColor;
        }

        void Animate(float scale)
        {
            Vector3 start = transform.localScale;
            Vector3 end = new Vector3(scale, scale, 1f);
            TweenRunner.Run(0.14f, k =>
            {
                if (this == null) return;
                transform.localScale = Vector3.LerpUnclamped(start, end, k);
                if (image == null) return;
                Color target = scale > 1f ? Color.Lerp(baseColor, Color.white, 0.12f) : baseColor;
                image.color = Color.LerpUnclamped(image.color, target, k * 0.5f);
            }, 0f, Ease.OutCubic);
        }
    }
}
