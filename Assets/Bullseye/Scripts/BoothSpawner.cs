using UnityEngine;

namespace BullseyeBlitz
{
    public sealed class BoothSpawner : MonoBehaviour
    {
        [SerializeField] private BoothTarget[] _slots;
        private BlitzConfig _config;
        private double _next;
        public BoothTarget[] Slots => _slots;
        public void Configure(BoothTarget[] slots) { _slots = slots; }
        public void Begin(BlitzConfig config)
        {
            _config = config;
            Clear();
            Spawn(false, 0);
            Spawn(false, 0);
            Schedule(0);
        }
        public void Tick(double time)
        {
            foreach (BoothTarget target in _slots) { target.Tick(time); }
            if (time < _next) { return; }
            int active = 0, bombs = 0, exposed = 0, exposedCoins = 0;
            foreach (BoothTarget target in _slots)
            {
                if (target.State != TargetState.Hidden) { active++; if (target.Bomb) { bombs++; } }
                if (target.State == TargetState.Exposed) { exposed++; if (!target.Bomb) { exposedCoins++; } }
            }
            if (active < _config.MaximumTargets)
            {
                bool bomb = time >= 1 && bombs < _config.MaximumBombs && Random.value < _config.BombChance;
                if (exposed > 0 && exposedCoins == 0) { bomb = false; }
                Spawn(bomb, time);
            }
            Schedule(time);
        }
        private void Schedule(double time) { _next = time + Random.Range(_config.SpawnInterval.x, _config.SpawnInterval.y); }
        private void Spawn(bool bomb, double time)
        {
            int start = Random.Range(0, _slots.Length);
            for (int n = 0; n < _slots.Length; n++)
            {
                int i = (start + n) % _slots.Length;
                if (_slots[i].State != TargetState.Hidden) { continue; }
                int total = 0;
                foreach (int weight in _config.Weights) { total += weight; }
                int roll = Random.Range(0, total), reward = _config.Rewards[0];
                for (int j = 0; j < _config.Weights.Length; j++)
                {
                    roll -= _config.Weights[j];
                    if (roll < 0) { reward = _config.Rewards[j]; break; }
                }
                _slots[i].Spawn(_config, bomb, bomb ? 0 : reward, time, (i / 3) * 10);
                return;
            }
        }
        public void Clear() { foreach (BoothTarget target in _slots) { target.Hide(); } }
    }
}
