using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PegRush
{
    public sealed class ModalPresentation
    {
        private readonly UIFactory _factory;
        private readonly UITheme _theme;
        private readonly RectTransform _root;
        private readonly GameSession _session;
        private RectTransform _panel;
        private TextMeshProUGUI _mute;
        private TextMeshProUGUI _reduced;
        private string _mode;

        public ModalPresentation(UIFactory factory, UITheme theme, RectTransform root, GameSession session)
        {
            _factory = factory;
            _theme = theme;
            _root = root;
            _session = session;
            Image shield = root.GetComponent<Image>();
            shield.sprite = null;
            shield.color = new Color(0.01f, 0.03f, 0.05f, 0.94f);
            shield.raycastTarget = true;
        }

        public void Show(string mode)
        {
            foreach (Transform child in _root) Object.Destroy(child.gameObject);
            _mode = mode;
            _mute = null;
            _reduced = null;
            _root.gameObject.SetActive(true);
            _panel = _factory.Panel("Dialog", _root, _theme.Surface);
            TextMeshProUGUI kicker = _factory.Text("PEGRUSH / " + (mode == "help" ? "THE BASICS" : mode == "settings" ? "YOUR PREFERENCES" : "ONE MOMENT"), _panel, 11, _theme.Mint, true);
            UIFactory.Place(kicker, 24, 24, 340, 24);
            TextMeshProUGUI title = _factory.Text(mode == "settings" ? "Make it yours." : mode == "help" ? "Ready to drop?" : "Balls in flight.", _panel, 30, _theme.Ink, true);
            UIFactory.Place(title, 24, 58, 350, 50);
            if (mode == "settings")
            {
                TextMeshProUGUI note = _factory.Text("Small details. Your kind of play.", _panel, 15, _theme.Muted);
                UIFactory.Place(note, 24, 120, 340, 45);
                Button mute = _factory.Button("Mute", "", _panel, _session.ToggleMute);
                UIFactory.Place(mute, 24, 190, 328, 54);
                _mute = mute.GetComponentInChildren<TextMeshProUGUI>();
                Button reduced = _factory.Button("Reduced", "", _panel, _session.ToggleReduced);
                UIFactory.Place(reduced, 24, 258, 328, 54);
                _reduced = reduced.GetComponentInChildren<TextMeshProUGUI>();
                Button close = _factory.Button("ModalClose", "Back to it", _panel, _session.CloseModal, true);
                UIFactory.Place(close, 24, 352, 328, 54);
            }
            else if (mode == "leave")
            {
                TextMeshProUGUI body = _factory.Text("Your drops are still finding their pockets.\nStay and play, or head home once the\nboard has cleared.", _panel, 17, _theme.Muted);
                UIFactory.Place(body, 24, 132, 330, 110);
                Button stay = _factory.Button("Stay", "Stay here", _panel, _session.CloseModal, true);
                UIFactory.Place(stay, 24, 270, 328, 54);
                Button drain = _factory.Button("Drain", "Home when the board clears", _panel, _session.Drain);
                UIFactory.Place(drain, 24, 338, 328, 54);
            }
            else
            {
                TextMeshProUGUI body = _factory.Text("<b>01  Choose your drop</b>\nStart with 1,000 virtual coins. Choose a cost\nof 1, 5, 10, 25 or 50 coins.\n\n<b>02  Find your rhythm</b>\nTap Drop, or press Space once per ball.\nTab moves focus; Enter activates a control.\n\n<b>03  Make it your board</b>\nChoose 8–16 rows. Green is steady, Gold\nvaries, Red has bigger edge rewards.\nProfiles change rewards, never physics.\nRows and profiles unlock when balls clear.\n\n<b>04  Follow the pocket</b>\nReward = drop cost × the pocket multiplier.\nUnresolved drops refund once on reopening.\nFree refill unlocks below 1 coin when clear.\n\nProgress is saved in this browser only.\nClearing site data can erase your coins.", _panel, 15, _theme.Muted);
                UIFactory.Place(body, 24, 128, 330, 445);
                Button close = _factory.Button("ModalClose", "Got it. Let's play.", _panel, _session.CloseModal, true);
                UIFactory.Place(close, 24, 606, 328, 54);
            }
        }

        public void Hide()
        {
            _root.gameObject.SetActive(false);
        }

        public void Refresh(float width, float height)
        {
            if (!_root.gameObject.activeSelf || _panel == null) return;
            float panelHeight = _mode == "help" ? 688 : 436;
            float panelWidth = Mathf.Min(376, width - 24);
            UIFactory.Place(_panel, (width - panelWidth) * 0.5f, Mathf.Max(12, (height - panelHeight) * 0.5f), panelWidth, panelHeight);
            if (_mute != null) _mute.text = "Sound  ·  " + (_session.Muted ? "Muted" : "On");
            if (_reduced != null) _reduced.text = "Reduced effects  ·  " + (_session.Reduced ? "On" : "Off");
        }
    }
}
