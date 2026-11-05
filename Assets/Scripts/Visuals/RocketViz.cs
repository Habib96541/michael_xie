using UnityEngine;
using UnityEngine.UI;

namespace Crash
{
    /// The ship riding the curve head: body, exhaust flame, halo and bob animation.
    public class RocketViz : MonoBehaviour
    {
        public RectTransform rt;
        public RectTransform bodyRt;
        public Image body;
        public Image flame;
        public Image flameCore;
        public Image halo;
        public float tension = 1f;
        public bool flying;

        float _t;
        float _bobSeed = 0.37f;

        public static RocketViz Create(RectTransform parent)
        {
            var root = UIKit.Rect(parent, "Rocket");
            root.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(104f, 236f));

            var v = root.gameObject.AddComponent<RocketViz>();
            v.rt = root;

            v.halo = UIKit.Img(root, "Halo", Tex.Glow(128), Pal.Cyan.WithAlpha(0.20f));
            v.halo.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 118f), new Vector2(330f, 330f));

            v.flame = UIKit.Img(root, "Flame", Tex.Flame(128, 260), Color.white);
            v.flame.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(58f, 140f));

            v.flameCore = UIKit.Img(root, "FlameCore", Tex.Flame(128, 260), Color.white);
            v.flameCore.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(26f, 76f));
            v.flameCore.color = new Color(1f, 1f, 1f, 0.95f);

            v.body = UIKit.Img(root, "Body", Tex.Rocket(168, 360), Color.white);
            v.bodyRt = v.body.rectTransform;
            v.bodyRt.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(104f, 236f));
            v.body.preserveAspect = true;

            return v;
        }

        public void SetVisible(bool visible)
        {
            if (rt != null) rt.gameObject.SetActive(visible);
        }

        public void SetTension(float multiplier)
        {
            tension = Mathf.Max(1f, multiplier);
        }

        public void ResetShip()
        {
            if (halo != null) halo.color = Pal.Cyan.WithAlpha(0.20f);
            if (body != null) body.color = Color.white;
            if (flame != null) flame.enabled = true;
            if (flameCore != null) flameCore.enabled = true;
            if (bodyRt != null) bodyRt.localScale = Vector3.one;
            if (rt != null) rt.localRotation = Quaternion.identity;
        }

        public void CashOutLook()
        {
            if (body != null) body.color = Color.Lerp(Color.white, Pal.Gold, 0.55f);
            if (halo != null) halo.color = Pal.Gold.WithAlpha(0.30f);
            if (flame != null) flame.enabled = false;
            if (flameCore != null) flameCore.enabled = false;
        }

        public void HideBody()
        {
            if (body != null) body.enabled = false;
            if (flame != null) flame.enabled = false;
            if (flameCore != null) flameCore.enabled = false;
            if (halo != null) halo.enabled = false;
        }

        void Update()
        {
            float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            _t += dt;
            float m = Mathf.Clamp(tension, 1f, 30f);

            if (flame != null && flame.enabled)
            {
                float flicker = 1f + 0.16f * Mathf.Sin(_t * 34f) + 0.07f * Mathf.Sin(_t * 61f + 1.3f);
                float len = Mathf.Lerp(110f, 190f, Mathf.Clamp01((m - 1f) / 6f)) * flicker;
                flame.rectTransform.sizeDelta = new Vector2(58f * (1f + 0.06f * Mathf.Sin(_t * 27f)), len);
                var c = flame.color;
                c.a = 0.85f + 0.15f * Mathf.Sin(_t * 41f);
                flame.color = c;
                if (flameCore != null)
                {
                    flameCore.rectTransform.sizeDelta = new Vector2(26f, len * 0.55f);
                    flameCore.rectTransform.anchoredPosition = new Vector2(0f, -4f);
                }
            }

            if (halo != null && halo.enabled)
            {
                float pulse = 0.16f + 0.07f * Mathf.Sin(_t * 3.1f) + Mathf.Clamp01((m - 1f) / 10f) * 0.16f;
                var hc = halo.color;
                hc.a = pulse;
                halo.color = hc;
                float hs = 1f + 0.05f * Mathf.Sin(_t * 2.2f);
                halo.rectTransform.localScale = new Vector3(hs, hs, 1f);
            }

            if (bodyRt != null && body != null && body.enabled)
            {
                float bob = Mathf.Sin(_t * 4.4f + _bobSeed) * 2.2f;
                bodyRt.anchoredPosition = new Vector2(0f, bob);
            }
        }
    }
}
