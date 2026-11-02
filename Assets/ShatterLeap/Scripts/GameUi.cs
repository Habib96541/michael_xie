using System;
using UnityEngine;
using UnityEngine.UI;

namespace ShatterLeap
{
    public sealed class GameUi : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;
        private RunController _run;
        private GameConfig _config;
        private RectTransform _root;
        private Font _font;
        private Text _timer;
        private Text _instruction;
        private Text _status;
        private Text _preview;
        private Text _feedback;
        private int _width;
        private int _height;
        private Rect _safeArea;
        private string _page = "";
        private Action _confirmation;
        private static readonly Color Ink = new Color(0.035f, 0.06f, 0.09f, 0.97f);
        private static readonly Color Teal = new Color(0.25f, 0.92f, 0.85f);
        private static readonly Color Pink = new Color(0.91f, 0.19f, 0.42f);
        private static readonly Color Muted = new Color(0.65f, 0.75f, 0.8f);

        public void Initialize(RunController run, GameConfig config)
        {
            _run = run;
            _config = config;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _run.Changed += Refresh;
        }
        private void OnDestroy()
        {
            if (_run != null) { _run.Changed -= Refresh; }
        }
        public void Tick()
        {
            if (_width != Screen.width || _height != Screen.height || _safeArea != Screen.safeArea) { Rebuild(); }
            if (_timer && _run.Run != null)
            {
                _timer.text = $"{Mathf.CeilToInt(_run.Run.Remaining):00}s";
                _timer.color = _run.Run.Remaining < 10 ? Pink : Color.white;
            }
            if (_preview && _run.Run != null)
            {
                _preview.text = $"REMEMBER THE PATH   {Mathf.CeilToInt(Mathf.Max(0, _run.PreviewRemaining))}";
            }
        }
        private void Refresh()
        {
            _page = "";
            _run.DialogOpen = false;
            Rebuild();
        }
        private void Rebuild()
        {
            _width = Screen.width;
            _height = Screen.height;
            _safeArea = Screen.safeArea;
            if (_root)
            {
                _root.gameObject.SetActive(false);
                Destroy(_root.gameObject);
            }
            _timer = null;
            _preview = null;
            _feedback = null;
            bool portrait = _height > _width;
            _canvas.GetComponent<CanvasScaler>().scaleFactor = portrait
                ? Mathf.Min(_width / 480f, _height / 800f)
                : Mathf.Min(_width / 1100f, _height / 650f);
            _root = Rect("Safe area", _canvas.transform);
            _root.anchorMin = new Vector2(_safeArea.xMin / _width, _safeArea.yMin / _height);
            _root.anchorMax = new Vector2(_safeArea.xMax / _width, _safeArea.yMax / _height);
            _root.offsetMin = Vector2.zero;
            _root.offsetMax = Vector2.zero;
            if (_run.Run == null) { Home(); }
            else { Hud(); }
            if (_run.Paused && _page == "") { PauseScreen(); }
            else if (_page == "settings") { Settings(); }
            else if (_page == "help") { Help(); }
            else if (_page == "confirm") { ConfirmScreen(); }
        }
        private void Home()
        {
            RectTransform card = Card("Home", 440, 625);
            Image logo = ImageAt("ShatterLeap logo", card, new Vector2(0, 190), new Vector2(420, 205), Color.white);
            logo.sprite = _config.Logo;
            logo.type = Image.Type.Simple;
            logo.preserveAspect = true;
            Label(card, "ONE PATH. ONE CHANCE.", 0, 75, 380, 28, 18, Teal);
            Image coin = ImageAt("Coin", card, new Vector2(-65, 26), new Vector2(32, 32), Color.white);
            coin.sprite = _config.CoinIcon;
            coin.type = Image.Type.Simple;
            coin.preserveAspect = true;
            Label(card, $"{_run.Saves.Data.Coins:N0}  BANKED", 25, 26, 250, 36, 23, Color.white);
            string[] labels = { "EASY", "NORMAL", "HARD" };
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                Button(card, labels[i], (i - 1) * 132, -34, 122, 48, () => _run.SelectDifficulty(index), _run.Saves.Data.Difficulty == i ? Teal * 0.65f : new Color(0.12f, 0.2f, 0.25f));
            }
            Difficulty difficulty = _config.Difficulties[_run.Saves.Data.Difficulty];
            Label(card, $"{difficulty.Rows} rows  /  {difficulty.PreviewSeconds:0}s preview  /  {difficulty.PlaySeconds:0}s run", 0, -84, 400, 32, 18, Muted);
            Button(card, "PLAY", 0, -144, 390, 62, _run.BeginRun, Pink);
            Button(card, "SETTINGS", -100, -215, 190, 48, () => Open("settings"));
            Button(card, "HOW TO PLAY", 100, -215, 190, 48, () => Open("help"));
            Label(card, "Complete the bridge to bank your coins.", 0, -275, 410, 30, 17, Muted);
        }
        private void Hud()
        {
            RunRules run = _run.Run;
            RectTransform top = Rect("HUD", _root);
            Place(top, new Vector2(0.5f, 1), new Vector2(0, -48), new Vector2(430, 86));
            ImageAt("HUD background", top, Vector2.zero, new Vector2(430, 86), Ink);
            _timer = Label(top, "", -155, 15, 100, 40, 31, Color.white);
            _status = Label(top, $"STEP {Mathf.Min(run.Steps + 1, run.Difficulty.Rows)} / {run.Difficulty.Rows}", -3, 19, 175, 30, 21, Teal);
            Label(top, $"{run.Earnings} ROUND  /  {_run.Saves.Data.Coins} BANKED", -20, -19, 340, 28, 16, Muted);
            Button(top, "II", 174, 8, 58, 58, _run.Pause);
            RectTransform bottom = Rect("Choices", _root);
            Place(bottom, new Vector2(0.5f, 0), new Vector2(0, 84), new Vector2(430, 100));
            if (run.State == RunState.Preview)
            {
                _preview = Label(bottom, "", 0, 22, 440, 44, 24, Teal);
                Label(bottom, "Follow the glowing markers. Choices unlock after preview.", 0, -24, 430, 42, 17, Color.white);
            }
            else if (run.State == RunState.CameraTransition)
            {
                Label(bottom, "GET READY", 0, 4, 430, 50, 26, Teal);
            }
            else if (!run.IsTerminal)
            {
                Button left = Button(bottom, "A   /   LEFT", -110, 0, 208, 66, () => _run.Choose(0));
                Button right = Button(bottom, "D   /   RIGHT", 110, 0, 208, 66, () => _run.Choose(1));
                left.interactable = _run.CanChoose;
                right.interactable = _run.CanChoose;
                _instruction = Label(bottom, run.AtFinish ? "Reaching the finish..." : "Choose the next row", 0, 55, 430, 28, 18, Color.white);
                if (run.Steps > 0)
                {
                    _feedback = Label(bottom, $"SAFE STEP   +{run.Steps * _config.StepValue} provisional coins", 0, 91, 430, 28, 18, Teal);
                }
            }
            else if (run.State == RunState.Results) { Results(); }
            else
            {
                Label(bottom, run.Outcome == RunState.Won ? "BRIDGE CLEARED" : run.Outcome == RunState.Falling ? "GLASS BROKEN" : "TIME'S UP", 0, 0, 430, 60, 30, run.Outcome == RunState.Won ? Teal : Pink);
            }
        }
        private void Results()
        {
            RunRules run = _run.Run;
            bool won = run.Outcome == RunState.Won;
            RectTransform card = Card("Results", 440, 510);
            Label(card, won ? "YOU MADE IT" : "ROUND OVER", 0, 177, 400, 55, 36, won ? Teal : Pink);
            Label(card, won ? "The bridge is behind you." : run.Outcome == RunState.Falling ? "The fragile glass gave way." : "The countdown reached zero.", 0, 123, 400, 40, 21, Color.white);
            Label(card, $"{run.Steps} / {run.Difficulty.Rows} successful steps", 0, 63, 400, 35, 23, Muted);
            Label(card, won ? $"+{run.Payout} COINS BANKED" : "0 COINS AWARDED", 0, 5, 400, 44, 29, won ? Teal : Color.white);
            Label(card, won ? $"{run.Earnings} step coins + {run.Difficulty.Bonus} completion bonus" : "Only a completed bridge pays out.", 0, -36, 400, 35, 18, Muted);
            Label(card, $"Wallet: {_run.Saves.Data.Coins:N0} coins", 0, -80, 400, 35, 22, Color.white);
            Button(card, "RETRY", 0, -141, 390, 58, _run.BeginRun, Pink);
            Button(card, "HOME", 0, -213, 390, 48, _run.ShowHome);
        }
        private void PauseScreen()
        {
            RectTransform card = Card("Pause", 440, 500);
            Label(card, "TAKE A BREATH", 0, 132, 400, 55, 32, Teal);
            Label(card, "Your timer is paused.", 0, 78, 390, 35, 20, Muted);
            Button(card, "RESUME", 0, 12, 390, 58, _run.Resume, Pink);
            Button(card, "RESTART", 0, -62, 390, 48, () => Confirm(_run.BeginRun));
            Button(card, "HOME", 0, -136, 390, 48, () => Confirm(_run.ShowHome));
            Button(card, "SETTINGS", 0, -206, 390, 40, () => Open("settings"));
        }
        private void Settings()
        {
            RectTransform card = Card("Settings", 440, 600);
            Label(card, "SETTINGS", 0, 238, 400, 45, 32, Teal);
            Label(card, "MUSIC VOLUME", 0, 162, 390, 30, 20, Color.white);
            Slider(card, 110, _run.Saves.Data.Music, value => { _run.Saves.Data.Music = value; _run.SettingsChanged(); });
            Label(card, "SOUND VOLUME", 0, 60, 390, 30, 20, Color.white);
            Slider(card, 8, _run.Saves.Data.Sound, value => { _run.Saves.Data.Sound = value; _run.SettingsChanged(); });
            Button(card, "VIBRATION: " + (_run.Saves.Data.Vibration ? "ON" : "OFF"), 0, -68, 390, 48, () =>
            {
                _run.Saves.Data.Vibration = !_run.Saves.Data.Vibration;
                _run.SettingsChanged();
                Rebuild();
            });
            Button(card, "REDUCED CAMERA EFFECTS: " + (_run.Saves.Data.ReducedEffects ? "ON" : "OFF"), 0, -145, 390, 48, () =>
            {
                _run.Saves.Data.ReducedEffects = !_run.Saves.Data.ReducedEffects;
                _run.SettingsChanged();
                Rebuild();
            });
            Label(card, "Vibration is available only on supported devices.", 0, -200, 400, 38, 16, Muted);
            Button(card, "BACK", 0, -260, 390, 48, Close, Pink);
        }
        private void Help()
        {
            RectTransform card = Card("How to play", 440, 540);
            Label(card, "HOW TO PLAY", 0, 205, 400, 50, 32, Teal);
            Label(card, "01  REMEMBER\nMemorize the glowing path.\n\n02  CHOOSE\nA / D, next-panel clicks, or the large left / right buttons.\n\n03  SURVIVE\nFragile glass ends the round. Finish before the timer expires.\n\n04  BANK YOUR WIN\nOnly completed rounds pay. Failed or abandoned rounds pay nothing. Previous winnings stay saved.", 0, -5, 380, 360, 20, Color.white, TextAnchor.UpperLeft);
            Button(card, "GOT IT", 0, -221, 390, 52, Close, Pink);
        }
        private void Confirm(Action action)
        {
            _confirmation = action;
            Open("confirm");
        }
        private void ConfirmScreen()
        {
            RectTransform card = Card("Confirm abandonment", 440, 310);
            Label(card, "LEAVE THIS ROUND?", 0, 91, 400, 45, 29, Pink);
            Label(card, "All provisional round earnings will be lost.\nYour previously banked coins stay saved.", 0, 17, 390, 80, 22, Color.white);
            Button(card, "KEEP PLAYING", -102, -93, 195, 54, Close);
            Button(card, "LEAVE ROUND", 102, -93, 195, 54, () => _confirmation?.Invoke(), Pink);
        }
        private void Open(string page)
        {
            _page = page;
            _run.DialogOpen = true;
            Rebuild();
        }
        private void Close()
        {
            _page = "";
            _run.DialogOpen = false;
            Rebuild();
        }
        private RectTransform Card(string name, float width, float height)
        {
            Image dimmer = ImageAt(name + " backdrop", _root, Vector2.zero, Vector2.zero, new Color(0.015f, 0.025f, 0.04f, 0.7f));
            RectTransform backdrop = dimmer.rectTransform;
            backdrop.anchorMin = Vector2.zero;
            backdrop.anchorMax = Vector2.one;
            backdrop.sizeDelta = Vector2.zero;
            dimmer.raycastTarget = true;
            RectTransform card = Rect(name, backdrop);
            Place(card, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, height));
            ImageAt("Card", card, Vector2.zero, new Vector2(width, height), Ink);
            ImageAt("Accent", card, new Vector2(0, height / 2), new Vector2(width, 3), Teal);
            return card;
        }
        private RectTransform Rect(string name, Transform parent)
        {
            GameObject item = new GameObject(name, typeof(RectTransform));
            item.transform.SetParent(parent, false);
            return item.GetComponent<RectTransform>();
        }
        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
        private Image ImageAt(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            RectTransform rect = Rect(name, parent);
            Place(rect, new Vector2(0.5f, 0.5f), position, size);
            Image image = rect.gameObject.AddComponent<Image>();
            if (_config.UiPanel)
            {
                image.sprite = _config.UiPanel;
                image.type = Image.Type.Sliced;
            }
            image.color = color;
            image.raycastTarget = false;
            return image;
        }
        private Text Label(Transform parent, string text, float x, float y, float width, float height, int size, Color color, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            RectTransform rect = Rect(text.Length > 30 ? "Text" : text, parent);
            Place(rect, new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(width, height));
            Text label = rect.gameObject.AddComponent<Text>();
            label.font = _font;
            label.fontSize = size;
            label.color = color;
            label.text = text;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }
        private Button Button(Transform parent, string label, float x, float y, float width, float height, Action action, Color? color = null)
        {
            height = Mathf.Max(height, 64);
            Image background = ImageAt(label, parent, new Vector2(x, y), new Vector2(width, height), color ?? new Color(0.12f, 0.25f, 0.3f));
            background.raycastTarget = true;
            Button button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.75f, 1, 0.96f);
            colors.pressedColor = new Color(0.5f, 0.75f, 0.7f);
            colors.disabledColor = new Color(0.3f, 0.35f, 0.38f, 0.8f);
            button.colors = colors;
            button.onClick.AddListener(() => { _run.Gesture(); action(); });
            Label(background.transform, label, 0, 0, width - 12, height - 8, label.Length > 25 ? 16 : 22, Color.white);
            return button;
        }
        private void Slider(Transform parent, float y, float value, Action<float> change)
        {
            Image track = ImageAt("Volume slider", parent, new Vector2(0, y), new Vector2(360, 64), new Color(0.12f, 0.25f, 0.3f));
            track.raycastTarget = true;
            // Slider stretches the handle along the cross axis; a zero height delta keeps it inside the track.
            Image handle = ImageAt("Handle", track.transform, Vector2.zero, new Vector2(26, 0), Teal);
            Slider slider = track.gameObject.AddComponent<Slider>();
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = value;
            slider.onValueChanged.AddListener(number => change(number));
        }
    }
}
