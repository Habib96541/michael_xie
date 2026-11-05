using System;
using CyberScratch.Core;
using UnityEngine;

namespace CyberScratch.Audio
{
    public class CyberAudioService : MonoBehaviour
    {
        private static CyberAudioService _instance;
        public static CyberAudioService Instance => _instance;

        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioSource _scratchSource;
        [SerializeField] private AudioSource _anticipationSource;
        [SerializeField] private AudioSource _ambientSource;

        private AudioClip _scratchLoopClip;
        private AudioClip _foilSnapClip;
        private AudioClip _anticipationPulseClip;
        private AudioClip _rowWinStingerClip;
        private AudioClip _tallyTickClip;
        private AudioClip _buttonClickClip;
        private AudioClip _ambientDroneClip;

        private bool _isMuted;
        private float _targetScratchVolume;
        private float _currentScratchVolume;
        private float _targetScratchPitch = 1f;

        public bool IsMuted => _isMuted;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                // The scene's audio service can share a GameObject with its game manager.
                // Removing the whole object would silently delete the gameplay manager too.
                Destroy(this);
                return;
            }
            _instance = this;

            // Keep standalone audio alive across scene changes, but never persist a scene
            // root that also owns the gameplay manager and its scene-specific references.
            if (GetComponent<CyberScratchGameManager>() == null)
            {
                DontDestroyOnLoad(gameObject);
            }

            EnsureAudioSources();
            GenerateProceduralClips();

            if (_ambientSource != null && _ambientDroneClip != null)
            {
                _ambientSource.clip = _ambientDroneClip;
                _ambientSource.loop = true;
                _ambientSource.volume = 0.18f;
                _ambientSource.Play();
            }

            if (_scratchSource != null && _scratchLoopClip != null)
            {
                _scratchSource.clip = _scratchLoopClip;
                _scratchSource.loop = true;
                _scratchSource.volume = 0f;
                _scratchSource.Play();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }

