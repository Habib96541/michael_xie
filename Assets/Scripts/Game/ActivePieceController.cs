using System;
using UnityEngine;
using DropStack.Data;
using DropStack.Audio;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DropStack.Game
{
    public class ActivePieceController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DropStackBoard board;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private SpeedCurveManager speedManager;

        [Header("Tuning")]
        [SerializeField] private float lockDelay = 0.5f;
        [SerializeField] private int maxLockResets = 15;
        [SerializeField] private float dasDelay = 0.16f; // Delayed Auto Shift
        [SerializeField] private float arrInterval = 0.04f; // Auto Repeat Rate
        [SerializeField] private float softDropInterval = 0.04f;

        // Wall kick test offsets (SRS-style kick fallback)
        private static readonly Vector2Int[] KickOffsets = new Vector2Int[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(-1, 1),
            new Vector2Int(1, 1),
            new Vector2Int(-2, 0),
            new Vector2Int(2, 0),
            new Vector2Int(0, -1)
        };

        // Active State
        public PolyominoShape CurrentShape { get; private set; }
        public Vector2Int BoardPosition { get; private set; }
        public int RotationIndex { get; private set; }
        public bool HasActivePiece => CurrentShape != null;

        // Gravity & Locking
        private float dropTimer;
        private float lockTimer;
        private int lockResetCount;
        private bool isGrounded;

        // DAS / ARR Timers
        private int moveDirection; // -1 = Left, 1 = Right, 0 = None
        private float dasTimer;
        private float arrTimer;
        private bool isSoftDropping;
        private int touchDirection;
        private bool touchSoftDrop;
        public bool InputEnabled { get; private set; }
        public void SetDifficulty(GameDifficulty mode) { lockDelay = DifficultyRules.LockDelay(mode); }

        public void SetInputEnabled(bool enabled)
        {
            InputEnabled = enabled;
            ResetInput();
        }

        public void ResetInput()
        {
            moveDirection = touchDirection = 0;
            isSoftDropping = touchSoftDrop = false;
            dasTimer = arrTimer = 0f;
        }

        // Events
        public event Action<PolyominoShape, Vector2Int, int> OnPieceMoved;
        public event Action<PolyominoShape, Vector2Int, int> OnPieceLocked;
        public event Action OnHoldRequested;
        public event Action<int> OnHardDropExecuted; // cells dropped

        public void Initialize(DropStackBoard b, ScoreManager s, SpeedCurveManager sp)
        {
            board = b;
            scoreManager = s;
            speedManager = sp;
        }

        public bool SpawnPiece(PolyominoShape shape)
        {
            if (shape == null || board == null) return false;

            CurrentShape = shape;
            RotationIndex = 0;

            // Spawn centered at column 4-5, row 20
            var bounds = shape.GetBoundingBox(0);
            int spawnX = (DropStackBoard.Columns / 2) - (bounds.size.x / 2) - bounds.xMin;
            int spawnY = DropStackBoard.VisibleRows - 1 - (bounds.yMax - 1);

            BoardPosition = new Vector2Int(spawnX, spawnY);
            dropTimer = 0f;
            lockTimer = 0f;
            lockResetCount = 0;
            isGrounded = false;
            isSoftDropping = false;

            // Check if spawn location is blocked -> Game Over
            if (!board.IsValidPosition(CurrentShape, BoardPosition, RotationIndex))
            {
                // Try 1 row up in buffer
                if (board.IsValidPosition(CurrentShape, BoardPosition + Vector2Int.up, RotationIndex))
                {
                    BoardPosition += Vector2Int.up;
                }
                else
                {
                    // Blocked at spawn
                    return false;
                }
            }

            OnPieceMoved?.Invoke(CurrentShape, BoardPosition, RotationIndex);
            return true;
        }

        public void ClearActivePiece()
        {
            CurrentShape = null;
            OnPieceMoved?.Invoke(null, BoardPosition, RotationIndex);
        }

        private void Update()
        {
            if (!InputEnabled || !HasActivePiece || board == null || board.IsClearingLines) return;

            HandleInput();
            if (!InputEnabled || !HasActivePiece || board.IsClearingLines) return;
            HandleHorizontalMovement();
            HandleGravityAndLocking();
        }

        #region Input Handling

        private void HandleInput()
        {
            // Support both New Input System and Legacy Input for maximum compatibility
            bool leftDown = false, leftHeld = false;
            bool rightDown = false, rightHeld = false;
            bool softDropHeld = false;
            bool hardDropDown = false;
            bool rotCWDown = false;
            bool rotCCWDown = false;
            bool holdDown = false;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                leftDown = kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame;
                leftHeld = kb.leftArrowKey.isPressed || kb.aKey.isPressed;

                rightDown = kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame;
                rightHeld = kb.rightArrowKey.isPressed || kb.dKey.isPressed;

                softDropHeld = kb.downArrowKey.isPressed || kb.sKey.isPressed;
                hardDropDown = kb.spaceKey.wasPressedThisFrame;

                rotCWDown = kb.upArrowKey.wasPressedThisFrame || kb.xKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame;
                rotCCWDown = kb.zKey.wasPressedThisFrame || kb.qKey.wasPressedThisFrame;

                holdDown = kb.cKey.wasPressedThisFrame || kb.leftShiftKey.wasPressedThisFrame || kb.hKey.wasPressedThisFrame;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            // Compile legacy reads only when that backend is enabled.
            if (!leftDown && (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))) leftDown = true;
            if (!leftHeld && (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))) leftHeld = true;

            if (!rightDown && (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))) rightDown = true;
            if (!rightHeld && (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))) rightHeld = true;

            if (!softDropHeld && (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))) softDropHeld = true;
            if (!hardDropDown && Input.GetKeyDown(KeyCode.Space)) hardDropDown = true;

            if (!rotCWDown && (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.W))) rotCWDown = true;
            if (!rotCCWDown && (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Q))) rotCCWDown = true;

            if (!holdDown && (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.H))) holdDown = true;
