using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PegRush
{
    public sealed class UIFactory
    {
        private readonly UITheme _theme;
        public readonly List<Button> Buttons = new List<Button>();

        public UIFactory(UITheme theme)
        {
            _theme = theme;
        }

        public RectTransform Panel(string name, Transform parent, Color color, bool outline = false)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            Image image = obj.GetComponent<Image>();
            image.sprite = outline ? _theme.Outline : _theme.Rounded;
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.color = color;
            image.raycastTarget = false;
            return obj.GetComponent<RectTransform>();
        }

        public TextMeshProUGUI Text(string value, Transform parent, float size, Color color, bool bold = false, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
        {
            GameObject obj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
            text.font = bold ? _theme.Bold : _theme.Regular;
            text.fontSize = size;
            text.color = color;
            text.text = value;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            text.margin = Vector4.zero;
            return text;
        }

        public Button Button(string id, string value, Transform parent, UnityAction action, bool primary = false)
        {
            RectTransform rect = Panel(id, parent, primary ? _theme.Mint : _theme.Elevated);
            rect.GetComponent<Image>().raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.targetGraphic = rect.GetComponent<Image>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.pressedColor = new Color(0.68f, 0.87f, 0.87f);
            colors.selectedColor = new Color(1.25f, 1.25f, 1.25f);
            colors.disabledColor = new Color(0.5f, 0.58f, 0.61f, 0.55f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.onClick.AddListener(action);
            TextMeshProUGUI label = Text(value, rect, 16, primary ? _theme.Background : _theme.Ink, true, TextAlignmentOptions.Center);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            label.rectTransform.anchoredPosition = Vector2.zero;
            label.rectTransform.sizeDelta = new Vector2(-16, -6);
            RectTransform ring = Panel("Focus border", rect, _theme.Mint, true);
            Fill(ring);
            rect.gameObject.AddComponent<ButtonFocus>().Wire(ring.GetComponent<Image>(), button);
            Buttons.Add(button);
            return button;
        }

        public static void Place(Component component, float x, float y, float width, float height)
        {
            RectTransform rect = component.transform as RectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        public static void Fill(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
