using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MineGrid
{
    public sealed class PopupPresenter : MonoBehaviour
    {
        private GameObject _overlay;
        private GameObject _settings;
        private RectTransform _panel;
        private Image _emblem;
        private Image _panelImage;
        private TMP_Text _title;
        private TMP_Text _body;
        private Button _primary;
        private Button _secondary;
        private TMP_Text _primaryLabel;
        private TMP_Text _secondaryLabel;
        private UITheme _theme;
        private Action _primaryAction;
        private Action _secondaryAction;
        public bool IsOpen => _overlay && _overlay.activeSelf;

        public void Initialize(GameObject overlay, GameObject settings, RectTransform panel, Image emblem, TMP_Text title, TMP_Text body, Button primary, Button secondary, UITheme theme)
        {
            _overlay = overlay;
            _settings = settings;
            _panel = panel;
            _emblem = emblem;
            _title = title;
            _body = body;
            _primary = primary;
            _secondary = secondary;
            _theme = theme;
            _panelImage = panel.GetComponent<Image>();
            _primaryLabel = primary.GetComponentInChildren<TMP_Text>();
            _secondaryLabel = secondary.GetComponentInChildren<TMP_Text>();
            _primary.onClick.AddListener(Primary);
            _secondary.onClick.AddListener(Secondary);
        }

        public void Show(string heading, string body, string action, Action callback, string secondary = null, Action secondaryCallback = null)
        {
            bool instructions = heading == "How to play", settings = heading == "Settings";
            Place(_panel, .075f, instructions ? .16f : .24f, .925f, instructions ? .86f : .80f);
            Place(_body.rectTransform, .065f, instructions ? .34f : .35f, .935f, instructions ? .80f : .59f);
            _emblem.gameObject.SetActive(!instructions && !settings);
            _emblem.sprite = heading == "Mine hit!" ? _theme.mine : heading == "Perfect grid!" ? _theme.gem : _theme.coin;
            _panelImage.color = heading == "Mine hit!" ? new Color(.25f, .12f, .23f) : _theme.panel;
            _title.text = heading;
            _body.text = body;
            _overlay.SetActive(true);
            _settings.SetActive(false);
            _primaryAction = callback;
            _secondaryAction = secondaryCallback;
            _primaryLabel.text = action;
            _secondary.gameObject.SetActive(secondary != null);
            if (secondary != null)
            {
                _secondaryLabel.text = secondary;
            }
        }

        public void Close()
        {
            _overlay.SetActive(false);
            _primaryAction = null;
            _secondaryAction = null;
        }

        private void Primary() => _primaryAction?.Invoke();
        private void Secondary() => _secondaryAction?.Invoke();
        private static void Place(RectTransform rect, float x0, float y0, float x1, float y1)
        {
            rect.anchorMin = new Vector2(x0, y0);
            rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private void OnDestroy()
        {
            if (_primary)
            {
                _primary.onClick.RemoveListener(Primary);
            }

            if (_secondary)
            {
                _secondary.onClick.RemoveListener(Secondary);
            }
        }
    }
}
