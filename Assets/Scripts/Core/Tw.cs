using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crash
{
    public enum Ease { Linear, OutCubic, InCubic, OutQuint, OutBack, OutElastic, InOutSine, OutExpo }

    /// Minimal tween runner. Lives on a DontDestroyOnLoad object, always unscaled.
    public class TweenRunner : MonoBehaviour
    {
        class Item
        {
            public float t;
            public float dur;
            public Action<float> step;
            public Action done;
            public Ease ease;
        }

        static TweenRunner _inst;
        readonly List<Item> _items = new List<Item>();
        readonly List<Item> _pool = new List<Item>();

        public static TweenRunner I
        {
            get
            {
                if (_inst == null)
                {
                    var go = new GameObject("[Tweens]");
                    DontDestroyOnLoad(go);
                    _inst = go.AddComponent<TweenRunner>();
                }
                return _inst;
            }
        }

        public static float Eval(Ease e, float t)
        {
            if (t <= 0f) t = 0f; else if (t >= 1f) t = 1f;
            switch (e)
            {
                case Ease.Linear: return t;
                case Ease.InCubic: return t * t * t;
                case Ease.OutQuint: return 1f - Mathf.Pow(1f - t, 5f);
                case Ease.OutExpo: return t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
                case Ease.OutBack:
                {
                    const float c1 = 1.70158f;
                    const float c3 = c1 + 1f;
                    return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
                }
                case Ease.OutElastic:
                {
                    if (t <= 0f) return 0f;
                    if (t >= 1f) return 1f;
                    const float c4 = (2f * Mathf.PI) / 3f;
                    return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
                }
                case Ease.InOutSine: return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
                default: return 1f - Mathf.Pow(1f - t, 3f);
            }
        }

        public static void To(float dur, Action<float> step, Ease ease = Ease.OutCubic, Action done = null)
        {
            if (step == null) return;
            var r = I;
            Item it;
            if (r._pool.Count > 0)
            {
                it = r._pool[r._pool.Count - 1];
                r._pool.RemoveAt(r._pool.Count - 1);
            }
            else it = new Item();
            it.t = 0f;
            it.dur = Mathf.Max(0.0001f, dur);
            it.step = step;
            it.done = done;
            it.ease = ease;
            r._items.Add(it);
        }

        public static void Delay(float seconds, Action action)
        {
            if (action == null) return;
            To(seconds, _ => { }, Ease.Linear, action);
        }

        void Finish(int index, Item it)
        {
            _items[index] = null;
            it.step = null;
            it.done = null;
            _pool.Add(it);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < _items.Count; i++)
            {
                var it = _items[i];
                if (it == null) continue;

                it.t += dt;
                float k = Mathf.Clamp01(it.t / it.dur);

                bool completed = k >= 1f;
                Action done = null;
                if (completed) done = it.done;

                try
                {
                    if (!completed) it.step(Eval(it.ease, k));
                    else it.step(1f);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    Finish(i, it);
                    continue;
                }

                if (completed)
                {
                    Finish(i, it);
                    if (done != null)
                    {
                        try { done(); }
                        catch (Exception e) { Debug.LogException(e); }
                    }
                }
            }

            for (int i = _items.Count - 1; i >= 0; i--)
                if (_items[i] == null) _items.RemoveAt(i);
        }
    }
}
