using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace BullseyeBlitz
{
    [DefaultExecutionOrder(200)]
    public sealed class BlitzPointer : MonoBehaviour
    {
        private BlitzGame _game;
        private int _touchId = -1;
        private bool _touchHeld;
        private int _mouseAfter;
        private readonly List<RaycastResult> _uiHits = new List<RaycastResult>();
        public void Configure(BlitzGame game) { _game = game; }
        private void LateUpdate()
        {
            Touchscreen screen = Touchscreen.current;
            if (screen != null)
            {
                if (_touchHeld)
                {
                    bool active = false;
                    foreach (TouchControl touch in screen.touches)
                    {
                        if (touch.touchId.ReadValue() == _touchId && touch.press.isPressed) { active = true; }
                    }
                    if (!active) { _touchHeld = false; _touchId = -1; _mouseAfter = Time.frameCount + 2; }
                    return;
                }
                foreach (TouchControl touch in screen.touches)
                {
                    if (!touch.press.wasPressedThisFrame) { continue; }
                    _touchHeld = true; _touchId = touch.touchId.ReadValue(); _mouseAfter = Time.frameCount + 2;
                    Fire(touch.position.ReadValue()); return;
                }
            }
            if (Time.frameCount > _mouseAfter && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Fire(Mouse.current.position.ReadValue());
            }
        }
        private void Fire(Vector2 position)
        {
            if (!_game.CanShoot) { return; }
            if (EventSystem.current != null)
            {
                PointerEventData data = new PointerEventData(EventSystem.current) { position = position };
                _uiHits.Clear(); EventSystem.current.RaycastAll(data, _uiHits);
                if (_uiHits.Count > 0) { return; }
            }
            Camera camera = _game.GameCamera;
            Vector3 point = camera.ScreenToWorldPoint(new Vector3(position.x, position.y, -camera.transform.position.z));
            _game.Shoot(point);
        }
    }
}
