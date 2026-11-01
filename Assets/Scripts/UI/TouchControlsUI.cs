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
                btnHardDrop.onClick.RemoveAllListeners();
                btnHardDrop.onClick.AddListener(() => controller?.ExecuteHardDrop());
            }

            if (btnRotateCW != null)
            {
                btnRotateCW.onClick.RemoveAllListeners();
                btnRotateCW.onClick.AddListener(() => controller?.Rotate(1));
            }

            if (btnRotateCCW != null)
            {
                btnRotateCCW.onClick.RemoveAllListeners();
                btnRotateCCW.onClick.AddListener(() => controller?.Rotate(-1));
            }

            if (btnHold != null)
            {
                btnHold.onClick.RemoveAllListeners();
                btnHold.onClick.AddListener(() => controller?.RequestHold());
            }

        }

        private void AddPointerDownUp(Button btn, System.Action onDown, System.Action onUp)
        {
            EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();
            trigger.triggers.Clear();

            // PointerDown
            var entryDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            entryDown.callback.AddListener((data) => onDown?.Invoke());
            trigger.triggers.Add(entryDown);

            // PointerUp
            var entryUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            entryUp.callback.AddListener((data) => onUp?.Invoke());
            trigger.triggers.Add(entryUp);
            var entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            entryExit.callback.AddListener((data) => onUp?.Invoke());
            trigger.triggers.Add(entryExit);
        }
    }
}
