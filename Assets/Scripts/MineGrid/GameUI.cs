using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MineGrid
{
    public sealed class GameUI : MonoBehaviour
    {
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("config")]
        private GameConfig _config;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("theme")]
        private UITheme _theme;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("compactBrand")]
        private GameObject _compactBrand;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("coinFeedback")]
        private CoinFeedback _coinFeedback;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("popupEmblem")]
        private Image _popupEmblem;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("popupPanel")]
        private RectTransform _popupPanel;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("milestoneFills")]
        private Image[] _milestoneFills;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("popupPresenter")]
        private PopupPresenter _popupPresenter;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("navigator")]
        private ScreenNavigator _navigator;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("walletView")]
        private WalletView _walletView;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("progressionView")]
        private ProgressionView _progressionView;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("audioService")]
        private AudioService _audioService;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("title")]
        private GameObject _title;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("home")]
        private GameObject _home;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("game")]
        private GameObject _game;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("popup")]
        private GameObject _popup;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("wheelPage")]
        private GameObject _wheelPage;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("progressionPage")]
        private GameObject _progressionPage;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("settingsControls")]
        private GameObject _settingsControls;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("balance")]
        private TMP_Text _balance;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("mineLabel")]
        private TMP_Text _mineLabel;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("scoreLabel")]
        private TMP_Text _scoreLabel;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("nextReward")]
        private TMP_Text _nextReward;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("status")]
        private TMP_Text _status;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("popupTitle")]
        private TMP_Text _popupTitle;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("popupBody")]
        private TMP_Text _popupBody;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("cooldown")]
        private TMP_Text _cooldown;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("progression")]
        private TMP_Text _progression;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("dailyLabel")]
        private TMP_Text _dailyLabel;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("mineSlider")]
        private Slider _mineSlider;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("start")]
        private Button _start;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("finish")]
        private Button _finish;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("back")]
        private Button _back;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("spin")]
        private Button _spin;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("claim")]
        private Button _claim;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("popupAction")]
        private Button _popupAction;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("popupSecondary")]
        private Button _popupSecondary;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("mute")]
        private Toggle _mute;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("reduced")]
        private Toggle _reduced;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("tiles")]
        private TileView[] _tiles;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("wheel")]
        private RectTransform _wheel;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("particles")]
        private RectTransform _particles;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("boardTransform")]
        private RectTransform _boardTransform;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("milestoneLabels")]
        private TMP_Text[] _milestoneLabels;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("milestoneButtons")]
        private Button[] _milestoneButtons;
        [SerializeField]
        private CosmeticCatalog _catalog;
        [SerializeField]
        private ShopView _shopView;
        [SerializeField]
        private CosmeticApplicator _cosmeticApplicator;
        [SerializeField]
        private GameObject _shopPage;
        private CosmeticService _cosmeticService;
        public CosmeticService Cosmetics => _cosmeticService;
        public ShopView ShopScreen => _shopView;
        public bool IsPopupOpen => _popupPresenter.IsOpen;
        public GameObject ShopPage => _shopPage;

        public void Shop()
        {
            if (_round.State == RoundState.Playing || _spinning || _ending || IsPopupOpen)
            {
                return;
            }

            ShowPage(_shopPage);
            _shopView.Refresh();
        }

        private void CosmeticsChanged() => RefreshWallet();
        private Round _round;
        private SaveService _saves;
        private readonly System.Random _random = new System.Random();
        private string _completedId;
        private bool _spinning;
        private bool _ending;
        private int _selected = 3;
        private int _displayedScore;
        private Coroutine _scoreRoutine;
        public bool ReducedEffects => _saves != null && _saves.Data.reducedEffects;
        public AudioService Audio => _audioService;
        public Round CurrentRound => _round;
        public SaveService Persistence => _saves;
        public Button StartControl => _start;
        public Button SpinControl => _spin;
        public Button FinishControl => _finish;
        public Slider MineSelector => _mineSlider;
        public TileView[] TileViews => _tiles;
        public GameObject PopupObject => _popup;
        public Button PopupPrimary => _popupAction;
        public Button PopupSecondary => _popupSecondary;
        public GameObject HomePage => _home;
        public GameObject ProgressionPage => _progressionPage;
        public GameObject SettingsControls => _settingsControls;
        public Toggle ReducedToggle => _reduced;
        public Toggle MuteToggle => _mute;
        public GameObject TitlePage => _title;

        public void ShowTitle()
        {
            ShowPage(_title);
        }

        private void Awake()
        {
            _saves = new SaveService();
            _round = new Round(_config);
            _audioService.Initialize(_saves.Data.muted);
            _mineSlider.onValueChanged.AddListener(SelectMines);
            _start.onClick.AddListener(StartRound);
            _finish.onClick.AddListener(FinishRound);
            _spin.onClick.AddListener(Spin);
            _claim.onClick.AddListener(Claim);
            _mute.onValueChanged.AddListener(SetMute);
            _reduced.onValueChanged.AddListener(SetReduced);
            _popupPresenter.Initialize(_popup, _settingsControls, _popupPanel, _popupEmblem, _popupTitle, _popupBody, _popupAction, _popupSecondary, _theme);
            _navigator.Initialize(new[] { _title, _home, _game, _wheelPage, _progressionPage, _shopPage }, _compactBrand, () => ReducedEffects);
            _walletView.Initialize(_balance, _coinFeedback);
            _progressionView.Initialize(_config, _saves, _progression, _milestoneLabels, _milestoneButtons, _milestoneFills, _claim, ClaimIndex);
            _cosmeticService = new CosmeticService(_saves, _catalog);
            _cosmeticApplicator.Initialize(_cosmeticService);
            _shopView.Initialize(this, _cosmeticService);
            _cosmeticService.Changed += CosmeticsChanged;
            _mute.SetIsOnWithoutNotify(_saves.Data.muted);
            _reduced.SetIsOnWithoutNotify(ReducedEffects);
            for (int i = 0; i < _tiles.Length; i++)
            {
                _tiles[i].Bind(i, Reveal);
            }

            _mineSlider.SetValueWithoutNotify(_saves.Data.selectedMines);
            SelectMines(_saves.Data.selectedMines);
            RefreshWallet();
            ShowPage(_title);
            _popup.SetActive(false);
            if (_saves.Data.pendingCoins > 0)
            {
                ShowPendingReward();
            }
        }

        public void Home()
        {
            if (_spinning || _ending)
            {
                return;
            }

            if (_round.State == RoundState.Playing)
            {
                ShowPopup("Leave this round?", $"Your {_round.Score:N0} unbanked coins will be forfeited.\nYour saved coins are safe.", "Keep playing", ClosePopup, "Leave round", () =>
                {
                    if (_round.Forfeit())
                    {
                        _saves.Bank(0, _round.Id);
                    }

                    ClosePopup();
                    ShowPage(_home);
                    RefreshWallet();
                });
                return;
            }

            ClosePopup();
            ShowPage(_home);
            RefreshWallet();
        }

        public void Play()
        {
            if (_round.State == RoundState.Playing || _spinning || _ending)
            {
                return;
            }

            ShowPage(_game);
            ClosePopup();
            Setup();
        }

        public void Wheel()
        {
            if (_round.State == RoundState.Playing || _spinning)
            {
                return;
            }

            ShowPage(_wheelPage);
            RefreshWallet();
            if (_saves.Data.pendingCoins > 0)
            {
                ShowPendingReward();
            }
        }

        public void Progression()
        {
            if (_round.State == RoundState.Playing || _spinning)
            {
                return;
            }

            ShowPage(_progressionPage);
            RefreshWallet();
        }

        private void ShowPage(GameObject page)
        {
            _navigator.Show(page);
        }

        private void SelectMines(float value)
        {
            if (_round.State == RoundState.Playing)
            {
                return;
            }

            _selected = Mathf.Clamp(Mathf.RoundToInt(value), 1, 24);
            _saves.Data.selectedMines = _selected;
            _saves.Save();
            _mineLabel.text = $"{_selected:00} mines  /  {25 - _selected:00} safe";
            _nextReward.text = $"Next gem  +{_config.Score(1, _selected):N0}";
        }

        private void Setup()
        {
            StopScore();
            _displayedScore = 0;
            _ending = false;
            _boardTransform.anchoredPosition = Vector2.zero;
            _round = new Round(_config);
            _mineSlider.interactable = true;
            _start.gameObject.SetActive(true);
            _finish.gameObject.SetActive(false);
            _back.interactable = true;
            _status.text = "Choose your risk. Find your fortune.";
            _scoreLabel.text = "0";
            SelectMines(_selected);
            foreach (var tile in _tiles)
            {
                tile.Show(false, false, false, false);
            }
        }

        private void StartRound()
        {
            if (_round.State != RoundState.Setup || _popup.activeSelf || _ending)
            {
                return;
            }

            _round.Start(_selected, _random);
            _mineSlider.interactable = false;
            _start.gameObject.SetActive(false);
            _finish.gameObject.SetActive(true);
            _back.interactable = true;
            RenderBoard(-1);
            _status.text = "Find gems • Finish anytime to bank";
        }

        private void Reveal(int index)
        {
            if (_popup.activeSelf || _ending || !_round.Reveal(index))
            {
                return;
            }

            _audioService.Play(_round.State == RoundState.Lost ? 1 : 0);
            RenderBoard(index);
            if (_round.State != RoundState.Playing)
            {
                EndRound();
            }
        }

        private void FinishRound()
        {
            if (_popup.activeSelf || _ending || !_round.Finish())
            {
                return;
            }

            RenderBoard(-1);
            EndRound();
        }

        private void RenderBoard(int changed)
        {
            bool playing = _round.State == RoundState.Playing;
            for (int i = 0; i < _tiles.Length; i++)
            {
                bool shown = _round.Board.IsRevealed(i) || (!playing && _round.State != RoundState.Lost);
                _tiles[i].Show(shown, _round.Board.IsMine(i), playing, i == changed, i == _round.TriggeredMine, !_round.Board.IsRevealed(i) && !playing);
            }

            UpdateScore(_round.Score);
            _status.text = $"{_round.Board.SafeRevealed} / {25 - _round.Board.MineCount} gems found";
        }

        private void StopScore()
        {
            if (_scoreRoutine != null)
            {
                StopCoroutine(_scoreRoutine);
            }

            _scoreRoutine = null;
        }

        private void UpdateScore(int target)
        {
            StopScore();
            _scoreRoutine = StartCoroutine(CountScore(target));
        }

        private IEnumerator CountScore(int target)
        {
            int from = _displayedScore;
            for (float t = 0; !ReducedEffects && t < .2f; t += Time.unscaledDeltaTime)
            {
                _displayedScore = Mathf.RoundToInt(Mathf.Lerp(from, target, t / .2f));
                _scoreLabel.text = $"{_displayedScore:N0}";
                yield return null;
            }

            _displayedScore = target;
            _scoreLabel.text = $"{target:N0}";
        }

        private void EndRound()
        {
            if (_completedId == _round.Id)
            {
                return;
            }

            _completedId = _round.Id;
            // Commit once immediately. Presentation never owns banking.
            _saves.Bank(_round.Banked, _round.Id);
            RefreshWallet();
            _ending = true;
            _finish.gameObject.SetActive(false);
            _back.interactable = false;
            StartCoroutine(PresentResult(_round));
        }

        private IEnumerator PresentResult(Round completed)
        {
            if (completed.State == RoundState.Lost)
            {
                if (!ReducedEffects)
                {
                    for (float t = 0; t < .2f; t += Time.unscaledDeltaTime)
                    {
                        _boardTransform.anchoredPosition = new Vector2(Mathf.Sin(t * 140) * 8, Mathf.Cos(t * 170) * 5);
                        yield return null;
                    }

                    _boardTransform.anchoredPosition = Vector2.zero;
                }

                for (int i = 0; i < _tiles.Length; i++)
                {
                    _tiles[i].Show(true, completed.Board.IsMine(i), false, false, i == completed.TriggeredMine, !completed.Board.IsRevealed(i));
                    if (!ReducedEffects)
                    {
                        yield return new WaitForSecondsRealtime(.018f);
                    }
                }
            }

            _ending = false;
            _back.interactable = true;
            string outcome = completed.State == RoundState.Lost ? "Mine hit!" : completed.State == RoundState.Won ? "Perfect grid!" : "Treasure banked!";
            ShowPopup(outcome, $"<size=40><color=#FFDA77>+{completed.Banked:N0}</color></size> coins\n{completed.Board.MineCount} mines • {completed.Board.SafeRevealed} gems", "Play again", Again, "Home", Home);
            if (completed.State != RoundState.Lost)
            {
                _audioService.Play(2);
                if (!ReducedEffects)
                {
                    StartCoroutine(Celebrate());
                }
            }
        }

        public void Again()
        {
            if (_ending || _round.State == RoundState.Playing)
            {
                return;
            }

            ClosePopup();
            if (_game.activeSelf)
            {
                Setup();
            }
        }

        public void ClosePopup()
        {
            _popupPresenter.Close();
        }

        public void HowToPlay() => ShowPopup("How to play", $"Choose 1–24 mines, then start.\nEach gem adds {_config.pointsPerGem} × mine count coins.\nWith 3 mines: 4 gems = {_config.Score(4, 3)} coins.\nFinish anytime to bank, even at zero.\nA mine loses the unbanked reward.\nFind all safe tiles to win automatically.\nThere is no entry fee. Coins are virtual.", "Got it", ClosePopup);
        public void Settings()
        {
            ShowPopup("Settings", "Make yourself comfortable.\nSettings save on this device.", "Done", ClosePopup);
            _settingsControls.SetActive(true);
            Place(_popupBody.rectTransform, .07f, .62f, .93f, .80f);
        }

        public void ShowPopup(string heading, string body, string action, Action callback, string secondary = null, Action secondaryCallback = null)
        {
            _popupPresenter.Show(heading, body, action, callback, secondary, secondaryCallback);
        }

        private static void Place(RectTransform r, float x0, float y0, float x1, float y1)
        {
            r.anchorMin = new Vector2(x0, y0);
            r.anchorMax = new Vector2(x1, y1);
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        private void SetMute(bool value)
        {
            _saves.Data.muted = value;
            _audioService.Mute(value);
            _saves.Save();
        }

        private void SetReduced(bool value)
        {
            _saves.Data.reducedEffects = value;
            _saves.Save();
        }

        public void RefreshWallet()
        {
            _walletView.Refresh(_saves.Data.coins);
            _progressionView.Refresh();
        }

        private void Claim() => ClaimIndex(_progressionView.NextMilestone);
        private void ClaimIndex(int index)
        {
            if (_saves.Claim(_config, index))
            {
                RefreshWallet();
                _audioService.Play(2);
                ShowPopup("Milestone unlocked!", $"+{_config.milestoneCoins[index]:N0} coins\nYour treasure keeps growing.", "Continue", ClosePopup);
                if (!ReducedEffects)
                {
                    StartCoroutine(Celebrate());
                }
            }
        }

        private void Update()
        {
            if (_saves == null)
            {
                return;
            }

            long left = _saves.SpinSecondsLeft;
            _cooldown.text = _saves.Data.pendingCoins > 0 ? "A reward is waiting to be claimed" : left == 0 ? "Your free daily spin is ready!" : $"Next spin in {left / 3600:00}:{left / 60 % 60:00}:{left % 60:00}";
            _dailyLabel.text = _saves.Data.pendingCoins > 0 ? "Claim treasure" : left == 0 ? "Daily • ready!" : "Daily treasure";
            _spin.interactable = left == 0 && !_spinning && _saves.Data.pendingCoins == 0;
        }

        private void Spin()
        {
            if (_popup.activeSelf || _spinning || !_saves.Spin(_config, _random, out int segment))
            {
                return;
            }

            _spinning = true;
            StartCoroutine(AnimateWheel(segment));
        }

        private IEnumerator AnimateWheel(int segment)
        {
            float from = _wheel.localEulerAngles.z;
            float target = from + 360f * 5 + Mathf.Repeat(segment * (360f / _config.wheelCoins.Length) - from, 360f);
            float duration = ReducedEffects ? .1f : 4;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                _wheel.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(from, target, 1 - Mathf.Pow(1 - t / duration, 4)));
                yield return null;
            }

            _wheel.localRotation = Quaternion.Euler(0, 0, target);
            _spinning = false;
            ShowPendingReward();
        }

        private void ShowPendingReward()
        {
            ShowPopup("Daily treasure!", $"{_saves.Data.pendingCoins:N0} coins are waiting for you.\nClaim your reward!", "Claim reward", () =>
            {
                if (_saves.ClaimPending())
                {
                    RefreshWallet();
                    _audioService.Play(2);
                    if (!ReducedEffects)
                    {
                        StartCoroutine(Celebrate());
                    }
                }

                ClosePopup();
            });
        }

        private IEnumerator Celebrate()
        {
            for (int i = 0; i < 24; i++)
            {
                var go = new GameObject("Sparkle", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_particles, false);
                var r = (RectTransform)go.transform;
                r.anchoredPosition = new Vector2(_random.Next(-260, 260), _random.Next(-300, 300));
                r.sizeDelta = new Vector2(9, 20);
                go.GetComponent<Image>().color = Color.HSVToRGB((float)_random.NextDouble(), .6f, 1);
                go.GetComponent<Image>().raycastTarget = false;
                Destroy(go, 1.5f);
            }

            for (float t = 0; t < 1.5f; t += Time.unscaledDeltaTime)
            {
                foreach (RectTransform child in _particles)
                {
                    child.anchoredPosition += Vector2.down * (150 * Time.unscaledDeltaTime);
                    child.Rotate(0, 0, 180 * Time.unscaledDeltaTime);
                }

                yield return null;
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                _saves?.Save();
            }
        }

        private void OnDestroy()
        {
            if (_cosmeticService != null)
            {
                _cosmeticService.Changed -= CosmeticsChanged;
            }

            _mineSlider.onValueChanged.RemoveListener(SelectMines);
            _start.onClick.RemoveListener(StartRound);
            _finish.onClick.RemoveListener(FinishRound);
            _spin.onClick.RemoveListener(Spin);
            _claim.onClick.RemoveListener(Claim);
            _mute.onValueChanged.RemoveListener(SetMute);
            _reduced.onValueChanged.RemoveListener(SetReduced);
        }
    }
}
