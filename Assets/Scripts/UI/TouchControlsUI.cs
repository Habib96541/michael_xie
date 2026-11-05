using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DropStack.Game;

namespace DropStack.UI
{
    public class TouchControlsUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ActivePieceController controller;

        [Header("Buttons")]
        [SerializeField] private Button btnLeft;
        [SerializeField] private Button btnRight;
        [SerializeField] private Button btnSoftDrop;
        [SerializeField] private Button btnHardDrop;
        [SerializeField] private Button btnRotateCW;
        [SerializeField] private Button btnRotateCCW;
        [SerializeField] private Button btnHold;

        private void Start() => SetupButtonListeners();
        private void OnDisable() { controller?.ResetInput(); }

        private void SetupButtonListeners()
        {
            if (btnLeft != null)
            {
                AddPointerDownUp(btnLeft, () => controller?.StartMove(-1), () => controller?.StopMove());
            }

            if (btnRight != null)
            {
                AddPointerDownUp(btnRight, () => controller?.StartMove(1), () => controller?.StopMove());
            }

            if (btnSoftDrop != null)
            {
                AddPointerDownUp(btnSoftDrop, () => controller?.SetSoftDrop(true), () => controller?.SetSoftDrop(false));
            }

            if (btnHardDrop != null)
            {
                BindImmediatePress(btnHardDrop, () => controller?.ExecuteHardDrop());
            }

            if (btnRotateCW != null)
            {
                BindImmediatePress(btnRotateCW, () => controller?.Rotate(1));
            }

            if (btnRotateCCW != null)
            {
                BindImmediatePress(btnRotateCCW, () => controller?.Rotate(-1));
            }

            if (btnHold != null)
            {
                BindImmediatePress(btnHold, () => controller?.RequestHold());
            }

        }

        private static void BindImmediatePress(Button button, UnityEngine.Events.UnityAction action)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
            AddTrigger(button, EventTriggerType.PointerDown, data =>
            {
                if (data is not PointerEventData pointer || pointer.button != PointerEventData.InputButton.Left || !button.IsInteractable()) return;

                // A touch or mouse press acts immediately. Suppress the second action on release.
                pointer.eligibleForClick = false;
                button.onClick.Invoke();
            });
        }

        private void AddPointerDownUp(Button btn, System.Action onDown, System.Action onUp)
        {
            AddTrigger(btn, EventTriggerType.PointerDown, data =>
            {
                if (data is PointerEventData pointer && pointer.button == PointerEventData.InputButton.Left && btn.IsInteractable()) onDown();
            });
            AddTrigger(btn, EventTriggerType.PointerUp, _ => onUp());
            AddTrigger(btn, EventTriggerType.PointerExit, _ => onUp());
        }

        private static void AddTrigger(Button button, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> callback)
        {
            EventTrigger trigger = button.GetComponent<EventTrigger>();
            if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(callback);
            trigger.triggers.Add(entry);
        }
    }
}
