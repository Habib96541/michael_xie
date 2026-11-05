using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CyberWhack.VFX
{
    /// <summary>
    /// Decaying trauma shake on the playfield only, plus a short full-screen
    /// flash. The HUD stays put, and shake uses unscaled time so a hit-stop
    /// still reads.
    /// </summary>
    public class PlayfieldShake : MonoBehaviour
    {
        private RectTransform _rect;
        private float _trauma;
        private float _time;
        private float _flash;
        private Image _flashImage;
        private Color _flashColor = new Color(1f, 0.96f, 0.82f, 1f);
        private static float _hitStopUntil;
        private static int _hitStopUsers;

        public void AddTrauma(float amount)
        {
            if (ArcadeFeel.ShakeScale <= 0.01f) return;
            _trauma = Mathf.Clamp01(_trauma + amount);
        }

        public void PunchFlash(float alpha)
        {
            PunchFlash(alpha, new Color(1f, 0.96f, 0.82f, 1f));
        }

        public void PunchFlash(float alpha, Color tint)
        {
            if (ArcadeFeel.FlashScale <= 0.01f) return;
            _flashColor = tint;
            _flash = Mathf.Max(_flash, alpha);
            EnsureFlash();
            if (_flashImage != null)
            {
                _flashImage.gameObject.SetActive(true);
                _flashImage.transform.SetAsLastSibling();
            }
        }

        private void Awake()
        {
            _rect = transform as RectTransform;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (_trauma > 0f && _rect != null)
            {
                _trauma = Mathf.Max(0f, _trauma - 1.7f * dt);
                float shake = _trauma * _trauma * ArcadeFeel.ShakeScale;
                _time += dt * 34f;
                float x = 16f * shake * Mathf.Sin(_time * 1.7f);
                float y = 10f * shake * Mathf.Sin(_time * 2.3f + 1.2f);
                _rect.anchoredPosition = new Vector2(x, y);
            }
            else if (_rect != null && _rect.anchoredPosition != Vector2.zero)
            {
                _rect.anchoredPosition = Vector2.zero;
            }

            if (_flash > 0f && _flashImage != null)
            {
                _flash = Mathf.Max(0f, _flash - dt * 3.4f);
                Color color = _flashColor;
                color.a = _flash;
                _flashImage.color = color;
                if (_flash <= 0f) _flashImage.gameObject.SetActive(false);
            }
        }

        private void EnsureFlash()
        {
            if (_flashImage != null) return;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            Transform existing = canvas.transform.Find("ImpactFlash");
            GameObject flashObject;
            if (existing != null)
            {
                flashObject = existing.gameObject;
            }
            else
            {
                flashObject = new GameObject("ImpactFlash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                flashObject.transform.SetParent(canvas.transform, false);
            }

            RectTransform rect = flashObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            _flashImage = flashObject.GetComponent<Image>();
            _flashImage.raycastTarget = false;
            _flashImage.color = new Color(1f, 1f, 1f, 0f);
            flashObject.SetActive(false);
        }

        public static void RequestHitStop(float duration, float scale)
        {
            if (duration <= 0f || CyberGameDirectorMissing()) return;
            MonoBehaviour host = ArcadeFeel.Host;
            if (host == null || !host.isActiveAndEnabled) return;
            float until = Time.unscaledTime + duration;
            if (until <= _hitStopUntil) return;
            _hitStopUntil = until;
            host.StartCoroutine(HitStopRoutine(scale));
        }

        public static void CancelHitStop()
        {
            _hitStopUntil = 0f;
            _hitStopUsers = 0;
            Time.timeScale = 1f;
        }

        private static bool CyberGameDirectorMissing()
        {
            return ArcadeFeel.Host == null;
        }

        private static IEnumerator HitStopRoutine(float scale)
        {
            _hitStopUsers++;
            Time.timeScale = Mathf.Clamp(scale, 0.02f, 1f);
            while (Time.unscaledTime < _hitStopUntil)
            {
                yield return null;
            }
            _hitStopUsers = Mathf.Max(0, _hitStopUsers - 1);
            if (_hitStopUsers == 0)
            {
                Time.timeScale = 1f;
            }
        }
    }

    public static class ArcadeFeel
    {
        private const string ShakePref = "CyberWhack_Shake";
        private static PlayfieldShake _shake;

        public static float ShakeScale { get; private set; } = 0.8f;
        public static float FlashScale { get; private set; } = 1f;
        public static bool ShakeEnabled => ShakeScale > 0.05f;
        public static MonoBehaviour Host { get; private set; }

        public static void Attach(RectTransform playfield, MonoBehaviour host)
        {
            Host = host;
            if (!PlayerPrefs.HasKey(ShakePref))
            {
                ShakeScale = 0.8f;
            }
            else
            {
                ShakeScale = PlayerPrefs.GetInt(ShakePref, 1) == 0 ? 0f : 0.8f;
            }
            FlashScale = ShakeScale <= 0.01f ? 0f : 1f;

            if (playfield == null) return;
            _shake = playfield.GetComponent<PlayfieldShake>();
            if (_shake == null) _shake = playfield.gameObject.AddComponent<PlayfieldShake>();
        }

        public static bool ToggleShake()
        {
            bool enable = !ShakeEnabled;
            ShakeScale = enable ? 0.8f : 0f;
            FlashScale = enable ? 1f : 0f;
            PlayerPrefs.SetInt(ShakePref, enable ? 1 : 0);
            PlayerPrefs.Save();
            return enable;
        }

        public static void Impact(float trauma, float flash)
        {
            if (_shake == null) return;
            _shake.AddTrauma(trauma);
            if (flash > 0f) _shake.PunchFlash(flash);
        }

        public static void ImpactTinted(float trauma, Color tint, float flash)
        {
            if (_shake == null) return;
            _shake.AddTrauma(trauma);
            if (flash > 0f) _shake.PunchFlash(flash, tint);
        }

        public static void HitStop(float duration, float scale)
        {
            PlayfieldShake.RequestHitStop(duration, scale);
        }

        public static void CancelHitStop()
        {
            PlayfieldShake.CancelHitStop();
        }
    }
}
