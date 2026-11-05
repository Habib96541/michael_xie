using UnityEngine;
using UnityEngine.UI;

namespace TeslaRoadster
{
    internal sealed class RoadsterSpeedLines
    {
        struct Streak
        {
            public Image image;
            public Vector2 ray;
            public float spacing, offset;
        }

        readonly Streak[] streaks = new Streak[24];
        readonly Vector2 vanishingPoint;

        public RoadsterSpeedLines(RectTransform layer)
        {
            Vector2 size = layer.sizeDelta;
            vanishingPoint = new Vector2(size.x * 0.5f, size.y * 0.64f);
            for (int i = 0; i < streaks.Length; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float spread = Mathf.Repeat((i + 1) * 0.618034f, 1f);
                Vector2 ray = new(side * size.x * Mathf.Lerp(0.34f, 0.495f, spread),
                    size.y * Mathf.Lerp(-0.63f, 0.35f, Mathf.Repeat(i * 0.381966f + 0.26f, 1f)));
                var image = new GameObject("Speed streak", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                RectTransform rect = image.rectTransform;
                rect.SetParent(layer, false);
                rect.anchorMin = rect.anchorMax = Vector2.zero;
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(ray.y, ray.x) * Mathf.Rad2Deg);
                image.raycastTarget = false;
                image.color = Color.clear;
                streaks[i] = new Streak { image = image, ray = ray, spacing = Mathf.Lerp(22f, 43f, spread), offset = spread };
            }
        }

        public void SetFlow(float distance, float speed, float thrust)
        {
            float strength = Mathf.InverseLerp(15f, 48f, speed);
            foreach (Streak streak in streaks)
            {
                float phase = Mathf.Repeat(distance / streak.spacing + streak.offset, 1f);
                float advance = Mathf.Lerp(0.26f, 1f, phase * phase);
                float span = Mathf.Lerp(0.035f, 0.18f, phase) * (0.65f + strength * 0.35f + thrust * 0.45f);
                float fade = Mathf.SmoothStep(0f, 1f, phase * 2.4f) * Mathf.SmoothStep(0f, 1f, (1f - phase) * 5f);
                float alpha = fade * strength * (0.25f + 0.26f * strength + 0.4f * thrust);
                RectTransform rect = streak.image.rectTransform;
                rect.anchoredPosition = vanishingPoint + streak.ray * (advance - span * 0.5f);
                rect.sizeDelta = new Vector2(streak.ray.magnitude * span, Mathf.Lerp(0.8f, 2.7f, phase));
                streak.image.color = new Color(1f, 0.96f, 0.91f, alpha);
            }
        }
    }
}
