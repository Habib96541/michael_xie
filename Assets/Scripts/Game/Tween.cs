using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonDash
{
    /// <summary>Easing curves used by every UI animation.</summary>
    public static class Ease
    {
        public static float Linear(float t) { return t; }

        public static float OutCubic(float t)
        {
            float u = 1f - t;
            return 1f - u * u * u;
        }

        public static float OutQuint(float t)
        {
            float u = 1f - t;
            return 1f - u * u * u * u * u;
        }

        public static float InOutSine(float t)
        {
            return 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI);
        }

        /// <summary>Overshoots past 1 and settles: the classic "card pops in" curve.</summary>
        public static float OutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        public static float OutBounce(float t)
        {
            if (t < 1f / 2.75f) return 7.5625f * t * t;
            if (t < 2f / 2.75f)
            {
                t -= 1.5f / 2.75f;
                return 7.5625f * t * t + 0.75f;
            }
            if (t < 2.5f / 2.75f)
            {
                t -= 2.25f / 2.75f;
                return 7.5625f * t * t + 0.9375f;
            }
            t -= 2.625f / 2.75f;
            return 7.5625f * t * t + 0.984375f;
        }
    }

    /// <summary>
    /// Tiny tween engine. Every UI motion in the game goes through this so animations are
    /// consistent and can be cancelled wholesale when a screen changes.
    /// </summary>
    public sealed class TweenRunner : MonoBehaviour
    {
        sealed class Item
        {
            public float Delay;
            public float Duration;
            public float Elapsed;
            public bool Started;
            public Action<float> Step;
            public Func<float, float> Curve;
            public Action Done;
            public string Tag;
        }

        static TweenRunner instance;
        readonly List<Item> items = new List<Item>();
        readonly List<Item> pending = new List<Item>();

        public static TweenRunner Instance
        {
            get
            {
                if (instance == null)
                {
                    var host = new GameObject("Tween Runner");
                    instance = host.AddComponent<TweenRunner>();
                    DontDestroyOnLoad(host);
                }
                return instance;
            }
        }

        /// <summary>Animates 0..1 over duration. Uses unscaled time so pauses still animate UI.</summary>
        public static void Run(float duration, Action<float> step, float delay = 0f,
                               Func<float, float> curve = null, Action done = null, string tag = null)
        {
            var item = new Item();
            item.Delay = Mathf.Max(0f, delay);
            item.Duration = Mathf.Max(0f, duration);
            item.Step = step;
            item.Curve = curve ?? Ease.OutCubic;
            item.Done = done;
            item.Tag = tag;
            Instance.pending.Add(item);
        }

        public static void Delay(float seconds, Action action)
        {
            Run(0f, null, seconds, null, action);
        }

        public static void KillTag(string tag)
        {
            if (instance == null) return;
            for (int i = instance.items.Count - 1; i >= 0; i--)
                if (instance.items[i].Tag == tag) instance.items.RemoveAt(i);
        }

        public static void KillAll()
        {
            if (instance == null) return;
            instance.items.Clear();
            instance.pending.Clear();
        }

        void Update()
        {
            if (pending.Count > 0)
            {
                items.AddRange(pending);
                pending.Clear();
            }

            float dt = Time.unscaledDeltaTime;
            for (int i = items.Count - 1; i >= 0; i--)
            {
                Item item = items[i];
                if (!item.Started)
                {
                    item.Delay -= dt;
                    if (item.Delay > 0f) continue;
                    item.Started = true;
                }

                item.Elapsed += dt;
                float raw = item.Duration <= 0f ? 1f : Mathf.Clamp01(item.Elapsed / item.Duration);
                if (item.Step != null) item.Step(item.Curve(raw));
                if (raw >= 1f)
                {
                    items.RemoveAt(i);
                    if (item.Done != null) item.Done();
                }
            }
        }
    }
}
