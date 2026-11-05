using System.Collections.Generic;
using UnityEngine;

namespace NeonDash
{
    /// <summary>
    /// Lightweight particle system: jump dust, orb sparks, portal rings, death explosions and a
    /// drifting background of dust motes. Uses one mesh rebuilt each frame instead of Unity's
    /// ParticleSystem so it stays dependency-free and predictable in WebGL.
    /// </summary>
    public sealed class Fx : MonoBehaviour
    {
        struct Particle
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public Color Color;
            public float Life;
            public float MaxLife;
            public float Size;
            public float Angle;
            public float Spin;
            public float Drag;
            public float Gravity;
        }

        const int MaxParticles = 700;

        readonly List<Particle> particles = new List<Particle>();
        MeshFilter filter;
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Color> colors = new List<Color>();
        readonly List<int> tris = new List<int>();

        float shake;
        Vector2 impact;
        Color impactColor;
        float impactTime;
        const float ImpactSeconds = 0.30f;

        public float ShakeAmount { get { return shake; } }

        void Awake()
        {
            MeshFilter created;
            MeshTools.CreateMeshObject("Particles", transform, 0, out created);
            filter = created;
            filter.mesh = new Mesh();
            filter.mesh.MarkDynamic();
        }

        public void Emit(Vector2 position, Color color, int count, float speed, float size, float life, float gravity = 0f)
        {
            for (int i = 0; i < count && particles.Count < MaxParticles; i++)
            {
                float angle = Random.value * Mathf.PI * 2f;
                float magnitude = speed * (0.35f + Random.value * 0.75f);
                var p = new Particle();
                p.Position = position;
                p.Velocity = new Vector2(Mathf.Cos(angle) * magnitude, Mathf.Sin(angle) * magnitude);
                p.Color = color;
                p.MaxLife = life * (0.7f + Random.value * 0.6f);
                p.Life = p.MaxLife;
                p.Size = size * (0.6f + Random.value * 0.7f);
                p.Angle = angle;
                p.Spin = (Random.value - 0.5f) * 380f;
                p.Drag = 2.6f;
                p.Gravity = gravity;
                particles.Add(p);
            }
        }

        public void ResetForRun()
        {
            particles.Clear();
            impactTime = 0f;
            shake = 0f;
            filter.mesh.Clear();
        }

        public void Shatter(Vector2 position, float angle, int gravitySign, Color color)
        {
            impact = position;
            impactColor = color;
            impactTime = ImpactSeconds;
            AddShake(0.42f);

            var rotation = Quaternion.Euler(0f, 0f, angle);
            for (int row = 0; row < 4; row++)
            for (int col = 0; col < 4; col++)
            {
                if (particles.Count >= MaxParticles) break;
                Vector2 offset = rotation * new Vector3((col - 1.5f) * 0.21f, (row - 1.5f) * 0.21f, 0f);
                float life = Random.Range(0.26f, 0.48f);
                particles.Add(new Particle
                {
                    Position = position + offset,
                    Velocity = offset.normalized * Random.Range(4.8f, 9.0f) + new Vector2(2.3f, 0.9f * gravitySign),
                    Color = (row + col) % 3 == 0 ? Palette.Cyan : color,
                    Life = life, MaxLife = life,
                    Size = Random.Range(0.14f, 0.25f), Angle = angle,
                    Spin = Random.Range(-690f, 690f), Drag = 2.4f, Gravity = -11f * gravitySign
                });
            }
            Emit(position, Color.white, 9, 13f, 0.10f, 0.21f, -2f * gravitySign);
        }

        public void Trail(Vector2 position, Color color, float size)
        {
            if (particles.Count >= MaxParticles) return;
            var p = new Particle();
            p.Position = position + new Vector2(Random.Range(-0.12f, 0.12f), Random.Range(-0.12f, 0.12f));
            p.Velocity = new Vector2(Random.Range(-1.2f, -0.2f), Random.Range(-0.5f, 0.5f));
            p.Color = color;
            p.MaxLife = 0.32f;
            p.Life = p.MaxLife;
            p.Size = size;
            p.Angle = Random.value * Mathf.PI;
            p.Spin = (Random.value - 0.5f) * 240f;
            p.Drag = 1.5f;
            p.Gravity = 0f;
            particles.Add(p);
        }

