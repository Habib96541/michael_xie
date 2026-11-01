using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.UI;

namespace PegRush
{
    public sealed class GameUI : MonoBehaviour
    {
        [SerializeField] private Sprite _rounded;
        [SerializeField] private Font _font;
        private GameSession _session;
        private Button _homePlay;
        private SessionState _lastState = (SessionState)(-1);
        private Camera _camera;
        private RectTransform _root;
        private RectTransform _home;
        private RectTransform _game;
        private RectTransform _modal;
        private Text _wallet;
        private Text _stats;
        private Text _notice;
        private Text _history;
        private Text _rowLabel;
        private Text _amountLabel;
        private Text _profileLabel;
        private Button _minus;
        private Button _plus;
        private Button _profile;
        private Button _drop;
        private Text _mute;
        private Text _reduced;
        private readonly Text[] _multipliers = new Text[17];
        private readonly Image[] _pockets = new Image[17];
        private readonly float[] _flashes = new float[17];
        public void Wire(Sprite rounded, Font font)
        {
            _rounded = rounded;
            _font = font;
        }
        public void Initialize(GameSession session, Camera camera)
        {
            _session = session;
            _camera = camera;
            _root = GetComponent<RectTransform>();
            _home = Panel("Home", _root, Color.clear);
            _game = Panel("Game", _root, Color.clear);
            Label("PEGRUSH", _home, 64, new Color(0.25f, 1f, 0.78f), 0, 185, 590, 90);
            Label("FIND YOUR FLOW", _home, 18, Color.white, 0, 112, 500, 40);
            Label("A little bounce. A bright possibility.", _home, 20, new Color(0.57f, 0.79f, 0.86f), 0, 50, 600, 45);
            _homePlay = MakeButton("Play", _home, () => _session.Play(), 0, -45, 290, 74);
            MakeButton("Settings", _home, () => _session.OpenModal("settings"), 0, -125, 290, 74);
            MakeButton("How to Play", _home, () => _session.OpenModal("help"), 0, -200, 290, 74);
            Label("VIRTUAL COINS • PURE ARCADE", _home, 15, new Color(0.57f, 0.79f, 0.86f), 0, -295, 620, 30);
            _wallet = Label("", _game, 24, Color.white, 0, 0, 400, 45);
            _stats = Label("", _game, 16, new Color(0.6f, 0.83f, 0.89f), 0, 0, 550, 30);
            _notice = Label("", _game, 17, Color.white, 0, 0, 590, 52);
            _history = Label("", _game, 15, new Color(0.5f, 0.85f, 0.79f), 0, 0, 650, 45);
            Button home = MakeButton("Home", _game, () => _session.Home(), 0, 0, 90, 44);
            home.name = "HomeButton";
            Button settings = MakeButton("...", _game, () => _session.OpenModal("settings"), 0, 0, 60, 44);
            settings.name = "SettingsButton";
            _minus = MakeButton("-", _game, () => _session.ChangeRows(-1), 0, 0, 48, 48);
            _plus = MakeButton("+", _game, () => _session.ChangeRows(1), 0, 0, 48, 48);
            _rowLabel = Label("", _game, 19, Color.white, 0, 0, 130, 44);
            Button amount = MakeButton("", _game, () => _session.ChangeAmount(), 0, 0, 160, 50);
            amount.name = "Amount";
            _amountLabel = amount.GetComponentInChildren<Text>();
            _profile = MakeButton("", _game, () => _session.ChangeProfile(), 0, 0, 160, 50);
            _profileLabel = _profile.GetComponentInChildren<Text>();
            _drop = MakeButton("DROP", _game, () => _session.Drop(), 0, 0, 300, 60);
            Button refill = MakeButton("Free refill", _game, () => _session.Refill(), 0, 0, 135, 44);
            refill.name = "Refill";
            for (int i = 0; i < 17; i++)
            {
                RectTransform pocket = Panel("Multiplier " + i, _game, Color.white);
                _pockets[i] = pocket.GetComponent<Image>();
                _multipliers[i] = Label("", pocket, 15, new Color(0.02f, 0.1f, 0.15f), 0, 0, 40, 34);
            }
            _modal = Panel("Modal shield", _root, new Color(0.01f, 0.035f, 0.075f, 0.98f));
            _modal.GetComponent<Image>().sprite = null;
            HideModal();
        }
        private RectTransform Panel(string name, Transform parent, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            Image image = obj.GetComponent<Image>();
            image.sprite = _rounded;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = color.a > 0f;
            return obj.GetComponent<RectTransform>();
        }
        private Text Label(string value, Transform parent, int size, Color color, float x, float y, float width, float height)
        {
            GameObject obj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            Text text = obj.GetComponent<Text>();
            text.font = _font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            Place(obj.GetComponent<RectTransform>(), x, y, width, height);
            return text;
        }
        private Button MakeButton(string value, Transform parent, UnityAction action, float x, float y, float width, float height)
        {
            if (Screen.width < Screen.height * 0.95f)
            {
                height = Mathf.Max(height, 44f / Mathf.Min(Screen.width / 600f, Screen.height / 1000f));
            }
            RectTransform panel = Panel(value, parent, new Color(0.08f, 0.28f, 0.34f));
            Place(panel, x, y, width, height);
            Button button = panel.gameObject.AddComponent<Button>();
            button.targetGraphic = panel.GetComponent<Image>();
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(action);
            Text label = Label(value, panel, 20, Color.white, 0, 0, width - 8, height - 4);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.sizeDelta = new Vector2(-8, -4);
            return button;
        }
        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = rect.anchorMin;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }
        public void ShowModal(string mode)
        {
            foreach (Transform child in _modal) Destroy(child.gameObject);
            _modal.gameObject.SetActive(true);
            if (mode == "settings")
            {
                Label("SETTINGS", _modal, 32, Color.white, 0, 180, 560, 50);
                _mute = MakeButton("", _modal, () => _session.ToggleMute(), 0, 65, 320, 60).GetComponentInChildren<Text>();
                _reduced = MakeButton("", _modal, () => _session.ToggleReduced(), 0, -15, 320, 60).GetComponentInChildren<Text>();
                MakeButton("Back", _modal, () => _session.CloseModal(), 0, -140, 260, 60);
            }
            else if (mode == "leave")
            {
                Label("BALLS IN FLIGHT", _modal, 30, Color.white, 0, 140, 580, 50);
                Label("Stay to keep playing, or return home\nafter every active ball resolves.", _modal, 21, Color.white, 0, 45, 580, 110);
                MakeButton("Stay", _modal, () => _session.CloseModal(), 0, -75, 320, 60);
                MakeButton("Home when clear", _modal, () => _session.Drain(), 0, -155, 320, 60);
            }
            else
            {
                Label("HOW TO PLAY", _modal, 30, Color.white, 0, 240, 580, 50);
                Label("Start with 1,000 virtual coins.\nChoose an amount: 1, 5, 10, 25 or 50.\nTap Drop or press Space once per ball.\nUse Tab and Enter to navigate controls.\n\nRows change the board from 8 to 16.\nGreen is flatter; Gold varies; Red favors edges.\nProfiles change rewards and color, never physics.\nControls unlock when all balls finish.\n\nReward = cost × receiving pocket multiplier.\nUnresolved drops refund once on reopening.\nBelow 1 coin, use free refill on a clear board.\n\nSaves live in this browser only. Clearing site\ndata or private browsing may erase progress.", _modal, 19, Color.white, 0, -5, 610, 450);
                MakeButton("Got it", _modal, () => _session.CloseModal(), 0, -290, 260, 60);
            }
        }
        public void HideModal()
        {
            _modal.gameObject.SetActive(false);
        }
        public void Highlight(int pocket)
        {
            _flashes[pocket] = Time.unscaledTime + 0.5f;
        }
        public void Refresh()
        {
            GetComponent<CanvasScaler>().scaleFactor = Mathf.Min(Screen.width / 600f, Screen.height / 1000f);
            float width = _root.rect.width;
            float height = _root.rect.height;
            _home.sizeDelta = new Vector2(width, height);
            _game.sizeDelta = new Vector2(width, height);
            _modal.sizeDelta = new Vector2(width, height);
            _home.gameObject.SetActive(_session.State == SessionState.Home);
            _game.gameObject.SetActive(_session.State != SessionState.Home);
            _boardVisible(_session.State != SessionState.Home);
            _wallet.text = GameSession.Coins(_session.Balance) + "  COINS";
            _stats.text = _session.Active + " / 50 in flight    •    " + _session.Drops + " drops";
            _notice.text = _session.Notice;
            _history.text = _session.History;
            _rowLabel.text = "Rows  " + _session.Rows;
            _amountLabel.text = "Amount  " + _session.Amount;
            _profileLabel.text = GameSession.Profiles[_session.Profile] + "  >";
            bool unlocked = _session.Active == 0 && _session.State == SessionState.Playing;
            _minus.interactable = unlocked && _session.Rows > 8;
            _plus.interactable = unlocked && _session.Rows < 16;
            _profile.interactable = unlocked;
            _drop.interactable = _session.State == SessionState.Playing;
            _drop.image.color = GameSession.Colors[_session.Profile] * new Color(0.65f, 0.65f, 0.65f, 1f);
            if (_mute != null) _mute.text = "Sound: " + (_session.Muted ? "Muted" : "On");
            if (_reduced != null) _reduced.text = "Reduced effects: " + (_session.Reduced ? "On" : "Off");
            Layout(width, height);
            Navigate();
        }
        private void Navigate()
        {
            bool tab = Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame;
            if (_lastState == _session.State && !tab) return;
            Transform scope = _session.State == SessionState.Modal ? _modal : _session.State == SessionState.Home ? _home : _game;
            List<Button> choices = new List<Button>();
            foreach (Button button in scope.GetComponentsInChildren<Button>())
            {
                if (button.IsActive() && button.IsInteractable()) choices.Add(button);
            }
            if (choices.Count == 0) return;
            EventSystem events = EventSystem.current;
            if (_lastState != _session.State)
            {
                events.SetSelectedGameObject(choices[0].gameObject);
                _lastState = _session.State;
            }
            if (Keyboard.current == null || !Keyboard.current.tabKey.wasPressedThisFrame) return;
            int selected = choices.FindIndex(button => button.gameObject == events.currentSelectedGameObject);
            int direction = Keyboard.current.shiftKey.isPressed ? -1 : 1;
            events.SetSelectedGameObject(choices[(selected + direction + choices.Count) % choices.Count].gameObject);
        }
        private void _boardVisible(bool visible)
        {
            _camera.cullingMask = visible ? -1 : 0;
        }
        private void Layout(float width, float height)
        {
            float scale = width / Screen.width;
            Rect safe = Screen.safeArea;
            float top = height * 0.5f - (Screen.height - safe.yMax) * scale;
            float bottom = -height * 0.5f + safe.yMin * scale;
            bool portrait = width < height * 0.95f;
            float boardBottom = bottom + (portrait ? 330f : 210f);
            float boardTop = top - 110f;
            Place(_wallet.rectTransform, 0, top - 28, 360, 42);
            Place(_stats.rectTransform, 0, top - 70, Mathf.Min(width - 20, 550), 30);
            Place((RectTransform)_game.Find("HomeButton"), -width * 0.5f + 55, top - (portrait ? 42 : 30), 90, portrait ? 74 : 44);
            Place((RectTransform)_game.Find("SettingsButton"), width * 0.5f - 45, top - (portrait ? 42 : 30), portrait ? 74 : 60, portrait ? 74 : 44);
            float controls = bottom + (portrait ? 235 : 140);
            Place(_minus.GetComponent<RectTransform>(), -140, controls, portrait ? 74 : 48, portrait ? 74 : 48);
            Place(_rowLabel.rectTransform, -45, controls, 124, 44);
            Place(_plus.GetComponent<RectTransform>(), 45, controls, portrait ? 74 : 48, portrait ? 74 : 48);
            Place(_profile.GetComponent<RectTransform>(), portrait ? 165 : 230, controls, 160, portrait ? 74 : 50);
            Place((RectTransform)_game.Find("Amount"), portrait ? -165 : -300, portrait ? controls - 80 : controls, 170, portrait ? 74 : 50);
            Place(_drop.GetComponent<RectTransform>(), portrait ? 105 : 0, portrait ? controls - 80 : bottom + 70, portrait ? 280 : 300, portrait ? 74 : 60);
            Place((RectTransform)_game.Find("Refill"), portrait ? -170 : 300, bottom + 57, 135, portrait ? 74 : 44);
            Place(_notice.rectTransform, portrait ? 75 : 0, portrait ? bottom + 68 : bottom + 19, portrait ? width - 280 : width - 340, portrait ? 65 : 36);
            Place(_history.rectTransform, 0, portrait ? bottom + 13 : top - 96, width - 20, 32);
            float viewportHeight = Mathf.Max(100, boardTop - boardBottom);
            float viewportWidth = width - 50;
            _camera.rect = new Rect(25 / width, (boardBottom + height * 0.5f) / height, viewportWidth / width, viewportHeight / height);
            _camera.transform.position = new Vector3(0f, (_session.Rows + 2f) * 0.5f, -10f);
            _camera.orthographicSize = Mathf.Max((_session.Rows + 4f) * 0.5f, (_session.Rows + 2.5f) * 0.5f / (viewportWidth / viewportHeight));
            int[] table = _session.Config.Table(_session.Rows, _session.Profile);
            for (int i = 0; i < 17; i++)
            {
                bool visible = i <= _session.Rows;
                _pockets[i].gameObject.SetActive(visible);
                if (!visible) continue;
                Vector3 point = _camera.WorldToViewportPoint(new Vector3(i - _session.Rows * 0.5f, 0.35f, 0f));
                float x = 25 + point.x * viewportWidth - width * 0.5f;
                float y = boardBottom + point.y * viewportHeight;
                y -= (i % 2) * 35f;
                float cell = Mathf.Min(66f, viewportHeight / (2f * _camera.orthographicSize) * 1.8f - 2);
                Place(_pockets[i].rectTransform, x, y, cell, 32);
                Place(_multipliers[i].rectTransform, 0, 0, cell, 32);
                _multipliers[i].fontSize = 18;
                _multipliers[i].text = (table[i] / 100f).ToString("0.##") + "×";
                _pockets[i].color = !_session.Reduced && _flashes[i] > Time.unscaledTime ? Color.white : GameSession.Colors[_session.Profile];
            }
        }
    }
}









