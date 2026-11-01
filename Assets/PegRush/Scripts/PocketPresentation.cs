using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PegRush
{
    public sealed class PocketPresentation
    {
        private readonly RectTransform[] _chips = new RectTransform[17];
        private readonly RectTransform[] _stems = new RectTransform[17];
        private readonly Image[] _fills = new Image[17];
        private readonly TextMeshProUGUI[] _values = new TextMeshProUGUI[17];
        private readonly TextMeshProUGUI[] _rewards = new TextMeshProUGUI[17];
        private readonly float[] _expiry = new float[17];
        private readonly long[] _earned = new long[17];
        private readonly UITheme _theme;

        public PocketPresentation(UIFactory factory, Transform parent, UITheme theme)
        {
            _theme = theme;
            for (int i = 0; i < 17; i++)
            {
                _stems[i] = factory.Panel("Pocket guide " + i, parent, theme.Border);
                _chips[i] = factory.Panel("Pocket " + i, parent, theme.Elevated);
                _fills[i] = _chips[i].GetComponent<Image>();
                _values[i] = factory.Text("", _chips[i], 14, theme.Ink, true, TextAlignmentOptions.Center);
                _values[i].textWrappingMode = TextWrappingModes.NoWrap;
                _values[i].enableAutoSizing = true;
                _values[i].fontSizeMin = 10;
                UIFactory.Fill(_values[i].rectTransform);
                _rewards[i] = factory.Text("", parent, 16, theme.Mint, true, TextAlignmentOptions.Center);
            }
        }

        public void Reward(int pocket, long amount)
        {
            if (Time.unscaledTime > _expiry[pocket]) _earned[pocket] = 0;
            _earned[pocket] += amount;
            _expiry[pocket] = Time.unscaledTime + 1.15f;
        }

        public void Layout(GameSession session, Camera camera, RectTransform root, float scale, bool portrait)
        {
            int[] values = session.Config.Table(session.Rows, session.Profile);
            float unit = camera.pixelHeight / (2f * camera.orthographicSize) / scale;
            for (int i = 0; i < 17; i++)
            {
                bool visible = i <= session.Rows;
                _chips[i].gameObject.SetActive(visible);
                _stems[i].gameObject.SetActive(visible);
                _rewards[i].gameObject.SetActive(visible && !session.Reduced && Time.unscaledTime < _expiry[i]);
                if (!visible) continue;
                Vector3 point = camera.WorldToScreenPoint(new Vector3(i - session.Rows * 0.5f, 0.35f, 0f));
                Vector3 origin = camera.WorldToScreenPoint(new Vector3(i - session.Rows * 0.5f, 1.35f, 0f));
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root, point, null, out Vector2 local);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root, origin, null, out Vector2 start);
                float x = local.x - root.rect.xMin;
                float y = root.rect.yMax - local.y + (i % 2) * 29f;
                float width = Mathf.Min(70f, unit * 1.8f - 2f);
                UIFactory.Place(_chips[i], x - width * 0.5f, y, width, 26);
                _values[i].fontSize = portrait ? 12f : 15f;
                _values[i].fontSizeMax = portrait ? 12f : 15f;
                _values[i].text = (values[i] / 100f).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "×";
                bool flash = !session.Reduced && Time.unscaledTime < _expiry[i];
                Color accent = GameSession.Colors[session.Profile];
                _fills[i].color = flash ? accent : Color.Lerp(_theme.Elevated, accent, 0.15f);
                _values[i].color = flash ? _theme.Background : accent;
                float startY = root.rect.yMax - start.y;
                UIFactory.Place(_stems[i], x - 0.6f, startY, 1.2f, Mathf.Max(1f, y - startY));
                _stems[i].GetComponent<Image>().color = new Color(accent.r, accent.g, accent.b, 0.25f);
                _rewards[i].text = "+" + GameSession.Coins(_earned[i]);
                _rewards[i].fontSize = portrait ? 13f : 17f;
                float rise = (1.15f - (_expiry[i] - Time.unscaledTime)) * 14f;
                UIFactory.Place(_rewards[i], x - 44, startY - 28 - rise, 88, 26);
            }
        }
    }
}
