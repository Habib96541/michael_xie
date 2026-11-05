using System;
using UnityEngine;

namespace CyberWhack.Audio
{
    /// <summary>
    /// Pure C# procedural cartoon audio synthesizer. Generates ultra-lightweight, copyright-safe,
    /// mono-channel audio clips in memory with zero external asset dependencies.
    /// Meets the strict <3.5 MB build size budget for mobile WebGL while delivering rich cartoon feedback.
    /// </summary>
    public static class ProceduralAudioGenerator
    {
        private const int SampleRate = 22050; // Optimized sample rate for mobile memory

        /// <summary>
        /// Satisfying cartoon mallet thwack / bonk when striking a mole.
        /// </summary>
        public static AudioClip CreateMetallicClunk()
        {
            float duration = 0.18f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = i / (float)SampleRate;
                float progress = t / duration;
                float envelope = Mathf.Exp(-t * 24.0f); // Fast punchy decay

                // Pitch envelope: downward bend from 560Hz down to 140Hz (classic rubber mallet bonk)
                float freq = Mathf.Lerp(560f, 140f, Mathf.Sqrt(progress));
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t);
                // Sub-resonance punch
                float punch = Mathf.Sin(2f * Mathf.PI * (freq * 0.5f) * t) * 0.4f;
                // Woody overtone
                float woodHarm = Mathf.Sin(2f * Mathf.PI * (freq * 2.15f) * t) * 0.25f;
                // Snappy impact pop
                float pop = (t < 0.012f) ? (UnityEngine.Random.value * 2f - 1f) * 0.4f * (1f - t / 0.012f) : 0f;

