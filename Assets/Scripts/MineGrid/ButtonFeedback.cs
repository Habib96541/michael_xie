using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MineGrid
{
    public sealed class ButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler
    {
        private GameUI _ui;
        private bool _held;
        private void Awake() => _ui = GetComponentInParent<GameUI>();
        public void OnPointerDown(PointerEventData e)
        {
            if (!GetComponent<UnityEngine.UI.Selectable>().IsInteractable())
            {
                return;
            }

            _held = true;
            if (!_ui || !_ui.ReducedEffects)
            {
                transform.localScale = Vector3.one * .96f;
            }

            if (_ui)
            {
                _ui.Audio.Play(3);
            }
        }

        public void OnPointerUp(PointerEventData e)
        {
            _held = false;
            transform.localScale = Vector3.one;
        }

        public void OnPointerExit(PointerEventData e)
        {
            _held = false;
            transform.localScale = Vector3.one;
        }

        public void OnPointerEnter(PointerEventData e)
        {
            if (!_held && (!_ui || !_ui.ReducedEffects) && GetComponent<UnityEngine.UI.Selectable>().IsInteractable())
            {
                transform.localScale = Vector3.one * 1.015f;
            }
        }

        private void OnDisable()
        {
            _held = false;
            transform.localScale = Vector3.one;
        }
    }
}
