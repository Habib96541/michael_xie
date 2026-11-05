using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DiceGame.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class UIButtonFeel : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerExitHandler,
        ISelectHandler,
        IDeselectHandler,
        ISubmitHandler
    {
        [SerializeField, Range(0.85f, 1.0f)] private float pressedScale = 0.94f;
        [SerializeField, Range(1.0f, 1.1f)] private float selectedScale = 1.025f;
        [SerializeField, Min(1.0f)] private float settleSpeed = 20.0f;

        private Button _button;
        private Vector3 _restScale;
        private float _targetScale = 1.0f;
        private float _releaseTimer;
        private bool _pointerDown;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _restScale = transform.localScale;
        }

        private void OnDisable()
        {
            _pointerDown = false;
            _releaseTimer = 0.0f;
            _targetScale = 1.0f;
            if (transform != null) transform.localScale = _restScale;
        }

        private void Update()
        {
            if (_releaseTimer > 0.0f)
            {
                _releaseTimer -= Time.unscaledDeltaTime;
                if (_releaseTimer <= 0.0f) UpdateRestTarget();
            }

            float scale = Mathf.Lerp(transform.localScale.x, _targetScale, 1.0f - Mathf.Exp(-settleSpeed * Time.unscaledDeltaTime));
            transform.localScale = _restScale * scale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_button == null || !_button.IsInteractable()) return;
            _pointerDown = true;
            _targetScale = pressedScale;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_pointerDown) return;
            _pointerDown = false;
            UpdateRestTarget();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_pointerDown) UpdateRestTarget();
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (!_pointerDown) _targetScale = selectedScale;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (!_pointerDown) _targetScale = 1.0f;
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (_button == null || !_button.IsInteractable()) return;
            _targetScale = pressedScale;
            _releaseTimer = 0.09f;
        }

        private void UpdateRestTarget()
        {
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject && _button != null && _button.IsInteractable())
                _targetScale = selectedScale;
            else
                _targetScale = 1.0f;
        }
    }
}
