using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Crash
{
    /// One place for all juice: shake, flash, sparks and floating text.
    public class Fx : MonoBehaviour
    {
        class P
        {
            public Image img;
            public Vector2 pos;
            public Vector2 vel;
            public float life;
            public float max;
            public float size;
            public float gravity;
            public float drag;
        }

        public static Fx I;

        RectTransform _layer;
        RectTransform _shake;
        Vector2 _shakeBase;
        float _shakeAmt;
        Image _flash;
        readonly List<P> _ps = new List<P>();

        public void Init(RectTransform layer, RectTransform shakeTarget)
        {
            I = this;
            _layer = layer;
            _shake = shakeTarget;
            if (_shake != null) _shakeBase = _shake.anchoredPosition;

            _flash = UIKit.Img(layer, "Flash", null, new Color(1f, 1f, 1f, 0f));
            _flash.rectTransform.Stretch(-400, -400, -400, -400);
            _flash.transform.SetAsLastSibling();
        }

        public void Shake(float amount, float dur = 0.55f)
        {
            _shakeAmt = Mathf.Max(_shakeAmt, amount);
        }

        public void Flash(Color c, float alpha = 0.45f, float dur = 0.45f)
        {
            if (_flash == null) return;
            alpha = Mathf.Clamp(alpha, 0f, 0.32f);
            _flash.color = c.WithAlpha(alpha);
            TweenRunner.To(dur, k =>
            {
                if (_flash != null) _flash.color = c.WithAlpha(Mathf.Lerp(alpha, 0f, k));
            }, Ease.OutCubic, () => { if (_flash != null) _flash.color = c.WithAlpha(0f); });
        }

        P Get()
        {
            for (int i = 0; i < _ps.Count; i++)
                if (_ps[i].life <= 0f) return _ps[i];
            var img = UIKit.Img(_layer, "Sparks", Tex.Circle(32), Color.white);
            img.transform.SetAsLastSibling();
            var p = new P { img = img };
            _ps.Add(p);
            return p;
        }

        public void Burst(Vector3 worldPos, Color color, int count, float power = 520f, float size = 9f, float gravity = 620f)
        {
            for (int i = 0; i < count; i++)
            {
                var p = Get();
                float ang = Random.value * Mathf.PI * 2f;
                float sp = power * Random.Range(0.25f, 1f);
                p.pos = worldPos;
                p.vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * sp + Vector2.up * power * 0.25f;
                p.max = Random.Range(0.4f, 1.0f);
                p.life = p.max;
                p.size = size * Random.Range(0.55f, 1.6f);
                p.gravity = gravity;
                p.drag = 1.6f;
                p.img.sprite = Random.value > 0.4f ? Tex.Circle(32) : Tex.Glow(64);
                p.img.color = color;
                p.img.rectTransform.sizeDelta = new Vector2(p.size, p.size);
                p.img.rectTransform.position = worldPos;
                p.img.enabled = true;
                p.img.transform.SetAsLastSibling();
            }
        }

        public void Trail(Vector3 worldPos, Color color, float size = 7f)
        {
            var p = Get();
            p.pos = (Vector2)worldPos + new Vector2(Random.Range(-5f, 5f), Random.Range(-5f, 5f));
            p.vel = new Vector2(Random.Range(-30f, 30f), Random.Range(-170f, -60f));
            p.max = Random.Range(0.22f, 0.42f);
            p.life = p.max;
            p.size = size * Random.Range(0.6f, 1.2f);
            p.gravity = -40f;
            p.drag = 0.4f;
            p.img.sprite = Tex.Glow(64);
            p.img.color = color;
            p.img.rectTransform.sizeDelta = new Vector2(p.size, p.size);
            p.img.rectTransform.position = worldPos;
            p.img.enabled = true;
            p.img.transform.SetAsLastSibling();
        }

        public void Float(Vector3 worldPos, string text, Color color, int size = 46, float rise = 170f, float dur = 1.15f)
        {
            var label = UIKit.Label(_layer, "Float", text, size, color, TextAnchor.MiddleCenter, true);
            var rt = label.rectTransform;
            rt.position = worldPos;
            rt.localScale = Vector3.one * 0.6f;
            Vector3 start = worldPos;
            TweenRunner.To(dur, k =>
            {
                if (label == null) return;
                rt.position = start + new Vector3(0f, rise * (1f - Mathf.Pow(1f - k, 2.2f)), 0f);
                rt.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.1f, Mathf.Min(1f, k * 7f));
                var c = label.color;
                c.a = 1f - Mathf.Clamp01((k - 0.5f) / 0.5f);
                label.color = c;
            }, Ease.Linear, () =>
            {
                if (label != null) Destroy(label.gameObject);
            });
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt > 0.06f) dt = 0.06f;
            for (int i = 0; i < _ps.Count; i++)
            {
                var p = _ps[i];
                if (p.life <= 0f || p.img == null) continue;
                p.life -= dt;
                if (p.life <= 0f)
                {
                    p.img.enabled = false;
                    continue;
                }
                p.vel.y -= p.gravity * dt;
                p.vel -= p.vel * (p.drag * dt);
                p.pos += p.vel * dt;
                p.img.rectTransform.position = p.pos;
                float k = Mathf.Clamp01(p.life / p.max);
                p.img.color = new Color(p.img.color.r, p.img.color.g, p.img.color.b, k);
                p.img.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.4f, 1f, k);
            }

            if (_shake == null) return;
            if (_shakeAmt > 0.5f)
            {
                _shakeAmt = Mathf.Lerp(_shakeAmt, 0f, dt * 7.5f);
                _shake.anchoredPosition = _shakeBase + Random.insideUnitCircle * _shakeAmt;
            }
            else if (_shakeAmt > 0f)
            {
                _shakeAmt = 0f;
                _shake.anchoredPosition = _shakeBase;
            }
        }
    }
}