        public void Ring(Vector2 position, Color color, int count = 18, float speed = 7f)
        {
            for (int i = 0; i < count && particles.Count < MaxParticles; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f;
                var p = new Particle();
                p.Position = position;
                p.Velocity = new Vector2(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed);
                p.Color = color;
                p.MaxLife = 0.42f;
                p.Life = p.MaxLife;
                p.Size = 0.2f;
                p.Angle = angle;
                p.Spin = 0f;
                p.Drag = 3.2f;
                p.Gravity = 0f;
                particles.Add(p);
            }
        }

        public void Ambient(Vector2 position, Color color)
        {
            if (particles.Count >= MaxParticles) return;
            var p = new Particle();
            p.Position = position;
            p.Velocity = new Vector2(-0.35f, 0.12f + Random.value * 0.1f);
            p.Color = color;
            p.MaxLife = 5.5f;
            p.Life = p.MaxLife;
            p.Size = 0.09f + Random.value * 0.06f;
            p.Angle = 0f;
            p.Spin = 0f;
            p.Drag = 0f;
            p.Gravity = 0f;
            particles.Add(p);
        }

        public void AddShake(float amount)
        {
            shake = Mathf.Min(1.4f, shake + amount);
        }

        void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            shake = Mathf.Max(0f, shake - dt * 2.6f);
            impactTime = Mathf.Max(0f, impactTime - dt);

            for (int i = particles.Count - 1; i >= 0; i--)
            {
                Particle p = particles[i];
                p.Life -= dt;
                if (p.Life <= 0f)
                {
                    particles.RemoveAt(i);
                    continue;
                }
                p.Velocity += new Vector2(0f, p.Gravity) * dt;
                if (p.Drag > 0f) p.Velocity *= 1f / (1f + p.Drag * dt);
                p.Position += p.Velocity * dt;
                p.Angle += p.Spin * dt;
                particles[i] = p;
            }

            Rebuild();
        }

        void Rebuild()
        {
            verts.Clear();
            colors.Clear();
            tris.Clear();

            if (impactTime > 0f)
            {
                float remaining = impactTime / ImpactSeconds;
                float radius = Mathf.Lerp(2.8f, 0.48f, remaining * remaining);
                MeshTools.AddRing(verts, colors, tris, impact.x, impact.y,
                    radius - 0.07f * remaining, radius, 36, Palette.Alpha(impactColor, remaining * 0.9f));
                if (remaining > 0.74f)
                    MeshTools.AddCircle(verts, colors, tris, impact.x, impact.y, radius * 0.7f,
                        24, Palette.Alpha(Color.white, (remaining - 0.74f) * 2.7f));
            }

            for (int i = 0; i < particles.Count; i++)
            {
                Particle p = particles[i];
                float fade = Mathf.Clamp01(p.Life / p.MaxLife);
                Color c = p.Color;
                c.a *= fade * fade;
                float half = p.Size * (0.4f + fade * 0.8f) * 0.5f;
                float cos = Mathf.Cos(p.Angle * Mathf.Deg2Rad);
                float sin = Mathf.Sin(p.Angle * Mathf.Deg2Rad);
                int start = verts.Count;

                verts.Add(new Vector3(p.Position.x - cos * half + sin * half, p.Position.y - sin * half - cos * half, 0f));
                verts.Add(new Vector3(p.Position.x + cos * half + sin * half, p.Position.y + sin * half - cos * half, 0f));
                verts.Add(new Vector3(p.Position.x + cos * half - sin * half, p.Position.y + sin * half + cos * half, 0f));
                verts.Add(new Vector3(p.Position.x - cos * half - sin * half, p.Position.y - sin * half + cos * half, 0f));

                for (int v = 0; v < 4; v++) colors.Add(c);
                tris.Add(start); tris.Add(start + 2); tris.Add(start + 1);
                tris.Add(start); tris.Add(start + 3); tris.Add(start + 2);
            }

            Mesh mesh = filter.mesh;
            mesh.Clear();
            if (verts.Count == 0) return;
            mesh.SetVertices(verts);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
        }
    }
}
