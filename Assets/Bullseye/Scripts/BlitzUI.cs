using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace BullseyeBlitz
{
    public sealed class BlitzUI : MonoBehaviour
    {
        private RectTransform _safe;
        private GameObject _modal;
        private Font _font;
        private Sprite _button;
        private Text _timer;
        private Text _coins;
        private Text _countdown;
        private RectTransform _content;
        public Action PauseRequested;
        public Action ActionInvoked;
        public bool ModalOpen => _modal != null;

        public void Configure(Sprite button)
        {
            _button = button;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(600, 800);
            scaler.matchWidthOrHeight = 0f;
            gameObject.AddComponent<GraphicRaycaster>();
            _safe = Rect("Safe area", transform, Vector2.zero, Vector2.one);
            _timer = Label(_safe, "20.0", new Vector2(0.04f, .9f), new Vector2(.34f, .98f), 35);
            _coins = Label(_safe, "ROUND COINS  0", new Vector2(.35f, .9f), new Vector2(.81f, .98f), 26);
            Button(_safe, "II", new Vector2(.83f, .91f), new Vector2(.97f, .98f), () => PauseRequested?.Invoke());
            _countdown = Label(_safe, "", new Vector2(.25f, .35f), new Vector2(.75f, .65f), 100);
        }
        private void Update()
        {
            if (_safe == null) { return; }
            Rect area = Screen.safeArea;
            _safe.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            _safe.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            if (_content != null)
            {
                float fraction = Mathf.Min(.96f, 760f / Mathf.Max(1, _safe.rect.width));
                _content.anchorMin = new Vector2((1 - fraction) * .5f, .02f);
                _content.anchorMax = new Vector2((1 + fraction) * .5f, .98f);
            }
        }
        public void Hud(double remaining, int pending, string countdown)
        {
            _timer.text = remaining.ToString("0.0") + "s";
            _timer.color = remaining <= 5 ? new Color(1, .35f, .3f) : Color.white;
            _coins.text = "ROUND COINS  " + pending;
            _countdown.text = countdown;
        }
        public void Close() { if (_modal != null) { _modal.SetActive(false); Destroy(_modal); _modal = null; _content = null; } }
        public RectTransform Panel(string title, string message)
        {
            Close();
            RectTransform cover = Rect("Menu overlay", _safe, Vector2.zero, Vector2.one);
            _modal = cover.gameObject;
            Image background = cover.gameObject.AddComponent<Image>();
            background.color = new Color(.075f, .035f, .15f, 1);
            _content = Rect("Menu content", cover, new Vector2(.05f, .02f), new Vector2(.95f, .98f));
            Label(_content, "*  CARNIVAL ARCADE  *", new Vector2(.06f, .93f), new Vector2(.94f, .98f), 18, new Color(.85f, .65f, 1));
            Label(_content, "BULLSEYE BLITZ", new Vector2(.02f, .81f), new Vector2(.98f, .93f), 51, new Color(1, .78f, .22f));
            Label(_content, title, new Vector2(.06f, .71f), new Vector2(.94f, .81f), 29);
            Label(_content, message, new Vector2(.06f, .49f), new Vector2(.94f, .70f), 24);
            return _content;
        }
        public void Action(RectTransform panel, string label, int row, UnityAction callback)
        {
            float top = .46f - row * .095f;
            Button(panel, label, new Vector2(.18f, top - .075f), new Vector2(.82f, top), callback);
        }
        public void Volume(RectTransform panel, string label, float y, float value, UnityAction<float> changed)
        {
            Label(panel, label, new Vector2(.12f, y), new Vector2(.42f, y + .055f), 24);
            RectTransform root = Rect(label + " slider", panel, new Vector2(.46f, y + .01f), new Vector2(.87f, y + .05f));
            root.gameObject.AddComponent<Image>().color = new Color(.35f, .25f, .5f);
            Slider slider = root.gameObject.AddComponent<Slider>();
            RectTransform handle = Rect("Handle", root, Vector2.zero, Vector2.one);
            Image image = handle.gameObject.AddComponent<Image>();
            image.color = new Color(1, .75f, .2f);
            handle.sizeDelta = new Vector2(25, 0);
            slider.handleRect = handle; slider.targetGraphic = image;
            slider.value = value;
            slider.onValueChanged.AddListener(changed);
        }
        private void Button(Transform parent, string title, Vector2 min, Vector2 max, UnityAction callback)
        {
            RectTransform rect = Rect(title, parent, min, max);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = _button;
            image.type = Image.Type.Sliced;
            if (_button == null) { image.color = new Color(.4f, .2f, .7f); }
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => { ActionInvoked?.Invoke(); callback(); });
            Label(rect, title, new Vector2(.05f, .05f), new Vector2(.95f, .95f), 27);
        }
        private Text Label(Transform parent, string value, Vector2 min, Vector2 max, int size, Color? color = null)
        {
            RectTransform rect = Rect(value.Length > 20 ? "Text" : value, parent, min, max);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = _font; text.text = value; text.fontSize = size;
            text.resizeTextForBestFit = true; text.resizeTextMinSize = 14; text.resizeTextMaxSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color ?? Color.white; text.raycastTarget = false;
            return text;
        }
        private RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            return rect;
        }
    }
}
