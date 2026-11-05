using BullRide.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BullRide
{
    public sealed class UiLobby : MonoBehaviour
    {
        static readonly int[] Amounts = { 5, 10, 25, 50, 100 };
        readonly Button[] chips = new Button[Amounts.Length];
        RectTransform card;
        Text cash, error;
        InputField money;
        Button refill;
        public string InputText => money.text.Trim();

        public void Build(BullRideApp app)
        {
            UiKit.Stretch((RectTransform)transform);
            card = UiKit.Panel(transform, "Menu card", Palette.Panel).rectTransform;
            UiKit.Place(card, new Vector2(0, .5f), new Vector2(52, 0), new Vector2(450, 518), new Vector2(0, .5f));
            UiKit.Stripe(card, Palette.Accent);
            Label("Brand", "DUST CREEK RODEO", 17, Palette.Accent, 219);
            Texture2D logo = Resources.Load<Texture2D>("BullRideLogo");
            if (logo != null)
            {
                var mark = UiKit.Panel(card, "Bull Ride logo", Color.white);
                mark.sprite = Sprite.Create(logo, new Rect(0,0,logo.width,logo.height), UiKit.Center);
                mark.type = Image.Type.Simple;
                mark.preserveAspect = true;
                mark.raycastTarget = false;
                UiKit.Place(mark.rectTransform, UiKit.Center, new Vector2(0,162), new Vector2(340,116));
            }
            else
            {
                Text title = Label("Title", "BULL RIDE", 56, Palette.Text, 161);
                title.font = UiKit.Display;
                title.fontStyle = FontStyle.Normal;
            }
            Label("Subtitle", "Stay in the saddle for 8 seconds.", 17, Palette.TextDim, 92);
            Label("You have", "YOU HAVE", 15, Palette.TextDim, 59);
            cash = Label("Bank", "$100.00", 49, Palette.Accent, 22);
            cash.font = UiKit.Display;
            Label("Money caption", "MONEY", 15, Palette.TextDim, -28);

            Image field = UiKit.Panel(card, "Money field", new Color(.053f, .068f, .092f));
            UiKit.Place(field.rectTransform, UiKit.Center, new Vector2(0, -63), new Vector2(380, 48));
            Text fieldText = UiKit.Label(field.transform, "Amount", "", 25, Palette.Text, TextAnchor.MiddleLeft);
            UiKit.Stretch(fieldText.rectTransform);
            fieldText.rectTransform.offsetMin = new Vector2(33, 0);
            fieldText.rectTransform.offsetMax = new Vector2(-12, 0);
            UiKit.Place(UiKit.Label(field.transform, "Currency", "$", 23, Palette.Accent,
                TextAnchor.MiddleCenter).rectTransform, new Vector2(0, .5f), new Vector2(17, 0), new Vector2(25, 44));
            money = field.gameObject.AddComponent<InputField>();
            money.targetGraphic = field;
            money.textComponent = fieldText;
            money.keyboardType = TouchScreenKeyboardType.NumbersAndPunctuation;
            money.onValueChanged.AddListener(_ => error.text = "");
            money.onEndEdit.AddListener(_ =>
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) app.StartFromLobby();
            });

            for (int i = 0; i < chips.Length; i++)
            {
                int amount = Amounts[i];
                chips[i] = UiKit.Button(card, "Stake " + amount, "$" + amount, new Vector2(70, 37), 18,
                    new Color(.18f, .21f, .27f), () => money.text = amount.ToString());
                ((RectTransform)chips[i].transform).anchoredPosition = new Vector2((i - 2) * 77, -119);
            }
            error = Label("Validation", "", 16, new Color(1, .72f, .56f), -156);
            Button play = UiKit.Button(card, "Play", "PLAY", new Vector2(380, 55), 27, UiKit.Signal, app.StartFromLobby);
            ((RectTransform)play.transform).anchoredPosition = new Vector2(0, -200);
            refill = UiKit.Button(card, "Refill", "GET $100", new Vector2(380, 31), 16, UiKit.Signal, app.TopUp);
            ((RectTransform)refill.transform).anchoredPosition = new Vector2(0, -242);
        }

        Text Label(string name, string caption, int size, Color color, float y)
        {
            Text label = UiKit.Label(card, name, caption, size, color, TextAnchor.MiddleCenter);
            label.fontStyle = FontStyle.Bold;
            UiKit.Place(label.rectTransform, UiKit.Center, new Vector2(0, y), new Vector2(400, size + 12));
            return label;
        }

        public void Layout(bool portrait, Vector2 available)
        {
            Vector2 size = new Vector2(450, 518);
            float edge = portrait ? 16 : 24;
            float width = portrait ? available.x - 2 * edge : available.x * .48f - edge;
            float height = portrait ? available.y * .59f : available.y - 24;
            float fit = Mathf.Clamp01(Mathf.Min(width / size.x, height / size.y));
            card.localScale = Vector3.one * fit;
            Vector2 anchor = portrait ? new Vector2(.5f, 0) : new Vector2(0, .5f);
            UiKit.Place(card, anchor, portrait ? new Vector2(0, edge) : new Vector2(edge, 0), size, anchor);
        }

        public void ResetInput()
        {
            money.SetTextWithoutNotify("10");
            error.text = "";
        }

        public void ShowError(string message) => error.text = message;

        public void Refresh(Wallet wallet)
        {
            cash.text = RideRules.Money(wallet.Balance);
            refill.gameObject.SetActive(wallet.Balance < 1);
            for (int i = 0; i < chips.Length; i++) chips[i].interactable = Amounts[i] <= wallet.Balance;
        }
    }
}
