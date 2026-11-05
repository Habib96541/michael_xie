using UnityEngine;

namespace CyberWhack.UI
{
    /// <summary>
    /// Idle bob and squash for the moles on the title screen.
    /// </summary>
    public class PreviewMascotMotion : MonoBehaviour
    {
        private RectTransform _rect;
        private Vector2 _origin;
        private float _phase;

        private void Awake()
        {
            _rect = transform as RectTransform;
            if (_rect != null) _origin = _rect.anchoredPosition;
            _phase = (GetInstanceID() & 7) * 0.55f;
        }

        private void Update()
        {
            if (_rect == null) return;
            float wave = Mathf.Sin(Time.unscaledTime * 1.8f + _phase);
            _rect.anchoredPosition = _origin + new Vector2(0f, wave * 8f);
            float squash = Mathf.Sin(Time.unscaledTime * 2.4f + _phase);
            float x = 1f + squash * 0.035f;
            float y = 1f - squash * 0.03f;
            _rect.localScale = new Vector3(x, y, 1f);
        }
    }
}
