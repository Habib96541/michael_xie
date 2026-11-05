using System.Collections;
using System.Collections.Generic;
using CyberScratch.Audio;
using CyberScratch.Data;
using CyberScratch.Scratch;
using CyberScratch.UI;
using UnityEngine;

namespace CyberScratch.Core
{
    public class CyberScratchGameManager : MonoBehaviour
    {
        private static CyberScratchGameManager _instance;
        public static CyberScratchGameManager Instance => _instance;

        [Header("Configuration")]
        [SerializeField] private CyberScratchConfig _config;

        [Header("Screens & Views")]
        [SerializeField] private MainMenuView _mainMenuView;
        [SerializeField] private HowToPlayModalView _howToPlayModal;
        [SerializeField] private CanvasGroup _gameplayContainerGroup;
        [SerializeField] private ScratchCardController _scratchCard;
        [SerializeField] private RowView[] _rowViews = new RowView[5];
        [SerializeField] private ScratchHUDView _hudView;
        [SerializeField] private WinModalView _winModal;

        private const float RoundDuration = 90f;
        private const float AddFundsAmount = 250f;
        public const string BalancePrefKey = "CyberScratch_Balance";
        public const string WagerIndexPrefKey = "CyberScratch_WagerIndex";
        public const string MainMenuSceneName = "MainMenuScene";

        public void SavePlayerState()
        {
            PlayerPrefs.SetFloat(BalancePrefKey, _currentBalance);
            PlayerPrefs.SetInt(WagerIndexPrefKey, _selectedWagerIndex);
            PlayerPrefs.Save();
        }

        private float _currentBalance;
        private int _selectedWagerIndex = 1; // default $2
        private TicketModel _currentTicket;
        private bool _isInitialized;
        private bool _isRoundActive;
        private bool _timerRunning;
        private float _timeRemaining = RoundDuration;
        private int _displayedSeconds = 90;

        public float CurrentBalance => _currentBalance;
        public float CurrentWager => (_config != null && _config.WagerTiers.Length > _selectedWagerIndex)
            ? _config.WagerTiers[_selectedWagerIndex]
            : 2f;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }
            _instance = this;

