using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Crash
{
    /// Draws the flight curve: grid, soft area fill, glow stroke and the bright stroke itself.
    /// Owns the time->pixel and multiplier->pixel mapping so the rocket and axis labels agree.
    public class CurveGraph : MaskableGraphic
    {
        public readonly List<Vector2> pts = new List<Vector2>(1024);
        public float viewT = Rules.TableSeconds;
        public float viewMax = 2.6f;
        public float stroke = 8f;
        public bool crashed;
        public Color colA = Pal.Cyan;
        public Color colB = Pal.Violet;
        public Color colCrash = Pal.Red;
        public bool showGrid = true;

        float _glowPulse;

        public float HeadT => pts.Count > 0 ? pts[pts.Count - 1].x : 0f;
        public float HeadM => pts.Count > 0 ? pts[pts.Count - 1].y : 1f;

        public void ResetCurve(float t, float m)
        {
            pts.Clear();
            pts.Add(new Vector2(t, m));
            viewT = Rules.TableSeconds;
            viewMax = 2.6f;
            crashed = false;
            SetVerticesDirty();
        }

        public void Push(float t, float m)
        {
            if (pts.Count == 0) { pts.Add(new Vector2(t, m)); }
            else
            {
                var last = pts[pts.Count - 1];
                if (t - last.x < 0.045f) pts[pts.Count - 1] = new Vector2(t, m);
                else pts.Add(new Vector2(t, m));
            }
            if (pts.Count > 900) pts.RemoveRange(0, 150);
            SetVerticesDirty();
        }

        // Slight upward bend so the price line accelerates like a real forecast.
        const float YBend = 0.85f;

        public float YNorm(float m)
        {
            float span = Mathf.Max(0.05f, viewMax - 1f);
            float x = Mathf.Clamp01((m - 1f) / span);
            return Mathf.Pow(x, YBend);
        }

        public float MultAtNorm(float n)
        {
            float span = Mathf.Max(0.05f, viewMax - 1f);
            return 1f + span * Mathf.Pow(Mathf.Clamp01(n), 1f / YBend);
        }

        /// Local position (same space as anchoredPosition) for a curve point.
        public Vector2 Local(float t, float m)
        {
            var r = rectTransform.rect;
            float x = Mathf.Clamp01(t / Mathf.Max(0.001f, viewT)) * r.width + r.xMin;
            float y = YNorm(m) * r.height + r.yMin;
            return new Vector2(x, y);
        }

        static readonly float[] Ladder = { 2.6f, 3.2f, 4f, 5f, 6.4f, 8f, 10f, 13f, 16f, 20f, 26f, 34f, 44f };

        public void StepView(float dt, bool instant = false)
        {
            // The table never scrolls or zooms horizontally: one table width is the
            // whole round, so the rocket always dies at the right edge.
            viewT = Rules.TableSeconds;

            // Vertical scale climbs in fixed steps, so the rows read like a price axis.
            float targetMax = Mathf.Max(2.6f, HeadM * 1.18f);
            float pick = Ladder[Ladder.Length - 1];
            for (int i = 0; i < Ladder.Length; i++)
            {
                if (Ladder[i] >= targetMax) { pick = Ladder[i]; break; }
            }
            if (instant) viewMax = pick;
            else viewMax = Mathf.Lerp(viewMax, pick, Mathf.Min(1f, dt * 2.2f));
            if (viewMax < HeadM * 1.04f) viewMax = HeadM * 1.04f;
            SetVerticesDirty();
        }

        void Update()
        {
            _glowPulse += Time.unscaledDeltaTime;
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            SetVerticesDirty();
        }

        void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d,
            Color ca, Color cb, Color cc, Color cd)
        {
            int i = vh.currentVertCount;
            vh.AddVert(a, ca, Vector2.zero);
            vh.AddVert(b, cb, Vector2.zero);
            vh.AddVert(c, cc, Vector2.zero);
            vh.AddVert(d, cd, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i + 2, i + 3, i);
        }

        void Line(VertexHelper vh, Vector2 a, Vector2 b, float w, Color c)
        {
            Vector2 d = b - a;
            if (d.sqrMagnitude < 0.0001f) return;
            Vector2 n = new Vector2(-d.y, d.x).normalized * (w * 0.5f);
            Quad(vh, a + n, b + n, b - n, a - n, c, c, c, c);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            if (r.width < 4f || r.height < 4f) return;

            if (showGrid)
            {
                Color row = new Color(1f, 1f, 1f, 0.055f);
                Color col = new Color(1f, 1f, 1f, 0.033f);

                // the last column of the table is the crash zone
                float zx = r.xMin + r.width * (7f / 8f);
                Quad(vh, new Vector2(zx, r.yMin), new Vector2(zx, r.yMax),
                    new Vector2(r.xMax, r.yMax), new Vector2(r.xMax, r.yMin),
                    new Color(1f, 0.25f, 0.35f, 0f), new Color(1f, 0.25f, 0.35f, 0f),
                    new Color(1f, 0.25f, 0.35f, 0.075f), new Color(1f, 0.25f, 0.35f, 0.075f));

                for (int i = 0; i <= 6; i++)
                {
                    float y = r.yMin + r.height * (i / 6f);
                    Line(vh, new Vector2(r.xMin, y), new Vector2(r.xMax, y), 1.5f, row);
                }
                for (int i = 0; i <= 8; i++)
                {
                    float x = r.xMin + r.width * (i / 8f);
                    Line(vh, new Vector2(x, r.yMin), new Vector2(x, r.yMax), 1.5f, col);
                }

                // the end of the table: where the rocket always bursts
                Line(vh, new Vector2(r.xMax - 1f, r.yMin), new Vector2(r.xMax - 1f, r.yMax), 3f,
                    new Color(1f, 0.32f, 0.42f, 0.28f));
            }

            int n2 = pts.Count;
            if (n2 < 2) return;

            var local = new Vector2[n2];
            for (int i = 0; i < n2; i++) local[i] = Local(pts[i].x, pts[i].y);

            Color a0 = crashed ? colCrash : colA;
            Color b0 = crashed ? Color.Lerp(colCrash, Pal.Amber, 0.35f) : colB;

            // area fill
            float bottom = r.yMin + 2f;
            for (int i = 0; i < n2 - 1; i++)
            {
                Vector2 p0 = local[i], p1 = local[i + 1];
                float k0 = i / (float)(n2 - 1);
                float k1 = (i + 1) / (float)(n2 - 1);
                Color top0 = Color.Lerp(a0, b0, k0).WithAlpha(0.30f);
                Color top1 = Color.Lerp(a0, b0, k1).WithAlpha(0.30f);
                Color bot = Color.Lerp(a0, b0, k0).WithAlpha(0f);
                Quad(vh, new Vector2(p0.x, bottom), new Vector2(p0.x, p0.y),
                    new Vector2(p1.x, p1.y), new Vector2(p1.x, bottom), bot, top0, top1, bot);
            }

            // glow stroke (drawn first, wider, translucent)
            float gw = stroke * 3.4f;
            for (int i = 0; i < n2 - 1; i++)
            {
                Vector2 p0 = local[i], p1 = local[i + 1];
                float k0 = i / (float)(n2 - 1);
                Color c0 = Color.Lerp(a0, b0, k0).WithAlpha(0.10f);
                Color c1 = Color.Lerp(a0, b0, (i + 1f) / (n2 - 1)).WithAlpha(0.10f);
                Vector2 d = (p1 - p0);
                if (d.sqrMagnitude < 0.0001f) continue;
                Vector2 nn = new Vector2(-d.y, d.x).normalized * (gw * 0.5f);
                Quad(vh, p0 + nn, p1 + nn, p1 - nn, p0 - nn, c0, c1, c1, c0);
            }

            // main stroke
            for (int i = 0; i < n2 - 1; i++)
            {
                Vector2 p0 = local[i], p1 = local[i + 1];
                Color c0 = Color.Lerp(a0, b0, i / (float)(n2 - 1));
                Color c1 = Color.Lerp(a0, b0, (i + 1f) / (n2 - 1));
                Vector2 d = (p1 - p0);
                if (d.sqrMagnitude < 0.0001f) continue;
                Vector2 nn = new Vector2(-d.y, d.x).normalized * (stroke * 0.5f);
                Quad(vh, p0 + nn, p1 + nn, p1 - nn, p0 - nn, c0, c1, c1, c0);
            }

            // round joints so the line has no notches on sharp bends
            for (int i = 1; i < n2 - 1; i++)
            {
                Color c0 = Color.Lerp(a0, b0, i / (float)(n2 - 1));
                float h = stroke * 0.5f;
                Vector2 p = local[i];
                Quad(vh, new Vector2(p.x - h, p.y - h), new Vector2(p.x - h, p.y + h),
                    new Vector2(p.x + h, p.y + h), new Vector2(p.x + h, p.y - h), c0, c0, c0, c0);
            }
        }
    }
}
