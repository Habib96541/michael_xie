using System;
using UnityEngine;

namespace DropStack.Audio
{
    public class DropStackAudioManager : MonoBehaviour
    {
        public static DropStackAudioManager Instance { get; private set; }

        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource bgmSource;

        private AudioClip moveClip;
        private AudioClip rotateClip;
        private AudioClip softDropClip;
        private AudioClip hardDropClip;
        private AudioClip lockClip;
        private AudioClip singleClearClip;
        private AudioClip multiClearClip;
        private AudioClip megaClearClip;
        private AudioClip comboClip;
        private AudioClip speedUpClip;
        private AudioClip warningTickClip;
        private AudioClip gameOverClip;
        private AudioClip bgmClip;

        public bool IsSoundEnabled { get; private set; } = true;
        public bool IsMusicEnabled { get; private set; } = true;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();

            sfxSource.playOnAwake = false;
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
            sfxSource.spatialBlend = bgmSource.spatialBlend = 0f;
            IsSoundEnabled = PlayerPrefs.GetInt("DropStack_Sound", 1) != 0;
            IsMusicEnabled = PlayerPrefs.GetInt("DropStack_Music", 1) != 0;
            sfxSource.mute = !IsSoundEnabled;
            bgmSource.mute = !IsMusicEnabled;

            GenerateAudioClips();
        }

        private void Start()
        {
            // The play button starts music after the browser's first user gesture.
        }

        public void SetSoundEnabled(bool enabled)
        {
            IsSoundEnabled = enabled;
            PlayerPrefs.SetInt("DropStack_Sound", enabled ? 1 : 0);
            PlayerPrefs.Save();
            if (sfxSource != null) sfxSource.mute = !IsSoundEnabled;
        }

        public void SetMusicEnabled(bool enabled)
        {
            IsMusicEnabled = enabled;
            PlayerPrefs.SetInt("DropStack_Music", enabled ? 1 : 0);
            PlayerPrefs.Save();
            if (bgmSource != null)
            {
                bgmSource.mute = !IsMusicEnabled;
                if (IsMusicEnabled && !bgmSource.isPlaying)
                {
                    PlayBGM();
                }
            }
        }

        public void ToggleSound() => SetSoundEnabled(!IsSoundEnabled);
        public void ToggleMusic() => SetMusicEnabled(!IsMusicEnabled);

        public void PlayMove() => PlaySound(moveClip, 0.4f, UnityEngine.Random.Range(0.95f, 1.05f));
        public void PlayRotate() => PlaySound(rotateClip, 0.5f, UnityEngine.Random.Range(0.95f, 1.05f));
        public void PlaySoftDrop() => PlaySound(softDropClip, 0.25f, 1.1f);
        public void PlayHardDrop() => PlaySound(hardDropClip, 0.85f, 1.0f);
        public void PlayLock() => PlaySound(lockClip, 0.55f, 1.0f);

        public void PlayLineClear(int lineCount)
        {
            if (lineCount >= 5) PlaySound(megaClearClip, 1.0f, 1.0f);
            else if (lineCount >= 2) PlaySound(multiClearClip, 0.85f, 1.0f);
            else PlaySound(singleClearClip, 0.7f, 1.0f);
        }

        public void PlayCombo(int streak)
        {
            float pitch = Mathf.Clamp(1.0f + (streak - 1) * 0.12f, 1.0f, 2.2f);
            PlaySound(comboClip, 0.75f, pitch);
        }

        public void PlaySpeedUp() => PlaySound(speedUpClip, 0.85f, 1.0f);
        public void PlayWarningTick() => PlaySound(warningTickClip, 0.65f, 1.0f);
        public void PlayGameOver() => PlaySound(gameOverClip, 0.9f, 1.0f);
        public void PlayClick() => PlaySound(rotateClip, 0.35f, 1.35f);

        public void SetPaused(bool paused)
        {
            if (bgmSource != null) bgmSource.volume = paused ? 0.07f : 0.20f;
        }

        private void PlaySound(AudioClip clip, float volume, float pitch)
        {
            if (!IsSoundEnabled || clip == null) return;
            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(clip, volume);
        }

        public void PlayBGM()
        {
            if (bgmClip != null && !bgmSource.isPlaying)
            {
                bgmSource.clip = bgmClip;
                bgmSource.volume = 0.20f;
                bgmSource.Play();
            }
        }

