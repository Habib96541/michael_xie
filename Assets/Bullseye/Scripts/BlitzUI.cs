using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace BullseyeBlitz
{
    public sealed class BlitzUI : MonoBehaviour
    {
        private readonly List<Text> _labels = new List<Text>();
        private RectTransform _safe;
        private RectTransform _hud;
        private GameObject _modal;
        private Font _font;
        private Font _displayFont;
        private Sprite _ticket;
        private static readonly Color Gold = new Color32(241, 190, 106, 255);
        private static readonly Color Cream = new Color32(255, 235, 196, 255);
        private static readonly Color Ink = new Color32(15, 31, 37, 255);
        private static readonly Color Crimson = new Color32(132, 40, 45, 255);
        private Text _timer;
        private Text _coins;
        private Text _countdown;
        private RectTransform _content;
        public Action PauseRequested;
        public Action ActionInvoked;
        public bool ModalOpen => _modal != null;

        public void Configure(BlitzConfig config)
        {
            _font = config.BodyFont != null ? config.BodyFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _displayFont = config.DisplayFont != null ? config.DisplayFont : _font;
            _ticket = MakeTicket();
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(600, 800);
            scaler.matchWidthOrHeight = 0f;
            gameObject.AddComponent<GraphicRaycaster>();
            _safe = Rect("Safe area", transform, Vector2.zero, Vector2.one);
            _hud = Rect("HUD", _safe, Vector2.zero, Vector2.one);
            RectTransform timePlate = Plate(_hud, "Time ticket", new Vector2(.035f, .90f), new Vector2(.32f, .98f), Ink);
            RectTransform coinsPlate = Plate(_hud, "Coin ticket", new Vector2(.34f, .90f), new Vector2(.81f, .98f), Ink);
            _timer = Label(timePlate, "20.0", new Vector2(.05f, .12f), new Vector2(.95f, .88f), 31, Cream, true);
            _coins = Label(coinsPlate, "ROUND COINS  0", new Vector2(.05f, .12f), new Vector2(.95f, .88f), 24, Gold);
            Button(_hud, "II", new Vector2(.83f, .91f), new Vector2(.97f, .98f), () => PauseRequested?.Invoke());
            _countdown = Label(_safe, "", new Vector2(.25f, .35f), new Vector2(.75f, .65f), 100, Gold, true);
            AddShadow(_countdown, 3);
        }
        private void Update()
        {
            if (_safe == null) { return; }
            _hud.gameObject.SetActive(!ModalOpen);
            Rect area = Screen.safeArea;
            _safe.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            _safe.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            if (_content != null)
            {
                float fraction = Mathf.Min(.96f, .92f * _safe.rect.height / Mathf.Max(1, _safe.rect.width));
                _content.anchorMin = new Vector2((1 - fraction) * .5f, .02f);
                _content.anchorMax = new Vector2((1 + fraction) * .5f, .98f);
            }
        }
        public void Hud(double remaining, int pending, string countdown)
        {
            _timer.text = remaining.ToString("0.0") + "s";
            _timer.color = remaining <= 5 ? new Color32(255, 139, 114, 255) : Cream;
            _coins.text = "ROUND COINS  " + pending;
            _countdown.text = countdown;
        }
        public void Close()
        {
            if (_modal == null)
            {
                return;
            }

            _labels.RemoveAll(label => label == null || label.transform.IsChildOf(_modal.transform));
            _modal.SetActive(false);
            Destroy(_modal);
            _modal = null;
            _content = null;
        }
        public RectTransform Panel(string title, string message)
        {
            Close();
            RectTransform cover = Rect("Menu overlay", _safe, Vector2.zero, Vector2.one);
            _modal = cover.gameObject;
            Image background = cover.gameObject.AddComponent<Image>();
            bool opaque = title == "PAUSED" || title == "LEAVE THIS ROUND?";
            background.color = new Color32(8, 18, 24, opaque ? (byte)255 : (byte)224);
            _content = Rect("Menu content", cover, new Vector2(.05f, .02f), new Vector2(.95f, .98f));
            Plate(_content, "Inset ticket frame", new Vector2(.025f, .008f), new Vector2(.975f, .995f), Ink);
            Label(_content, "STEP RIGHT UP  /  THE MIDWAY", new Vector2(.12f, .92f), new Vector2(.88f, .975f), 15, Gold);
            Text marquee = Label(_content, "BULLSEYE BLITZ", new Vector2(.075f, .81f), new Vector2(.925f, .927f), 43, Cream, true);
            AddShadow(marquee, 2);
            Label(_content, title, new Vector2(.075f, .711f), new Vector2(.925f, .81f), 24, Gold);
            Label(_content, message, new Vector2(.10f, .49f), new Vector2(.90f, .70f), 22, Cream);
            return _content;
        }
        public void Action(RectTransform panel, string label, int row, UnityAction callback)
        {
            float top = .46f - row * .09f;
            Button(panel, label, new Vector2(.18f, top - .075f), new Vector2(.82f, top), callback);
        }
        public void Volume(RectTransform panel, string label, float y, float value, UnityAction<float> changed)
        {
            Label(panel, label, new Vector2(.12f, y), new Vector2(.42f, y + .055f), 24);
            RectTransform root = Rect(label + " slider", panel, new Vector2(.46f, y + .01f), new Vector2(.87f, y + .05f));
            root.gameObject.AddComponent<Image>().color = new Color32(94, 70, 55, 255);
            Slider slider = root.gameObject.AddComponent<Slider>();
            RectTransform handle = Rect("Handle", root, Vector2.zero, Vector2.one);
            Image image = handle.gameObject.AddComponent<Image>();
            image.color = Gold;
            handle.sizeDelta = new Vector2(25, 0);
            slider.handleRect = handle; slider.targetGraphic = image;
            slider.value = value;
            slider.onValueChanged.AddListener(changed);
        }
        private void Button(Transform parent, string title, Vector2 min, Vector2 max, UnityAction callback)
        {
            RectTransform rect = Rect(title, parent, min, max);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = _ticket;
            image.type = Image.Type.Sliced;
            image.color = Gold;
            bool primary = title.StartsWith("Play") || title.StartsWith("Retry") || title == "Resume";
            RectTransform inset = Plate(rect, "Button face", new Vector2(.009f, .065f), new Vector2(.991f, .935f), primary ? Gold : Crimson);
            inset.GetComponent<Image>().raycastTarget = false;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => { ActionInvoked?.Invoke(); callback(); });
            Label(rect, title, new Vector2(.075f, .13f), new Vector2(.925f, .87f), 24, primary ? Ink : Cream);
        }
        private Text Label(Transform parent, string value, Vector2 min, Vector2 max, int size, Color? color = null, bool display = false)
        {
            RectTransform rect = Rect(value.Length > 20 ? "Text" : value, parent, min, max);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = display ? _displayFont : _font; text.text = value; text.fontSize = size;
            text.resizeTextForBestFit = true; // A width-scaled Canvas can make short rows smaller than 14 local units.
            // Let the generator fit their glyphs instead of dropping the entire label.
            text.resizeTextMinSize = 1; text.resizeTextMaxSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color ?? Color.white; text.raycastTarget = false;
            _labels.Add(text);
            return text;
        }
        public BlitzBrowser.UiLabel[] GetTextDiagnostics()
        {
            List<BlitzBrowser.UiLabel> diagnostics = new List<BlitzBrowser.UiLabel>();
            foreach (Text label in _labels)
            {
                if (!label.isActiveAndEnabled || string.IsNullOrWhiteSpace(label.text))
                {
                    continue;
                }

                Vector3[] corners = new Vector3[4];
                label.rectTransform.GetWorldCorners(corners);
                diagnostics.Add(new BlitzBrowser.UiLabel
                {
                    Text = label.text,
                    Vertices = label.cachedTextGenerator.vertexCount,
                    Characters = label.cachedTextGenerator.characterCountVisible,
                    FontSize = label.cachedTextGenerator.fontSizeUsedForBestFit,
                    Left = corners[0].x / Screen.width,
                    Bottom = corners[0].y / Screen.height,
                    Right = corners[2].x / Screen.width,
                    Top = corners[2].y / Screen.height
                });
            }

            return diagnostics.ToArray();
        }

        private RectTransform Plate(Transform parent, string name, Vector2 min, Vector2 max, Color tint)
        {
            RectTransform rect = Rect(name, parent, min, max);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = _ticket;
            image.type = Image.Type.Sliced;
            image.color = Gold;
            RectTransform fill = Rect("Ticket interior", rect, Vector2.zero, Vector2.one);
            fill.offsetMin = new Vector2(2f, 2f);
            fill.offsetMax = new Vector2(-2f, -2f);
            Image fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.sprite = _ticket;
            fillImage.type = Image.Type.Sliced;
            fillImage.color = tint;
            fillImage.raycastTarget = false;
            image.raycastTarget = false;
            return rect;
        }

        private static void AddShadow(Text label, float distance)
        {
            Shadow shadow = label.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color32(41, 18, 19, 240);
            shadow.effectDistance = new Vector2(distance, -distance);
        }

        private static Sprite MakeTicket()
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Abs(x - 31.5f) - 22f);
                    float dy = Mathf.Max(0, Mathf.Abs(y - 31.5f) - 22f);
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    byte alpha = (byte)(255 * Mathf.Clamp01(9.5f - distance));
                    byte shade = (byte)(distance >= 7f ? 192 : 255);
                    pixels[y * size + x] = new Color32(shade, shade, shade, alpha);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f),
                100, 0, SpriteMeshType.FullRect, new Vector4(12, 12, 12, 12));
        }

        private void OnDestroy()
        {
            if (_ticket == null) { return; }
            Destroy(_ticket.texture);
            Destroy(_ticket);
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
