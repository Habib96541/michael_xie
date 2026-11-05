using System;
using System.Collections;
using UnityEngine;

namespace BottleFlip
{
    /// <summary>Easing curves used by every animation in the game.</summary>
    public static class Ease
    {
        public static float Linear(float t) => t;
        public static float InQuad(float t) => t * t;
        public static float OutQuad(float t) => 1f - (1f - t) * (1f - t);
        public static float InOutQuad(float t) => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
        public static float InCubic(float t) => t * t * t;
        public static float OutCubic(float t) { float f = 1f - t; return 1f - f * f * f; }
        public static float InOutCubic(float t) => t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
        public static float OutQuart(float t) { float f = 1f - t; return 1f - f * f * f * f; }
        public static float OutExpo(float t) => t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);

        public static float OutBack(float t, float overshoot = 1.70158f)
        {
            float c3 = overshoot + 1f;
            float p = t - 1f;
            return 1f + c3 * p * p * p + overshoot * p * p;
        }

        public static float InBack(float t, float overshoot = 1.70158f)
        {
            float c3 = overshoot + 1f;
            return c3 * t * t * t - overshoot * t * t;
        }

        public static float OutElastic(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            const float c4 = (2f * Mathf.PI) / 3f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }

        public static float OutBounce(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;
            if (t < 1f / d1) return n1 * t * t;
            if (t < 2f / d1) { t -= 1.5f / d1; return n1 * t * t + 0.75f; }
            if (t < 2.5f / d1) { t -= 2.25f / d1; return n1 * t * t + 0.9375f; }
            t -= 2.625f / d1;
            return n1 * t * t + 0.984375f;
        }

        public static float Clamp01(float t) => t < 0f ? 0f : (t > 1f ? 1f : t);
    }

    /// <summary>Hidden host that keeps small animation coroutines alive.</summary>
    public class TweenHost : MonoBehaviour
    {
        static TweenHost _instance;

        public static TweenHost Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("~TweenHost");
                    go.hideFlags = HideFlags.HideAndDontSave;
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    _instance = go.AddComponent<TweenHost>();
                }
                return _instance;
            }
        }

        public static TweenHost Ensure() => Instance;
    }

    /// <summary>Tiny tween library: fire and forget, unscaled by default so UI stays snappy during slow motion.</summary>
    public static class Tween
    {
        public static Coroutine To(float duration, Action<float> update, Func<float, float> ease = null, Action done = null, bool unscaled = true)
        {
            ease ??= Ease.Linear;
            if (duration <= 0f)
            {
                update?.Invoke(1f);
                done?.Invoke();
                return null;
            }
            return TweenHost.Instance.StartCoroutine(Run(duration, update, ease, done, unscaled));
        }

        static IEnumerator Run(float duration, Action<float> update, Func<float, float> ease, Action done, bool unscaled)
        {
            float t = 0f;
            while (t < duration)
            {
                update?.Invoke(ease(Ease.Clamp01(t / duration)));
                yield return null;
                t += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            }
            update?.Invoke(ease(1f));
            done?.Invoke();
        }

        public static Coroutine Delay(float duration, Action done)
        {
            return TweenHost.Instance.StartCoroutine(DelayRoutine(duration, done));
        }

        static IEnumerator DelayRoutine(float duration, Action done)
        {
            float t = 0f;
            while (t < duration)
            {
                yield return null;
                t += Time.unscaledDeltaTime;
            }
            done?.Invoke();
        }

        public static Coroutine Float(float from, float to, float duration, Action<float> update, Func<float, float> ease = null, Action done = null, bool unscaled = true)
        {
            return To(duration, t => update(Mathf.LerpUnclamped(from, to, t)), ease, done, unscaled);
        }
    }
}