            // Cleanup procedurally generated audio clips to prevent leaks
            DestroyClipSafely(ref _scratchLoopClip);
            DestroyClipSafely(ref _foilSnapClip);
            DestroyClipSafely(ref _anticipationPulseClip);
            DestroyClipSafely(ref _rowWinStingerClip);
            DestroyClipSafely(ref _tallyTickClip);
            DestroyClipSafely(ref _buttonClickClip);
            DestroyClipSafely(ref _ambientDroneClip);
        }

        private void DestroyClipSafely(ref AudioClip clip)
        {
            if (clip != null)
            {
                Destroy(clip);
                clip = null;
            }
        }

        private void EnsureAudioSources()
        {
            if (_sfxSource == null) _sfxSource = gameObject.AddComponent<AudioSource>();
            if (_scratchSource == null) _scratchSource = gameObject.AddComponent<AudioSource>();
            if (_anticipationSource == null) _anticipationSource = gameObject.AddComponent<AudioSource>();
            if (_ambientSource == null) _ambientSource = gameObject.AddComponent<AudioSource>();

            _sfxSource.playOnAwake = false;
            _scratchSource.playOnAwake = false;
            _anticipationSource.playOnAwake = false;
            _ambientSource.playOnAwake = false;
        }

        private void Update()
        {
            if (_isMuted)
            {
                if (_scratchSource != null) _scratchSource.volume = 0f;
                return;
            }

            // Smooth scratch volume and pitch response without allocations
            if (_scratchSource != null)
            {
                _currentScratchVolume = Mathf.MoveTowards(_currentScratchVolume, _targetScratchVolume, Time.unscaledDeltaTime * 12f);
                _scratchSource.volume = _currentScratchVolume;
                _scratchSource.pitch = Mathf.MoveTowards(_scratchSource.pitch, _targetScratchPitch, Time.unscaledDeltaTime * 6f);
            }

            // Decay target scratch volume unless actively refreshed by drag
            _targetScratchVolume = Mathf.MoveTowards(_targetScratchVolume, 0f, Time.unscaledDeltaTime * 8f);
        }

        public void SetMute(bool mute)
        {
            _isMuted = mute;
            if (_sfxSource != null) _sfxSource.mute = mute;
            if (_scratchSource != null) _scratchSource.mute = mute;
            if (_anticipationSource != null) _anticipationSource.mute = mute;
            if (_ambientSource != null) _ambientSource.mute = mute;
        }

        public void ToggleMute()
        {
            SetMute(!_isMuted);
        }

        public void UpdateScratchIntensity(float velocityNormalized)
        {
            if (_isMuted) return;

            float intensity = Mathf.Clamp01(velocityNormalized);
            _targetScratchVolume = Mathf.Lerp(0.1f, 0.65f, intensity);
            _targetScratchPitch = Mathf.Lerp(0.9f, 1.35f, intensity);
        }

        public void StopScratchSound()
        {
            _targetScratchVolume = 0f;
        }

        public void PlayFoilSnap()
        {
            if (_isMuted || _sfxSource == null || _foilSnapClip == null) return;
            _sfxSource.PlayOneShot(_foilSnapClip, 0.8f);
        }

        public void PlayAnticipationPulse()
        {
            if (_isMuted || _anticipationSource == null || _anticipationPulseClip == null) return;
            _anticipationSource.Stop();
            _anticipationSource.clip = _anticipationPulseClip;
            _anticipationSource.volume = 0.75f;
            _anticipationSource.Play();
        }

        public void StopAnticipationPulse()
        {
            if (_anticipationSource != null && _anticipationSource.isPlaying)
            {
                _anticipationSource.Stop();
            }
        }

        public void PlayRowWinStinger()
        {
            if (_isMuted || _sfxSource == null || _rowWinStingerClip == null) return;
            _sfxSource.PlayOneShot(_rowWinStingerClip, 0.9f);
        }

        public void PlayTallyTick()
        {
            if (_isMuted || _sfxSource == null || _tallyTickClip == null) return;
            _sfxSource.PlayOneShot(_tallyTickClip, 0.4f);
        }

        public void PlayButtonClick()
        {
            if (_isMuted || _sfxSource == null || _buttonClickClip == null) return;
            _sfxSource.PlayOneShot(_buttonClickClip, 0.6f);
        }

        #region Procedural Waveform Synthesis

        private void GenerateProceduralClips()
        {
            const int sampleRate = 44100;

            // 1. Scratch loop clip (white noise filtered with subtle metallic texture)
            {
                int samplesCount = sampleRate; // 1 second loop
                float[] samples = new float[samplesCount];
                float filterState = 0f;
                float resonanceState = 0f;

                for (int i = 0; i < samplesCount; i++)
                {
                    float noise = UnityEngine.Random.Range(-1f, 1f);
                    // Bandpass filter
                    filterState += (noise - filterState) * 0.35f;
                    resonanceState += (filterState - resonanceState) * 0.25f;
                    float metallic = Mathf.Sin(i * 0.15f) * 0.2f * noise;
                    samples[i] = (filterState - resonanceState) + metallic;
                }
                _scratchLoopClip = AudioClip.Create("CyberScratch_Loop", samplesCount, 1, sampleRate, false);
                _scratchLoopClip.SetData(samples, 0);
            }

            // 2. Foil snap clip (crisp metallic pop)
            {
                int samplesCount = (int)(sampleRate * 0.12f);
                float[] samples = new float[samplesCount];
                for (int i = 0; i < samplesCount; i++)
                {
                    float t = (float)i / sampleRate;
                    float progress = (float)i / samplesCount;
                    float freq = Mathf.Lerp(1600f, 220f, progress);
                    float env = Mathf.Exp(-progress * 28f);
                    float snap = Mathf.Sin(2f * Mathf.PI * freq * t);
                    float metalGrain = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-progress * 45f) * 0.5f;
                    samples[i] = Mathf.Clamp((snap + metalGrain) * env, -1f, 1f);
                }
                _foilSnapClip = AudioClip.Create("CyberFoil_Snap", samplesCount, 1, sampleRate, false);
                _foilSnapClip.SetData(samples, 0);
            }

            // 3. Anticipation pulse clip (rising resonant cyber hum)
            {
                float duration = 1.4f;
                int samplesCount = (int)(sampleRate * duration);
                float[] samples = new float[samplesCount];
                for (int i = 0; i < samplesCount; i++)
                {
                    float t = (float)i / sampleRate;
                    float progress = (float)i / samplesCount;
                    // Frequency rises from 85Hz to 190Hz
                    float freq = Mathf.Lerp(85f, 190f, Mathf.Pow(progress, 1.4f));
                    float vibrato = Mathf.Sin(2f * Mathf.PI * 8f * t) * 6f;
                    float wave = Mathf.Sin(2f * Mathf.PI * (freq + vibrato) * t);
                    float harmonic = Mathf.Sin(4f * Mathf.PI * (freq + vibrato) * t) * 0.35f;
                    // Pulse envelope
                    float env = Mathf.Sin(progress * Mathf.PI * 0.9f);
                    samples[i] = Mathf.Clamp((wave + harmonic) * env * 0.8f, -1f, 1f);
                }
                _anticipationPulseClip = AudioClip.Create("Cyber_Anticipation", samplesCount, 1, sampleRate, false);
                _anticipationPulseClip.SetData(samples, 0);
            }

            // 4. Row win stinger (C5 -> E5 -> G5 -> C6 synth chime arpeggio)
            {
                float duration = 0.85f;
                int samplesCount = (int)(sampleRate * duration);
                float[] samples = new float[samplesCount];

                float[] notePitches = new float[] { 523.25f, 659.25f, 783.99f, 1046.50f };
                float noteDuration = 0.12f;

                for (int n = 0; n < notePitches.Length; n++)
                {
                    float noteStart = n * noteDuration;
                    int startSample = (int)(noteStart * sampleRate);
                    float pitch = notePitches[n];

                    for (int i = startSample; i < samplesCount; i++)
                    {
                        float localT = (float)(i - startSample) / sampleRate;
                        float env = Mathf.Exp(-localT * 6.5f);
                        // Bell-like FM synthesis: carrier + modulator
                        float mod = Mathf.Sin(2f * Mathf.PI * (pitch * 2f) * localT) * 1.5f;
                        float noteSample = Mathf.Sin(2f * Mathf.PI * pitch * localT + mod) * env * 0.4f;
                        samples[i] += noteSample;
                    }
                }

                // Normalize to avoid clipping
                for (int i = 0; i < samplesCount; i++)
                {
                    samples[i] = Mathf.Clamp(samples[i], -1f, 1f);
                }

                _rowWinStingerClip = AudioClip.Create("Cyber_RowWin", samplesCount, 1, sampleRate, false);
                _rowWinStingerClip.SetData(samples, 0);
            }

            // 5. Tally tick clip
            {
                int samplesCount = (int)(sampleRate * 0.035f);
                float[] samples = new float[samplesCount];
                for (int i = 0; i < samplesCount; i++)
                {
                    float t = (float)i / sampleRate;
                    float progress = (float)i / samplesCount;
                    float freq = Mathf.Lerp(1200f, 600f, progress);
                    float env = Mathf.Exp(-progress * 22f);
                    samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.5f;
                }
                _tallyTickClip = AudioClip.Create("Cyber_Tick", samplesCount, 1, sampleRate, false);
                _tallyTickClip.SetData(samples, 0);
            }

            // 6. UI button click clip
            {
                int samplesCount = (int)(sampleRate * 0.06f);
                float[] samples = new float[samplesCount];
                for (int i = 0; i < samplesCount; i++)
                {
                    float t = (float)i / sampleRate;
                    float progress = (float)i / samplesCount;
                    float env = Mathf.Exp(-progress * 26f);
                    float tone1 = Mathf.Sin(2f * Mathf.PI * 750f * t);
                    float tone2 = Mathf.Sin(2f * Mathf.PI * 1400f * t) * 0.5f;
                    samples[i] = (tone1 + tone2) * env * 0.55f;
                }
                _buttonClickClip = AudioClip.Create("Cyber_Click", samplesCount, 1, sampleRate, false);
                _buttonClickClip.SetData(samples, 0);
            }

            // 7. Ambient cyberpunk drone
            {
                float duration = 4.0f;
                int samplesCount = (int)(sampleRate * duration);
                float[] samples = new float[samplesCount];
                for (int i = 0; i < samplesCount; i++)
                {
                    float t = (float)i / sampleRate;
                    float sub = Mathf.Sin(2f * Mathf.PI * 65.41f * t) * 0.35f;
                    float fifth = Mathf.Sin(2f * Mathf.PI * 98.00f * t + Mathf.Sin(t * 0.8f)) * 0.2f;
                    float octave = Mathf.Sin(2f * Mathf.PI * 130.81f * t) * 0.1f;
                    samples[i] = (sub + fifth + octave) * 0.5f;
                }
                _ambientDroneClip = AudioClip.Create("Cyber_Ambient", samplesCount, 1, sampleRate, false);
                _ambientDroneClip.SetData(samples, 0);
            }
        }

        #endregion
    }
}