        public void StopBGM()
        {
            bgmSource.Stop();
        }

        #region Procedural Audio Synthesis

        private void GenerateAudioClips()
        {
            moveClip = CreateToneClip("Move", 0.035f, 480f, 400f, WaveType.Sine, 0.3f);
            rotateClip = CreateToneClip("Rotate", 0.065f, 320f, 640f, WaveType.Triangle, 0.4f);
            softDropClip = CreateToneClip("SoftDrop", 0.025f, 240f, 180f, WaveType.Sine, 0.25f);
            hardDropClip = CreateImpactClip("HardDrop", 0.14f, 130f, 40f);
            lockClip = CreateClickClip("Lock", 0.045f, 540f);
            singleClearClip = CreateChimeClip("SingleClear", 0.22f, new[] { 523.25f, 659.25f }); // C5, E5
            multiClearClip = CreateChimeClip("MultiClear", 0.32f, new[] { 523.25f, 659.25f, 783.99f, 1046.50f }); // C5, E5, G5, C6
            megaClearClip = CreateChimeClip("MegaClear", 0.45f, new[] { 440f, 554.37f, 659.25f, 880f, 1108.73f }); // A-major grand fanfare
            comboClip = CreateToneClip("Combo", 0.12f, 600f, 850f, WaveType.Square, 0.35f);
            speedUpClip = CreateSirenClip("SpeedUp", 0.35f, 350f, 1100f);
            warningTickClip = CreateTickClip("Tick", 0.03f, 800f);
            gameOverClip = CreateChimeClip("GameOver", 0.55f, new[] { 523.25f, 493.88f, 440.0f, 349.23f }); // Descending minor
            bgmClip = GenerateBgmLoop();
        }

        private enum WaveType { Sine, Triangle, Square, Sawtooth }

        private AudioClip CreateToneClip(string name, float duration, float startFreq, float endFreq, WaveType wave, float decay)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float currentFreq = Mathf.Lerp(startFreq, endFreq, t);
                float phase = (2f * Mathf.PI * currentFreq * i) / sampleRate;

                float sample = 0f;
                switch (wave)
                {
                    case WaveType.Sine:
                        sample = Mathf.Sin(phase);
                        break;
                    case WaveType.Triangle:
                        sample = Mathf.PingPong(phase / Mathf.PI, 1f) * 2f - 1f;
                        break;
                    case WaveType.Square:
                        sample = Mathf.Sin(phase) >= 0 ? 0.7f : -0.7f;
                        break;
                    case WaveType.Sawtooth:
                        sample = (float)((phase / (2 * Mathf.PI)) % 1.0) * 2f - 1f;
                        break;
                }

                // Envelope: quick attack, smooth exponential decay
                float attack = Mathf.Clamp01(i / (sampleRate * 0.005f));
                float envelope = attack * Mathf.Exp(-t * (1.0f / Mathf.Max(0.01f, decay)));
                data[i] = sample * envelope;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateImpactClip(string name, float duration, float startFreq, float endFreq)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float currentFreq = Mathf.Lerp(startFreq, endFreq, Mathf.Pow(t, 0.4f));
                float phase = (2f * Mathf.PI * currentFreq * i) / sampleRate;

