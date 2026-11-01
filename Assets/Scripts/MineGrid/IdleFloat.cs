using UnityEngine;

namespace MineGrid
{
    public sealed class IdleFloat : MonoBehaviour
    {
        private GameUI _ui;
        private RectTransform _rect;
        private Vector2 _origin;
        private void Awake()
        {
            _rect = (RectTransform)transform;
            _origin = _rect.anchoredPosition;
            _ui = GetComponentInParent<GameUI>();
        }

        private void Update()
        {
            _rect.anchoredPosition = _origin + Vector2.up * ((_ui && _ui.ReducedEffects) ? 0 : Mathf.Sin(Time.unscaledTime * 1.5f) * 5);
        }

        private void OnDisable()
        {
            if (_rect)
            {
                _rect.anchoredPosition = _origin;
            }
        }
    }
}
