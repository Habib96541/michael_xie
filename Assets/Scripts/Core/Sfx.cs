using UnityEngine;

namespace Crash
{
    /// Every sound in the game is synthesised at runtime.
    public class Sfx : MonoBehaviour
    {
        public static Sfx I;

        AudioSource[] _pool;
        AudioSource _engine;
        int _next;
        bool _muted;
        bool _fading;

        AudioClip _blip, _tick, _go, _cash, _crash, _shield, _over, _loop;

        public bool Muted => _muted;

        void Awake()
        {
            I = this;
            _muted = PlayerPrefs.GetInt("crash_mute", 0) == 1;

            _pool = new AudioSource[5];
            for (int i = 0; i < _pool.Length; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                s.volume = 1f;
                _pool[i] = s;
            }
            _engine = gameObject.AddComponent<AudioSource>();
            _engine.playOnAwake = false;
            _engine.loop = true;
            _engine.spatialBlend = 0f;
            _engine.volume = 0f;

            Build();
        }

        // ---------------- synthesis ----------------

        delegate float Wave(float t);

        const int Rate = 44100;

        static float Saw(float f, float t) => 2f * (t * f - Mathf.Floor(t * f + 0.5f));
        static float Sin(float f, float t) => Mathf.Sin(2f * Mathf.PI * f * t);
        static float Tri(float f, float t) => 2f * Mathf.Abs(Saw(f, t)) - 1f;

        static float Hash(float t)
        {
            float s = Mathf.Sin(t * 78.233f) * 43758.5453f;
            return (s - Mathf.Floor(s)) * 2f - 1f;
        }

        static float Env(float t, float dur, float attack = 0.006f, float curve = 2.2f)
        {
            if (t < 0f || t > dur) return 0f;
            float a = attack <= 0f ? 1f : Mathf.Clamp01(t / attack);
            float d = Mathf.Clamp01(1f - t / dur);
            return a * Mathf.Pow(d, curve);
        }

