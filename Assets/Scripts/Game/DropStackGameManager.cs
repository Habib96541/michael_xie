using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DropStack.Data;
using DropStack.Audio;
using DropStack.Rendering;
using DropStack.UI;

namespace DropStack.Game
{
    public enum GameState
    {
        StartMenu,
        Playing,
        Paused,
        GameOver
    }

    public class DropStackGameManager : MonoBehaviour
    {
        public static DropStackGameManager Instance { get; private set; }

        [Header("Systems")]
        [SerializeField] private DropStackBoard board;
        [SerializeField] private ActivePieceController pieceController;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private SpeedCurveManager speedManager;
        [SerializeField] private BoardRenderer boardRenderer;
        [SerializeField] private ActivePieceRenderer activePieceRenderer;

        // Game Flow State
        public GameState CurrentState { get; private set; } = GameState.StartMenu;
        public float TimeRemaining { get; private set; } = 90.0f;
        public float ElapsedTime => SpeedCurveManager.TotalGameDuration - TimeRemaining;

        // Piece Pipeline
        private readonly BagRandomizer bagRandomizer = new();
        private readonly List<PolyominoType> nextQueue = new();
        private const int NextQueueSize = 3;

        public PolyominoType HoldPiece { get; private set; } = PolyominoType.None;
        public IReadOnlyList<PolyominoType> NextQueue => nextQueue;
        public bool CanHold { get; private set; } = true;

        private int lastWarningSecond = -1;
        private bool targetAwarded;
        public GameDifficulty Difficulty { get; private set; } = GameDifficulty.Neutral;
        public int TargetLines => DifficultyRules.Target(Difficulty);
        public event Action<GameDifficulty> OnDifficultyChanged;
        public bool SetDifficulty(GameDifficulty mode)
        {
            if (CurrentState != GameState.StartMenu || !Enum.IsDefined(typeof(GameDifficulty),mode)) return false;
            Difficulty=mode;
            PlayerPrefs.SetInt("DropStack_Difficulty",(int)mode); PlayerPrefs.Save();
            speedManager.SetDifficulty(mode); pieceController.SetDifficulty(mode); scoreManager.SetDifficulty(mode); scoreManager.ResetScore();
            bagRandomizer.Reset(DifficultyRules.Pieces(mode));
            nextQueue.Clear(); for(int i=0;i<NextQueueSize;i++) nextQueue.Add(bagRandomizer.Next());
            OnNextQueueChanged?.Invoke(nextQueue); OnDifficultyChanged?.Invoke(mode);
            return true;
        }

        public void ReturnToMenu()
        {
            scoreManager.SaveHighScore();
            InitializeGame();
        }

        // Events
        public event Action<GameState> OnGameStateChanged;
        public event Action<float> OnTimerUpdated; // timeRemaining
        public event Action<PolyominoType> OnHoldChanged;
        public event Action<IReadOnlyList<PolyominoType>> OnNextQueueChanged;
        public event Action<string, bool> OnGameOver; // reason, isNewHighScore
        public event Action<string> OnSpeedBannerTriggered; // message

