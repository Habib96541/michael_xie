using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PegRush
{
    [Serializable]
    public sealed class UIControlSnapshot
    {
        public string Name;
        public float X;
        public float Y;
        public float Width;
        public float Height;
        public bool Interactable;
    }

    public sealed class GameUI : MonoBehaviour
    {
        [SerializeField] private Sprite _rounded;
        [SerializeField] private Font _font;
        [SerializeField] private UITheme _theme;
        private GameSession _session;
        private Camera _camera;
        private CanvasScaler _scaler;
        private RectTransform _safe;
        private RectTransform _home;
        private RectTransform _game;
        private RectTransform _modal;
        private RectTransform _boardCard;
        private RectTransform _controlsCard;
        private RectTransform _walletCard;
        private RectTransform _release;
        private UIFactory _factory;
        private HomePresentation _homeView;
        private ModalPresentation _modalView;
        private PocketPresentation _pockets;
        private TextMeshProUGUI _brand;
        private TextMeshProUGUI _wallet;
        private TextMeshProUGUI _walletCaption;
        private TextMeshProUGUI _boardTitle;
        private TextMeshProUGUI _boardInfo;
        private TextMeshProUGUI _flight;
        private TextMeshProUGUI _controlTitle;
        private TextMeshProUGUI _amountCaption;
        private TextMeshProUGUI _rowsCaption;
        private TextMeshProUGUI _rowsValue;
        private TextMeshProUGUI _profileCaption;
        private TextMeshProUGUI _lock;
        private TextMeshProUGUI _notice;
        private TextMeshProUGUI _history;
        private TextMeshProUGUI _sessionStats;
        private Button _homeButton;
        private Button _settings;
        private Button _amount;
        private Button _minus;
        private Button _plus;
        private Button _drop;
        private Button _refill;
        private readonly Button[] _profiles = new Button[3];
        private SessionState _lastState = (SessionState)(-1);
        private float _releasedUntil;
        private float _scale;
        private bool _portrait;
        private bool _homeUnderModal;
        private readonly Vector3[] _corners = new Vector3[4];
        private readonly List<Button> _navigation = new List<Button>();

        public void Wire(Sprite rounded, Font font)
        {
            _rounded = rounded;
            _font = font;
        }

        public void ApplyTheme(UITheme theme)
        {
            _theme = theme;
        }

        public void Initialize(GameSession session, Camera camera)
        {
            _session = session;
            _camera = camera;
            _scaler = GetComponent<CanvasScaler>();
            _factory = new UIFactory(_theme);
            _safe = _factory.Panel("Safe area", transform, Color.clear);
            _home = _factory.Panel("Home screen", _safe, Color.clear);
            _game = _factory.Panel("Gameplay screen", _safe, Color.clear);
            _homeView = new HomePresentation(_factory, _home, _theme, session);
            BuildGame();
            _modal = _factory.Panel("Modal shield", _safe, Color.clear);
            _modalView = new ModalPresentation(_factory, _theme, _modal, session);
            _modalView.Hide();
        }

        private void BuildGame()
        {
            _brand = _factory.Text("PEG<color=#4EEAAE>RUSH</color> /", _game, 27, _theme.Ink, true);
            _homeButton = _factory.Button("Home", "Home", _game, _session.Home);
            _settings = _factory.Button("Settings", "Settings", _game, () => _session.OpenModal("settings"));
            _walletCard = _factory.Panel("Wallet", _game, _theme.Surface);
            _walletCaption = _factory.Text("VIRTUAL COINS", _walletCard, 10, _theme.Muted, true);
            _wallet = _factory.Text("", _walletCard, 24, _theme.Ink, true);
            _boardCard = _factory.Panel("Board frame", _game, _theme.Border, true);
            _boardTitle = _factory.Text("THE BOARD", _game, 12, _theme.Muted, true);
            _boardInfo = _factory.Text("", _game, 15, _theme.Ink, true);
            _flight = _factory.Text("", _game, 13, _theme.Mint, true, TextAlignmentOptions.MidlineRight);
            _release = _factory.Panel("Release light", _game, _theme.Mint);
            _controlsCard = _factory.Panel("Drop controls", _game, _theme.Surface);
            _controlTitle = _factory.Text("Your next drop.", _controlsCard, 26, _theme.Ink, true);
            _amountCaption = _factory.Text("DROP AMOUNT", _controlsCard, 11, _theme.Muted, true);
            _amount = _factory.Button("Amount", "", _controlsCard, _session.ChangeAmount);
            _rowsCaption = _factory.Text("ROWS", _controlsCard, 11, _theme.Muted, true);
            _minus = _factory.Button("RowsMinus", "−", _controlsCard, () => _session.ChangeRows(-1));
            _plus = _factory.Button("RowsPlus", "+", _controlsCard, () => _session.ChangeRows(1));
            _rowsValue = _factory.Text("", _controlsCard, 21, _theme.Ink, true, TextAlignmentOptions.Center);
            _profileCaption = _factory.Text("REWARD PROFILE", _controlsCard, 11, _theme.Muted, true);
            for (int i = 0; i < 3; i++)
            {
                int profile = i;
                _profiles[i] = _factory.Button("Profile" + GameSession.Profiles[i], GameSession.Profiles[i], _controlsCard, () => _session.SetProfile(profile));
            }
            _lock = _factory.Text("", _controlsCard, 12, _theme.Muted);
            _drop = _factory.Button("Drop", "", _controlsCard, _session.Drop, true);
            _sessionStats = _factory.Text("", _controlsCard, 12, _theme.Muted, false, TextAlignmentOptions.Center);
            _refill = _factory.Button("FreeRefill", "Free refill", _game, _session.Refill);
            _notice = _factory.Text("", _game, 14, _theme.Ink);
            _history = _factory.Text("", _game, 13, _theme.Muted);
            _pockets = new PocketPresentation(_factory, _game, _theme);
        }

        public void Highlight(int pocket)
        {
            _pockets.Reward(pocket, 0);
        }

        public void ShowReward(int pocket, long amount)
        {
            _pockets.Reward(pocket, amount);
        }

        public void Released()
        {
            _releasedUntil = Time.unscaledTime + 0.2f;
        }

        public void ShowModal(string mode)
        {
            _homeUnderModal = _lastState == SessionState.Home;
            _modalView.Show(mode);
        }

        public void HideModal()
        {
            _modalView.Hide();
        }

        public void Refresh()
        {
            _portrait = Screen.width < Screen.height * 0.95f;
            _scale = _portrait ? Mathf.Min(Screen.width / 400f, Screen.height / 900f) : Mathf.Min(Screen.width / 1440f, Screen.height / 900f);
            _scaler.scaleFactor = Mathf.Max(0.1f, _scale);
            Rect safe = Screen.safeArea;
            RectTransform canvas = GetComponent<RectTransform>();
            UIFactory.Place(_safe, safe.xMin / _scale, (Screen.height - safe.yMax) / _scale, safe.width / _scale, safe.height / _scale);
            float width = safe.width / _scale;
            float height = safe.height / _scale;
            UIFactory.Fill(_home);
            UIFactory.Fill(_game);
            UIFactory.Fill(_modal);
            bool home = _session.State == SessionState.Home || (_session.State == SessionState.Modal && _homeUnderModal);
            _home.gameObject.SetActive(home);
            _game.gameObject.SetActive(!home);
            _camera.enabled = !home;
            _camera.cullingMask = home ? 0 : -1;
            if (home) _homeView.Layout(width, height, _portrait);
            else LayoutGame(width, height, safe);
            _modalView.Refresh(width, height);
            Navigate();
        }

        private void LayoutGame(float width, float height, Rect safe)
        {
            float margin = _portrait ? 12 : 36;
            float header = _portrait ? 108 : 104;
            UIFactory.Place(_brand, margin + 4, 15, _portrait ? 170 : 260, 45);
            _brand.fontSize = _portrait ? 24 : 32;
            UIFactory.Place(_walletCard, _portrait ? width - 162 : width - 476, 12, _portrait ? 150 : 208, 58);
            UIFactory.Place(_walletCaption, 14, 8, 170, 14);
            UIFactory.Place(_wallet, 14, 23, 170, 30);
            _wallet.fontSize = _portrait ? 21 : 25;
            _wallet.text = GameSession.Coins(_session.Balance);
            _homeButton.gameObject.SetActive(!_portrait);
            _settings.gameObject.SetActive(!_portrait);
            UIFactory.Place(_homeButton, width - 252, 18, 94, 48);
            UIFactory.Place(_settings, width - 146, 18, 110, 48);
            float controlsHeight = _portrait ? 256 : height - header - 110;
            float controlsWidth = _portrait ? width - margin * 2 : 324;
            float boardWidth = _portrait ? width - margin * 2 : width - controlsWidth - margin * 3;
            float boardHeight = _portrait ? height - header - controlsHeight - 99 : height - header - 92;
            Rect board = new Rect(margin, header, boardWidth, boardHeight);
            UIFactory.Place(_boardCard, board.x, board.y, board.width, board.height);
            UIFactory.Place(_boardTitle, board.x + 16, board.y + 14, 160, 20);
            UIFactory.Place(_boardInfo, board.x + 16, board.y + 37, 210, 23);
            _boardInfo.text = _session.Rows + " rows  /  " + GameSession.Profiles[_session.Profile];
            _boardTitle.text = _portrait ? "THE BOARD / " + _session.Drops + " DROPS" : "THE BOARD";
            UIFactory.Place(_flight, board.x + board.width - 160, board.y + 15, 142, 24);
            _flight.text = _session.Active + " / 50 in flight";
            Rect viewport = new Rect(board.x + 8, board.y + 64, board.width - 16, Mathf.Max(100, board.height - 88));
            _camera.rect = new Rect((safe.xMin + viewport.x * _scale) / Screen.width, (safe.yMax - (viewport.y + viewport.height) * _scale) / Screen.height, viewport.width * _scale / Screen.width, viewport.height * _scale / Screen.height);
            float top = _session.Config.SpawnY(_session.Rows) + 0.55f;
            const float bottom = -1.9f;
            _camera.transform.position = new Vector3(0, (top + bottom) * 0.5f, -10);
            _camera.orthographicSize = Mathf.Max((top - bottom) * 0.5f, (_session.Rows + 2.2f) * 0.5f / (viewport.width / viewport.height));
            Vector3 spawn = _camera.WorldToScreenPoint(new Vector3(0, _session.Config.SpawnY(_session.Rows) + 0.15f));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_game, spawn, null, out Vector2 local);
            UIFactory.Place(_release, local.x - _game.rect.xMin - 12, _game.rect.yMax - local.y - 2, 24, 4);
            _release.GetComponent<Image>().color = !_session.Reduced && Time.unscaledTime < _releasedUntil ? Color.white : _theme.Mint;
            _pockets.Layout(_session, _camera, _game, _scale, _portrait);
            float controlsX = _portrait ? margin : width - margin - controlsWidth;
            float controlsY = _portrait ? header + boardHeight + 10 : header;
            UIFactory.Place(_controlsCard, controlsX, controlsY, controlsWidth, controlsHeight);
            LayoutControls(controlsWidth, controlsHeight);
            if (_portrait)
            {
                bool needsRefill = _session.Balance < 100 && _session.Active == 0;
                UIFactory.Place(_notice, margin + 4, height - 75, needsRefill ? width - 156 : width - 2 * margin - 4, 34);
                _notice.fontSize = 12;
                UIFactory.Place(_history, margin + 4, height - 38, needsRefill ? width - 156 : width - 2 * margin - 48, 24);
                _history.fontSize = 11;
                _refill.gameObject.SetActive(needsRefill);
                UIFactory.Place(_refill, width - 126, height - 76, 114, 46);
            }
            else
            {
                UIFactory.Place(_notice, margin + 4, height - 70, boardWidth - 8, 32);
                UIFactory.Place(_history, margin + 4, height - 35, boardWidth - 8, 24);
                _refill.gameObject.SetActive(true);
                UIFactory.Place(_refill, controlsX, header + controlsHeight + 16, controlsWidth, 48);
            }
            _notice.text = _session.Notice;
            _history.text = _session.History.Length == 0 ? "RECENT DROPS / Your story starts here." : "RECENT / " + (_portrait ? _session.CompactHistory : _session.History);
        }

        private void LayoutControls(float width, float height)
        {
            bool unlocked = _session.Active == 0 && _session.State == SessionState.Playing;
            _minus.interactable = unlocked && _session.Rows > 8;
            _plus.interactable = unlocked && _session.Rows < 16;
            _drop.interactable = _session.State == SessionState.Playing;
            _rowsValue.text = _session.Rows.ToString();
            _amount.GetComponentInChildren<TextMeshProUGUI>().text = _session.Amount + " coins  →";
            _lock.text = _session.Active > 0 ? "Rows and profiles unlock when the board clears." : "Profiles change rewards, never physics.";
            _sessionStats.text = _session.Drops + " drops this session  ·  Virtual coins only";
            _drop.GetComponentInChildren<TextMeshProUGUI>().text = "Drop ball  ↓\n<size=12>" + _session.Amount + " coins  ·  Space</size>";
            _drop.image.color = GameSession.Colors[_session.Profile];
            _controlTitle.gameObject.SetActive(!_portrait);
            float pad = 20;
            if (_portrait)
            {
                UIFactory.Place(_amountCaption, pad, 10, 160, 18);
                UIFactory.Place(_rowsCaption, width - 180, 10, 160, 18);
                UIFactory.Place(_amount, pad, 33, width - 220, 48);
                UIFactory.Place(_minus, width - 180, 33, 50, 48);
                UIFactory.Place(_rowsValue, width - 128, 33, 64, 48);
                UIFactory.Place(_plus, width - 60, 33, 50, 48);
                UIFactory.Place(_profileCaption, pad, 91, 200, 18);
                float cell = (width - pad * 2 - 16) / 3;
                for (int i = 0; i < 3; i++) UIFactory.Place(_profiles[i], pad + i * (cell + 8), 115, cell, 48);
                UIFactory.Place(_lock, pad, 169, width - pad * 2, 24);
                _lock.fontSize = 11;
                UIFactory.Place(_drop, pad, 197, width - pad * 2, 50);
                _sessionStats.gameObject.SetActive(false);
                // Compact nav stays alongside the brand, above the board.
                _homeButton.gameObject.SetActive(true);
                _settings.gameObject.SetActive(true);
                UIFactory.Place(_homeButton, 16, 52, 62, 50);
                UIFactory.Place(_settings, 84, 52, 80, 50);
                _homeButton.GetComponentInChildren<TextMeshProUGUI>().fontSize = 13;
                _settings.GetComponentInChildren<TextMeshProUGUI>().fontSize = 13;
            }
            else
            {
                UIFactory.Place(_controlTitle, pad, 24, width - 40, 48);
                UIFactory.Place(_amountCaption, pad, 98, width - 40, 22);
                UIFactory.Place(_amount, pad, 127, width - 40, 54);
                UIFactory.Place(_rowsCaption, pad, 207, width - 40, 22);
                UIFactory.Place(_minus, pad, 238, 54, 50);
                UIFactory.Place(_rowsValue, 84, 238, width - 168, 50);
                UIFactory.Place(_plus, width - 74, 238, 54, 50);
                UIFactory.Place(_profileCaption, pad, 315, width - 40, 22);
                float cell = (width - 56) / 3;
                for (int i = 0; i < 3; i++) UIFactory.Place(_profiles[i], pad + i * (cell + 8), 348, cell, 50);
                UIFactory.Place(_lock, pad, 412, width - 40, 52);
                _lock.fontSize = 13;
                UIFactory.Place(_drop, pad, height - 136, width - 40, 72);
                _sessionStats.gameObject.SetActive(true);
                UIFactory.Place(_sessionStats, pad, height - 50, width - 40, 32);
                _homeButton.GetComponentInChildren<TextMeshProUGUI>().fontSize = 16;
                _settings.GetComponentInChildren<TextMeshProUGUI>().fontSize = 16;
            }
            for (int i = 0; i < 3; i++)
            {
                _profiles[i].interactable = unlocked;
                bool selected = i == _session.Profile;
                _profiles[i].image.color = selected ? GameSession.Colors[i] : _theme.Elevated;
                _profiles[i].GetComponentInChildren<TextMeshProUGUI>().color = selected ? _theme.Background : GameSession.Colors[i];
                _profiles[i].GetComponentInChildren<TextMeshProUGUI>().fontSize = _portrait ? 13 : 14;
            }
        }

        private void Navigate()
        {
            bool tab = Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame;
            if (_lastState == _session.State && !tab) return;
            Transform scope = _session.State == SessionState.Modal ? _modal : _session.State == SessionState.Home ? _home : _game;
            _navigation.Clear();
            _factory.Buttons.RemoveAll(button => button == null);
            foreach (Button button in _factory.Buttons)
            {
                if (button.transform.IsChildOf(scope) && button.IsActive() && button.IsInteractable()) _navigation.Add(button);
            }
            if (_navigation.Count == 0) return;
            EventSystem events = EventSystem.current;
            if (_lastState != _session.State)
            {
                events.SetSelectedGameObject(_navigation[0].gameObject);
                _lastState = _session.State;
            }
            if (!tab) return;
            int current = _navigation.FindIndex(button => button.gameObject == events.currentSelectedGameObject);
            int direction = Keyboard.current.shiftKey.isPressed ? -1 : 1;
            events.SetSelectedGameObject(_navigation[(current + direction + _navigation.Count) % _navigation.Count].gameObject);
        }

        public UIControlSnapshot[] Controls()
        {
            List<UIControlSnapshot> result = new List<UIControlSnapshot>();
            foreach (Button button in _factory.Buttons)
            {
                if (button == null || !button.IsActive()) continue;
                if (_session.State == SessionState.Modal && !button.transform.IsChildOf(_modal)) continue;
                ((RectTransform)button.transform).GetWorldCorners(_corners);
                result.Add(new UIControlSnapshot
                {
                    Name = button.name,
                    X = (_corners[0].x + _corners[2].x) * 0.5f,
                    Y = Screen.height - (_corners[0].y + _corners[2].y) * 0.5f,
                    Width = _corners[2].x - _corners[0].x,
                    Height = _corners[2].y - _corners[0].y,
                    Interactable = button.interactable
                });
            }
            return result.ToArray();
        }
    }
}