        static AudioClip Make(string name, float dur, Wave wave)
        {
            int n = Mathf.Max(8, Mathf.RoundToInt(dur * Rate));
            var data = new float[n];
            for (int i = 0; i < n; i++)
                data[i] = Mathf.Clamp(wave(i / (float)Rate), -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        void Build()
        {
            _blip = Make("blip", 0.055f, t => (Sin(1500f, t) * 0.7f + Sin(3000f, t) * 0.2f) * Env(t, 0.055f, 0.002f, 3f));

            _tick = Make("tick", 0.13f, t =>
                (Tri(880f, t) * 0.55f + Sin(1760f, t) * 0.2f) * Env(t, 0.13f, 0.002f, 2.6f));

            _go = Make("go", 0.42f, t =>
            {
                float f = Mathf.Lerp(240f, 980f, Mathf.Pow(Mathf.Clamp01(t / 0.30f), 0.65f));
                float body = Saw(f, t) * 0.35f + Sin(f * 2f, t) * 0.3f;
                float air = Hash(t) * 0.10f * Mathf.Clamp01(1f - t / 0.42f);
                return (body + air) * Env(t, 0.42f, 0.01f, 1.5f);
            });

            _cash = Make("cash", 0.75f, t =>
            {
                float[] notes = { 1046.5f, 1318.5f, 1568f, 2093f };
                float sum = 0f;
                for (int i = 0; i < notes.Length; i++)
                {
                    float st = i * 0.075f;
                    if (t >= st)
                    {
                        float lt = t - st;
                        sum += (Sin(notes[i], lt) * 0.5f + Sin(notes[i] * 2f, lt) * 0.16f) *
                               Env(lt, 0.45f, 0.004f, 2.4f);
                    }
                }
                return sum * 0.55f;
            });

            _crash = Make("crash", 0.85f, t =>
            {
                float boom = Hash(t * 1.4f) * Mathf.Pow(Mathf.Clamp01(1f - t / 0.55f), 2.2f) * 0.55f;
                float f = Mathf.Lerp(190f, 38f, Mathf.Clamp01(t / 0.7f));
                float drop = (Saw(f, t) * 0.35f + Sin(f * 0.5f, t) * 0.45f) * Env(t, 0.85f, 0.004f, 1.7f);
                float crack = Hash(t * 3.1f) * Env(t, 0.05f, 0.001f, 3f) * 0.5f;
                return (boom + drop + crack) * 0.95f;
            });

            _shield = Make("shield", 0.5f, t =>
            {
                float a = Sin(1174f, t) * Env(t, 0.5f, 0.004f, 2.6f);
                float b = Sin(1760f, t) * Env(t, 0.4f, 0.004f, 3.2f) * 0.7f;
                float c = Sin(2349f, t) * Env(t, 0.3f, 0.004f, 4f) * 0.4f;
                return (a + b + c) * 0.4f;
            });

            _over = Make("over", 1.1f, t =>
            {
                float[] notes = { 523.25f, 415.3f, 311.13f };
                float sum = 0f;
                for (int i = 0; i < notes.Length; i++)
                {
                    float st = i * 0.16f;
                    if (t >= st)
                    {
                        float lt = t - st;
                        sum += (Saw(notes[i], lt) * 0.18f + Sin(notes[i], lt) * 0.4f) *
                               Env(lt, 0.75f, 0.01f, 2f);
                    }
                }
                return sum * 0.6f;
            });

            // seamless 2s engine loop: all partials complete whole cycles
            _loop = Make("engine", 2f, t =>
            {
                float hum = Saw(55f, t) * 0.22f + Saw(110.5f, t) * 0.14f + Saw(165f, t) * 0.06f;
                float sub = Sin(27.5f, t) * 0.25f;
                float air = Sin(330f, t) * 0.03f;
                return hum + sub + air;
            });
        }

        // ---------------- playback ----------------

        void Play(AudioClip clip, float volume, float pitch = 1f)
        {
            if (_muted || clip == null || _pool == null) return;
            var src = _pool[_next];
            _next = (_next + 1) % _pool.Length;
            src.pitch = pitch;
            src.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        public void Blip(float volume = 0.16f, float pitch = 1f) => Play(_blip, volume, pitch);
        public void Tick() => Play(_tick, 0.28f);
        public void Go() => Play(_go, 0.42f);
        public void Cash() => Play(_cash, 0.5f);
        public void Crash() => Play(_crash, 0.65f);
        public void ShieldUp() => Play(_shield, 0.4f);
        public void GameOver() => Play(_over, 0.55f);

        public bool EngineOn => _engine != null && _engine.isPlaying;
        public float EngineLevel => _engine != null ? _engine.volume : 0f;

        /// Fade-out of the engine loop is driven here so it always finishes, even
        /// when the caller only asks for silence once (crash, cash out, restart).
        void Update()
        {
            if (_engine == null || !_fading) return;
            _engine.volume = Mathf.MoveTowards(_engine.volume, 0f, Time.unscaledDeltaTime * 2.2f);
            if (_engine.volume <= 0.001f)
            {
                _engine.volume = 0f;
                if (_engine.isPlaying) _engine.Stop();
                _fading = false;
            }
        }

        public void EngineRun(float multiplier, bool active)
        {
            if (_muted || _loop == null || _engine == null)
            {
                if (_engine) { _engine.volume = 0f; if (_engine.isPlaying) _engine.Stop(); }
                _fading = false;
                return;
            }
            if (active)
            {
                _fading = false;
                if (!_engine.isPlaying)
                {
                    _engine.clip = _loop;
                    _engine.pitch = 0.95f;
                    _engine.volume = 0.001f;
                    _engine.Play();
                }
                float m = Mathf.Max(1f, multiplier);
                float pitch = Mathf.Clamp(0.95f + Mathf.Log(m, 2f) * 0.30f, 0.95f, 2.3f);
                float vol = Mathf.Lerp(0.10f, 0.30f, Mathf.Clamp01((m - 1f) / 8f));
                _engine.pitch = Mathf.Lerp(_engine.pitch, pitch, Time.unscaledDeltaTime * 4f);
                _engine.volume = Mathf.Lerp(_engine.volume, vol, Time.unscaledDeltaTime * 4f);
            }
            else
            {
                _fading = true;
            }
        }

        public void SetMuted(bool muted)
        {
            _muted = muted;
            PlayerPrefs.SetInt("crash_mute", muted ? 1 : 0);
            PlayerPrefs.Save();
            if (_muted && _engine != null) { _engine.Stop(); _engine.volume = 0f; _fading = false; }
        }

        public bool ToggleMute()
        {
            SetMuted(!_muted);
            if (!_muted) Blip(0.2f);
            return _muted;
        }
    }
}
