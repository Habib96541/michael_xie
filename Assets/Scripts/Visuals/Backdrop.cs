using UnityEngine;
using UnityEngine.UI;

namespace Crash
{
    /// Twinkling, drifting star field drawn as one mesh.
    public class StarField : MaskableGraphic
    {
        struct Star
        {
            public Vector2 p;
            public float size;
            public float speed;
            public float phase;
            public float bright;
        }

        Star[] _stars;
        float _t;
        public Texture2D SoftTex;

        public void Init(int count, int seed)
        {
            var rnd = new System.Random(seed);
            _stars = new Star[count];
            for (int i = 0; i < count; i++)
            {
                _stars[i] = new Star
                {
                    p = new Vector2((float)rnd.NextDouble(), (float)rnd.NextDouble()),
                    size = 0.55f + (float)rnd.NextDouble() * 1.15f,
                    speed = 0.004f + (float)rnd.NextDouble() * 0.020f,
                    phase = (float)rnd.NextDouble() * 6.283f,
                    bright = 0.14f + (float)rnd.NextDouble() * 0.55f
                };
            }
            SetVerticesDirty();
        }

        public override Texture mainTexture => SoftTex != null ? SoftTex : s_WhiteTexture;

        void Update()
        {
            if (_stars == null) return;
            float dt = Time.unscaledDeltaTime;
            _t += dt;
            for (int i = 0; i < _stars.Length; i++)
            {
                var s = _stars[i];
                s.p.y += s.speed * dt;
                if (s.p.y > 1.04f)
                {
                    s.p.y = -0.04f;
                    s.p.x = (float)System.Math.Round(s.p.x * 37.0 % 100.0) / 100f;
                }
                _stars[i] = s;
            }
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_stars == null) return;
            var r = rectTransform.rect;
            if (r.width < 1f) return;
            for (int i = 0; i < _stars.Length; i++)
            {
                var s = _stars[i];
                float tw = 0.5f + 0.5f * Mathf.Sin(_t * 1.4f + s.phase);
                var col = new Color(0.78f, 0.86f, 1f, s.bright * (0.35f + 0.65f * tw) * 0.9f);
                float cx = r.xMin + s.p.x * r.width;
                float cy = r.yMin + s.p.y * r.height;
                float h = s.size * 2.6f;
                int v = vh.currentVertCount;
                vh.AddVert(new Vector3(cx - h, cy - h), col, new Vector2(0f, 0f));
                vh.AddVert(new Vector3(cx - h, cy + h), col, new Vector2(0f, 1f));
                vh.AddVert(new Vector3(cx + h, cy + h), col, new Vector2(1f, 1f));
                vh.AddVert(new Vector3(cx + h, cy - h), col, new Vector2(1f, 0f));
                vh.AddTriangle(v, v + 1, v + 2);
                vh.AddTriangle(v + 2, v + 3, v);
            }
        }
    }

    public class BgAnim : MonoBehaviour
    {
        public Image[] blobs;
        public RectTransform[] drift;
        Vector2[] _base;
        float[] _phase;

        void Start()
        {
            if (drift == null) return;
            _base = new Vector2[drift.Length];
            _phase = new float[drift.Length];
            for (int i = 0; i < drift.Length; i++)
            {
                if (drift[i] == null) continue;
                _base[i] = drift[i].anchoredPosition;
                _phase[i] = i * 1.7f;
            }
        }

        void Update()
        {
            if (_base == null) return;
            float t = Time.unscaledTime;
            for (int i = 0; i < _base.Length; i++)
            {
                if (drift[i] == null) continue;
                float k = _phase[i];
                drift[i].anchoredPosition = _base[i] + new Vector2(Mathf.Sin(t * 0.21f + k) * 38f, Mathf.Cos(t * 0.17f + k) * 30f);
                if (blobs != null && i < blobs.Length && blobs[i] != null)
                {
                    var c = blobs[i].color;
                    c.a = Mathf.Lerp(c.a, 0.10f + 0.05f * Mathf.Sin(t * 0.35f + k), Time.unscaledDeltaTime * 2f);
                    blobs[i].color = c;
                    blobs[i].rectTransform.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(t * 0.27f + k));
                }
            }
        }
    }

    public static class Backdrop
    {
        public static RectTransform Create(Transform parent)
        {
            var root = UIKit.Rect(parent, "Backdrop");
            root.Stretch();

            var baseImg = UIKit.Img(root, "Base", null, Pal.BgBot);
            baseImg.rectTransform.Stretch();

            var top = UIKit.Img(root, "TopGradient", Tex.Fade(256), Pal.BgTop);
            top.rectTransform.Stretch();
            top.color = Pal.BgTop;

            var blobs = new Image[3];
            blobs[0] = MakeBlob(root, "NebulaA", Pal.Violet, new Vector2(0.22f, 0.78f), 1500, 0.13f);
            blobs[1] = MakeBlob(root, "NebulaB", Pal.Cyan, new Vector2(0.85f, 0.30f), 1200, 0.085f);
            blobs[2] = MakeBlob(root, "NebulaC", Pal.Magenta, new Vector2(0.55f, 0.05f), 1000, 0.06f);

            var stars = UIKit.Rect(root, "Stars").gameObject.AddComponent<StarField>();
            stars.rectTransform.Stretch();
            stars.raycastTarget = false;
            stars.SoftTex = Tex.Circle(32).texture;
            stars.Init(160, 20261103);

            var vig = UIKit.Img(root, "Vignette", Tex.Vignette(256), Color.white);
            vig.rectTransform.Stretch(-120, -120, -120, -120);
            vig.color = new Color(1f, 1f, 1f, 0.75f);

            var anim = root.gameObject.AddComponent<BgAnim>();
            anim.blobs = blobs;
            anim.drift = new RectTransform[3];
            for (int i = 0; i < 3; i++) anim.drift[i] = blobs[i].rectTransform;

            return root;
        }

        static Image MakeBlob(Transform parent, string name, Color color, Vector2 anchor, float size, float alpha)
        {
            var img = UIKit.Img(parent, name, Tex.Glow(128), color.WithAlpha(alpha));
            img.rectTransform.Place(anchor, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
            return img;
        }
    }
}
