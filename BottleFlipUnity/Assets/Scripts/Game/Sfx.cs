using System;
using UnityEngine;

namespace BottleFlip
{
    /// <summary>All sound is synthesized at runtime, so the build ships with zero audio assets.</summary>
    public class Sfx : MonoBehaviour
    {
        const int Rate = 44100;

        AudioSource _src;
        AudioClip _whoosh, _land, _fall, _click, _best, _pop;

        public bool Muted { get; private set; }

        public void Build()
        {
            _src = gameObject.AddComponent<AudioSource>();
            _src.playOnAwake = false;
            _src.spatialBlend = 0f;
            _src.volume = 0.85f;

            _whoosh = BuildWhoosh();
            _land = BuildLand();
            _fall = BuildFall();
            _click = BuildClick();
            _best = BuildBest();
            _pop = BuildPop();

            Muted = Prefs.GetInt("bf_muted", 0) == 1;
            ApplyMute();
        }

        public void ToggleMute()
        {
            Muted = !Muted;
            Prefs.SetInt("bf_muted", Muted ? 1 : 0);
            ApplyMute();
            if (!Muted) Click();
        }

        void ApplyMute()
        {
            AudioListener.volume = Muted ? 0f : 1f;
        }

        void Play(AudioClip clip, float volume, float pitch = 1f)
        {
            if (_src == null || clip == null) return;
            _src.pitch = pitch;
            _src.PlayOneShot(clip, volume);
        }

        public void Whoosh() => Play(_whoosh, 0.5f, UnityEngine.Random.Range(0.94f, 1.07f));
        public void Land() => Play(_land, 0.5f, UnityEngine.Random.Range(0.96f, 1.05f));
        public void Fall() => Play(_fall, 0.75f, UnityEngine.Random.Range(0.95f, 1.03f));
        public void Click() => Play(_click, 0.45f);
        public void Pop() => Play(_pop, 0.4f, UnityEngine.Random.Range(0.96f, 1.06f));
        public void Best() => Play(_best, 0.7f);

        // ---------- Synthesis ----------

        static AudioClip Make(string name, float seconds, Func<float, float> sample, float gain = 1f)
        {
            int count = Mathf.CeilToInt(seconds * Rate);
            var data = new float[count];
            float peak = 0.0001f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)Rate;
                data[i] = sample(t);
                peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            }
            float norm = gain / peak;
            for (int i = 0; i < count; i++) data[i] *= norm;

            var clip = AudioClip.Create(name, count, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Noise() => UnityEngine.Random.value * 2f - 1f;

        static AudioClip BuildWhoosh()
        {
            float lp = 0f, hp = 0f, hpPrev = 0f;
            return Make("whoosh", 0.32f, t =>
            {
                float env = Mathf.Sin(Mathf.Clamp01(t / 0.32f) * Mathf.PI);
                env *= env;
                float n = Noise();
                float cutoff = Mathf.Lerp(0.04f, 0.35f, Mathf.Clamp01(t / 0.2f));
                lp += (n - lp) * cutoff;
                hp = 0.92f * (hp + lp - hpPrev);
                hpPrev = lp;
                float tone = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(320f, 700f, t / 0.32f) * t) * 0.16f;
                return (hp + tone) * env * 0.85f;
            });
        }

        /// <summary>Soft, satisfying "tok" for a bottle planting on its base. Plays constantly, so it stays gentle.</summary>
        static AudioClip BuildLand()
        {
            return Make("land", 0.22f, t =>
            {
                float env = Mathf.Exp(-t * 24f);
                float tick = Mathf.Exp(-t * 70f);
                float body = Mathf.Sin(2f * Mathf.PI * 420f * t) * env * 0.7f;
                float ring = Mathf.Sin(2f * Mathf.PI * 1140f * t) * tick * 0.45f;
                float noise = Noise() * Mathf.Exp(-t * 110f) * 0.3f;
                return body + ring + noise;
            });
        }

        /// <summary>Heavy thud plus a plastic rattle: the bottle hit the table and tipped over.</summary>
        static AudioClip BuildFall()
        {
            return Make("fall", 0.6f, t =>
            {
                float drop = Mathf.Clamp01(t / 0.22f);
                float thud = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(135f, 46f, drop) * t) * Mathf.Exp(-t * 8.5f) * 0.9f;

                float rattle = 0f;
                float[] hits = { 0.12f, 0.23f, 0.33f };
                for (int i = 0; i < hits.Length; i++)
                {
                    float lt = t - hits[i];
                    if (lt > 0f)
                        rattle += Mathf.Sin(2f * Mathf.PI * (250f + i * 40f) * lt) * Mathf.Exp(-lt * 34f) * 0.32f;
                }

                float noise = Noise() * Mathf.Exp(-t * 16f) * 0.22f;
                return thud + rattle + noise;
            });
        }

        static AudioClip BuildClick()
        {
            return Make("click", 0.075f, t =>
            {
                float env = Mathf.Exp(-t * 70f);
                float a = Mathf.Sin(2f * Mathf.PI * 1180f * t);
                float b = Mathf.Sin(2f * Mathf.PI * 1760f * t) * 0.4f;
                return (a + b) * env;
            });
        }

        static AudioClip BuildPop()
        {
            return Make("pop", 0.14f, t =>
            {
                float env = Mathf.Exp(-t * 30f);
                float f = Mathf.Lerp(900f, 420f, Mathf.Clamp01(t / 0.09f));
                return Mathf.Sin(2f * Mathf.PI * f * t) * env;
            });
        }

        static AudioClip BuildBest()
        {
            float[] notes = { 783.99f, 1046.5f, 1318.5f, 1567.98f };
            return Make("best", 0.85f, t =>
            {
                float sum = 0f;
                for (int i = 0; i < notes.Length; i++)
                {
                    float start = i * 0.07f;
                    if (t < start) continue;
                    float lt = t - start;
                    float env = Mathf.Exp(-lt * 6.5f) * Mathf.Clamp01(lt * 500f);
                    sum += (Mathf.Sin(2f * Mathf.PI * notes[i] * lt) * 0.6f
                          + Mathf.Sin(2f * Mathf.PI * notes[i] * 2.02f * lt) * 0.2f) * env;
                }
                return sum * 0.55f;
            });
        }
    }
}
