using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CyberWhack.UI
{
    /// <summary>
    /// Press squash and a small release overshoot. Optional idle breathe
    /// for the primary call-to-action.
    /// </summary>
    public class ButtonPop : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private float _scale = 1f;
        private float _target = 1f;
        private bool _breathe;

        public void SetBreathe(bool breathe)
        {
            _breathe = breathe;
        }

        public static void Attach(Button button, bool breathe)
        {
            if (button == null) return;
            ButtonPop pop = button.GetComponent<ButtonPop>();
            if (pop == null) pop = button.gameObject.AddComponent<ButtonPop>();
            pop.SetBreathe(breathe);

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.98f, 0.92f, 1f);
            colors.pressedColor = new Color(0.96f, 0.86f, 0.55f, 1f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = 0.04f;
            button.colors = colors;

            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.Automatic;
            button.navigation = navigation;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _target = 0.92f;
            _scale = 0.94f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _target = 1f;
            _scale = 1.07f;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _scale = Mathf.Lerp(_scale, _target, 1f - Mathf.Exp(-16f * dt));
            if (Mathf.Abs(_scale - _target) < 0.004f) _scale = _target;

            float breathe = _breathe ? 1f + Mathf.Sin(Time.unscaledTime * 3.1f) * 0.035f : 1f;
            transform.localScale = new Vector3(breathe * _scale, breathe * _scale, 1f);
        }
    }
}
