using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace com.javierquevedo
{
    /// <summary>
    /// Manages Pop, Shatter & Combo VFX animations and trauma-based camera shake:
    /// 1. Expanding shockwave ring of neon plasma
    /// 2. Shattering crystalline polygon shards flying outward with directional motion
    /// 3. Concentrated electric spark flash at the center point
    /// 4. Floating glowing text callouts: '+100', '+300', 'x3 COMBO', 'SUPER COMBO', 'PERFECT CLEAR'
    /// 5. Smooth trauma-squared camera shake
    /// </summary>
    public class CyberVFXManager : MonoBehaviour
    {
        private static CyberVFXManager _instance;
        public static CyberVFXManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("CyberVFXManager");
                    _instance = go.AddComponent<CyberVFXManager>();
                }
                return _instance;
            }
        }

        private float _trauma = 0f;
        private float _shakeClock = 0f;
        private Vector3 _baseCameraPosition;
        private bool _hasBaseCameraPosition = false;
        private Camera _targetCamera;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        public void RegisterCamera(Camera cam, Vector3 restPos)
        {
            _targetCamera = cam;
            _baseCameraPosition = restPos;
            _hasBaseCameraPosition = true;
            if (_targetCamera != null)
                _targetCamera.transform.position = _baseCameraPosition;
        }

        public void AddCameraTrauma(float amount)
        {
            _trauma = Mathf.Clamp01(_trauma + amount);
        }

        void LateUpdate()
        {
            if (!_hasBaseCameraPosition || _targetCamera == null) return;

            if (_trauma > 0.001f)
            {
                _trauma = Mathf.Max(0f, _trauma - Time.unscaledDeltaTime * 2.2f);
                float shake = _trauma * _trauma;
                _shakeClock += Time.unscaledDeltaTime * 34f;
                float ox = Mathf.Sin(_shakeClock * 1.7f) * 0.22f * shake;
                float oy = Mathf.Cos(_shakeClock * 2.3f) * 0.18f * shake;
                _targetCamera.transform.position = _baseCameraPosition + new Vector3(ox, oy, 0f);
            }
            else
            {
                _targetCamera.transform.position = _baseCameraPosition;
            }
        }

        public void SpawnOrbPopBurst(Vector3 position, BubbleColor color)
        {
            Color primary = CyberArtGenerator.GetPrimaryColor(color);
            Color core = CyberArtGenerator.GetCoreColor(color);

            // 1. Expanding neon plasma shockwave ring
            StartCoroutine(AnimateShockwave(position, primary));

            // 2. Concentrated center electric spark flash
            StartCoroutine(AnimateCenterSpark(position, core));

            // 3. Shattering crystalline polygon shards flying outward
            int shardCount = 7;
            for (int i = 0; i < shardCount; i++)
            {
                float angle = (i * (360f / shardCount) + Random.Range(-18f, 18f)) * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                float speed = Random.Range(2.4f, 4.8f);
                StartCoroutine(AnimateShard(position, dir * speed, Color.Lerp(primary, Color.white, Random.Range(0.15f, 0.55f))));
            }
        }

        private IEnumerator AnimateShockwave(Vector3 pos, Color color)
        {
            GameObject go = new GameObject("ShockwaveRing");
            go.transform.position = new Vector3(pos.x, pos.y, -0.35f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CyberArtGenerator.GetShockwaveSprite();
            sr.sortingOrder = 25;
            sr.color = color;

            float duration = 0.24f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Ease-out expansion
                float easeOut = 1f - Mathf.Pow(1f - t, 3f);
                float scale = Mathf.Lerp(0.28f, 1.35f, easeOut);
                go.transform.localScale = new Vector3(scale, scale, 1f);
                Color c = color;
                c.a = (1f - t) * 0.9f;
                sr.color = c;
                yield return null;
            }
            Destroy(go);
        }

        private IEnumerator AnimateCenterSpark(Vector3 pos, Color color)
        {
            GameObject go = new GameObject("ElectricSpark");
            go.transform.position = new Vector3(pos.x, pos.y, -0.4f);
            go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 90f));
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CyberArtGenerator.GetSparkSprite();
            sr.sortingOrder = 26;
            sr.color = Color.white;

            float duration = 0.14f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float scale = t < 0.35f ? Mathf.Lerp(0.3f, 1.25f, t / 0.35f) : Mathf.Lerp(1.25f, 0.1f, (t - 0.35f) / 0.65f);
                go.transform.localScale = new Vector3(scale, scale, 1f);
                sr.color = Color.Lerp(Color.white, new Color(color.r, color.g, color.b, 0f), t);
                yield return null;
            }
            Destroy(go);
        }

        private IEnumerator AnimateShard(Vector3 startPos, Vector3 velocity, Color color)
        {
            GameObject go = new GameObject("CrystalShard");
            go.transform.position = new Vector3(startPos.x, startPos.y, -0.38f);
            float baseAngle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg - 90f;
            go.transform.rotation = Quaternion.Euler(0f, 0f, baseAngle);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CyberArtGenerator.GetShardSprite();
            sr.sortingOrder = 24;
            sr.color = color;

            float duration = Random.Range(0.22f, 0.34f);
            float elapsed = 0f;
            float spinSpeed = Random.Range(-320f, 320f);
            Vector3 pos = go.transform.position;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                pos += velocity * Time.deltaTime;
                velocity *= (1f - 3.5f * Time.deltaTime);
                velocity.y -= 3.8f * Time.deltaTime;
                go.transform.position = pos;
                go.transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);

                float s = Mathf.Lerp(0.75f, 0.12f, t);
                go.transform.localScale = new Vector3(s * 0.65f, s * 1.15f, 1f);
                Color c = color;
                c.a = 1f - t * t;
                sr.color = c;
                yield return null;
            }
            Destroy(go);
        }

        public void SpawnFloatingCallout(Vector3 worldPos, string text, Color neonColor, float scaleMultiplier = 1.0f)
        {
            StartCoroutine(AnimateFloatingCallout(worldPos, text, neonColor, scaleMultiplier));
        }

        private IEnumerator AnimateFloatingCallout(Vector3 worldPos, string text, Color neonColor, float scaleMult)
        {
            GameObject root = new GameObject("FloatingCallout");
            root.transform.position = new Vector3(
                Mathf.Clamp(worldPos.x, 1.6f, 8.9f),
                Mathf.Clamp(worldPos.y, 1.2f, 9.2f),
                -0.8f
            );

            // Neon drop glow / shadow TextMesh
            GameObject shadowGo = new GameObject("GlowShadow");
            shadowGo.transform.SetParent(root.transform, false);
            shadowGo.transform.localPosition = new Vector3(0.045f, -0.045f, 0.02f);
            TextMesh shadowTm = shadowGo.AddComponent<TextMesh>();
            shadowTm.text = text;
            shadowTm.anchor = TextAnchor.MiddleCenter;
            shadowTm.alignment = TextAlignment.Center;
            shadowTm.fontSize = 48;
            shadowTm.characterSize = 0.085f * scaleMult;
            shadowTm.fontStyle = FontStyle.Bold;
            shadowTm.color = new Color(0.02f, 0.03f, 0.06f, 0.92f);
            MeshRenderer shadowMr = shadowGo.GetComponent<MeshRenderer>();
            if (shadowMr != null) shadowMr.sortingOrder = 40;

            // Foreground glowing text
            TextMesh tm = root.AddComponent<TextMesh>();
            tm.text = text;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = 48;
            tm.characterSize = 0.085f * scaleMult;
            tm.fontStyle = FontStyle.Bold;
            tm.color = neonColor;
            MeshRenderer mr = root.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 41;

            float duration = 0.85f;
            float elapsed = 0f;
            Vector3 startPos = root.transform.position;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Pop-in overshoot scale then gentle float upward
                float popScale = t < 0.18f
                    ? Mathf.Lerp(0.4f, 1.22f, t / 0.18f)
                    : (t < 0.32f ? Mathf.Lerp(1.22f, 1.0f, (t - 0.18f) / 0.14f) : 1.0f);

                root.transform.localScale = Vector3.one * popScale;
                root.transform.position = startPos + new Vector3(0f, t * 1.15f, 0f);

                float alpha = t < 0.65f ? 1f : 1f - ((t - 0.65f) / 0.35f);
                Color fg = Color.Lerp(Color.white, neonColor, Mathf.Clamp01(t * 2.5f));
                fg.a = alpha;
                tm.color = fg;

                Color bg = shadowTm.color;
                bg.a = alpha * 0.9f;
                shadowTm.color = bg;

                yield return null;
            }

            Destroy(root);
        }
    }
}
