using UnityEngine;
using System.Collections;

namespace com.javierquevedo
{
    public enum GameState { Playing, Loose, Win };

    public class Game
    {
        /* Constants */
        public const int PointsPerExplosion = 100;
        public const int PointsPerFall = 150;
        public const float DefaultMatchDuration = 90.0f;

        /* Public properties */
        public GameState state;
        public bool isTimedMode = true;
        public bool isPaused = false;
        public float timeRemaining = DefaultMatchDuration;
        public float timeElapsed = 0f;

        /* Private iVars */
        private int _points;
        private int _bubblesDestroyed;
        private int _combo;
        private int _maxCombo;
        private int _shotsFired;
        private int _shotsHit;
        private int _maxPowerCells = 5;
        private int _activePowerCells = 5;
        private int _rowDropCycle = 0;
        private int _opponentScore = 0;
        private float _opponentTickTimer = 0f;
        private int _highScore = 0;

        public Game()
        {
            _highScore = PlayerPrefs.GetInt("CyberOrbs_HighScore", 0);
            Reset();
        }

        public void Reset()
        {
            this.state = GameState.Playing;
            this.isPaused = false;
            this._points = 0;
            this._bubblesDestroyed = 0;
            this._combo = 0;
            this._maxCombo = 1;
            this._shotsFired = 0;
            this._shotsHit = 0;
            this._maxPowerCells = 5;
            this._activePowerCells = 5;
            this._rowDropCycle = 0;
            this.timeRemaining = DefaultMatchDuration;
            this.timeElapsed = 0f;
            this._opponentScore = 0;
            this._opponentTickTimer = 0f;
        }

        public int score
        {
            get { return this._points; }
        }

        public int highScore
        {
            get { return Mathf.Max(this._highScore, this._points); }
        }

        public int bubblesDestroyed
        {
            get { return this._bubblesDestroyed; }
        }

        public int combo
        {
            get { return Mathf.Max(1, this._combo); }
        }

        public int rawComboStreak
        {
            get { return this._combo; }
        }

        public int maxCombo
        {
            get { return Mathf.Max(1, this._maxCombo); }
        }

        public int shotsFired
        {
            get { return this._shotsFired; }
        }

        public int shotsHit
        {
            get { return this._shotsHit; }
        }

        public int accuracyPercent
        {
            get
            {
                if (_shotsFired <= 0) return 100;
                return Mathf.Clamp(Mathf.RoundToInt(((float)_shotsHit / _shotsFired) * 100f), 0, 100);
            }
        }

        public int activePowerCells
        {
            get { return this._activePowerCells; }
        }

        public int maxPowerCells
        {
            get { return this._maxPowerCells; }
        }

        public int opponentScore
        {
            get { return this._opponentScore; }
        }

        public string tierName
        {
            get
            {
                if (_points >= 12000) return "ELITE";
                if (_points >= 7500)  return "MASTER";
                if (_points >= 4000)  return "VANGUARD";
                if (_points >= 1500)  return "OPERATIVE";
                return "NOVICE";
            }
        }

        public void RegisterShotResult(bool poppedCluster)
        {
            _shotsFired++;
            if (poppedCluster)
            {
                _shotsHit++;
            }
            else
            {
                _combo = 0;
                _activePowerCells = Mathf.Max(0, _activePowerCells - 1);
            }
        }

        public bool ConsumeRowDropIfDepleted()
        {
            if (_activePowerCells > 0)
                return false;

            // Classic Bubble Shooter foul progression: 5 -> 4 -> 3 -> 2 -> 1 -> 5
            _rowDropCycle = (_rowDropCycle + 1) % 5;
            int nextMax = 5 - _rowDropCycle;
            _maxPowerCells = 5; // Always display 5-cell battery housing
            _activePowerCells = Mathf.Clamp(nextMax, 2, 5);
            return true;
        }

        public int destroyBubbles(int bubbleCount, bool exploded)
        {
            if (exploded)
            {
                _combo++;
                if (_combo > _maxCombo)
                    _maxCombo = _combo;
            }

            int multiplier = Mathf.Max(1, _combo);
            int basePoints = exploded ? bubbleCount * PointsPerExplosion : bubbleCount * PointsPerFall;
            int earned = basePoints * multiplier;

            this._points += earned;
            this._bubblesDestroyed += bubbleCount;

            if (this._points > this._highScore)
            {
                this._highScore = this._points;
                PlayerPrefs.SetInt("CyberOrbs_HighScore", this._highScore);
            }

            // Small bonus time on big combos in Timed Mode
            if (isTimedMode && exploded && _combo >= 3)
            {
                timeRemaining = Mathf.Min(99f, timeRemaining + 1.5f);
            }

            return earned;
        }

        public void AddClearBonus(int bonus)
        {
            this._points += bonus;
            if (this._points > this._highScore)
            {
                this._highScore = this._points;
                PlayerPrefs.SetInt("CyberOrbs_HighScore", this._highScore);
            }
        }

        public void TickTimer(float deltaTime)
        {
            if (state != GameState.Playing || isPaused) return;

            timeElapsed += deltaTime;

            // Simulate live PvP opponent score progression
            _opponentTickTimer += deltaTime;
            if (_opponentTickTimer >= 2.2f)
            {
                _opponentTickTimer = 0f;
                int step = UnityEngine.Random.Range(120, 380);
                _opponentScore += step;
            }

            if (isTimedMode)
            {
                timeRemaining = Mathf.Max(0f, timeRemaining - deltaTime);
            }
        }
    }
}
