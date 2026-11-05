using UnityEngine;
using UnityEngine.UI;

namespace NeonDash
{
    public enum IconKind { Play, Pause, Shuffle, Copy, Retry, Home, Records, Back, Sound, Muted }

    /// <summary>Resolution-independent glyphs on a 24-unit grid.</summary>
    public sealed class UiIcon : MaskableGraphic
    {
        IconKind kind;

        public static UiIcon Add(Transform parent, IconKind kind, Color tint, float size, float offset = 0f)
        {
            var icon = new GameObject(kind + " Icon", typeof(RectTransform)).AddComponent<UiIcon>();
            icon.transform.SetParent(parent, false);
            icon.SetKind(kind);
            icon.color = tint;
            icon.raycastTarget = false;
            UiKit.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(offset, 0f), Vector2.one * size);
            return icon;
        }

        public void SetKind(IconKind value)
        {
            kind = value;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            switch (kind)
            {
                case IconKind.Play:
                    Shape(mesh, new Vector2(6, 3), new Vector2(20, 12), new Vector2(6, 21));
                    break;
                case IconKind.Pause:
                    Rect(mesh, 5, 4, 5, 16);
                    Rect(mesh, 14, 4, 5, 16);
                    break;
                case IconKind.Shuffle:
                    Line(mesh, 3, 6, 8, 6); Line(mesh, 8, 6, 16, 18); Line(mesh, 16, 18, 21, 18);
                    Line(mesh, 3, 18, 8, 18); Line(mesh, 8, 18, 10, 15);
                    Line(mesh, 14, 9, 16, 6); Line(mesh, 16, 6, 21, 6);
                    Arrow(mesh, 22, 18); Arrow(mesh, 22, 6);
                    break;
                case IconKind.Copy:
                    Outline(mesh, 3, 3, 12, 13);
                    Line(mesh, 8, 19, 8, 21); Line(mesh, 8, 21, 20, 21);
                    Line(mesh, 20, 21, 20, 8); Line(mesh, 20, 8, 18, 8);
                    break;
                case IconKind.Retry:
                    Arc(mesh, 12, 12, 8, 150, 440);
                    Shape(mesh, new Vector2(8, 20), new Vector2(4, 12), new Vector2(1, 20));
                    break;
                case IconKind.Home:
                    Shape(mesh, new Vector2(2, 11), new Vector2(12, 21), new Vector2(22, 11));
                    Rect(mesh, 5, 3, 5, 9); Rect(mesh, 14, 3, 5, 9);
                    break;
                case IconKind.Records:
                    Rect(mesh, 3, 4, 4, 8); Rect(mesh, 10, 4, 4, 16); Rect(mesh, 17, 4, 4, 12);
                    break;
                case IconKind.Back:
                    Line(mesh, 4, 12, 21, 12); Line(mesh, 4, 12, 11, 19); Line(mesh, 4, 12, 11, 5);
                    break;
                case IconKind.Sound:
                case IconKind.Muted:
                    Shape(mesh, new Vector2(2, 8), new Vector2(6, 8), new Vector2(11, 4),
                        new Vector2(11, 20), new Vector2(6, 16), new Vector2(2, 16));
                    if (kind == IconKind.Sound)
                    {
                        Arc(mesh, 10, 12, 7, -45, 45);
                        Arc(mesh, 10, 12, 11, -45, 45);
                    }
                    else
                    {
                        Line(mesh, 15, 9, 21, 15); Line(mesh, 15, 15, 21, 9);
                    }
                    break;
            }
        }

        void Arrow(VertexHelper mesh, float x, float y)
        {
            Shape(mesh, new Vector2(x, y), new Vector2(x - 5, y + 4), new Vector2(x - 5, y - 4));
        }

        void Outline(VertexHelper mesh, float x, float y, float w, float h)
        {
            Line(mesh, x, y, x + w, y); Line(mesh, x + w, y, x + w, y + h);
            Line(mesh, x + w, y + h, x, y + h); Line(mesh, x, y + h, x, y);
        }

        void Rect(VertexHelper mesh, float x, float y, float w, float h)
        {
            Shape(mesh, new Vector2(x, y), new Vector2(x + w, y), new Vector2(x + w, y + h), new Vector2(x, y + h));
        }

        void Arc(VertexHelper mesh, float x, float y, float radius, float start, float end)
        {
            var center = new Vector2(x, y);
            var previous = center + Direction(start) * radius;
            for (int i = 1; i <= 12; i++)
            {
                var next = center + Direction(Mathf.Lerp(start, end, i / 12f)) * radius;
                Line(mesh, previous.x, previous.y, next.x, next.y);
                previous = next;
            }
        }

        static Vector2 Direction(float angle)
        {
            float rad = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        void Line(VertexHelper mesh, float x, float y, float endX, float endY)
        {
            var from = new Vector2(x, y);
            var to = new Vector2(endX, endY);
            var delta = (to - from).normalized;
            var edge = new Vector2(-delta.y, delta.x) * 1.05f;
            Shape(mesh, from - edge, to - edge, to + edge, from + edge);
        }

        void Shape(VertexHelper mesh, params Vector2[] points)
        {
            Rect bounds = GetPixelAdjustedRect();
            int first = mesh.currentVertCount;
            foreach (Vector2 p in points)
                mesh.AddVert(bounds.min + Vector2.Scale(bounds.size, p / 24f), color, Vector2.zero);
            for (int i = 1; i < points.Length - 1; i++) mesh.AddTriangle(first, first + i, first + i + 1);
        }
    }
}
