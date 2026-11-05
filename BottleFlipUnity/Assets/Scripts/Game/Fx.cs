using UnityEngine;

namespace BottleFlip
{
    /// <summary>All particle juice: landing dust, flip sparkles, victory confetti and shockwave rings.</summary>
    public class Fx : MonoBehaviour
    {
        ParticleSystem _dust;
        ParticleSystem _sparkle;
        ParticleSystem _confetti;

        public void Build()
        {
            _dust = CreateSystem("DustFX", Art.Unlit(Art.SoftCircle, Color.white, true, "DustMat"), 0.15f, 400);
            _sparkle = CreateSystem("SparkleFX", Art.Unlit(Art.Glow, Pal.AmberSoft, true, "SparkMat"), 0.4f, 400);
            _confetti = CreateSystem("ConfettiFX", Art.Unlit(Art.Confetti, Color.white, false, "ConfettiMat"), 1.15f, 900);
        }

        ParticleSystem CreateSystem(string name, Material mat, float gravity, int max)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 1f;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0f;
            main.startLifetime = 1f;
            main.startSize = 0.2f;
            main.startColor = Color.white;
            main.gravityModifier = gravity;
            main.maxParticles = max;

            var emission = ps.emission;
            emission.enabled = false;

            var shape = ps.shape;
            shape.enabled = false;

            var size = ps.sizeOverLifetime;
            size.enabled = false;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = false;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = mat;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            ps.Play();
            return ps;
        }

        public void Dust(Vector3 pos, int count = 14, float power = 1f)
        {
            if (_dust == null) return;
            for (int i = 0; i < count; i++)
            {
                float a = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
                float speed = Random.Range(0.7f, 1.9f) * power;
                var ep = new ParticleSystem.EmitParams
                {
                    position = pos + new Vector3(Mathf.Cos(a) * 0.16f, 0.03f, Mathf.Sin(a) * 0.16f),
                    velocity = new Vector3(Mathf.Cos(a) * speed, Random.Range(0.25f, 0.85f), Mathf.Sin(a) * speed * 0.7f),
                    startLifetime = Random.Range(0.35f, 0.7f),
                    startSize = Random.Range(0.16f, 0.42f) * power,
                    startColor = Pal.Hex("DCE9FF").Alpha(Random.Range(0.25f, 0.5f)),
                    rotation = Random.Range(0f, 6.28f),
                    applyShapeToPosition = false
                };
                _dust.Emit(ep, 1);
            }
        }

        public void Sparkle(Vector3 pos, int count = 16, Color? color = null)
        {
            if (_sparkle == null) return;
            Color c = color ?? Pal.AmberSoft;
            for (int i = 0; i < count; i++)
            {
                Vector3 dir = Random.insideUnitSphere;
                dir.z *= 0.35f;
                var ep = new ParticleSystem.EmitParams
                {
                    position = pos + Random.insideUnitSphere * 0.3f,
                    velocity = dir.normalized * Random.Range(0.6f, 2.2f),
                    startLifetime = Random.Range(0.25f, 0.6f),
                    startSize = Random.Range(0.08f, 0.26f),
                    startColor = c.Alpha(Random.Range(0.5f, 1f)),
                    applyShapeToPosition = false
                };
                _sparkle.Emit(ep, 1);
            }
        }

        public void Confetti(Vector3 pos, int count = 160, float power = 1f)
        {
            if (_confetti == null) return;
            Color[] colors = { Pal.Amber, Pal.Coral, Pal.Teal, Color.white, Pal.AmberSoft };
            for (int i = 0; i < count; i++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                float speed = Random.Range(1.4f, 4.4f) * power;
                var ep = new ParticleSystem.EmitParams
                {
                    position = pos + new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(-0.1f, 0.5f), Random.Range(-0.3f, 0.3f)),
                    velocity = new Vector3(Mathf.Cos(a) * speed, Random.Range(3.2f, 7.2f) * power, Mathf.Sin(a) * speed * 0.5f),
                    startLifetime = Random.Range(1.3f, 2.8f),
                    startSize = Random.Range(0.07f, 0.19f),
                    startColor = colors[Random.Range(0, colors.Length)],
                    rotation = Random.Range(0f, 6.28f),
                    angularVelocity = Random.Range(-8f, 8f),
                    applyShapeToPosition = false
                };
                _confetti.Emit(ep, 1);
            }
        }

        /// <summary>Expanding shockwave ring used when the bottle actually stands.</summary>
        public void Ring(Vector3 pos, Color color, float radius = 2.6f, float duration = 0.55f)
        {
            var go = new GameObject("ShockRing");
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.AddComponent<MeshFilter>().sharedMesh = Art.QuadMesh(1f, "RingQuad");
            var mr = go.AddComponent<MeshRenderer>();
            var mat = Art.Unlit(Art.Glow, color, true, "RingMat");
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var tr = go.transform;
            Tween.To(duration, t =>
            {
                float s = Mathf.Lerp(0.4f, radius, Ease.OutCubic(t));
                tr.localScale = new Vector3(s, s, 1f);
                mat.SetColor("_TintColor", color.Alpha(1f - t));
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", color.Alpha(1f - t));
            }, Ease.Linear, () => Destroy(go));
        }
    }
}
