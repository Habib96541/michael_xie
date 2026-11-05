using BullRide.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BullRide
{
    public sealed class UiResult : MonoBehaviour
    {
        Text title, cash;
        RectTransform card;
        Button again;
        AudioBuzz sound;
        bool rewardShown;
        double finalFactor;
        decimal paid, balance;
        float revealStart;
        readonly Text[] values = new Text[4];

        public void Build(BullRideApp app, AudioBuzz audio)
        {
            sound = audio;
            UiKit.Stretch((RectTransform)transform);
            card = UiKit.Panel(transform, "Result card", Palette.Panel).rectTransform;
            UiKit.Place(card, UiKit.Center, Vector2.zero, new Vector2(460, 450));
            UiKit.Stripe(card, Palette.Accent);
            Caption(card, "Result brand", "RIDE RESULT", 16, Palette.Accent, 0, 185);
            title = Caption(card, "Title", "", 60, Palette.Text, 0, 132);
            title.font = UiKit.Display;
            Caption(card, "Ride payout", "RIDE PAYOUT", 15, Palette.TextDim, 0, 76);
            cash = Caption(card, "Bank", "", 46, Palette.Accent, 0, 40);
            cash.font = UiKit.Display;
            string[] labels = { "PUT IN", "MULTIPLIER", "YOU HAVE", "TIME" };
            for (int i = 0; i < labels.Length; i++)
            {
                float y = -17 - i * 34;
                Caption(card, labels[i], labels[i], 17, Palette.TextDim, -99, y, TextAnchor.MiddleLeft);
                values[i] = Caption(card, labels[i] + " value", "", 19, Palette.Text, 96, y, TextAnchor.MiddleRight);
            }

            again = UiKit.Button(card, "Ride again", "RIDE AGAIN", new Vector2(382, 56), 26, UiKit.Signal, app.EnterLobby);
            ((RectTransform)again.transform).anchoredPosition = new Vector2(0, -173);
        }

        public void Layout(Vector2 available)
        {
            float fit = Mathf.Clamp01(Mathf.Min((available.x - 24) / 460, (available.y - 24) / 450));
            card.localScale = Vector3.one * fit;
        }

        static Text Caption(Transform parent, string name, string text, int size, Color color, float x, float y,
            TextAnchor align = TextAnchor.MiddleCenter)
        {
            Text label = UiKit.Label(parent, name, text, size, color, align);
            label.fontStyle = FontStyle.Bold;
            UiKit.Place(label.rectTransform, UiKit.Center, new Vector2(x, y),
                new Vector2(align == TextAnchor.MiddleCenter ? 390 : 188, size + 10));
            return label;
        }

        public void Present(RideSim sim, Wallet wallet)
        {
            title.text = sim.Whistle ? "WHISTLE" : "THROWN";
            finalFactor = sim.Multiplier;
            paid = wallet.Paid;
            balance = wallet.Balance;
            values[0].text = RideRules.Money(wallet.Stake);
            values[3].text = RideRules.Time(sim.RideTime);
            rewardShown = false;
            revealStart = Time.unscaledTime;
            again.interactable = false;
            UpdateReveal(0);
        }

        void Update() => UpdateReveal(Time.unscaledTime - revealStart);

        void UpdateReveal(float elapsed)
        {
            float progress = Mathf.Clamp01(elapsed / .95f);
            double factor = finalFactor * (1 - (1 - progress) * (1 - progress));
            values[1].text = progress < 1 ? RideRules.Factor(factor) :
                factor.ToString("0.0###", System.Globalization.CultureInfo.InvariantCulture) + "x";
            values[1].color = Palette.Accent;
            bool reveal = elapsed >= 1.17f;
            values[2].text = reveal ? RideRules.Money(balance) : "…";
            values[2].color = reveal ? Palette.Accent : Palette.TextDim;
            cash.text = reveal ? RideRules.Money(paid) : "…";
            float pop = Mathf.Clamp01((elapsed - 1.17f) / .38f);
            float scale = pop < .40f ? Mathf.Lerp(.58f, 1.26f, Mathf.SmoothStep(0, 1, pop / .40f)) :
                Mathf.Lerp(1.26f, 1, Mathf.SmoothStep(0, 1, (pop - .40f) / .60f));
            cash.rectTransform.localScale = Vector3.one * (reveal ? scale : 1);
            cash.color = Color.Lerp(Palette.Accent, new Color(1, .94f, .72f), reveal ? 1 - pop : 0);
            if (reveal && !rewardShown)
            {
                rewardShown = true;
                sound.Reward();
            }
            again.interactable = reveal;
        }
    }
}
