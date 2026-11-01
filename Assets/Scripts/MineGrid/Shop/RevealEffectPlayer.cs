using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MineGrid
{
    public sealed class RevealEffectPlayer : MonoBehaviour
    {
        private readonly List<GameObject> _particles = new List<GameObject>();
        private Coroutine _animation;
        public void Play(RevealStyle style, Color color, bool reduced)
        {
            Clear();
            if (reduced)
            {
                return;
            }

            _animation = StartCoroutine(Animate(style, color));
        }

        private IEnumerator Animate(RevealStyle style, Color color)
        {
            int count = style == RevealStyle.Starburst ? 12 : style == RevealStyle.GoldenTrail ? 6 : 8;
            var rects = new RectTransform[count];
            var images = new Image[count];
            for (int i = 0; i < count; i++)
            {
                var particle = new GameObject("Cosmetic sparkle", typeof(RectTransform), typeof(Image));
                particle.transform.SetParent(transform, false);
                _particles.Add(particle);
                rects[i] = (RectTransform)particle.transform;
                rects[i].sizeDelta = style == RevealStyle.Starburst ? new Vector2(12, 12) : new Vector2(5, 15);
                rects[i].localRotation = Quaternion.Euler(0, 0, style == RevealStyle.Starburst ? 45 : i * 45);
                images[i] = particle.GetComponent<Image>();
                images[i].raycastTarget = false;
            }

            for (float time = 0; time < .65f; time += Time.unscaledDeltaTime)
            {
                for (int i = 0; i < count; i++)
                {
                    float angle = i * Mathf.PI * 2 / count;
                    rects[i].anchoredPosition = style == RevealStyle.GoldenTrail ? new Vector2(Mathf.Sin(time * 9 + i) * 18, 25 - time * 110 + i * 7) : new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (15 + time * (style == RevealStyle.Starburst ? 130 : 90));
                    images[i].color = new Color(color.r, color.g, color.b, 1 - time / .65f);
                }

                yield return null;
            }

            _animation = null;
            Clear();
        }

        private void Clear()
        {
            if (_animation != null)
            {
                StopCoroutine(_animation);
            }

            _animation = null;
            foreach (GameObject particle in _particles)
            {
                if (particle)
                {
                    Destroy(particle);
                }
            }

            _particles.Clear();
        }

        private void OnDisable() => Clear();
    }
}