                // Sine bass punch + subtle white noise click at the start
                float sine = Mathf.Sin(phase);
                float noise = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-t * 28f) * 0.3f;
                float envelope = Mathf.Exp(-t * 12f);

                data[i] = (sine * 0.85f + noise) * envelope;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateClickClip(string name, float duration, float freq)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float phase = (2f * Mathf.PI * freq * i) / sampleRate;
                float val = Mathf.Sin(phase);
                float env = Mathf.Exp(-t * 30f);
                data[i] = val * env;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateTickClip(string name, float duration, float freq)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float phase = (2f * Mathf.PI * freq * i) / sampleRate;
                float val = Mathf.Sin(phase) * 0.7f + (UnityEngine.Random.value * 2f - 1f) * 0.3f;
                float env = Mathf.Exp(-t * 50f);
                data[i] = val * env;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateChimeClip(string name, float totalDuration, float[] noteFreqs)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * totalDuration);
            float[] data = new float[samples];

            float noteDuration = totalDuration / noteFreqs.Length;

            for (int i = 0; i < samples; i++)
            {
                float currentTime = (float)i / sampleRate;
                int noteIndex = Mathf.Clamp(Mathf.FloorToInt(currentTime / noteDuration), 0, noteFreqs.Length - 1);
                float noteTime = currentTime - (noteIndex * noteDuration);

                float freq = noteFreqs[noteIndex];
                float phase = (2f * Mathf.PI * freq * i) / sampleRate;
                float sample = Mathf.Sin(phase) * 0.7f + Mathf.Sin(phase * 2f) * 0.2f;

                float envelope = Mathf.Exp(-noteTime * 8f);
                data[i] = sample * envelope;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateSirenClip(string name, float duration, float lowFreq, float highFreq)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float freq = Mathf.Lerp(lowFreq, highFreq, Mathf.Pow(t, 1.5f));
                float phase = (2f * Mathf.PI * freq * i) / sampleRate;
                float sample = Mathf.Sin(phase);
                float envelope = Mathf.Sin(t * Mathf.PI); // Smooth rise and fall
                data[i] = sample * envelope * 0.75f;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateBgmLoop()
        {
            // Upbeat 128 BPM electronic puzzle rhythm loop (approx 7.5 seconds, 16 beats)
            int sampleRate = 22050; // lighter memory footprint
            float bpm = 130f;
            float beatDuration = 60f / bpm;
            int totalBeats = 16;
            float totalDuration = totalBeats * beatDuration;
            int totalSamples = Mathf.CeilToInt(totalDuration * sampleRate);
            float[] data = new float[totalSamples];

            // 4 chords progression: Am (A3), F (F3), C (C3), G (G3)
            float[] rootFreqs = { 220.00f, 174.61f, 130.81f, 196.00f };
            // 16th note arpeggio patterns (+0, +3/4, +7, +12)
            int[] chordTypes = { 1, 0, 0, 0 }; // 1 = minor (Am), 0 = major

            for (int beat = 0; beat < totalBeats; beat++)
            {
                int chordIndex = (beat / 4) % 4;
                float root = rootFreqs[chordIndex];
                int isMinor = chordTypes[chordIndex];
                float third = isMinor == 1 ? root * 1.1892f : root * 1.2599f;
                float fifth = root * 1.4983f;
                float octave = root * 2.0f;
                float[] chordNotes = { root, third, fifth, octave };

                // 4 sixteenth notes per beat
                for (int sub = 0; sub < 4; sub++)
                {
                    int noteIndex = (sub + beat) % 4;
                    float freq = chordNotes[noteIndex];

                    float noteStartTime = (beat * beatDuration) + (sub * (beatDuration / 4f));
                    int startSample = Mathf.FloorToInt(noteStartTime * sampleRate);
                    int noteSamples = Mathf.FloorToInt((beatDuration / 4f) * sampleRate);

                    for (int s = 0; s < noteSamples; s++)
                    {
                        int currentSample = startSample + s;
                        if (currentSample >= totalSamples) break;

                        float tNote = (float)s / sampleRate;
                        float phase = 2f * Mathf.PI * freq * (s / (float)sampleRate);

                        // Melodic synth tone (sine + soft saw)
                        float synth = Mathf.Sin(phase) * 0.6f + ((float)((phase / (2 * Mathf.PI)) % 1.0) * 2f - 1f) * 0.2f;
                        float env = Mathf.Exp(-tNote * 18f);

                        // Bass kick on beats 0, 4, 8, 12 and snare/hi-hat on offbeats
                        float kick = 0f;
                        if (sub == 0 && (beat % 2 == 0))
                        {
                            float kickPhase = 2f * Mathf.PI * (110f - tNote * 180f) * tNote;
                            kick = Mathf.Sin(kickPhase) * Mathf.Exp(-tNote * 22f) * 0.45f;
                        }

                        // Hi-hat noise on every offbeat 16th
                        float hat = 0f;
                        if (sub % 2 == 1)
                        {
                            hat = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-tNote * 55f) * 0.12f;
                        }

                        data[currentSample] += (synth * env * 0.28f) + kick + hat;
                    }
                }
            }

            // Normalize slightly to avoid clipping
            for (int i = 0; i < totalSamples; i++)
            {
                data[i] = Mathf.Clamp(data[i], -0.92f, 0.92f);
            }

            AudioClip bgm = AudioClip.Create("DropStackBGM", totalSamples, 1, sampleRate, false);
            bgm.SetData(data, 0);
            return bgm;
        }

        #endregion
    }
}
