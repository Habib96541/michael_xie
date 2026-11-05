using UnityEngine;
using UnityEngine.UI;

namespace CyberWhack.VFX
{
    public enum ImpactFlavor
    {
        Whack,
        Gold,
        ArmorCrack,
        ArmorBreak,
        Bomb,
        Miss,
        Emerge
    }

    /// <summary>
    /// Pooled "POW" layer: one cartoon flash, a dust ring, and a handful of
    /// spinning mud clods or sparkles per tap. Everything is built once and
    /// re-tinted per flavor, so a frantic round allocates nothing.
    /// </summary>
    public sealed class ComicImpactFx : MonoBehaviour
    {
        private const int BurstCount = 14;
        private const int ChunkCount = 10;
        private const int ConfettiCount = 32;

        private sealed class Chunk
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Spin;
            public float SpinSpeed;
            public float Curve;
            public float Size;
            public float Life;
            public float Age;
            public float Gravity;
            public Color Color;
        }

        private sealed class Burst
        {
            public GameObject Root;
            public RectTransform Rect;
            public CanvasGroup Group;
            public Image Flash;
            public RectTransform FlashRect;
            public Image Ring;
            public RectTransform RingRect;
            public Chunk[] Chunks;
            public float Age;
            public float Duration;
            public float FlashSpin;
            public bool Active;
        }

        private struct HitStyle
        {
            public Color Star;
            public Color Ring;
            public Color ChunkA;
            public Color ChunkB;
            public int Chunks;
            public bool Sparkles;
            public bool Shards;
            public bool Smoke;
            public float Speed;
            public float Gravity;
            public float Duration;
            public float FlashSize;
            public float RingSize;
        }

        private Burst[] _bursts;
        private int _burstIndex;
        private Sprite _burstSprite;
        private Sprite _sparkle;
        private Sprite _clod;
        private Sprite _shard;
        private Sprite _softDot;

        private RectTransform[] _confetti;
        private Image[] _confettiImages;
        private Vector2[] _confettiPositions;
        private Vector2[] _confettiVelocities;
        private float[] _confettiSpin;
        private float[] _confettiSpinSpeed;
        private float[] _confettiPhase;
        private float _confettiAge;
        private bool _confettiActive;

        private void Awake()
        {
            _softDot = PolishSprites.SoftDot;
            _sparkle = PolishSprites.Sparkle;
            _clod = PolishSprites.Clod;
            _shard = PolishSprites.Shard;
            _burstSprite = PolishSprites.BurstStar;
            BuildBursts();
            BuildConfetti();
        }

        public void ConfigureBurstSprite(Sprite sprite)
        {
            if (sprite == null) return;
            _burstSprite = sprite;
            if (_bursts == null) return;
            for (int i = 0; i < _bursts.Length; i++)
            {
                if (_bursts[i]?.Flash != null) _bursts[i].Flash.sprite = sprite;
            }
        }

        private void BuildBursts()
        {
            _bursts = new Burst[BurstCount];
            for (int i = 0; i < BurstCount; i++)
            {
                GameObject root = new GameObject($"ImpactBurst_{i}", typeof(RectTransform), typeof(CanvasGroup));
                root.transform.SetParent(transform, false);

                RectTransform rect = root.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = Vector2.zero;

                CanvasGroup group = root.GetComponent<CanvasGroup>();
                group.blocksRaycasts = false;
                group.interactable = false;

                Image flash = CreateImage("Flash", rect, _burstSprite);
                Image ring = CreateImage("Ring", rect, _softDot);

                Chunk[] chunks = new Chunk[ChunkCount];
                for (int c = 0; c < ChunkCount; c++)
                {
                    Image image = CreateImage($"Chunk_{c}", rect, _clod);
                    chunks[c] = new Chunk
                    {
                        Rect = image.rectTransform,
                        Image = image
                    };
                }

                _bursts[i] = new Burst
                {
                    Root = root,
                    Rect = rect,
                    Group = group,
                    Flash = flash,
                    FlashRect = flash.rectTransform,
                    Ring = ring,
                    RingRect = ring.rectTransform,
                    Chunks = chunks
                };

                root.SetActive(false);
            }
        }