                samples[i] = Mathf.Clamp((wave + punch + woodHarm + pop) * envelope * 0.95f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("SFX_CartoonBonk", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Magical high-pitched crystal ding / star sparkle for the Golden Overclocked Mole.
        /// </summary>
        public static AudioClip CreateCrystalChime()
        {
            float duration = 0.40f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = i / (float)SampleRate;
                float envelope = Mathf.Exp(-t * 9.0f); // Sparkling sustain

                // High golden chimes (C6: 1046.5Hz, E6: 1318.5Hz, G6: 1567.98Hz, C7: 2093Hz)
                float wave1 = Mathf.Sin(2f * Mathf.PI * 1318.5f * t);
                float wave2 = Mathf.Sin(2f * Mathf.PI * 2093.0f * t) * 0.55f;
                float wave3 = Mathf.Sin(2f * Mathf.PI * 2637.0f * t) * 0.30f;
                // Tremolo shimmer
                float shimmer = 1f + 0.18f * Mathf.Sin(2f * Mathf.PI * 28f * t);

                samples[i] = Mathf.Clamp((wave1 + wave2 + wave3) * shimmer * envelope * 0.75f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("SFX_CrystalChime", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Construction hard-hat clank and crack sound for the Armored Mole.
        /// </summary>
        public static AudioClip CreateArmorCrunch()
        {
            float duration = 0.20f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = i / (float)SampleRate;
                float envelope = Mathf.Exp(-t * 20.0f);

                // Plastic/metal helmet clank + crack
                float f = Mathf.Lerp(820f, 220f, t / duration);
                float clank = Mathf.Sin(2f * Mathf.PI * f * t);
                float ring = Mathf.Sin(2f * Mathf.PI * 1480f * t) * 0.35f;

                // Plastic fracture crunch noise burst
                float crackNoise = (t < 0.045f) ? (UnityEngine.Random.value * 2f - 1f) * 0.55f : 0f;

                samples[i] = Mathf.Clamp((clank + ring + crackNoise) * envelope * 0.9f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("SFX_ArmorCrunch", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Cartoon ACME bomb explosion boom for the EMP Decoy Mine hazard.
        /// </summary>
        public static AudioClip CreateEmpHazardBuzz()
        {
            float duration = 0.45f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = i / (float)SampleRate;
                float envelope = Mathf.Exp(-t * 8.5f); // Deep reverberant rumble

                // Heavy sub-bass pitch drop (85Hz down to 28Hz)
                float f = Mathf.Lerp(85f, 28f, t / duration);
                float subBoom = Mathf.Sin(2f * Mathf.PI * f * t) * 0.8f;

                // Blast wave noise burst
                float blastNoise = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-t * 22f) * 0.65f;
                // Distorted overdrive
                float combined = subBoom + blastNoise;
                float distorted = Mathf.Clamp(combined * 1.6f, -0.95f, 0.95f);

                samples[i] = Mathf.Clamp(distorted * envelope, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("SFX_BombExplosion", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Muted hollow tap when striking empty dirt.
        /// </summary>
        public static AudioClip CreateMutedHollowTap()
        {
            float duration = 0.08f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = i / (float)SampleRate;
                float envelope = Mathf.Exp(-t * 52.0f); // Rapid muffled thud

                float f = Mathf.Lerp(190f, 75f, t / duration);
                float thud = Mathf.Sin(2f * Mathf.PI * f * t);

                samples[i] = Mathf.Clamp(thud * envelope * 0.7f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("SFX_MutedHollowTap", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Cheerful ascending cartoon fanfare for combo tier upgrades.
        /// </summary>
        public static AudioClip CreateComboTierUp()
        {
            float duration = 0.28f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            // 3 ascending notes: C5 (523Hz), E5 (659Hz), G5 (784Hz)
            for (int i = 0; i < totalSamples; i++)
            {
                float t = i / (float)SampleRate;
                float noteIdx = Mathf.Clamp(Mathf.Floor(t / 0.08f), 0, 2);
                float f = (noteIdx == 0) ? 523.25f : (noteIdx == 1) ? 659.25f : 783.99f;
                float noteT = t - (noteIdx * 0.08f);
                float envelope = Mathf.Exp(-noteT * 12.0f);

                float wave = Mathf.Sin(2f * Mathf.PI * f * t);
                float harm = Mathf.Sin(2f * Mathf.PI * (f * 2f) * t) * 0.25f;

                samples[i] = Mathf.Clamp((wave + harm) * envelope * 0.8f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("SFX_ComboTierUp", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Crisp cartoon bubble pop / button tap.
        /// </summary>
        public static AudioClip CreateUiClick()
        {
            float duration = 0.06f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = i / (float)SampleRate;
                float envelope = Mathf.Exp(-t * 60.0f);

                // Upward pitch chirp (pop sound)
                float f = Mathf.Lerp(450f, 1200f, t / duration);
                float wave = Mathf.Sin(2f * Mathf.PI * f * t);

                samples[i] = Mathf.Clamp(wave * envelope * 0.8f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("SFX_UiClick", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// A light, playful four-bar arcade loop with warm bass and plucky mallet notes.
        /// The final beat is sparse so the loop point stays soft under gameplay effects.
        /// </summary>
        public static AudioClip CreateBackgroundMusic()
        {
            const int beatsPerMinute = 100;
            const int beatsPerLoop = 16;
            int samplesPerBeat = SampleRate * 60 / beatsPerMinute;
            int totalSamples = samplesPerBeat * beatsPerLoop;
            float secondsPerBeat = 60f / beatsPerMinute;
            float[] samples = new float[totalSamples];

            // C, Am, F, G: bright and familiar, with a gentle storybook arcade feel.
            float[,] chordNotes =
            {
                { 261.63f, 329.63f, 392.00f },
                { 220.00f, 261.63f, 329.63f },
                { 174.61f, 220.00f, 261.63f },
                { 196.00f, 246.94f, 293.66f }
            };
            float[] bassNotes = { 65.41f, 55.00f, 43.65f, 49.00f };
            float[] melodyNotes =
            {
                392.00f, 0f, 659.25f, 0f, 783.99f, 659.25f, 523.25f, 0f,
                440.00f, 0f, 659.25f, 0f, 523.25f, 0f, 392.00f, 0f,
                349.23f, 0f, 523.25f, 0f, 698.46f, 523.25f, 440.00f, 0f,
                392.00f, 0f, 493.88f, 0f, 587.33f, 493.88f, 392.00f, 0f
            };

            for (int i = 0; i < totalSamples; i++)
            {
                float time = i / (float)SampleRate;
                float beatPosition = i / (float)samplesPerBeat;
                int bar = Mathf.Min((int)(beatPosition / 4f), 3);
                float barTime = time - bar * secondsPerBeat * 4f;

                float pad = 0f;
                for (int chordTone = 0; chordTone < 3; chordTone++)
                {
                    float frequency = chordNotes[bar, chordTone];
                    pad += Mathf.Sin(2f * Mathf.PI * frequency * time) * 0.026f;
                    pad += Mathf.Sin(2f * Mathf.PI * (frequency * 2f) * time) * 0.006f;
                }
                if (bar > 0 && barTime < 0.08f)
                {
                    float previousPad = 0f;
                    for (int chordTone = 0; chordTone < 3; chordTone++)
                    {
                        float frequency = chordNotes[bar - 1, chordTone];
                        previousPad += Mathf.Sin(2f * Mathf.PI * frequency * time) * 0.026f;
                        previousPad += Mathf.Sin(2f * Mathf.PI * (frequency * 2f) * time) * 0.006f;
                    }

                    float chordBlend = Mathf.SmoothStep(0f, 1f, barTime / 0.08f);
                    pad = Mathf.Lerp(previousPad, pad, chordBlend);
                }
                float padAttack = bar == 0 ? Mathf.Clamp01(time / 0.22f) : 1f;
                float loopFadeIn = Mathf.Clamp01(time / 0.08f);
                float loopFadeOut = Mathf.Clamp01((totalSamples / (float)SampleRate - time) / 0.08f);
                pad *= padAttack * loopFadeIn * loopFadeOut;

                int beat = Mathf.FloorToInt(beatPosition);
                float beatTime = time - beat * secondsPerBeat;
                float bass = 0f;
                if ((beat % 4) == 0 || (beat % 4) == 2)
                {
                    float bassEnvelope = Mathf.Exp(-beatTime * 8.5f) * Mathf.Clamp01(beatTime / 0.012f);
                    float bassPitch = bassNotes[bar];
                    bass = Mathf.Sin(2f * Mathf.PI * bassPitch * beatTime) * bassEnvelope * 0.16f;
                }

                int eighth = Mathf.Min((int)(beatPosition * 2f), melodyNotes.Length - 1);
                float melodyFrequency = melodyNotes[eighth];
                float eighthTime = time - eighth * (secondsPerBeat * 0.5f);
                float melody = 0f;
                if (melodyFrequency > 0f)
                {
                    float noteEnvelope = Mathf.Exp(-eighthTime * 7.5f) * Mathf.Clamp01(eighthTime / 0.008f);
                    float pluck = Mathf.Sin(2f * Mathf.PI * melodyFrequency * eighthTime);
                    pluck += Mathf.Sin(2f * Mathf.PI * melodyFrequency * 2f * eighthTime) * 0.22f;
                    melody = pluck * noteEnvelope * 0.12f;
                }

                samples[i] = Mathf.Clamp((pad + bass + melody) * loopFadeOut, -0.75f, 0.75f);
            }

            AudioClip clip = AudioClip.Create("BGM_MoonlitBurrow", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// A short soft arcade tick for each ready-countdown number.
        /// </summary>
        public static AudioClip CreateCountdownTick()
        {
            const float duration = 0.13f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];
            for (int i = 0; i < totalSamples; i++)
            {
                float time = i / (float)SampleRate;
                float envelope = Mathf.Exp(-time * 24f) * Mathf.Clamp01(time / 0.004f);
                float tone = Mathf.Sin(2f * Mathf.PI * 620f * time);
                float overtone = Mathf.Sin(2f * Mathf.PI * 1240f * time) * 0.2f;
                samples[i] = (tone + overtone) * envelope * 0.48f;
            }

            AudioClip clip = AudioClip.Create("SFX_CountdownTick", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// A bright two-note cue that marks the start of a round.
        /// </summary>
        public static AudioClip CreateRoundStartCue()
        {
            const float duration = 0.38f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];
            for (int i = 0; i < totalSamples; i++)
            {
                float time = i / (float)SampleRate;
                float firstNoteTime = time;
                float secondNoteTime = time - 0.12f;
                float firstEnvelope = Mathf.Exp(-firstNoteTime * 13f) * Mathf.Clamp01(firstNoteTime / 0.004f);
                float secondEnvelope = secondNoteTime > 0f
                    ? Mathf.Exp(-secondNoteTime * 9f) * Mathf.Clamp01(secondNoteTime / 0.004f)
                    : 0f;
                float first = Mathf.Sin(2f * Mathf.PI * 783.99f * firstNoteTime) * firstEnvelope * 0.34f;
                float second = Mathf.Sin(2f * Mathf.PI * 1046.5f * secondNoteTime) * secondEnvelope * 0.38f;
                float sparkle = secondNoteTime > 0f
                    ? Mathf.Sin(2f * Mathf.PI * 2093f * secondNoteTime) * secondEnvelope * 0.08f
                    : 0f;
                samples[i] = first + second + sparkle;
            }

            AudioClip clip = AudioClip.Create("SFX_RoundStart", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// A soft descending marimba cue to close the round without sounding harsh.
        /// </summary>
        public static AudioClip CreateRoundEndCue()
        {
            const float duration = 0.48f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];
            for (int i = 0; i < totalSamples; i++)
            {
                float time = i / (float)SampleRate;
                float firstEnvelope = Mathf.Exp(-time * 7f) * Mathf.Clamp01(time / 0.006f);
                float secondNoteTime = time - 0.16f;
                float secondEnvelope = secondNoteTime > 0f
                    ? Mathf.Exp(-secondNoteTime * 8f) * Mathf.Clamp01(secondNoteTime / 0.006f)
                    : 0f;
                float first = Mathf.Sin(2f * Mathf.PI * 587.33f * time) * firstEnvelope * 0.28f;
                float second = Mathf.Sin(2f * Mathf.PI * 392f * secondNoteTime) * secondEnvelope * 0.34f;
                float warmth = Mathf.Sin(2f * Mathf.PI * 784f * secondNoteTime) * secondEnvelope * 0.08f;
                samples[i] = first + second + warmth;
            }

            AudioClip clip = AudioClip.Create("SFX_RoundEnd", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
