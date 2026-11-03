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
        private int _displayedStep;
        private float _rewardAt;
        private static readonly Color Ink = new Color(0.035f, 0.052f, 0.075f, 1f);
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
            if (_feedback)
            {
                Color feedbackColor = Teal;
                feedbackColor.a = _run.Saves.Data.ReducedEffects ? (Time.time - _rewardAt < 1.6f ? 1 : 0) : Mathf.Clamp01(1 - (Time.time - _rewardAt - 1.2f) / 0.4f);
                _feedback.color = feedbackColor;
            }
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
            if (_run.Run == null)
            {
                if (_page == "") { Home(); }
            }
            else if (_run.Run.State == RunState.Results && !_run.Paused) { Results(); }
            else if (!_run.Paused && _page == "") { Hud(); }
            if (_run.Paused && _page == "") { PauseScreen(); }
            else if (_page == "settings") { Settings(); }
            else if (_page == "help") { Help(); }
            else if (_page == "confirm") { ConfirmScreen(); }
        }
        private void Home()
        {
            bool wide = Screen.width > Screen.height;
            float column = wide ? 235 : 0;
            RectTransform card = Card("Home", wide ? 980 : 440, 625);
            Image logo = ImageAt("ShatterLeap logo", card, new Vector2(wide ? -235 : 0, wide ? 72 : 190), new Vector2(wide ? 455 : 410, wide ? 255 : 205), Color.white);
            logo.sprite = _config.Logo;
            logo.type = Image.Type.Simple;
            logo.preserveAspect = true;
            Label(card, "MEMORY  /  COURAGE  /  ONE WAY FORWARD", wide ? -235 : 0, wide ? -90 : 78, 430, 30, 14, Muted);
            if (wide)
            {
                ImageAt("Divider", card, new Vector2(0, 0), new Vector2(1, 485), new Color(0.15f, 0.23f, 0.28f));
                Label(card, "THE BRIDGE REMEMBERS.\nDO YOU?", -235, -159, 420, 75, 27, Color.white);
                Label(card, "PHASE 01    /    THE GLASS TRIAL", -235, 250, 420, 28, 16, Teal);
            }
            float walletY = wide ? 216 : 27;
            ImageAt("Wallet well", card, new Vector2(column, walletY), new Vector2(390, 66), new Color(0.07f, 0.095f, 0.12f));
            Image coin = ImageAt("Coin", card, new Vector2(column - 142, walletY), new Vector2(32, 32), Color.white);
            coin.sprite = _config.CoinIcon;
            coin.type = Image.Type.Simple;
            coin.preserveAspect = true;
            Label(card, $"{_run.Saves.Data.Coins:N0}", column - 52, walletY, 140, 40, 29, new Color(1, 0.82f, 0.46f));
            Label(card, "BANKED COINS", column + 91, walletY, 150, 30, 14, Muted);
            float difficultyY = wide ? 68 : -39;
            if (wide) { Label(card, "SELECT YOUR TRIAL", column, 130, 390, 30, 17, Muted); }
            ImageAt("Difficulty rail", card, new Vector2(column, difficultyY), new Vector2(402, 76), new Color(0.018f, 0.03f, 0.045f));
            string[] labels = { "EASY", "NORMAL", "HARD" };
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                bool selected = _run.Saves.Data.Difficulty == i;
                Button(card, labels[i], column + (i - 1) * 132, difficultyY, 122, 64, () => _run.SelectDifficulty(index), selected ? new Color(0.06f, 0.30f, 0.29f) : new Color(0.055f, 0.08f, 0.105f));
                if (selected) { ImageAt("Selected difficulty", card, new Vector2(column + (i - 1) * 132, difficultyY - 29), new Vector2(70, 2), Teal); }
            }
            Difficulty difficulty = _config.Difficulties[_run.Saves.Data.Difficulty];
            Label(card, $"{difficulty.Rows} ROWS     {difficulty.PreviewSeconds:0}s PREVIEW     {difficulty.PlaySeconds:0}s LIMIT", column, wide ? 7 : -94, 400, 28, 15, Muted);
            Button(card, "PLAY", column, wide ? -75 : -150, 390, 72, _run.BeginRun, Pink);
            Button(card, "SETTINGS", column - 100, wide ? -161 : -230, 190, 64, () => Open("settings"));
            Button(card, "HOW TO PLAY", column + 100, wide ? -161 : -230, 190, 64, () => Open("help"));
            Label(card, "Finish the bridge. Bank the reward.", column, wide ? -245 : -283, 410, 28, 16, Muted);
        }
        private void Hud()
        {
            RunRules run = _run.Run;
            RectTransform top = Rect("HUD", _root);
            Place(top, new Vector2(0.5f, 1), new Vector2(0, -48), new Vector2(430, 86));
            ImageAt("HUD background", top, Vector2.zero, new Vector2(430, 86), Ink);
            ImageAt("HUD rule", top, new Vector2(0, -42), new Vector2(400, 2), new Color(0.08f, 0.28f, 0.29f));
            _timer = Label(top, "", -155, 15, 100, 40, 31, Color.white);
            _status = Label(top, $"STEP {Mathf.Min(run.Steps + 1, run.Difficulty.Rows)} / {run.Difficulty.Rows}", -3, 19, 175, 30, 21, Teal);
            Label(top, $"{run.Earnings} ROUND  /  {_run.Saves.Data.Coins} BANKED", -20, -19, 340, 28, 16, Muted);
            Button(top, "II", 174, 8, 58, 58, _run.Pause);
            RectTransform bottom = Rect("Choices", _root);
            Place(bottom, new Vector2(0.5f, 0), new Vector2(0, 104), new Vector2(430, 100));
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
                    if (_displayedStep != run.Steps) { _displayedStep = run.Steps; _rewardAt = Time.time; }
                    _feedback = Label(top, $"SAFE STEP   +{run.Steps * _config.StepValue} provisional coins", 0, -62, 430, 28, 18, Teal);
                }
            }
            else if (run.State == RunState.Results) { Results(); }
            else
            {
                bool falling = run.Outcome == RunState.Falling;
                Label(falling ? top : bottom, run.Outcome == RunState.Won ? "BRIDGE CLEARED" : falling ? "GLASS BROKEN" : "TIME'S UP", 0, falling ? -72 : 0, 430, 48, 26, run.Outcome == RunState.Won ? Teal : Pink);
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
            Image dimmer = ImageAt(name + " backdrop", _root, Vector2.zero, Vector2.zero, new Color(0.008f, 0.016f, 0.026f, 0.93f));
            RectTransform backdrop = dimmer.rectTransform;
            backdrop.anchorMin = Vector2.zero;
            backdrop.anchorMax = Vector2.one;
            backdrop.sizeDelta = Vector2.zero;
            dimmer.raycastTarget = true;
            RectTransform card = Rect(name, backdrop);
            Place(card, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, height));
            ImageAt("Card shadow", card, new Vector2(0, -8), new Vector2(width + 12, height + 12), new Color(0, 0, 0, 0.35f));
            ImageAt("Card border", card, Vector2.zero, new Vector2(width + 2, height + 2), new Color(0.19f, 0.26f, 0.30f));
            ImageAt("Card", card, Vector2.zero, new Vector2(width, height), Ink);
            ImageAt("Accent", card, new Vector2(-width / 2 + 50, height / 2), new Vector2(56, 3), Teal);
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
            if (size >= 27) { label.fontStyle = FontStyle.Bold; }
            label.color = color;
            label.text = text;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }
        private Button Button(Transform parent, string label, float x, float y, float width, float height, Action action, Color? color = null)
        {
            height = Mathf.Max(height, 64);
            Image background = ImageAt(label, parent, new Vector2(x, y), new Vector2(width, height), color ?? new Color(0.085f, 0.12f, 0.155f));
            background.raycastTarget = true;
            Button button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.75f, 1, 0.96f);
            colors.pressedColor = new Color(0.5f, 0.75f, 0.7f);
            colors.disabledColor = new Color(0.3f, 0.35f, 0.38f, 0.8f);
            colors.selectedColor = new Color(0.75f, 1f, 0.96f);
            colors.fadeDuration = 0.09f;
            button.colors = colors;
            Outline outline = background.gameObject.AddComponent<Outline>();
            outline.effectColor = color.HasValue && color.Value == Pink ? new Color(1, 0.4f, 0.55f, 0.5f) : new Color(0.25f, 0.39f, 0.43f, 0.65f);
            outline.effectDistance = new Vector2(1, -1);
            button.onClick.AddListener(() => { _run.Gesture(); action(); });
            Text caption = Label(background.transform, label, 0, 0, width - 12, height - 8, label.Length > 25 ? 16 : 20, Color.white);
            caption.fontStyle = FontStyle.Bold;
            return button;
        }
        private void Slider(Transform parent, float y, float value, Action<float> change)
        {
            Image track = ImageAt("Volume slider", parent, new Vector2(0, y), new Vector2(360, 64), new Color(0.085f, 0.12f, 0.155f));
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