        private void Awake()
        {
            Application.targetFrameRate = 60;
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void Start()
        {
            if (pieceController != null)
            {
                pieceController.OnPieceLocked += HandlePieceLocked;
                pieceController.OnHoldRequested += HandleHoldRequested;
                pieceController.OnHardDropExecuted += HandleHardDropExecuted;
            }

            if (board != null)
            {
                board.OnBoardChanged += HandleBoardChanged;
            }

            if (speedManager != null)
            {
                speedManager.OnSpeedLevelChanged += HandleSpeedLevelChanged;
            }

            InitializeGame();
        }

        private void OnDestroy()
        {
            if (pieceController != null)
            {
                pieceController.OnPieceLocked -= HandlePieceLocked;
                pieceController.OnHoldRequested -= HandleHoldRequested;
                pieceController.OnHardDropExecuted -= HandleHardDropExecuted;
            }

            if (board != null)
            {
                board.OnBoardChanged -= HandleBoardChanged;
            }

            if (speedManager != null)
            {
                speedManager.OnSpeedLevelChanged -= HandleSpeedLevelChanged;
            }
        }

        public void InitializeGame()
        {
            StopAllCoroutines();
            Time.timeScale = 1f;
            pieceController.SetInputEnabled(false);
            pieceController.ClearActivePiece();
            CurrentState = GameState.StartMenu;
            targetAwarded=false;
            JuiceEffects.Instance?.ResetForRun();
            SetDifficulty((GameDifficulty)Mathf.Clamp(PlayerPrefs.GetInt("DropStack_Difficulty",1),0,2));
            TimeRemaining = SpeedCurveManager.TotalGameDuration;
            HoldPiece = PolyominoType.None;
            CanHold = true;
            lastWarningSecond = -1;

            board.ClearMatrix();
            boardRenderer.Initialize(DropStackBoard.Columns, DropStackBoard.VisibleRows, DropStackBoard.TotalRows);
            boardRenderer.RefreshBoard(DropStackBoard.Columns, DropStackBoard.TotalRows, board.GetCell);

            scoreManager.ResetScore();
            speedManager.ResetSpeed();

            // Populate next queue
            nextQueue.Clear();
            for (int i = 0; i < NextQueueSize; i++)
            {
                nextQueue.Add(bagRandomizer.Next());
            }

            OnHoldChanged?.Invoke(HoldPiece);
            OnNextQueueChanged?.Invoke(nextQueue);
            OnTimerUpdated?.Invoke(TimeRemaining);
            OnGameStateChanged?.Invoke(CurrentState);
        }

        public void StartGame()
        {
            scoreManager.SaveHighScore();
            StopAllCoroutines();
            Time.timeScale = 1f;
            pieceController.ClearActivePiece();
            activePieceRenderer.HideAll();
            CurrentState = GameState.StartMenu;
            targetAwarded=false;
            JuiceEffects.Instance?.ResetForRun();
            bagRandomizer.Reset(DifficultyRules.Pieces(Difficulty));
            TimeRemaining = SpeedCurveManager.TotalGameDuration;
            HoldPiece = PolyominoType.None;
            CanHold = true;
            lastWarningSecond = -1;

            board.ClearMatrix();
            boardRenderer.RefreshBoard(DropStackBoard.Columns, DropStackBoard.TotalRows, board.GetCell);
            scoreManager.ResetScore();
            speedManager.ResetSpeed();

            nextQueue.Clear();
            for (int i = 0; i < NextQueueSize; i++)
            {
                nextQueue.Add(bagRandomizer.Next());
            }

            OnHoldChanged?.Invoke(HoldPiece);
            OnNextQueueChanged?.Invoke(nextQueue);

            CurrentState = GameState.Playing;
            pieceController.SetInputEnabled(true);
            OnTimerUpdated?.Invoke(TimeRemaining);
            OnGameStateChanged?.Invoke(CurrentState);

            SpawnNextPiece();
        }

        public void TogglePause()
        {
            if (CurrentState == GameState.Playing)
            {
                CurrentState = GameState.Paused;
                ArcadeFeedback.Cancel();
                pieceController.SetInputEnabled(false);
                Time.timeScale = 0f;
                OnGameStateChanged?.Invoke(CurrentState);
            }
            else if (CurrentState == GameState.Paused)
            {
                CurrentState = GameState.Playing;
                pieceController.SetInputEnabled(true);
                Time.timeScale = 1f;
                OnGameStateChanged?.Invoke(CurrentState);
            }
        }

        public void ResumeGame()
        {
            if (CurrentState == GameState.Paused)
            {
                CurrentState = GameState.Playing;
                pieceController.SetInputEnabled(true);
                Time.timeScale = 1f;
                OnGameStateChanged?.Invoke(CurrentState);
            }
        }

        private void Update()
        {
            if (CurrentState != GameState.Playing) return;

            // Update strict 90-second countdown
            TimeRemaining -= Time.deltaTime;
            if (TimeRemaining <= 0f)
            {
                TimeRemaining = 0f;
                OnTimerUpdated?.Invoke(TimeRemaining);
                TriggerGameOver("TIME'S UP!");
                return;
            }

            OnTimerUpdated?.Invoke(TimeRemaining);
            speedManager.UpdateTime(ElapsedTime);

            // Warning ticks during the final 10 seconds
            int currentSec = Mathf.CeilToInt(TimeRemaining);
            if (currentSec <= 10 && currentSec != lastWarningSecond)
            {
                lastWarningSecond = currentSec;
                DropStackAudioManager.Instance?.PlayWarningTick();
                if (currentSec % 3 == 0) ArcadeFeedback.Pulse(20);
            }
        }

        #region Piece Spawning & Queue

        private void SpawnNextPiece()
        {
            if (CurrentState != GameState.Playing) return;

            // Pull first shape from queue
            PolyominoType nextType = nextQueue[0];
            nextQueue.RemoveAt(0);
            nextQueue.Add(bagRandomizer.Next());
            OnNextQueueChanged?.Invoke(nextQueue);

            PolyominoShape shape = PolyominoDatabase.GetShape(nextType);
            bool success = pieceController.SpawnPiece(shape);

            if (!success)
            {
                TriggerGameOver("BOARD TOPPED OUT!");
            }
        }

        private void HandleHoldRequested()
        {
            if (!CanHold || CurrentState != GameState.Playing || !pieceController.HasActivePiece) return;

            CanHold = false;
            PolyominoType currentType = pieceController.CurrentShape.type;
            pieceController.ClearActivePiece();

            DropStackAudioManager.Instance?.PlayMove();

            if (HoldPiece == PolyominoType.None)
            {
                HoldPiece = currentType;
                OnHoldChanged?.Invoke(HoldPiece);
                SpawnNextPiece();
            }
            else
            {
                PolyominoType swapType = HoldPiece;
                HoldPiece = currentType;
                OnHoldChanged?.Invoke(HoldPiece);

                PolyominoShape shapeToSpawn = PolyominoDatabase.GetShape(swapType);
                bool success = pieceController.SpawnPiece(shapeToSpawn);
                if (!success)
                {
                    TriggerGameOver("BOARD TOPPED OUT!");
                }
            }
        }

        private void HandleHardDropExecuted(int cells)
        {
            if (boardRenderer != null && pieceController != null)
            {
                Vector3 landingPos = boardRenderer.GridToWorld(pieceController.BoardPosition.x, pieceController.BoardPosition.y);
                JuiceEffects.Instance?.PlayHardDropEffect(landingPos);
                ArcadeFeedback.Pulse(24);
            }
        }

        private void HandlePieceLocked(PolyominoShape lockedShape, Vector2Int lockedPos, int lockedRot)
        {
            CanHold = true;

            // Check full lines
            List<int> fullLines = board.FindFullLines();

            if (fullLines.Count > 0)
            {
                StartCoroutine(ProcessLineClearRoutine(fullLines));
            }
            else
            {
                scoreManager.RegisterPieceLockedNoClear();

                // Check if stacked beyond visible rows
                if (board.HasBlocksAboveCeiling())
                {
                    TriggerGameOver("BOARD TOPPED OUT!");
                    return;
                }

                SpawnNextPiece();
            }
        }

        private IEnumerator ProcessLineClearRoutine(List<int> lines)
        {
            int lineCount = lines.Count;

            // Audio & Juice
            DropStackAudioManager.Instance?.PlayLineClear(lineCount);

            float avgY = 0f;
            foreach (var y in lines) avgY += y;
            avgY /= lines.Count;

            Vector3 centerPos = boardRenderer.GridToWorld(DropStackBoard.Columns / 2, Mathf.RoundToInt(avgY));
            JuiceEffects.Instance?.PlayLineClearEffect(centerPos, lineCount);
            ArcadeFeedback.Pulse(lineCount >= 3 ? 65 : 35);

            // Animate line flashes on renderer
            boardRenderer.PlayLineClearFlash(lines, DropStackBoard.Columns);

            // Clear matrix in board
            yield return StartCoroutine(board.ClearLinesCoroutine(lines, null));

            // Register score
            bool isPerfect = board.IsCompletelyEmpty();
            scoreManager.RegisterLineClear(lineCount, isPerfect);

            if (scoreManager.CurrentStreak >= 2)
            {
                DropStackAudioManager.Instance?.PlayCombo(scoreManager.CurrentStreak);
            }

            if (!targetAwarded && scoreManager.TotalLinesCleared >= TargetLines) {
                targetAwarded=true;
                scoreManager.RegisterTargetBonus(DifficultyRules.TargetBonus(Difficulty));
            }

            if (board.HasBlocksAboveCeiling())
            {
                TriggerGameOver("BOARD TOPPED OUT!");
                yield break;
            }
            SpawnNextPiece();
        }

        private void HandleBoardChanged()
        {
            if (boardRenderer != null && board != null)
            {
                boardRenderer.RefreshBoard(DropStackBoard.Columns, DropStackBoard.TotalRows, board.GetCell);
            }
        }

        private void HandleSpeedLevelChanged(int newLevel, float newInterval, bool isPeak)
        {
            if (CurrentState != GameState.Playing) return;

            DropStackAudioManager.Instance?.PlaySpeedUp();
            string banner = isPeak ? $"PEAK VELOCITY! (LEVEL {newLevel})" : $"SPEED ESCALATION! LEVEL {newLevel}";
            OnSpeedBannerTriggered?.Invoke(banner);
            JuiceEffects.Instance?.TriggerScreenShake(0.18f, 0.25f);
        }

        private void TriggerGameOver(string reason)
        {
            if (CurrentState == GameState.GameOver) return;

            CurrentState = GameState.GameOver;
            StopAllCoroutines();
            pieceController.SetInputEnabled(false);
            Time.timeScale = 1f;
            scoreManager.SaveHighScore();

            pieceController.ClearActivePiece();
            DropStackAudioManager.Instance?.PlayGameOver();

            OnGameOver?.Invoke(reason, scoreManager.IsNewHighScore);
            OnGameStateChanged?.Invoke(CurrentState);
        }

        #endregion

        private void OnApplicationFocus(bool focused)
        {
            if (!focused && CurrentState == GameState.Playing) TogglePause();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && CurrentState == GameState.Playing) TogglePause();
        }
    }
}
