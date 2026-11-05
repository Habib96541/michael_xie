using BullRide.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BullRide
{
    public sealed class UiHud : MonoBehaviour
    {
        Text time, factor, move, payout;
        Image clockFill;
        RectTransform clockHand;
        RectTransform controls, stick, knob;
        Image rideGuide, guideArc, guideTip, puck, centerZone;
        static readonly Color CenterGreen = new Color(.29f, .94f, .51f);
        int? pointer;
        public Vector2 Stick { get; private set; }

        public void Build()
        {
            UiKit.Stretch((RectTransform)transform);
            rideGuide = GuideImage(transform, "Ride guide", UiKit.GuideRing, new Color(.9f,.93f,1,.23f));
            guideArc = GuideImage(rideGuide.transform, "Balance arc", UiKit.GuideRing, UiKit.Signal);
            UiKit.Stretch(guideArc.rectTransform);
            guideArc.type = Image.Type.Filled;
            guideArc.fillMethod = Image.FillMethod.Radial360;
            guideArc.fillOrigin = (int)Image.Origin360.Top;
            guideTip = GuideImage(guideArc.transform, "Recovery indicator", UiKit.RecoveryArrow, Palette.Text);
            UiKit.Place(guideTip.rectTransform, new Vector2(.5f, .92f), Vector2.zero, new Vector2(20,20));
            centerZone = GuideImage(rideGuide.transform, "High buck target", UiKit.Circle, CenterGreen);
            puck = GuideImage(rideGuide.transform, "Lean puck", UiKit.Circle, Palette.Accent);
            rideGuide.gameObject.SetActive(false);
            Image clock = GuideImage(transform, "Ride clock", UiKit.Circle, Palette.Panel);
            UiKit.Place(clock.rectTransform, new Vector2(0,1), new Vector2(67,-65), new Vector2(106,106));
            Image track = GuideImage(clock.transform, "Clock track", UiKit.GuideRing, new Color(.36f,.29f,.2f));
            UiKit.Stretch(track.rectTransform);
            clockFill = GuideImage(clock.transform, "Time elapsed", UiKit.GuideRing, Palette.Accent);
            UiKit.Stretch(clockFill.rectTransform);
            clockFill.type = Image.Type.Filled;
            clockFill.fillMethod = Image.FillMethod.Radial360;
            clockFill.fillOrigin = (int)Image.Origin360.Top;
            clockHand = GuideImage(clock.transform, "Clock hand", UiKit.RecoveryArrow, Palette.Accent).rectTransform;
            UiKit.Place(clockHand, UiKit.Center, Vector2.zero, new Vector2(11,29), new Vector2(.5f,0));
            Dot(clock.transform, "Clock center", Palette.Accent, 6);
            time = Caption(clock.transform, "Time", "", 16, Palette.Text, new Vector2(0,-25), new Vector2(94,28));
            time.font = UiKit.Display;
            var cash = UiKit.Panel(transform, "Payout badge", Palette.Panel).rectTransform;
            UiKit.Place(cash, Vector2.one, new Vector2(-90,-52), new Vector2(152,64));
            UiKit.Stripe(cash, Palette.Accent);
            Caption(cash, "Payout label", "PAYOUT", 13, Palette.TextDim, new Vector2(0,15), new Vector2(140,18));
            payout = Caption(cash, "Payout", "", 22, Palette.Accent, new Vector2(0,-10), new Vector2(140,28));
            payout.font = UiKit.Display;

            factor = Caption(transform, "Multiplier", "0.1x", 69, Palette.Accent, new Vector2(0,124), new Vector2(330,86));
            factor.font = UiKit.Display;
            factor.gameObject.AddComponent<Outline>().effectColor = new Color(.08f,.04f,.015f,.95f);
            move = Caption(transform, "Move", "", 14, Palette.Text, new Vector2(0,83), new Vector2(220,24));

            controls = UiKit.Panel(transform, "Balance controls", Color.clear).rectTransform;
            UiKit.Place(controls, new Vector2(.5f,0), new Vector2(0,12), new Vector2(156,156), new Vector2(.5f,0));
            stick = Dial(controls).rectTransform;
            knob = Dot(stick, "Knob", new Color(.30f,.36f,.44f), 68);
            UiKit.Stretch(GuideImage(knob, "Knob rim", UiKit.GuideRing, Palette.Accent).rectTransform);
            Dot(knob, "Knob inset", new Color(.15f,.19f,.26f), 28);
            Caption(stick, "Forward", "FWD", 11, Palette.Accent, new Vector2(0,57), new Vector2(55,18));
            Caption(stick, "Back", "BACK", 11, Palette.Accent, new Vector2(0,-57), new Vector2(55,18));
            Caption(stick, "Left", "L", 11, Palette.Accent, new Vector2(-58,0), new Vector2(22,18));
            Caption(stick, "Right", "R", 11, Palette.Accent, new Vector2(58,0), new Vector2(22,18));

            var trigger = stick.gameObject.AddComponent<EventTrigger>();
            AddEvent(trigger, EventTriggerType.PointerDown, data =>
            {
                if (pointer.HasValue) return;
                pointer = data.pointerId;
                SetPointer(data);
            });
            AddEvent(trigger, EventTriggerType.Drag, SetPointer);
            AddEvent(trigger, EventTriggerType.PointerUp, data =>
            {
                if (pointer == data.pointerId) ResetStick();
            });

        }

        static Image GuideImage(Transform parent, string name, Sprite sprite, Color color)
        {
            Image image = UiKit.Panel(parent,name,color);
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            return image;
        }

        public void PresentGuide(RideSim sim, Camera camera)
        {
            bool riding = sim.Phase == RidePhase.Ride;
            rideGuide.gameObject.SetActive(riding);
            if (!riding) return;

            Vector3 center = camera.WorldToScreenPoint(new Vector3(sim.Position.x,
                1.18f + sim.Pose.Hop*.56f, sim.Position.y));
            var parent = (RectTransform)transform;
            if (center.z <= 0 || !RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, center, null, out Vector2 point))
            {
                rideGuide.gameObject.SetActive(false);
                return;
            }

            float scale = 1.5f / (center.z * Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * .5f));
            float max = Mathf.Min(parent.rect.width*.87f, parent.rect.height*.68f);
            float size = Mathf.Clamp(parent.rect.height * scale, Mathf.Min(130,max),max);
            UiKit.Place(rideGuide.rectTransform, UiKit.Center, point, new Vector2(size,size));
            Vector2 lean = sim.Lean;
            float strength = lean.magnitude;
            bool centered = strength < .14f;
            guideArc.fillAmount = .08f + .28f * Mathf.Clamp01(strength);
            guideArc.rectTransform.localRotation = Quaternion.Euler(0,0,
                centered ? 60 : Mathf.Atan2(lean.x, -lean.y) * Mathf.Rad2Deg);
            guideTip.gameObject.SetActive(!centered);
            UiKit.Place(puck.rectTransform, UiKit.Center, Vector2.ClampMagnitude(lean, 1) * (size * .38f),
                Vector2.one * Mathf.Clamp(size * .033f, 9, 17));
            centerZone.gameObject.SetActive(sim.HighBuck);
            float diameter = size * .76f * RideRules.CenterRadius;
            UiKit.Place(centerZone.rectTransform, UiKit.Center, Vector2.zero, Vector2.one * diameter);
            centerZone.color = new Color(CenterGreen.r, CenterGreen.g, CenterGreen.b, sim.InCenter ? .45f : .25f);
            puck.color = sim.HighBuck && sim.InCenter ? CenterGreen : strength >= .8f ? Palette.Danger : Palette.Accent;

        }

        void SetPointer(PointerEventData data)
        {
            if (pointer != data.pointerId) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(stick, data.position, data.pressEventCamera, out Vector2 point))
                Stick = Vector2.ClampMagnitude(point / 42, 1);
        }

        static void AddEvent(EventTrigger trigger, EventTriggerType type, System.Action<PointerEventData> action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(data => action((PointerEventData)data));
            trigger.triggers.Add(entry);
        }

        static Image Dial(Transform parent)
        {
            var dial = UiKit.Panel(parent, "Joystick", new Color(.073f, .097f, .14f));
            dial.sprite = UiKit.Circle;
            dial.type = Image.Type.Simple;
            UiKit.Place(dial.rectTransform, UiKit.Center, Vector2.zero, new Vector2(148,148));
            UiKit.Stretch(GuideImage(dial.transform, "Joystick rim", UiKit.GuideRing, Palette.Accent).rectTransform);
            return dial;
        }

        static RectTransform Dot(Transform parent, string name, Color color, float size)
        {
            var dot = UiKit.Panel(parent, name, color);
            dot.sprite = UiKit.Circle;
            dot.type = Image.Type.Simple;
            dot.raycastTarget = false;
            UiKit.Place(dot.rectTransform, UiKit.Center, Vector2.zero, new Vector2(size, size));
            return dot.rectTransform;
        }

        static Text Stat(Transform parent, string title, float x)
        {
            Caption(parent, title + " label", title, 13, Palette.TextDim, new Vector2(x, 19), new Vector2(160, 20));
            return Caption(parent, title + " value", "", 23, Palette.Text, new Vector2(x, -10), new Vector2(160, 31));
        }

        static Text Caption(Transform parent, string name, string value, int size, Color color, Vector2 offset, Vector2 box)
        {
            Text label = UiKit.Label(parent, name, value, size, color, TextAnchor.MiddleCenter);
            label.fontStyle = FontStyle.Bold;
            UiKit.Place(label.rectTransform, UiKit.Center, offset, box);
            return label;
        }

        public void Layout(bool portrait, Vector2 available)
        {
            var top = new Vector2(.5f,1);
            float offset = portrait ? 113 : 63;
            UiKit.Place(factor.rectTransform, top, new Vector2(0,-offset), new Vector2(330,86));
            UiKit.Place(move.rectTransform, top, new Vector2(0,-offset-46), new Vector2(220,24));
            controls.localScale = Vector3.one * Mathf.Clamp(available.y / 640, .73f, 1);
        }

        public void ResetStick()
        {
            pointer = null;
            Stick = Vector2.zero;
        }

        public void Present(RideSim sim, Wallet wallet, Vector2 input, int milestone, double popUntil)
        {
            float elapsed = Mathf.Clamp01((float)(sim.RideTime / RideRules.Duration));
            clockFill.fillAmount = elapsed;
            clockHand.localRotation = Quaternion.Euler(0, 0, -360 * elapsed);
            time.text = sim.RideTime.ToString("0.0") + " / 8";
            factor.text = RideRules.Factor(sim.Multiplier);
            move.text = sim.HighBuck ? "HIGH BUCK  /  HOLD CENTER" : sim.Move;
            move.color = sim.HighBuck ? CenterGreen : Palette.Text;
            payout.text = RideRules.Money(wallet.LivePayout(sim.Multiplier));
            float pulse = milestone > 0 ? Mathf.Clamp01((float)(popUntil - sim.RideTime) / .42f) : 0;
            factor.rectTransform.localScale = Vector3.one * (1 + .22f * pulse);
            controls.gameObject.SetActive(sim.CanBalance);
            knob.anchoredPosition = input * 35;

        }
    }
}
