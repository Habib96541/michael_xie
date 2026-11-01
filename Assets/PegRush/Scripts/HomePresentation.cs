using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PegRush
{
    public sealed class HomePresentation
    {
        private readonly RectTransform _root;
        private readonly RectTransform _illustration;
        private readonly TextMeshProUGUI _brand;
        private readonly TextMeshProUGUI _eyebrow;
        private readonly TextMeshProUGUI _title;
        private readonly TextMeshProUGUI _body;
        private readonly TextMeshProUGUI _foot;
        private readonly Button _play;
        private readonly Button _help;
        private readonly Button _settings;
        private readonly TextMeshProUGUI _artCaption;

        public HomePresentation(UIFactory factory, RectTransform root, UITheme theme, GameSession session)
        {
            _root = root;
            _eyebrow = factory.Text("THE DROP CLUB  /  VOL. 01", root, 13, theme.Mint, true);
            _brand = factory.Text("PEG<color=#4EEAAE>RUSH</color><size=24> /</size>", root, 72, theme.Ink, true);
            _title = factory.Text("A little bounce.\nA bright possibility.", root, 40, theme.Ink, true);
            _body = factory.Text("Find your flow in a pocket-sized arcade.\nPick a color. Make your drop.", root, 18, theme.Muted);
            _play = factory.Button("Play", "Let's play  →", root, session.Play, true);
            _help = factory.Button("HowToPlay", "How to play", root, () => session.OpenModal("help"));
            _settings = factory.Button("HomeSettings", "Settings", root, () => session.OpenModal("settings"));
            _foot = factory.Text("VIRTUAL COINS ONLY     /     NO PURCHASES", root, 12, theme.Muted);
            _illustration = factory.Panel("Home peg sculpture", root, theme.Border, true);
            for (int row = 0; row < 7; row++)
            {
                for (int column = 0; column <= row; column++)
                {
                    RectTransform peg = factory.Panel("Decorative peg", _illustration, row == 0 ? theme.Mint : theme.Border);
                    UIFactory.Place(peg, 188 + (column - row * 0.5f) * 42, 100 + row * 42, 10, 10);
                }
            }
            _artCaption = factory.Text("GOOD THINGS TAKE A FEW BOUNCES.", _illustration, 12, theme.Muted, true, TextAlignmentOptions.Center);
            UIFactory.Place(_artCaption, 20, 425, 360, 30);
        }

        public void Layout(float width, float height, bool portrait)
        {
            float x = portrait ? 24 : Mathf.Max(80, width * 0.1f);
            float top = portrait ? Mathf.Max(30, (height - 790) * 0.5f) : Mathf.Max(70, (height - 660) * 0.5f);
            UIFactory.Place(_eyebrow, x, top, 390, 24);
            UIFactory.Place(_brand, x - 3, top + 34, portrait ? width - 40 : 610, portrait ? 78 : 100);
            _brand.fontSize = portrait ? 57 : 88;
            UIFactory.Place(_title, x, top + (portrait ? 135 : 180), portrait ? width - 48 : 620, 110);
            _title.fontSize = portrait ? 34 : 46;
            UIFactory.Place(_body, x, top + (portrait ? 259 : 318), portrait ? width - 48 : 620, 76);
            _body.fontSize = portrait ? 16 : 19;
            float buttons = top + (portrait ? 370 : 430);
            UIFactory.Place(_play, x, buttons, portrait ? width - 48 : 330, 60);
            UIFactory.Place(_help, x, buttons + 76, portrait ? (width - 60) * 0.5f : 159, 50);
            UIFactory.Place(_settings, x + (portrait ? (width - 60) * 0.5f : 159) + 12, buttons + 76, portrait ? (width - 60) * 0.5f : 159, 50);
            UIFactory.Place(_foot, x, buttons + 150, portrait ? width - 48 : 620, 35);
            _foot.fontSize = portrait ? 10 : 12;
            _illustration.gameObject.SetActive(portrait || width > 1100);
            _artCaption.gameObject.SetActive(!portrait);
            float artScale = portrait ? Mathf.Clamp((height - buttons - 202) / 500f, 0.32f, 0.55f) : 1f;
            _illustration.localScale = Vector3.one * artScale;
            UIFactory.Place(_illustration, portrait ? (width - 400 * artScale) * 0.5f : width - 560, portrait ? buttons + 190 : (height - 500) * 0.5f, 400, 500);
        }
    }
}
