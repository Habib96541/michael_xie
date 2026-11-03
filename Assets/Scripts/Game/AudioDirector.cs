using System;
using UnityEngine;

namespace NeonDash
{
    /// <summary>
    /// All audio is synthesised at runtime: a looping 128 BPM chiptune track and short effects
    /// for jumps, orbs, pads, portals, death and UI clicks. No audio files to license or ship.
    /// </summary>
    public sealed class AudioDirector : MonoBehaviour
    {
        const int SampleRate = 44100;

        AudioSource musicSource;
        AudioSource sfxSource;

        AudioClip jumpClip;
        AudioClip orbClip;
        AudioClip padClip;
        AudioClip portalClip;
        AudioClip deathClip;
        AudioClip clickClip;
        AudioClip multiplyClip;

        public bool Muted { get; private set; }

        void Awake()
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = 0.34f;
            // Synthesise the loop up front so starting a run never hitches.
            musicSource.clip = BuildMusicLoop();

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.volume = 0.55f;

            jumpClip = BuildJump();
            orbClip = BuildTone("Orb", 980f, 1400f, 0.16f, 0.5f);
            padClip = BuildTone("Pad", 520f, 1200f, 0.26f, 0.55f);
            portalClip = BuildSweep();
            deathClip = BuildDeath();
            clickClip = BuildTone("Click", 720f, 480f, 0.07f, 0.35f);
            multiplyClip = BuildTone("Multiply", 660f, 1320f, 0.3f, 0.45f);
        }

        public void StartMusic()
        {
            if (musicSource.clip == null) musicSource.clip = BuildMusicLoop();
            if (!musicSource.isPlaying) musicSource.Play();
        }

        public void RestartMusic()
        {
            if (musicSource.clip == null) musicSource.clip = BuildMusicLoop();
            musicSource.Stop();
            musicSource.Play();
        }

        public void StopMusic() { musicSource.Stop(); }
        public void SetMuted(bool muted) { Muted = muted; musicSource.mute = muted; sfxSource.mute = muted; }
        public bool IsMuted { get { return Muted; } }

        public void PlayJump() { Play(jumpClip, 0.4f); }
        public void PlayOrb() { Play(orbClip, 0.55f); }
        public void PlayPad() { Play(padClip, 0.55f); }
        public void PlayPortal() { Play(portalClip, 0.5f); }
        public void PlayDeath() { Play(deathClip, 0.75f); }
        public void PlayClick() { Play(clickClip, 0.4f); }
        public void PlayMultiply() { Play(multiplyClip, 0.5f); }

        void Play(AudioClip clip, float volume)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip, volume);
        }

        // ------------------------------------------------------------------
        // Synthesis
        // ------------------------------------------------------------------
        static AudioClip MakeClip(string name, float[] samples)
        {
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        static AudioClip BuildTone(string name, float startHz, float endHz, float seconds, float gain)
        {
            int count = Mathf.CeilToInt(SampleRate * seconds);
            var samples = new float[count];
            float phase = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                float hz = Mathf.Lerp(startHz, endHz, t);
                phase += hz / SampleRate;
                float square = Mathf.Sign(Mathf.Sin(phase * Mathf.PI * 2f)) * 0.5f;
                float envelope = Mathf.Pow(1f - t, 2.2f);
                samples[i] = square * envelope * gain;
            }
            return MakeClip(name, samples);
        }

        static AudioClip BuildJump()
        {
            int count = Mathf.CeilToInt(SampleRate * 0.1f);
            var samples = new float[count];
            float phase = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                float hz = Mathf.Lerp(520f, 900f, Mathf.Sqrt(t));
                phase += hz / SampleRate;
                float square = Mathf.Sign(Mathf.Sin(phase * Mathf.PI * 2f));
                float envelope = Mathf.Pow(1f - t, 3f);
                samples[i] = square * envelope * 0.32f;
            }
            return MakeClip("Jump", samples);
        }

        static AudioClip BuildSweep()
        {
            int count = Mathf.CeilToInt(SampleRate * 0.3f);
            var samples = new float[count];
            float previous = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                float noise = UnityEngine.Random.Range(-1f, 1f);
                previous = Mathf.Lerp(previous, noise, 0.06f + t * 0.4f);
                float tone = Mathf.Sin(i / (float)SampleRate * Mathf.PI * 2f * Mathf.Lerp(300f, 1500f, t)) * 0.3f;
                samples[i] = (previous * 0.7f + tone) * Mathf.Pow(1f - t, 1.6f) * 0.55f;
            }
            return MakeClip("Portal", samples);
        }

        static AudioClip BuildDeath()
        {
            int count = Mathf.CeilToInt(SampleRate * 0.55f);
            var samples = new float[count];
            float phase = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                float hz = Mathf.Lerp(420f, 60f, t);
                phase += hz / SampleRate;
                float saw = (phase % 1f) * 2f - 1f;
                float noise = UnityEngine.Random.Range(-1f, 1f) * Mathf.Pow(1f - t, 4f);
                samples[i] = (saw * 0.35f + noise * 0.5f) * Mathf.Pow(1f - t, 1.4f) * 0.7f;
            }
            return MakeClip("Death", samples);
        }

        /// <summary>Four bars of 128 BPM chiptune: kick, hats, square bass and an arpeggio.</summary>
        static AudioClip BuildMusicLoop()
        {
            const float bpm = 128f;
            const int bars = 4;
            float beat = 60f / bpm;
            float total = beat * 4f * bars;
            int count = Mathf.CeilToInt(SampleRate * total);
            var samples = new float[count];

            int[] bassNotes = { 45, 45, 48, 43 };
            int[] arpNotes = { 69, 72, 76, 72, 69, 72, 79, 76 };

            for (int i = 0; i < count; i++)
            {
                float time = i / (float)SampleRate;
                float sixteenth = beat * 0.25f;
                int step = Mathf.FloorToInt(time / sixteenth);
                float stepPhase = (time % sixteenth) / sixteenth;

                // Kick on every beat.
                float kickPhase = (time % beat) / beat;
                float kick = Mathf.Sin(kickPhase * Mathf.PI * 2f * Mathf.Lerp(90f, 45f, kickPhase))
                             * Mathf.Pow(1f - kickPhase, 6f) * 0.5f;

                // Hat on the off-beat eighths.
                float eighth = (time % (beat * 0.5f)) / (beat * 0.5f);
                float hat = (UnityEngine.Random.value * 2f - 1f) * Mathf.Pow(1f - eighth, 22f) * 0.12f;

                // Bass: square wave per beat.
                int bassNote = bassNotes[(step / 16) % bassNotes.Length];
                float bassHz = 440f * Mathf.Pow(2f, (bassNote - 69) / 12f);
                float bass = Mathf.Sign(Mathf.Sin(time * bassHz * Mathf.PI * 2f)) * 0.10f;
                bass *= 1f - Mathf.Clamp01(stepPhase * 0.25f);

                // Arpeggio on sixteenths.
                int arpNote = arpNotes[step % arpNotes.Length];
                float arpHz = 440f * Mathf.Pow(2f, (arpNote - 69) / 12f);
                float arp = Mathf.Sin(time * arpHz * Mathf.PI * 2f) * 0.16f * Mathf.Pow(1f - stepPhase, 2.4f);

                float mixed = (kick + hat + bass + arp) * 0.9f;
                // Soft clip.
                samples[i] = (float)Math.Tanh(mixed * 1.3f) * 0.7f;
            }

            return MakeClip("Geometry Dash Theme", samples);
        }
    }
}
