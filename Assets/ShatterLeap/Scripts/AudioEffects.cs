using System.Runtime.InteropServices;
using UnityEngine;

namespace ShatterLeap
{
    public enum Cue { Ui, Preview, Jump, Safe, Break, Win, Lose }

    public sealed class AudioEffects : MonoBehaviour
    {
        [SerializeField] private GameConfig _config;
        [SerializeField] private AudioSource _music;
        [SerializeField] private AudioSource _sound;
        [SerializeField] private Material _shardMaterial;
        private Transform[] _shards;
        private Vector3[] _velocity;
        private float _shardTime;
        private bool _unlocked;
        private SaveData _settings;
        public void Initialize(SaveData settings)
        {
            _settings = settings;
            _shards = new Transform[_config.ShardCount];
            _velocity = new Vector3[_shards.Length];
            for (int i = 0; i < _shards.Length; i++)
            {
                GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(shard.GetComponent<Collider>());
                shard.name = "Pooled glass fragment";
                shard.transform.SetParent(transform);
                shard.transform.localScale = new Vector3(0.12f + i % 3 * 0.05f, 0.035f, 0.15f);
                shard.GetComponent<Renderer>().sharedMaterial = _shardMaterial;
                shard.SetActive(false);
                _shards[i] = shard.transform;
            }
            ApplySettings();
        }
        public void Unlock()
        {
            if (_unlocked) { return; }
            _unlocked = true;
            _music.clip = _config.Music;
            _music.loop = true;
            if (_music.clip) { _music.Play(); }
        }
        public void ApplySettings()
        {
            _music.volume = _settings.Music;
            _sound.volume = _settings.Sound;
        }
        public void Play(Cue cue)
        {
            if (!_unlocked) { return; }
            AudioClip clip = cue switch
            {
                Cue.Safe => _config.Coin,
                Cue.Break => _config.Glass,
                Cue.Win => _config.Success,
                Cue.Lose => _config.Failure,
                _ => _config.Click
            };
            // A single effect voice bounds playback even on rapid UI clicks.
            _sound.Stop();
            _sound.clip = clip;
            _sound.pitch = cue == Cue.Jump ? 0.8f : cue == Cue.Preview ? 1.2f : 1;
            if (clip) { _sound.Play(); }
            if (_settings.Vibration && (cue == Cue.Break || cue == Cue.Win))
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                ShatterVibrate(cue == Cue.Win ? 80 : 140);
#elif UNITY_ANDROID && !UNITY_EDITOR
                Handheld.Vibrate();
#endif
            }
        }
        public void BreakAt(Vector3 point)
        {
            _shardTime = 1.4f;
            for (int i = 0; i < _shards.Length; i++)
            {
                _shards[i].gameObject.SetActive(true);
                _shards[i].position = point + new Vector3((i % 5 - 2) * 0.3f, 0, (i / 5 - 2) * 0.3f);
                _velocity[i] = new Vector3(Mathf.Sin(i * 2.4f) * 2, 1 + i % 3, Mathf.Cos(i * 2.4f) * 2);
            }
        }
        public void ResetPool()
        {
            _shardTime = 0;
            if (_shards == null) { return; }
            foreach (Transform shard in _shards) { shard.gameObject.SetActive(false); }
        }
        public void Advance(float delta)
        {
            if (_shardTime <= 0) { return; }
            _shardTime -= delta;
            for (int i = 0; i < _shards.Length; i++)
            {
                _velocity[i] += Vector3.down * (9.8f * delta);
                _shards[i].position += _velocity[i] * delta;
                _shards[i].Rotate(140 * delta, 40 * delta, 80 * delta);
                if (_shardTime <= 0) { _shards[i].gameObject.SetActive(false); }
            }
        }
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void ShatterVibrate(int milliseconds);
#endif
    }
}
