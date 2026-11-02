using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ShatterLeap
{
    public sealed class ChoiceInput : MonoBehaviour
    {
        [SerializeField] private RunController _run;
        [SerializeField] private Camera _camera;
        private InputAction _left;
        private InputAction _right;
        private InputAction _pause;
        private readonly List<RaycastResult> _hits = new List<RaycastResult>();
        private void OnEnable()
        {
            _left = new InputAction("Left", InputActionType.Button, "<Keyboard>/a");
            _right = new InputAction("Right", InputActionType.Button, "<Keyboard>/d");
            _pause = new InputAction("Pause", InputActionType.Button, "<Keyboard>/escape");
            _left.Enable();
            _right.Enable();
            _pause.Enable();
        }
        private void OnDisable()
        {
            _left.Dispose();
            _right.Dispose();
            _pause.Dispose();
        }
        private void Update()
        {
            if (_pause.WasPressedThisFrame()) { _run.Pause(); }
            if (!_run.CanChoose) { return; }
            if (_left.WasPressedThisFrame()) { _run.Choose(0); return; }
            if (_right.WasPressedThisFrame()) { _run.Choose(1); return; }
            Vector2 point;
            Touchscreen touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
            {
                point = touch.primaryTouch.position.ReadValue();
            }
            else if ((touch == null || !touch.primaryTouch.press.isPressed) && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                point = Mouse.current.position.ReadValue();
            }
            else { return; }
            PointerEventData pointer = new PointerEventData(EventSystem.current) { position = point };
            _hits.Clear();
            EventSystem.current.RaycastAll(pointer, _hits);
            if (_hits.Count != 0) { return; }
            if (Physics.Raycast(_camera.ScreenPointToRay(point), out RaycastHit hit, 150, 1 << 8))
            {
                GlassPanel panel = hit.collider.GetComponentInParent<GlassPanel>();
                if (panel != null && panel.Row == _run.Run.Steps) { _run.Choose(panel.Side); }
            }
        }
    }
}
