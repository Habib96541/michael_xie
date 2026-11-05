using UnityEngine;
using System.Collections.Generic;

namespace com.javierquevedo
{
    /// <summary>
    /// Procedural sci-fi audio synthesizer for Cyber Orbs.
    /// Generates zero-dependency AudioClips in memory for railgun shots, wall ricochets,
    /// crystalline orb snaps, combo-pitched cluster pops, avalanche drops, battery cell depletion,
    /// UI button clicks, and victory/defeat fanfares.
    /// </summary>
    public class CyberAudioSynthesizer : MonoBehaviour
    {
        private static CyberAudioSynthesizer _instance;
        public static CyberAudioSynthesizer Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("CyberAudioSynthesizer");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<CyberAudioSynthesizer>();
                }
                return _instance;
            }
        }

        private AudioSource _sfxSource;
        private Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        private bool _isMuted = false;

        public bool IsMuted
        {
            get { return _isMuted; }
            set
            {
                _isMuted = value;
                if (_sfxSource != null)
                    _sfxSource.mute = _isMuted;
            }
        }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            if (transform.parent != null) transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.spatialBlend = 0f;
            BuildClips();
        }

        private void BuildClips()
        {
            const int sampleRate = 44100;

            // 1. Railgun Fire: high-tech plasma sweep + sub punch
            _clips["fire"] = CreateClip("fire", 0.18f, sampleRate, (t, norm) =>
            {
                float freq = Mathf.Lerp(920f, 190f, Mathf.Pow(norm, 0.55f));
                float env = Mathf.Pow(1f - norm, 1.4f);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.65f
                           + Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * 0.25f;
                return wave * env * 0.45f;
            });

            // 2. Wall Ricochet: crisp metallic cyan ping
            _clips["bounce"] = CreateClip("bounce", 0.09f, sampleRate, (t, norm) =>
            {
                float freq = Mathf.Lerp(1150f, 1680f, norm);
                float env = Mathf.Pow(1f - norm, 2.2f);
                return Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.32f;
            });

            // 3. Crystalline Lock/Snap
            _clips["snap"] = CreateClip("snap", 0.08f, sampleRate, (t, norm) =>
            {
                float freq = norm < 0.35f ? 520f : 780f;
                float env = Mathf.Pow(1f - norm, 1.8f);
                return Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.35f;
            });

            // 4. Cluster Pop / Shatter (base chord, pitched by combo)
            _clips["pop"] = CreateClip("pop", 0.28f, sampleRate, (t, norm) =>
            {
                float env = Mathf.Pow(1f - norm, 1.5f);
                float f1 = 587.33f; // D5
                float f2 = 880.00f; // A5
                float f3 = 1174.66f; // D6
                float shimmer = Mathf.Sin(2f * Mathf.PI * f1 * t) * 0.45f
                              + Mathf.Sin(2f * Mathf.PI * f2 * t) * 0.35f
                              + Mathf.Sin(2f * Mathf.PI * f3 * t) * 0.25f;
                return shimmer * env * 0.50f;
            });

            // 5. Avalanche Drop Bonus
            _clips["avalanche"] = CreateClip("avalanche", 0.36f, sampleRate, (t, norm) =>
            {
                float step = Mathf.Floor(norm * 4f);
                float freq = 523.25f * Mathf.Pow(1.25f, step);
                float env = Mathf.Pow(1f - norm, 1.2f);
                return Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.42f;
            });

            // 6. Row Drop / Penalty Alert
            _clips["rowdrop"] = CreateClip("rowdrop", 0.30f, sampleRate, (t, norm) =>
            {
                float freq = Mathf.Lerp(260f, 110f, norm);
                float env = Mathf.Pow(1f - norm, 1.1f);
                float saw = 2f * (t * freq - Mathf.Floor(0.5f + t * freq));
                return saw * env * 0.32f;
            });

            // 7. UI Button Click
            _clips["click"] = CreateClip("click", 0.055f, sampleRate, (t, norm) =>
            {
                float freq = norm < 0.5f ? 1320f : 1760f;
                float env = 1f - norm;
                return Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.30f;
            });

            // 8. Victory Fanfare
            _clips["victory"] = CreateClip("victory", 0.65f, sampleRate, (t, norm) =>
            {
                float freq = norm < 0.25f ? 523.25f : (norm < 0.5f ? 659.25f : (norm < 0.75f ? 783.99f : 1046.50f));
                float env = Mathf.Pow(1f - norm * 0.7f, 1.2f);
                return (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.6f + Mathf.Sin(4f * Mathf.PI * freq * t) * 0.25f) * env * 0.45f;
            });

            // 9. Game Over
            _clips["gameover"] = CreateClip("gameover", 0.55f, sampleRate, (t, norm) =>
            {
                float freq = norm < 0.33f ? 392.0f : (norm < 0.66f ? 349.23f : 261.63f);
                float env = Mathf.Pow(1f - norm, 1.2f);
                return Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.42f;
            });
        }

        private delegate float WaveSampleGenerator(float timeSeconds, float normalizedTime);

        private AudioClip CreateClip(string name, float duration, int sampleRate, WaveSampleGenerator generator)
        {
            int sampleCount = Mathf.Max(64, Mathf.RoundToInt(duration * sampleRate));
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float norm = (float)i / sampleCount;
                samples[i] = Mathf.Clamp(generator(t, norm), -1f, 1f);
            }
            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public void Play(string clipName, float volume = 1f, float pitch = 1f)
        {
            if (_isMuted || _sfxSource == null) return;
            if (_clips.ContainsKey(clipName) && _clips[clipName] != null)
            {
                _sfxSource.pitch = Mathf.Clamp(pitch, 0.5f, 2.0f);
                _sfxSource.PlayOneShot(_clips[clipName], Mathf.Clamp01(volume));
            }
        }
    }
}