        private void BuildConfetti()
        {
            _confetti = new RectTransform[ConfettiCount];
            _confettiImages = new Image[ConfettiCount];
            _confettiPositions = new Vector2[ConfettiCount];
            _confettiVelocities = new Vector2[ConfettiCount];
            _confettiSpin = new float[ConfettiCount];
            _confettiSpinSpeed = new float[ConfettiCount];
            _confettiPhase = new float[ConfettiCount];

            for (int i = 0; i < ConfettiCount; i++)
            {
                GameObject piece = new GameObject($"Confetti_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                piece.transform.SetParent(transform, false);

                RectTransform rect = piece.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(14f, 22f);

                Image image = piece.GetComponent<Image>();
                image.raycastTarget = false;

                _confetti[i] = rect;
                _confettiImages[i] = image;
                piece.SetActive(false);
            }
        }

        private static Image CreateImage(string name, RectTransform parent, Sprite sprite)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(120f, 120f);

            Image image = obj.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        public void Play(Vector2 canvasLocalPosition, ImpactFlavor flavor)
        {
            if (_bursts == null || _bursts.Length == 0) return;

            HitStyle style = GetStyle(flavor);
            Burst burst = _bursts[_burstIndex];
            _burstIndex = (_burstIndex + 1) % _bursts.Length;

            burst.Root.SetActive(true);
            burst.Root.transform.SetAsLastSibling();
            burst.Rect.anchoredPosition = canvasLocalPosition;
            burst.Age = 0f;
            burst.Duration = style.Duration;
            burst.FlashSpin = Random.Range(-90f, 90f);
            burst.Active = true;

            burst.Flash.sprite = _burstSprite;
            burst.Flash.color = style.Star;
            burst.Flash.enabled = true;
            burst.FlashRect.anchoredPosition = Vector2.zero;
            burst.FlashRect.sizeDelta = new Vector2(style.FlashSize, style.FlashSize);
            burst.FlashRect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            burst.FlashRect.localScale = Vector3.one * 0.22f;

            burst.Ring.color = style.Ring;
            burst.Ring.enabled = !style.Smoke;
            burst.RingRect.anchoredPosition = Vector2.zero;
            burst.RingRect.sizeDelta = new Vector2(style.RingSize, style.RingSize);
            burst.RingRect.localScale = Vector3.one * 0.35f;

            for (int i = 0; i < burst.Chunks.Length; i++)
            {
                Chunk chunk = burst.Chunks[i];
                bool used = i < style.Chunks;
                chunk.Image.enabled = used;
                if (!used) continue;

                Sprite sprite = _clod;
                if (style.Smoke) sprite = i % 2 == 0 ? _softDot : _sparkle;
                else if (style.Shards) sprite = i % 3 == 2 ? _shard : _sparkle;
                else if (style.Sparkles && i % 2 == 0) sprite = _sparkle;
                chunk.Image.sprite = sprite;

                float angle = Random.Range(0f, Mathf.PI * 2f);
                float speed = style.Speed * Random.Range(0.55f, 1.25f);
                chunk.Position = Vector2.zero;
                chunk.Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                chunk.Velocity.y += style.Smoke ? 55f : 120f;
                chunk.Spin = Random.Range(0f, 360f);
                chunk.SpinSpeed = Random.Range(-620f, 620f);
                chunk.Curve = Random.Range(0.6f, 1.4f);
                chunk.Size = style.Smoke
                    ? (i % 2 == 0 ? Random.Range(36f, 62f) : Random.Range(10f, 18f))
                    : Random.Range(14f, 28f);
                chunk.Life = style.Smoke ? Random.Range(0.55f, 0.86f) : Random.Range(0.32f, 0.58f);
                chunk.Age = 0f;
                chunk.Gravity = style.Gravity * Random.Range(0.75f, 1.25f);
                chunk.Color = Color.Lerp(style.ChunkA, style.ChunkB, Random.value);
                chunk.Color.a = style.Smoke ? 0.42f : 1f;

                chunk.Image.color = chunk.Color;
                chunk.Rect.sizeDelta = new Vector2(chunk.Size, chunk.Size);
                chunk.Rect.anchoredPosition = Vector2.zero;
                chunk.Rect.localRotation = Quaternion.Euler(0f, 0f, chunk.Spin);
                chunk.Rect.localScale = Vector3.one;
            }
        }

        public void PlayCelebration()
        {
            if (_confetti == null) return;

            RectTransform host = transform as RectTransform;
            float halfWidth = host != null ? Mathf.Max(240f, host.rect.width * 0.5f) : 540f;
            float halfHeight = host != null ? Mathf.Max(320f, host.rect.height * 0.5f) : 960f;

            Color[] palette =
            {
                new Color(1f, 0.87f, 0.35f, 1f),
                new Color(1f, 0.97f, 0.82f, 1f),
                new Color(0.31f, 0.78f, 0.36f, 1f),
                new Color(0.91f, 0.35f, 0.23f, 1f),
                new Color(0.98f, 0.62f, 0.19f, 1f)
            };

            for (int i = 0; i < _confetti.Length; i++)
            {
                RectTransform rect = _confetti[i];
                Image image = _confettiImages[i];
                rect.gameObject.SetActive(true);

                _confettiPositions[i] = new Vector2(Random.Range(-halfWidth * 0.85f, halfWidth * 0.85f), halfHeight * 0.92f + Random.Range(0f, 220f));
                _confettiVelocities[i] = new Vector2(Random.Range(-70f, 70f), Random.Range(-680f, -430f));
                _confettiSpin[i] = Random.Range(0f, 360f);
                _confettiSpinSpeed[i] = Random.Range(-420f, 420f);
                _confettiPhase[i] = Random.Range(0f, Mathf.PI * 2f);

                rect.anchoredPosition = _confettiPositions[i];
                rect.localRotation = Quaternion.Euler(0f, 0f, _confettiSpin[i]);
                rect.sizeDelta = new Vector2(Random.Range(10f, 18f), Random.Range(16f, 26f));

                Color color = palette[i % palette.Length];
                color.a = 0.92f;
                image.color = color;
                image.raycastTarget = false;
            }

            _confettiAge = 0f;
            _confettiActive = true;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            UpdateBursts(dt);
            UpdateConfetti(dt);
        }

        private void UpdateBursts(float dt)
        {
            if (_bursts == null) return;

            for (int i = 0; i < _bursts.Length; i++)
            {
                Burst burst = _bursts[i];
                if (burst == null || !burst.Active) continue;

                burst.Age += dt;
                if (burst.Age >= burst.Duration)
                {
                    burst.Active = false;
                    burst.Root.SetActive(false);
                    continue;
                }

                float flashT = Mathf.Clamp01(burst.Age / 0.20f);
                float flashScale = Mathf.Lerp(0.22f, 1.08f, EaseOutBack(flashT));
                burst.FlashRect.localScale = new Vector3(flashScale, flashScale, 1f);
                burst.FlashRect.Rotate(0f, 0f, burst.FlashSpin * dt);

                Color flashColor = burst.Flash.color;
                flashColor.a = Mathf.Clamp01(1f - burst.Age / 0.30f) * 0.92f;
                burst.Flash.color = flashColor;

                if (burst.Ring.enabled)
                {
                    float ringT = Mathf.Clamp01(burst.Age / 0.34f);
                    float ringScale = Mathf.Lerp(0.35f, 1.5f, ringT);
                    burst.RingRect.localScale = new Vector3(ringScale, ringScale, 1f);
                    Color ringColor = burst.Ring.color;
                    ringColor.a *= (1f - ringT);
                    burst.Ring.color = ringColor;
                    if (ringT >= 1f) burst.Ring.enabled = false;
                }

                for (int c = 0; c < burst.Chunks.Length; c++)
                {
                    Chunk chunk = burst.Chunks[c];
                    if (chunk == null || !chunk.Image.enabled) continue;

                    chunk.Age += dt;
                    if (chunk.Age >= chunk.Life)
                    {
                        chunk.Image.enabled = false;
                        continue;
                    }

                    float drag = 1f - Mathf.Clamp01(2.4f * dt);
                    chunk.Velocity *= drag;
                    chunk.Velocity.y -= chunk.Gravity * dt;
                    chunk.Position += chunk.Velocity * dt;

                    float wobble = Mathf.Sin((chunk.Age * 9f) + chunk.Curve * 3f) * 16f * dt;
                    chunk.Rect.anchoredPosition = chunk.Position + new Vector2(wobble, 0f);
                    chunk.Spin += chunk.SpinSpeed * dt;
                    chunk.Rect.localRotation = Quaternion.Euler(0f, 0f, chunk.Spin);

                    float lifeT = chunk.Age / chunk.Life;
                    float pop = lifeT < 0.25f ? Mathf.Lerp(0.4f, 1.15f, lifeT / 0.25f) : Mathf.Lerp(1.15f, 0.55f, (lifeT - 0.25f) / 0.75f);
                    float scale = pop * chunk.Curve;
                    chunk.Rect.localScale = new Vector3(scale, scale, 1f);
                    chunk.Rect.sizeDelta = new Vector2(chunk.Size, chunk.Size);

                    Color color = chunk.Color;
                    color.a = Mathf.Clamp01(1f - lifeT * lifeT);
                    chunk.Image.color = color;
                }
            }
        }

        private void UpdateConfetti(float dt)
        {
            if (!_confettiActive || _confetti == null) return;

            _confettiAge += dt;
            if (_confettiAge >= 2.6f)
            {
                _confettiActive = false;
                for (int i = 0; i < _confetti.Length; i++)
                {
                    if (_confetti[i] != null) _confetti[i].gameObject.SetActive(false);
                }
                return;
            }

            for (int i = 0; i < _confetti.Length; i++)
            {
                RectTransform rect = _confetti[i];
                if (rect == null) continue;

                _confettiVelocities[i].y -= 120f * dt;
                _confettiPositions[i] += _confettiVelocities[i] * dt;
                _confettiSpin[i] += _confettiSpinSpeed[i] * dt;

                float sway = Mathf.Sin(_confettiAge * 3.6f + _confettiPhase[i]) * 40f * dt;
                rect.anchoredPosition = _confettiPositions[i] + new Vector2(sway, 0f);
                rect.localRotation = Quaternion.Euler(0f, 0f, _confettiSpin[i]);

                Color color = _confettiImages[i].color;
                color.a = Mathf.Clamp01((2.6f - _confettiAge) * 1.4f) * 0.92f;
                _confettiImages[i].color = color;
            }
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float p = t - 1f;
            return 1f + c3 * p * p * p + c1 * p * p;
        }

        private static HitStyle GetStyle(ImpactFlavor flavor)
        {
            switch (flavor)
            {
                case ImpactFlavor.Gold:
                    return new HitStyle
                    {
                        Star = new Color(1f, 0.91f, 0.44f, 1f),
                        Ring = new Color(1f, 0.84f, 0.25f, 0.55f),
                        ChunkA = new Color(1f, 0.84f, 0.25f, 1f),
                        ChunkB = new Color(1f, 0.99f, 0.72f, 1f),
                        Chunks = 10,
                        Sparkles = true,
                        Shards = false,
                        Smoke = false,
                        Speed = 470f,
                        Gravity = 820f,
                        Duration = 0.72f,
                        FlashSize = 250f,
                        RingSize = 210f
                    };
                case ImpactFlavor.ArmorCrack:
                    return new HitStyle
                    {
                        Star = new Color(1f, 0.92f, 0.68f, 0.9f),
                        Ring = new Color(1f, 0.78f, 0.36f, 0.5f),
                        ChunkA = new Color(0.86f, 0.84f, 0.76f, 1f),
                        ChunkB = new Color(1f, 0.71f, 0.28f, 1f),
                        Chunks = 8,
                        Sparkles = false,
                        Shards = true,
                        Smoke = false,
                        Speed = 380f,
                        Gravity = 940f,
                        Duration = 0.62f,
                        FlashSize = 190f,
                        RingSize = 160f
                    };
                case ImpactFlavor.ArmorBreak:
                    return new HitStyle
                    {
                        Star = new Color(1f, 0.88f, 0.42f, 1f),
                        Ring = new Color(1f, 0.65f, 0.24f, 0.55f),
                        ChunkA = new Color(0.8f, 0.78f, 0.7f, 1f),
                        ChunkB = new Color(1f, 0.78f, 0.32f, 1f),
                        Chunks = 10,
                        Sparkles = false,
                        Shards = true,
                        Smoke = false,
                        Speed = 430f,
                        Gravity = 900f,
                        Duration = 0.68f,
                        FlashSize = 225f,
                        RingSize = 180f
                    };
                case ImpactFlavor.Bomb:
                    return new HitStyle
                    {
                        Star = new Color(1f, 0.48f, 0.22f, 1f),
                        Ring = new Color(1f, 0.34f, 0.16f, 0.6f),
                        ChunkA = new Color(0.24f, 0.18f, 0.14f, 1f),
                        ChunkB = new Color(0.45f, 0.32f, 0.24f, 1f),
                        Chunks = 10,
                        Sparkles = true,
                        Shards = false,
                        Smoke = true,
                        Speed = 260f,
                        Gravity = -70f,
                        Duration = 0.9f,
                        FlashSize = 260f,
                        RingSize = 230f
                    };
                case ImpactFlavor.Miss:
                    return new HitStyle
                    {
                        Star = new Color(0.55f, 0.38f, 0.24f, 0.55f),
                        Ring = new Color(0.4f, 0.24f, 0.12f, 0.35f),
                        ChunkA = new Color(0.44f, 0.27f, 0.14f, 1f),
                        ChunkB = new Color(0.66f, 0.43f, 0.24f, 1f),
                        Chunks = 6,
                        Sparkles = false,
                        Shards = false,
                        Smoke = false,
                        Speed = 210f,
                        Gravity = 900f,
                        Duration = 0.45f,
                        FlashSize = 130f,
                        RingSize = 110f
                    };
                case ImpactFlavor.Emerge:
                    return new HitStyle
                    {
                        Star = new Color(0.72f, 0.52f, 0.32f, 0.0f),
                        Ring = new Color(0.55f, 0.35f, 0.18f, 0.4f),
                        ChunkA = new Color(0.48f, 0.29f, 0.15f, 1f),
                        ChunkB = new Color(0.76f, 0.5f, 0.28f, 1f),
                        Chunks = 7,
                        Sparkles = false,
                        Shards = false,
                        Smoke = false,
                        Speed = 220f,
                        Gravity = 780f,
                        Duration = 0.55f,
                        FlashSize = 0f,
                        RingSize = 140f
                    };
                default:
                    return new HitStyle
                    {
                        Star = new Color(1f, 0.98f, 0.86f, 1f),
                        Ring = new Color(1f, 0.93f, 0.66f, 0.5f),
                        ChunkA = new Color(0.48f, 0.29f, 0.15f, 1f),
                        ChunkB = new Color(0.8f, 0.55f, 0.3f, 1f),
                        Chunks = 8,
                        Sparkles = true,
                        Shards = false,
                        Smoke = false,
                        Speed = 400f,
                        Gravity = 900f,
                        Duration = 0.6f,
                        FlashSize = 210f,
                        RingSize = 170f
                    };
            }
        }
    }
}
