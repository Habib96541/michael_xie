using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceGame.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class DiceAudioSynthesizer : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioSource loopSource;
        [SerializeField] private AudioClip backgroundLoopClip;
        [SerializeField] private AudioClip clickClip;
        [SerializeField] private AudioClip betAdjustClip;
        [SerializeField, Range(0.0f, 1.0f)] private float backgroundMusicVolume = 0.18f;

        private AudioClip _clipRattle;
        private AudioClip _clipDiceLand;
        private AudioClip _clipWinNormal;
        private AudioClip _clipWinHigh;
        private AudioClip _clipJackpot;
        private AudioClip _clipLoss;
        private AudioClip _clipFever;
        private AudioClip _clipClick;
        private AudioClip _clipTick;
        private AudioClip _clipTimerWarning;
        private AudioClip _clipBetAdjust;

        private readonly List<AudioClip> _allocatedClips = new List<AudioClip>(16);

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0.0f;
                audioSource.priority = 64;
            }

            if (loopSource == null)
            {
                loopSource = gameObject.AddComponent<AudioSource>();
                loopSource.loop = true;
                loopSource.playOnAwake = false;
            }
            if (loopSource != null)
            {
                loopSource.loop = true;
                loopSource.playOnAwake = false;
                loopSource.spatialBlend = 0.0f;
                loopSource.priority = 128;
            }

            GenerateAllClips();

            if (clickClip != null) _clipClick = clickClip;
            if (betAdjustClip != null) _clipBetAdjust = betAdjustClip;
            if (loopSource != null && backgroundLoopClip != null)
            {
                loopSource.clip = backgroundLoopClip;
                loopSource.volume = backgroundMusicVolume;
                if (!loopSource.isPlaying) loopSource.Play();
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _allocatedClips.Count; i++)
            {
                if (_allocatedClips[i] != null)
                {
                    Destroy(_allocatedClips[i]);
                }
            }
            _allocatedClips.Clear();
        }

        private void GenerateAllClips()
        {
            const int sampleRate = 44100;

            _clipRattle = CreateDiceRattleClip("DiceRattle", sampleRate);
            _clipDiceLand = CreateDiceLandClip("DiceLand", sampleRate);
            _clipWinNormal = CreateChimeClip("WinNormal", sampleRate, 523.25f, 659.25f, 0.35f);
            _clipWinHigh = CreateChimeClip("WinHigh", sampleRate, 659.25f, 783.99f, 0.45f);
            _clipJackpot = CreateJackpotFanfareClip("JackpotFanfare", sampleRate);
            _clipLoss = CreateLossThudClip("LossThud", sampleRate);
            _clipFever = CreateFeverIgniteClip("FeverIgnite", sampleRate);
            _clipClick = CreateClickClip("ButtonClick", sampleRate);
            _clipBetAdjust = CreateBetAdjustClip("BetChipAdjust", sampleRate);
            _clipTick = CreateTickClip("SliderTick", sampleRate);
            _clipTimerWarning = CreateTimerBeepClip("TimerBeep", sampleRate);
        }

        private AudioClip RegisterClip(AudioClip clip)
        {
            if (clip != null)
            {
                _allocatedClips.Add(clip);
            }
            return clip;
        }

        private AudioClip CreateDiceRattleClip(string name, int sampleRate)
        {
            float duration = 0.28f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];
            var rng = new System.Random(42);

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                float env = Mathf.Pow(1.0f - t, 1.8f);

                float pulse = Mathf.Sin(t * 80.0f * Mathf.PI);
                float noise = ((float)rng.NextDouble() * 2.0f - 1.0f);
                float crack = (rng.NextDouble() > 0.88) ? 0.9f : 0.0f;

                samples[i] = ((noise * 0.4f + crack * 0.6f) * Mathf.Abs(pulse)) * env * 0.5f;
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return RegisterClip(clip);
        }

        private AudioClip CreateDiceLandClip(string name, int sampleRate)
        {
            float duration = 0.22f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];
            var rng = new System.Random(77);

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                float env = Mathf.Exp(-t * 16.0f);
                float freq = Mathf.Lerp(190.0f, 45.0f, t);
                float sine = Mathf.Sin(2.0f * Mathf.PI * freq * (i / (float)sampleRate));
                float noise = ((float)rng.NextDouble() * 2.0f - 1.0f) * Mathf.Exp(-t * 35.0f);

                samples[i] = (sine * 0.7f + noise * 0.3f) * env * 0.85f;
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return RegisterClip(clip);
        }

        private AudioClip CreateChimeClip(string name, int sampleRate, float f1, float f2, float duration)
        {
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = (float)i / totalSamples;
                float env = Mathf.Exp(-progress * 6.5f);

                float wave1 = Mathf.Sin(2.0f * Mathf.PI * f1 * t);
                float wave2 = Mathf.Sin(2.0f * Mathf.PI * f2 * t);
                float shimmer = Mathf.Sin(2.0f * Mathf.PI * (f2 * 2.0f) * t) * 0.25f;

                samples[i] = (wave1 * 0.45f + wave2 * 0.45f + shimmer) * env * 0.75f;
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return RegisterClip(clip);
        }

        private AudioClip CreateJackpotFanfareClip(string name, int sampleRate)
        {
            float duration = 0.85f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f };
            float noteDuration = 0.14f;

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                int noteIndex = Mathf.Clamp((int)(t / noteDuration), 0, notes.Length - 1);
                float freq = notes[noteIndex];

                float noteLocalT = t - (noteIndex * noteDuration);
                float noteEnv = Mathf.Exp(-noteLocalT * 7.0f);
                if (noteIndex == notes.Length - 1)
                {
                    noteEnv = Mathf.Exp(-noteLocalT * 2.5f);
                }

                float wave = Mathf.Sin(2.0f * Mathf.PI * freq * t);
                float harm = Mathf.Sin(2.0f * Mathf.PI * freq * 2.0f * t) * 0.35f;

                samples[i] = (wave * 0.65f + harm * 0.35f) * noteEnv * 0.85f;
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return RegisterClip(clip);
        }

        private AudioClip CreateLossThudClip(string name, int sampleRate)
        {
            float duration = 0.30f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                float sec = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 9.0f);
                float freq = Mathf.Lerp(130.0f, 40.0f, t);

                float sine = Mathf.Sin(2.0f * Mathf.PI * freq * sec);
                float sub = Mathf.Sin(2.0f * Mathf.PI * (freq * 0.73f) * sec);

                samples[i] = (sine * 0.5f + sub * 0.5f) * env * 0.7f;
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return RegisterClip(clip);
        }

        private AudioClip CreateFeverIgniteClip(string name, int sampleRate)
        {
            float duration = 0.65f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                float sec = (float)i / sampleRate;
                float env = (t < 0.2f) ? (t / 0.2f) : Mathf.Exp(-(t - 0.2f) * 4.0f);

                float freq = Mathf.Lerp(220.0f, 880.0f, Mathf.Pow(t, 0.7f));
                float vibrato = Mathf.Sin(sec * 30.0f) * 15.0f;
                float wave = Mathf.Sin(2.0f * Mathf.PI * (freq + vibrato) * sec);

                samples[i] = wave * env * 0.75f;
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return RegisterClip(clip);
        }

        private AudioClip CreateClickClip(string name, int sampleRate)
        {
            float duration = 0.04f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                float env = Mathf.Exp(-t * 30.0f);
                float wave = Mathf.Sin(2.0f * Mathf.PI * 1800.0f * ((float)i / sampleRate));
                samples[i] = wave * env * 0.5f;
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return RegisterClip(clip);
        }

        private AudioClip CreateTickClip(string name, int sampleRate)
        {
            float duration = 0.02f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                float env = Mathf.Exp(-t * 60.0f);
                float wave = Mathf.Sin(2.0f * Mathf.PI * 950.0f * ((float)i / sampleRate));
                samples[i] = wave * env * 0.35f;
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return RegisterClip(clip);
        }

        private AudioClip CreateBetAdjustClip(string name, int sampleRate)
        {
            float duration = 0.16f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float body = Mathf.Exp(-t * 15.0f);
                float transient = Mathf.Exp(-t * 34.0f);
                float chipTone = Mathf.Sin(2.0f * Mathf.PI * 620.0f * t) * 0.38f;
                float highTone = Mathf.Sin(2.0f * Mathf.PI * 1240.0f * t) * 0.16f;
                float tap = Mathf.Sin(2.0f * Mathf.PI * 1570.0f * t) * transient * 0.30f;
                samples[i] = chipTone * body + highTone * transient * 0.32f + tap;
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return RegisterClip(clip);
        }

        private AudioClip CreateTimerBeepClip(string name, int sampleRate)
        {
            float duration = 0.09f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                float env = (t < 0.1f) ? (t / 0.1f) : Mathf.Exp(-(t - 0.1f) * 15.0f);
                float wave = Mathf.Sin(2.0f * Mathf.PI * 880.0f * ((float)i / sampleRate));
                samples[i] = wave * env * 0.5f;
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return RegisterClip(clip);
        }

        public void PlayRattle()
        {
            if (audioSource != null && _clipRattle != null)
            {
                audioSource.PlayOneShot(_clipRattle, 0.7f);
            }
        }

        public void PlayDiceLand()
        {
            if (audioSource != null && _clipDiceLand != null)
            {
                audioSource.PlayOneShot(_clipDiceLand, 0.9f);
            }
        }

        public void PlayWin(int streak)
        {
            if (audioSource == null) return;

            if (streak >= 3 && _clipWinHigh != null)
            {
                audioSource.pitch = Mathf.Clamp(1.0f + (streak - 3) * 0.08f, 1.0f, 1.4f);
                audioSource.PlayOneShot(_clipWinHigh, 0.95f);
                audioSource.pitch = 1.0f;
            }
            else if (_clipWinNormal != null)
            {
                audioSource.pitch = Mathf.Clamp(1.0f + streak * 0.05f, 1.0f, 1.25f);
                audioSource.PlayOneShot(_clipWinNormal, 0.85f);
                audioSource.pitch = 1.0f;
            }
        }

        public void PlayJackpot()
        {
            if (audioSource != null && _clipJackpot != null)
            {
                audioSource.PlayOneShot(_clipJackpot, 1.0f);
            }
        }

        public void PlayLoss()
        {
            if (audioSource != null && _clipLoss != null)
            {
                audioSource.PlayOneShot(_clipLoss, 0.75f);
            }
        }

        public void PlayFever()
        {
            if (audioSource != null && _clipFever != null)
            {
                audioSource.PlayOneShot(_clipFever, 0.95f);
            }
        }

        public void PlayClick()
        {
            if (audioSource != null && _clipClick != null)
            {
                audioSource.PlayOneShot(_clipClick, 0.6f);
            }
        }

        public void PlayBetAdjust()
        {
            if (audioSource != null && _clipBetAdjust != null)
            {
                audioSource.PlayOneShot(_clipBetAdjust, 0.64f);
            }
        }

        public void PlayTick()
        {
            if (audioSource != null && _clipTick != null)
            {
                audioSource.PlayOneShot(_clipTick, 0.4f);
            }
        }

        public void PlayTimerWarning()
        {
            if (audioSource != null && _clipTimerWarning != null)
            {
                audioSource.PlayOneShot(_clipTimerWarning, 0.7f);
            }
        }
    }
}

