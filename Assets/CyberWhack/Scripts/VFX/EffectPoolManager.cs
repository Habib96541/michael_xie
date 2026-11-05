using System.Collections.Generic;
using CyberWhack.Art;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CyberWhack.VFX
{
    public class EffectPoolManager : MonoBehaviour
    {
        public static EffectPoolManager Instance { get; private set; }

        [Header("Pool Config")]
        [SerializeField] private int shockwavePoolSize = 16;
        [SerializeField] private int textPoolSize = 16;
        [SerializeField] private Sprite shockwaveSprite;
        [SerializeField] private TMP_FontAsset fontAsset;

        private PlasmaShockwave[] _shockwaves;
        private int _shockwaveIdx;

        private FloatingTextCallout[] _textCallouts;
        private int _textIdx;

        private ComicImpactFx _impact;
        private RectTransform _rectTransform;

        public void ConfigureVisuals(Sprite newShockwaveSprite)
        {
            if (newShockwaveSprite == null) return;
            shockwaveSprite = newShockwaveSprite;

            if (_shockwaves == null) return;
            for (int i = 0; i < _shockwaves.Length; i++)
            {
                if (_shockwaves[i] == null) continue;
                Image image = _shockwaves[i].GetComponent<Image>();
                if (image != null) image.sprite = newShockwaveSprite;
            }

            if (_impact != null) _impact.ConfigureBurstSprite(newShockwaveSprite);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _rectTransform = GetComponent<RectTransform>();

            InitializeShockwavePool();
            InitializeTextPool();

            _impact = gameObject.GetComponent<ComicImpactFx>();
            if (_impact == null) _impact = gameObject.AddComponent<ComicImpactFx>();
            if (_impact != null && shockwaveSprite != null) _impact.ConfigureBurstSprite(shockwaveSprite);
        }

        private void InitializeShockwavePool()
        {
            if (shockwaveSprite == null)
            {
                shockwaveSprite = CyberAtlasGenerator.CreateShockwaveRingSprite();
            }

            _shockwaves = new PlasmaShockwave[shockwavePoolSize];
            for (int i = 0; i < shockwavePoolSize; i++)
            {
                GameObject obj = new GameObject($"Shockwave_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(PlasmaShockwave));
                obj.transform.SetParent(transform, false);

                RectTransform rt = obj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(180, 180);

                Image img = obj.GetComponent<Image>();
                img.raycastTarget = false;
                if (shockwaveSprite != null)
                {
                    img.sprite = shockwaveSprite;
                }
                img.color = Color.white; // Full vibrant comic starburst colors

                PlasmaShockwave sw = obj.GetComponent<PlasmaShockwave>();
                obj.SetActive(false);
                _shockwaves[i] = sw;
            }
        }

        private void InitializeTextPool()
        {
            _textCallouts = new FloatingTextCallout[textPoolSize];
            for (int i = 0; i < textPoolSize; i++)
            {
                GameObject obj = new GameObject($"Callout_{i}", typeof(RectTransform), typeof(CanvasGroup), typeof(FloatingTextCallout));
                obj.transform.SetParent(transform, false);

                RectTransform rt = obj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(250, 60);

                GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(obj.transform, false);

                RectTransform textRt = textObj.GetComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.sizeDelta = Vector2.zero;

                TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 34;
                tmp.fontStyle = FontStyles.Bold;
                tmp.raycastTarget = false;
                tmp.outlineWidth = 0.22f;
                tmp.outlineColor = new Color32(58, 28, 10, 255);
                if (fontAsset != null)
                {
                    tmp.font = fontAsset;
                }

                FloatingTextCallout callout = obj.GetComponent<FloatingTextCallout>();
                obj.SetActive(false);
                _textCallouts[i] = callout;
            }
        }

        public void PlayShockwave(Vector2 canvasLocalPos)
        {
            PlayShockwave(canvasLocalPos, Color.white);
        }

        public void PlayShockwave(Vector2 canvasLocalPos, Color tint)
        {
            if (_shockwaves == null || _shockwaves.Length == 0) return;
            PlasmaShockwave sw = _shockwaves[_shockwaveIdx];
            _shockwaveIdx = (_shockwaveIdx + 1) % _shockwaves.Length;
            sw.Play(canvasLocalPos, tint);
        }

        public void SpawnCallout(Vector2 canvasLocalPos, string text, Color color)
        {
            if (_textCallouts == null || _textCallouts.Length == 0) return;
            FloatingTextCallout callout = _textCallouts[_textIdx];
            _textIdx = (_textIdx + 1) % _textCallouts.Length;
            callout.Spawn(canvasLocalPos, text, color);
        }

        public void PlayImpact(Vector2 canvasLocalPos, ImpactFlavor flavor)
        {
            if (_impact != null) _impact.Play(canvasLocalPos, flavor);
        }

        public void PlayCelebration()
        {
            if (_impact != null) _impact.PlayCelebration();
        }

        /// <summary>Converts a raw pointer position into this full-screen pool's space.</summary>
        public Vector2 FromScreen(Vector2 screenPoint)
        {
            if (_rectTransform == null) return screenPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, screenPoint, null, out Vector2 local);
            return local;
        }

        /// <summary>Converts a world point (including playfield shake) into pool space.</summary>
        public Vector2 FromWorld(Vector3 worldPoint)
        {
            return _rectTransform != null ? (Vector2)_rectTransform.InverseTransformPoint(worldPoint) : (Vector2)worldPoint;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
