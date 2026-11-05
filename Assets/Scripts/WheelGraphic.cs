using UnityEngine;
using UnityEngine.UI;

namespace SpinTheWheel
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class WheelGraphic : MaskableGraphic
    {
        [SerializeField, Min(2)] private int segmentCount = 20;
        [SerializeField] private Color[] segmentColors =
        {
            new Color32(255, 174, 154, 255),
            new Color32(123, 204, 255, 255),
            new Color32(119, 229, 199, 255),
            new Color32(255, 223, 123, 255),
            new Color32(195, 178, 255, 255)
        };
        [SerializeField] private Color accentColor = new Color32(255, 192, 57, 255);

        public int SegmentCount => segmentCount;

        protected override void Awake()
        {
            base.Awake();
            useLegacyMeshGeneration = false;
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            Rect bounds = GetPixelAdjustedRect();
            Vector2 center = bounds.center;
            float radius = Mathf.Min(bounds.width, bounds.height) * 0.5f;
            if (radius <= 0f || segmentCount < 2)
                return;

            AddDisc(vertices, center, radius, accentColor);
            AddDisc(vertices, center, radius - 6f, Color.white);

            float sliceAngle = 360f / segmentCount;
            for (int index = 0; index < segmentCount; index++)
            {
                Color sliceColor = segmentColors != null && segmentColors.Length > 0
                    ? segmentColors[index % segmentColors.Length]
                    : Color.white;
                float start = 90f - index * sliceAngle - sliceAngle * 0.5f + 0.2f;
                AddSector(vertices, center, radius - 14f, start, sliceAngle - 0.4f, sliceColor);
            }

            AddDisc(vertices, center, 61f, accentColor);
            AddDisc(vertices, center, 58f, Color.white);
            AddDisc(vertices, center, 52f, new Color32(255, 248, 225, 255));
        }

        private void AddDisc(VertexHelper vertices, Vector2 center, float radius, Color tint, int steps = 160)
        {
            AddSector(vertices, center, radius, 0f, 360f, tint, steps);
        }

        private void AddSector(VertexHelper vertices, Vector2 center, float radius,
            float start, float sweep, Color tint, int steps = 8)
        {
            int firstVertex = vertices.currentVertCount;
            vertices.AddVert(center, tint * color, Vector2.zero);
            for (int step = 0; step <= steps; step++)
            {
                Vector2 position = center + Direction(start + sweep * step / steps) * radius;
                vertices.AddVert(position, tint * color, Vector2.zero);
                if (step > 0)
                    vertices.AddTriangle(firstVertex, firstVertex + step + 1, firstVertex + step);
            }
        }

        private static Vector2 Direction(float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }
    }
}
