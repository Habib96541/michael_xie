using CyberWhack.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace CyberWhack.VFX
{
    /// <summary>
    /// Living desert backdrop: warm sun halo, drifting dust, distant birds,
    /// a soft vignette and a barely-there breathing pan on the painted sand.
    /// Everything here sits behind the playfield and never blocks input.
    /// </summary>
    public sealed class DesertAtmosphere : MonoBehaviour
    {
        private const int DustCount = 22;
        private const int BirdCount = 3;

        private struct Mote
        {
            public RectTransform Rect;
            public Image Image;
            public float BaseX;
            public float Speed;
            public float Sway;
            public float Phase;
            public float Alpha;
        }

        private readonly Mote[] _motes = new Mote[DustCount];
        private readonly RectTransform[] _birdRects = new RectTransform[BirdCount];

        private RectTransform _rays;
        private Image _raysImage;
        private Image _sunHalo;
        private RectTransform _backdrop;
        private float _time;
        private bool _built;

        public static void Install(Canvas canvas, RectTransform safeArea, HatchNode[] nodes)
        {
            if (canvas == null) return;

            Transform existing = canvas.transform.Find("AtmosphereLayer");
            DesertAtmosphere layer;
            if (existing != null)
            {
                layer = existing.GetComponent<DesertAtmosphere>();
            }
            else
            {
                GameObject obj = new GameObject("AtmosphereLayer", typeof(RectTransform), typeof(DesertAtmosphere));
                obj.transform.SetParent(canvas.transform, false);
                layer = obj.GetComponent<DesertAtmosphere>();
            }

            if (layer != null) layer.Build(canvas);

            if (nodes != null)
            {
                for (int i = 0; i < nodes.Length; i++)
                {
                    HatchRimLife.Attach(nodes[i]);
                }
            }
        }

        private void Build(Canvas canvas)
        {
            if (_built) return;
            _built = true;

            RectTransform root = transform as RectTransform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            root.pivot = new Vector2(0.5f, 0.5f);

            Transform background = canvas.transform.Find("FullscreenBackground");
            _backdrop = background as RectTransform;
            transform.SetSiblingIndex(background != null ? background.GetSiblingIndex() + 1 : 0);

            _sunHalo = CreateLayerImage("SunHalo", root, PolishSprites.SunDisc,
                new Color(1f, 0.83f, 0.48f, 0.18f), new Vector2(0.5f, 0.84f), Vector2.zero, new Vector2(1020f, 1020f));

            Image rays = CreateLayerImage("SunRays", root, PolishSprites.SunRays,
                new Color(1f, 0.92f, 0.68f, 0.11f), new Vector2(0.5f, 0.84f), Vector2.zero, new Vector2(820f, 820f));
            _rays = rays.rectTransform;
            _raysImage = rays;

            CreateLayerImage("SunCore", root, PolishSprites.SoftDot,
                new Color(1f, 0.96f, 0.82f, 0.16f), new Vector2(0.5f, 0.85f), Vector2.zero, new Vector2(230f, 230f));

            for (int i = 0; i < BirdCount; i++)
            {
                Image bird = CreateLayerImage($"DistantBird_{i}", root, PolishSprites.Bird,
                    new Color(0.36f, 0.20f, 0.09f, 0.22f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46f, 34f));
                _birdRects[i] = bird.rectTransform;
                _birdRects[i].anchoredPosition = new Vector2(UnityEngine.Random.Range(-620f, 320f), UnityEngine.Random.Range(260f, 520f));
                float scale = UnityEngine.Random.Range(0.65f, 1.15f);
                _birdRects[i].localScale = new Vector3(scale, scale, 1f);
            }

            for (int i = 0; i < DustCount; i++)
            {
                float size = UnityEngine.Random.Range(4f, 13f);
                Image dot = CreateLayerImage($"DustMote_{i}", root, PolishSprites.SoftDot,
                    new Color(1f, 0.93f, 0.72f, 0.18f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
                dot.rectTransform.anchoredPosition = new Vector2(
                    UnityEngine.Random.Range(-540f, 540f),
                    UnityEngine.Random.Range(-980f, 980f));

                _motes[i] = new Mote
                {
                    Rect = dot.rectTransform,
                    Image = dot,
                    BaseX = dot.rectTransform.anchoredPosition.x,
                    Speed = UnityEngine.Random.Range(9f, 24f),
                    Sway = UnityEngine.Random.Range(6f, 22f),
                    Phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                    Alpha = UnityEngine.Random.Range(0.10f, 0.30f)
                };
            }

            BuildVignette(canvas);
        }

        private void BuildVignette(Canvas canvas)
        {
            Transform existing = canvas.transform.Find("AtmosphereVignette");
            GameObject obj;
            if (existing != null)
            {
                obj = existing.gameObject;
            }
            else
            {
                obj = new GameObject("AtmosphereVignette", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                obj.transform.SetParent(canvas.transform, false);
            }

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = obj.GetComponent<Image>();
            image.sprite = PolishSprites.Vignette;
            image.color = Color.white;
            image.raycastTarget = false;

            obj.transform.SetSiblingIndex(transform.GetSiblingIndex() + 1);
        }

        private static Image CreateLayerImage(string name, Transform parent, Sprite sprite, Color tint, Vector2 anchor, Vector2 position, Vector2 size)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = obj.GetComponent<Image>();
            image.sprite = sprite;
            image.color = tint;
            image.raycastTarget = false;
            image.preserveAspect = false;
            return image;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _time += dt;

            if (_backdrop != null)
            {
                float scale = 1.045f + Mathf.Sin(_time * 0.19f) * 0.006f;
                _backdrop.localScale = new Vector3(scale, scale, 1f);
                _backdrop.anchoredPosition = new Vector2(Mathf.Sin(_time * 0.13f) * 7f, Mathf.Cos(_time * 0.17f) * 9f);
            }

            if (_rays != null)
            {
                _rays.Rotate(0f, 0f, -5.5f * dt);
                if (_raysImage != null)
                {
                    Color color = _raysImage.color;
                    color.a = 0.10f + Mathf.Sin(_time * 0.55f) * 0.028f;
                    _raysImage.color = color;
                }
            }

            if (_sunHalo != null)
            {
                Color color = _sunHalo.color;
                color.a = 0.15f + Mathf.Sin(_time * 0.55f) * 0.035f;
                _sunHalo.color = color;
            }

            RectTransform root = transform as RectTransform;
            float halfWidth = root != null && root.rect.width > 1f ? root.rect.width * 0.5f : 540f;
            float halfHeight = root != null && root.rect.height > 1f ? root.rect.height * 0.5f : 960f;

            for (int i = 0; i < _motes.Length; i++)
            {
                Mote mote = _motes[i];
                if (mote.Rect == null) continue;

                Vector2 position = mote.Rect.anchoredPosition;
                position.y += mote.Speed * dt;
                position.x = mote.BaseX + Mathf.Sin(_time * 0.6f + mote.Phase) * mote.Sway;
                if (position.y > halfHeight + 30f)
                {
                    position.y = -halfHeight - 30f;
                    mote.BaseX = UnityEngine.Random.Range(-halfWidth, halfWidth);
                }

                mote.Rect.anchoredPosition = position;

                Color color = mote.Image.color;
                color.a = mote.Alpha * (0.55f + 0.45f * Mathf.Sin(_time * 0.9f + mote.Phase));
                mote.Image.color = color;
                _motes[i] = mote;
            }

            for (int i = 0; i < _birdRects.Length; i++)
            {
                RectTransform bird = _birdRects[i];
                if (bird == null) continue;

                Vector2 position = bird.anchoredPosition;
                position.x += (26f + i * 9f) * dt;
                position.y += Mathf.Sin(_time * 0.9f + i * 2.1f) * 3f * dt;

                if (position.x > halfWidth + 160f)
                {
                    position.x = -halfWidth - UnityEngine.Random.Range(80f, 320f);
                    position.y = Mathf.Lerp(-halfHeight * 0.05f, halfHeight * 0.42f, UnityEngine.Random.value);
                }

                bird.anchoredPosition = position;
                float flap = 1f + Mathf.Sin(_time * 5.5f + i * 1.3f) * 0.18f;
                bird.localScale = new Vector3(1f, flap, 1f);
            }
        }
    }

    /// <summary>
    /// Gentle breathing alpha and scale for the glows behind buttons and bars.
    /// </summary>
    public sealed class PulseGlow : MonoBehaviour
    {
        [SerializeField] private float speed = 2.2f;
        [SerializeField] private float scaleAmount = 0.06f;
        [SerializeField] private float baseAlpha = 0.25f;

        private Image _image;
        private Vector3 _baseScale = Vector3.one;
        private float _phase;

        public void Configure(float newSpeed, float newScaleAmount, float newAlpha)
        {
            speed = newSpeed;
            scaleAmount = newScaleAmount;
            baseAlpha = newAlpha;
        }

        private void Awake()
        {
            _image = GetComponent<Image>();
            _baseScale = transform.localScale;
            _phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            float wave = (Mathf.Sin(Time.unscaledTime * speed + _phase) + 1f) * 0.5f;
            transform.localScale = _baseScale * (1f + wave * scaleAmount);
            if (_image == null) return;

            Color color = _image.color;
            color.a = baseAlpha * (0.55f + wave * 0.45f);
            _image.color = color;
        }
    }

    /// <summary>
    /// Warm light rising from an occupied burrow, so a live hole reads at a
    /// glance and the empty dirt stays calm.
    /// </summary>
    public sealed class HatchRimLife : MonoBehaviour
    {
        private HatchNode _node;
        private Image _aura;
        private float _phase;

        public static void Attach(HatchNode node)
        {
            if (node == null) return;
            if (node.GetComponent<HatchRimLife>() == null) node.gameObject.AddComponent<HatchRimLife>();
        }

        private void Awake()
        {
            _node = GetComponent<HatchNode>();
            _phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

            GameObject obj = new GameObject("BurrowGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(transform, false);
            obj.transform.SetAsFirstSibling();

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.10f, 0.29f);
            rect.anchorMax = new Vector2(0.90f, 0.63f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _aura = obj.GetComponent<Image>();
            _aura.sprite = PolishSprites.SoftDot;
            _aura.color = new Color(1f, 0.82f, 0.45f, 0.04f);
            _aura.raycastTarget = false;
        }

        private void Update()
        {
            if (_aura == null) return;

            float pulse = (Mathf.Sin(Time.unscaledTime * 2.2f + _phase) + 1f) * 0.5f;
            bool occupied = _node != null && _node.IsOccupied;
            float alpha = occupied ? 0.15f + pulse * 0.12f : 0.02f + pulse * 0.03f;

            Color color = _aura.color;
            color.a = Mathf.Lerp(color.a, alpha, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
            _aura.color = color;

            float scale = occupied ? 1f + pulse * 0.05f : 1f;
            _aura.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
