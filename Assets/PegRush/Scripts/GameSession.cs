using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PegRush
{
    public enum SessionState { Home, Playing, Modal, Draining }

    public sealed class GameSession : MonoBehaviour
    {
        [SerializeField] private GameConfig _config;
        [SerializeField] private Board _board;
        [SerializeField] private Ball _ballPrefab;
        [SerializeField] private Camera _camera;
        [SerializeField] private GameUI _ui;
        [SerializeField] private AudioEffects _audio;
        private readonly List<Ball> _pool = new List<Ball>();
        private Wallet _wallet;
        private int _rows = 12;
        private int _profile;
        private int _amount = 10;
        private int _drops;
        private int _active;
        private bool _focused = true;
        private bool _paused;
        private SessionState _beforeModal;
        private readonly Queue<string> _history = new Queue<string>();
        public GameConfig Config => _config;
        public PhysicsMaterial2D BoardMaterial => _board.Material;
        public UIControlSnapshot[] Controls => _ui.Controls();
        public bool Reduced => _wallet.Data.Reduced;
        public int Rows => _rows;
        public int Profile => _profile;
        public int Amount => _amount;
        public int Active => _active;
        public int Drops => _drops;
        public long Balance => _wallet.Data.Coins;
        public bool Muted => _wallet.Data.Muted;
        public SessionState State { get; private set; }
        public string Notice { get; private set; } = "Choose your rhythm. Drop into possibility.";
        private string _historyText = "";
        private string _compactHistoryText = "";
        public string History => _historyText;
        public string CompactHistory => _compactHistoryText;
        public static readonly Color[] Colors = { new Color(0.22f, 1f, 0.66f), new Color(1f, 0.76f, 0.25f), new Color(1f, 0.35f, 0.4f) };
        public static readonly string[] Profiles = { "Green", "Gold", "Red" };

        public void Wire(GameConfig config, Board board, Ball ball, Camera camera, GameUI ui, AudioEffects audio)
        {
            _config = config;
            _board = board;
            _ballPrefab = ball;
            _camera = camera;
            _ui = ui;
            _audio = audio;
        }
        private void Awake()
        {
            _config.Validate();
            _wallet = Wallet.Open();
            Physics2D.IgnoreLayerCollision(8, 8);
            for (int i = 0; i < _config.Capacity; i++)
            {
                Ball ball = Instantiate(_ballPrefab, transform);
                ball.gameObject.SetActive(false);
                _pool.Add(ball);
            }
            _board.Configure(_config);
            _board.Generate(_rows);
            _ui.Initialize(this, _camera);
            State = SessionState.Home;
        }
        private void Update()
        {
            if (_focused && !_paused && State == SessionState.Playing && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) Drop();
            _ui.Refresh();
        }
        public void Play()
        {
            State = SessionState.Playing;
        }
        public void Drop()
        {
            if (!_focused || _paused || State != SessionState.Playing) return;
            if (_active >= _config.Capacity)
            {
                Notice = "Board full — wait for a ball to land. No coins charged.";
                return;
            }
            DropTransaction transaction = _wallet.Reserve(_amount, _rows, _profile, _config.Table(_rows, _profile));
            if (transaction == null)
            {
                Notice = _wallet.StorageAvailable ? "Not enough coins. Choose a smaller amount or use free refill." : "Browser storage unavailable. Enable site storage and reload.";
                return;
            }
            Ball ball = _pool.Find(item => !item.gameObject.activeSelf);
            _active++;
            _drops++;
            ball.Launch(this, transaction, new Vector2(Random.Range(-_config.SpawnSpread, _config.SpawnSpread), _config.SpawnY(_rows)), Colors[_profile], _config.Gravity);
            _ui.Released();
            Notice = "In flight • controls unlock when the board clears";
        }
        public bool Complete(Ball ball, string id, int pocket)
        {
            long reward = _wallet.Resolve(id, pocket);
            if (reward < 0) return false;
            _active--;
            string result = pocket < 0 ? "Recovered +" + Coins(reward) : "+" + Coins(reward);
            _history.Enqueue(result);
            while (_history.Count > 5) _history.Dequeue();
            _historyText = string.Join("  /  ", _history);
            string[] results = _history.ToArray();
            _compactHistoryText = string.Join(" / ", results, System.Math.Max(0, results.Length - 3), System.Math.Min(3, results.Length));
            Notice = pocket < 0 ? "Ball recovered • reserved coins refunded" : "Pocket " + (pocket + 1) + " • earned " + Coins(reward) + " coins";
            if (pocket >= 0)
            {
                _ui.ShowReward(pocket, reward);
                _audio.Play(Muted);
            }
            if (_active == 0 && State == SessionState.Draining) State = SessionState.Home;
            return true;
        }
        public static string Coins(long hundredths)
        {
            return (hundredths / 100m).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        }
        public void ChangeRows(int direction)
        {
            if (_active != 0 || State != SessionState.Playing) return;
            _rows = Mathf.Clamp(_rows + direction, 8, 16);
            _board.Generate(_rows);
        }
        public void ChangeProfile()
        {
            if (_active != 0 || State != SessionState.Playing) return;
            _profile = (_profile + 1) % 3;
        }
        public void SetProfile(int profile)
        {
            if (_active != 0 || State != SessionState.Playing) return;
            _profile = Mathf.Clamp(profile, 0, 2);
        }
        public void ChangeAmount()
        {
            int[] choices = { 1, 5, 10, 25, 50 };
            _amount = choices[(System.Array.IndexOf(choices, _amount) + 1) % choices.Length];
        }
        public void Home()
        {
            if (_active == 0) State = SessionState.Home;
            else OpenModal("leave");
        }
        public void OpenModal(string name)
        {
            _beforeModal = State;
            State = SessionState.Modal;
            _ui.ShowModal(name);
        }
        public void CloseModal()
        {
            State = _beforeModal;
            _ui.HideModal();
        }
        public void Drain()
        {
            _ui.HideModal();
            State = _active == 0 ? SessionState.Home : SessionState.Draining;
        }
        public void ToggleMute()
        {
            _wallet.Data.Muted = !Muted;
            _wallet.Save();
        }
        public void ToggleReduced()
        {
            _wallet.Data.Reduced = !Reduced;
            _wallet.Save();
        }
        public void Refill()
        {
            if (_wallet.Refill()) Notice = "Free refill • 1,000 virtual coins added";
            else Notice = "Refill unlocks below 1 coin after the board clears.";
        }
        private void OnApplicationFocus(bool focused)
        {
            _focused = focused;
            Time.timeScale = _focused && !_paused ? 1f : 0f;
        }
        private void OnApplicationPause(bool paused)
        {
            _paused = paused;
            Time.timeScale = _focused && !_paused ? 1f : 0f;
        }
        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }
    }
}