            // Enforce portrait upright orientation
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
        }

        private void Start()
        {
            if (!_isInitialized)
            {
                InitializeGame();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }

            if (_scratchCard != null)
            {
                _scratchCard.OnCellRevealed -= HandleCellRevealed;
                _scratchCard.OnAllCellsRevealed -= HandleAllCellsRevealed;
            }
        }

        public void InitializeGame()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            if (_config == null)
            {
                _config = ScriptableObject.CreateInstance<CyberScratchConfig>();
            }
            _config.InitializeDefaultsIfNeeded();

            _currentBalance = PlayerPrefs.GetFloat(BalancePrefKey, _config.StartingBalance);
            _selectedWagerIndex = PlayerPrefs.GetInt(WagerIndexPrefKey, Mathf.Clamp(_config.DefaultWagerIndex, 0, _config.WagerTiers.Length - 1));
            _isRoundActive = false;
            _timerRunning = false;
            _timeRemaining = RoundDuration;

            // Setup Main Menu
            if (_mainMenuView != null)
            {
                _mainMenuView.Setup(
                    _currentBalance,
                    onPlay: ShowGameplayScreen,
                    onHowToPlay: ShowHowToPlayModal,
                    onResetBalance: RefillBalance
                );
            }

            // Setup HUD
            if (_hudView != null)
            {
                _hudView.Setup(
                    _currentBalance,
                    _config.WagerTiers,
                    _selectedWagerIndex,
                    onBuyTicket: OnBuyTicketClicked,
                    onRevealAll: OnRevealAllClicked,
                    onWagerSelected: OnWagerTierSelected,
                    onMenu: ShowMainMenuScreen,
                    onInfo: ShowHowToPlayModal,
                    onAddFunds: AddFunds
                );
                _hudView.UpdateBuyTicketCost(CurrentWager);
            }

            if (_scratchCard != null)
            {
                _scratchCard.OnCellRevealed -= HandleCellRevealed;
                _scratchCard.OnAllCellsRevealed -= HandleAllCellsRevealed;

                _scratchCard.OnCellRevealed += HandleCellRevealed;
                _scratchCard.OnAllCellsRevealed += HandleAllCellsRevealed;
            }

            if (_mainMenuView != null)
            {
                ShowMainMenuScreen();
            }
            else
            {
                ShowGameplayScreen();
            }
        }

        private void Update()
        {
            if (!_timerRunning || !_isRoundActive) return;
            if (_hudView != null && _hudView.IsSettingsOpen) return;
            if (_howToPlayModal != null && _howToPlayModal.IsVisible) return;
            if (_mainMenuView != null && _mainMenuView.IsVisible) return;

            _timeRemaining -= Time.unscaledDeltaTime;
            int seconds = Mathf.CeilToInt(Mathf.Max(0f, _timeRemaining));
            if (seconds != _displayedSeconds)
            {
                _displayedSeconds = seconds;
                _hudView?.SetTimer(seconds, seconds <= 10);
            }

            if (_timeRemaining <= 0f)
            {
                _timerRunning = false;
                _hudView?.SetStatus("TIME'S UP");
                OnRevealAllClicked();
            }
        }

        public void ShowMainMenuScreen()
        {
            SavePlayerState();
            CyberAudioService.Instance?.PlayButtonClick();

            if (_mainMenuView != null)
            {
                _mainMenuView.UpdateBalance(_currentBalance);
                _mainMenuView.UpdateSoundState();
                _mainMenuView.Show();

                if (_gameplayContainerGroup != null)
                {
                    _gameplayContainerGroup.alpha = 0f;
                    _gameplayContainerGroup.interactable = false;
                    _gameplayContainerGroup.blocksRaycasts = false;
                }
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(MainMenuSceneName);
            }
        }

        public void ShowGameplayScreen()
        {
            if (_mainMenuView != null)
            {
                _mainMenuView.Hide();
            }

            if (_gameplayContainerGroup != null)
            {
                _gameplayContainerGroup.alpha = 1f;
                _gameplayContainerGroup.interactable = true;
                _gameplayContainerGroup.blocksRaycasts = true;
            }

            if (!_isRoundActive)
            {
                StartNewCardRound();
            }
        }

        public void ShowHowToPlayModal()
        {
            if (_howToPlayModal != null)
            {
                _howToPlayModal.Show();
            }
        }

        public void RefillBalance()
        {
            _currentBalance = _config.StartingBalance;
            SavePlayerState();
            _mainMenuView?.UpdateBalance(_currentBalance);
            _hudView?.AnimateBalanceRollup(_currentBalance, 0.45f);
            _hudView?.SetStatus("BALANCE RESTORED");
        }

        public void AddFunds()
        {
            _currentBalance += AddFundsAmount;
            SavePlayerState();
            _mainMenuView?.UpdateBalance(_currentBalance);
            _hudView?.AnimateBalanceRollup(_currentBalance, 0.35f);
            _hudView?.SetStatus($"+${AddFundsAmount:0} ADDED");
        }

        private void OnWagerTierSelected(int index)
        {
            if (_isRoundActive) return;

            _selectedWagerIndex = index;
            SavePlayerState();
            float wager = CurrentWager;
            if (_hudView != null)
            {
                _hudView.UpdateBuyTicketCost(wager);
            }
        }

        private void OnBuyTicketClicked()
        {
            float wager = CurrentWager;
            if (_currentBalance < wager)
            {
                _hudView?.SetStatus("INSUFFICIENT BALANCE! REFILL IN MENU.");
                return;
            }

            StartNewCardRound();
        }

        private void StartNewCardRound()
        {
            float wager = CurrentWager;
            if (_currentBalance < wager)
            {
                _hudView?.SetStatus("INSUFFICIENT BALANCE! PLEASE REFILL.");
                return;
            }

            _currentBalance -= wager;
            SavePlayerState();
            _isRoundActive = true;
            _timeRemaining = RoundDuration;
            _displayedSeconds = Mathf.CeilToInt(RoundDuration);
            _timerRunning = true;

            if (_hudView != null)
            {
                _hudView.UpdateBalanceInstant(_currentBalance);
                _hudView.SetTimer(_displayedSeconds, false);
                _hudView.UpdateWinDisplay(0f);
                _hudView.SetGameplayControlsInteractable(canRevealAll: true, canBuyTicket: false);
                _hudView.SetWagerTiersInteractable(false);
                _hudView.SetStatus("DRAG ACROSS THE FOIL • MATCH 3 SYMBOLS TO WIN");
            }

            _mainMenuView?.UpdateBalance(_currentBalance);

            // Generate deterministic RNG ticket
            _currentTicket = new TicketModel();
            _currentTicket.Generate(wager, _config.Symbols, _config.RowWinChance, _config.NearMissRate);

            // Collect all 15 ScratchCellViews
            List<ScratchCellView> allCells = new List<ScratchCellView>(15);
            for (int r = 0; r < 5; r++)
            {
                if (r < _rowViews.Length && _rowViews[r] != null)
                {
                    _rowViews[r].ResetRow();
                    _rowViews[r].UpdateCellSymbols(_currentTicket, _config);
                    allCells.AddRange(_rowViews[r].Cells);
                }
            }

            // Precisely map cell UVs from actual visual rect coordinates
            if (_scratchCard != null)
            {
                Canvas.ForceUpdateCanvases();
                ScratchCellView[] cells = allCells.ToArray();
                _scratchCard.CalculateCellUVRects(cells);
                _scratchCard.ResetCard();
                if (Application.isPlaying)
                {
                    StartCoroutine(RefreshScratchLayoutNextFrame(cells));
                }
            }
        }

        private IEnumerator RefreshScratchLayoutNextFrame(ScratchCellView[] cells)
        {
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            if (_scratchCard != null && cells != null)
            {
                _scratchCard.CalculateCellUVRects(cells);
            }
        }

        private void OnRevealAllClicked()
        {
            if (!_isRoundActive || _scratchCard == null || _scratchCard.IsAutoRevealing) return;

            _hudView?.SetGameplayControlsInteractable(canRevealAll: false, canBuyTicket: false);
            _hudView?.SetStatus("REVEALING ALL CELLS...");
            _scratchCard.RevealAll(_config.RevealAllDuration);
        }

        private void HandleCellRevealed(int row, int col)
        {
            if (_currentTicket == null) return;

            _currentTicket.MarkRevealed(row, col);

            if (row >= 0 && row < _rowViews.Length && _rowViews[row] != null)
            {
                _rowViews[row].EvaluateRowProgress(_currentTicket);
            }

            float liveWinnings = _currentTicket.CalculateTotalWinnings();
            if (_hudView != null)
            {
                _hudView.UpdateWinDisplay(liveWinnings);
            }
        }

        private void HandleAllCellsRevealed()
        {
            if (!_isRoundActive) return;
            StartCoroutine(FinishRoundRoutine());
        }

        private IEnumerator FinishRoundRoutine()
        {
            yield return new WaitForSecondsRealtime(0.25f);

            _isRoundActive = false;
            _timerRunning = false;
            _timeRemaining = RoundDuration;
            _displayedSeconds = Mathf.CeilToInt(RoundDuration);
            _hudView?.SetTimer(_displayedSeconds, false);
            float totalWinnings = _currentTicket != null ? _currentTicket.CalculateTotalWinnings() : 0f;
            int winningRows = _currentTicket != null ? _currentTicket.GetWinningRowCount() : 0;

            if (totalWinnings > 0f)
            {
                _currentBalance += totalWinnings;
                SavePlayerState();

                if (_hudView != null)
                {
                    _hudView.AnimateBalanceRollup(_currentBalance, 1.2f);
                    _hudView.UpdateWinDisplay(totalWinnings);
                    _hudView.SetStatus($"WINNER! YOU WON ${totalWinnings:N2}");
                }

                _mainMenuView?.UpdateBalance(_currentBalance);

                if (_winModal != null)
                {
                    _winModal.Show(totalWinnings, winningRows, () =>
                    {
                        if (_hudView != null)
                        {
                            _hudView.SetGameplayControlsInteractable(canRevealAll: false, canBuyTicket: true);
                            _hudView.SetWagerTiersInteractable(true);
                            _hudView.SetStatus("BUY A NEW TICKET TO PLAY AGAIN!");
                        }
                    });
                }
                else
                {
                    if (_hudView != null)
                    {
                        _hudView.SetGameplayControlsInteractable(canRevealAll: false, canBuyTicket: true);
                        _hudView.SetWagerTiersInteractable(true);
                    }
                }
            }
            else
            {
                if (_hudView != null)
                {
                    _hudView.SetStatus("NO MATCHES THIS ROUND. TRY AGAIN!");
                    _hudView.SetGameplayControlsInteractable(canRevealAll: false, canBuyTicket: true);
                    _hudView.SetWagerTiersInteractable(true);
                }
            }
        }
    }
}