#endif

            // Process Actions
            int desired = rightDown ? 1 : leftDown ? -1 : touchDirection != 0 ? touchDirection : leftHeld ? -1 : rightHeld ? 1 : 0;
            if (desired != moveDirection) BeginMove(desired);
            isSoftDropping = softDropHeld || touchSoftDrop;

            if (hardDropDown) { ExecuteHardDrop(); return; }
            if (holdDown) { RequestHold(); return; }
            if (rotCWDown) Rotate(1);
            if (rotCCWDown) Rotate(-1);
        }

        public void StartMove(int dir)
        {
            if (!InputEnabled || !HasActivePiece || board.IsClearingLines) return;
            touchDirection = dir;
            BeginMove(dir);
        }

        private void BeginMove(int dir)
        {
            moveDirection = dir;
            dasTimer = dasDelay;
            arrTimer = 0f;
            if (dir != 0) TryMove(new Vector2Int(dir, 0));
        }

        public void StopMove()
        {
            moveDirection = touchDirection = 0;
        }

        public void SetSoftDrop(bool active)
        {
            bool wasHeld = touchSoftDrop;
            touchSoftDrop = active && InputEnabled && HasActivePiece && board != null && !board.IsClearingLines;
            isSoftDropping = touchSoftDrop;
            if (touchSoftDrop && !wasHeld)
            {
                dropTimer = 0f;
                DropOneCell();
            }
        }

        public void ExecuteHardDrop()
        {
            if (!InputEnabled || !HasActivePiece || board == null || board.IsClearingLines) return;

            int dropDistance = CalculateGhostY() - BoardPosition.y;
            int cells = Mathf.Abs(dropDistance);

            BoardPosition = new Vector2Int(BoardPosition.x, BoardPosition.y + dropDistance);
            OnPieceMoved?.Invoke(CurrentShape, BoardPosition, RotationIndex);

            if (scoreManager != null && cells > 0)
            {
                scoreManager.RegisterDropScore(cells, true);
            }

            DropStackAudioManager.Instance?.PlayHardDrop();
            OnHardDropExecuted?.Invoke(cells);

            LockCurrentPiece();
        }

        public void Rotate(int dir)
        {
            if (!InputEnabled || !HasActivePiece || board == null || board.IsClearingLines) return;

            int targetRotation = (RotationIndex + dir + 4) % 4;

            // Attempt wall kicks
            foreach (var kick in KickOffsets)
            {
                Vector2Int testPos = BoardPosition + kick;
                if (board.IsValidPosition(CurrentShape, testPos, targetRotation))
                {
                    BoardPosition = testPos;
                    RotationIndex = targetRotation;
                    ResetLockDelay();
                    DropStackAudioManager.Instance?.PlayRotate();
                    OnPieceMoved?.Invoke(CurrentShape, BoardPosition, RotationIndex);
                    return;
                }
            }
        }

        public void RequestHold()
        {
            if (!InputEnabled || !HasActivePiece || board == null || board.IsClearingLines) return;
            OnHoldRequested?.Invoke();
        }

        #endregion

        #region Movement & Gravity

        private void HandleHorizontalMovement()
        {
            if (moveDirection == 0) return;

            if (dasTimer > 0f)
            {
                dasTimer -= Time.deltaTime;
            }
            else
            {
                arrTimer -= Time.deltaTime;
                if (arrTimer <= 0f)
                {
                    arrTimer = arrInterval;
                    TryMove(new Vector2Int(moveDirection, 0));
                }
            }
        }

        private bool TryMove(Vector2Int delta)
        {
            Vector2Int newPos = BoardPosition + delta;
            if (board.IsValidPosition(CurrentShape, newPos, RotationIndex))
            {
                BoardPosition = newPos;
                ResetLockDelay();
                DropStackAudioManager.Instance?.PlayMove();
                OnPieceMoved?.Invoke(CurrentShape, BoardPosition, RotationIndex);
                return true;
            }
            return false;
        }

        private void HandleGravityAndLocking()
        {
            float interval = isSoftDropping ? softDropInterval : (speedManager != null ? speedManager.CurrentDropInterval : 1.0f);
            dropTimer += Time.deltaTime;

            if (dropTimer >= interval)
            {
                dropTimer = 0f;
                DropOneCell();
            }

            // Check grounded state
            isGrounded = !board.IsValidPosition(CurrentShape, BoardPosition + Vector2Int.down, RotationIndex);

            if (isGrounded)
            {
                lockTimer += Time.deltaTime;
                if (lockTimer >= lockDelay)
                {
                    LockCurrentPiece();
                }
            }
            else
            {
                lockTimer = 0f;
            }
        }

        private void DropOneCell()
        {
            Vector2Int nextPosition = BoardPosition + Vector2Int.down;
            if (!board.IsValidPosition(CurrentShape, nextPosition, RotationIndex)) return;

            BoardPosition = nextPosition;
            isGrounded = false;
            if (isSoftDropping && scoreManager != null)
            {
                scoreManager.RegisterDropScore(1, false);
                DropStackAudioManager.Instance?.PlaySoftDrop();
            }
            OnPieceMoved?.Invoke(CurrentShape, BoardPosition, RotationIndex);
        }

        private void ResetLockDelay()
        {
            if (isGrounded && lockResetCount < maxLockResets)
            {
                lockTimer = 0f;
                lockResetCount++;
            }
        }

        private void LockCurrentPiece()
        {
            if (CurrentShape == null || board == null) return;

            DropStackAudioManager.Instance?.PlayLock();
            board.LockPiece(CurrentShape, BoardPosition, RotationIndex);

            var lockedShape = CurrentShape;
            var lockedPos = BoardPosition;
            var lockedRot = RotationIndex;

            ClearActivePiece();
            OnPieceLocked?.Invoke(lockedShape, lockedPos, lockedRot);
        }

        #endregion

        #region Ghost Calculation

        public int CalculateGhostY()
        {
            if (CurrentShape == null || board == null) return BoardPosition.y;

            int currentY = BoardPosition.y;
            while (board.IsValidPosition(CurrentShape, new Vector2Int(BoardPosition.x, currentY - 1), RotationIndex))
            {
                currentY--;
            }

            return currentY;
        }

        #endregion
    }
}
