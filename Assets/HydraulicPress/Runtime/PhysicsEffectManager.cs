using UnityEngine;

namespace HydraulicPress
{
    public sealed class PhysicsEffectManager : MonoBehaviour
    {
        public Mesh[] shardMeshes;
        public Material shardMaterial;
        public Material particleMaterial;
        private sealed class Fragment { public GameObject go; public Rigidbody body; public Renderer renderer; public float expires; }
        private Fragment[] fragments;
        private ParticleSystem[] particles;
        private int cursor;
        private MaterialPropertyBlock block;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        public int ActiveFragments { get; private set; }
        public bool LowPower { get; set; }

        public void Initialize(int budget)
        {
            fragments = new Fragment[budget]; block = new MaterialPropertyBlock();
            for (int i = 0; i < budget; i++)
            {
                var go = new GameObject("Pooled fracture " + i); go.transform.SetParent(transform);
                go.AddComponent<MeshFilter>().sharedMesh = shardMeshes[i % shardMeshes.Length];
                var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = shardMaterial;
                go.AddComponent<BoxCollider>().size = Vector3.one * .65f;
                var body = go.AddComponent<Rigidbody>(); body.mass = .06f; body.linearDamping = .15f; body.angularDamping = .25f;
                body.interpolation = RigidbodyInterpolation.Interpolate; body.collisionDetectionMode = CollisionDetectionMode.Discrete;
                fragments[i] = new Fragment { go = go, body = body, renderer = renderer }; go.SetActive(false);
            }
            particles = new ParticleSystem[3];
            for (int i = 0; i < particles.Length; i++)
            {
                var go = new GameObject("Pooled dust burst " + i); go.transform.SetParent(transform);
                var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); var main = ps.main;
                main.playOnAwake = false; main.loop = false; main.duration = .6f; main.startLifetime = .65f; main.startSpeed = 2;
                main.startSize = .045f; main.gravityModifier = .7f; main.maxParticles = 70; main.simulationSpace = ParticleSystemSimulationSpace.World;
                var emission = ps.emission; emission.enabled = false;
                var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .25f;
                var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0));
                go.GetComponent<ParticleSystemRenderer>().sharedMaterial = particleMaterial;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); particles[i] = ps;
            }
        }

        public void Burst(Vector3 position, Color color, CrushMaterial material, bool failure = false)
        {
            int count = failure ? fragments.Length : material == CrushMaterial.Brittle ? 24 : material == CrushMaterial.Liquid ? 18 : 10;
            count = Mathf.Min(count, fragments.Length);
            if (LowPower) count = Mathf.Max(5, count / 2);
            for (int i = 0; i < count; i++)
            {
                Fragment f = fragments[cursor++ % fragments.Length]; f.go.SetActive(false);
                f.go.transform.position = position + Random.insideUnitSphere * .15f;
                f.go.transform.rotation = Random.rotation;
                float scale = Random.Range(.09f, .21f);
                f.go.transform.localScale = material == CrushMaterial.Metal ? new Vector3(scale * 2, scale * .4f, scale) : Vector3.one * scale;
                block.SetColor(BaseColor, i % 4 == 0 ? Color.Lerp(color, Color.white, .45f) : color); f.renderer.SetPropertyBlock(block);
                f.go.SetActive(true);
                Vector3 direction = Random.insideUnitSphere; direction.y = Mathf.Abs(direction.y) * .8f + .4f;
                f.body.linearVelocity = direction * (failure ? 4.5f : 2.7f);
                f.body.angularVelocity = Random.insideUnitSphere * 14; f.expires = Time.time + 2.8f;
            }
            var ps = particles[cursor % particles.Length];
            ps.transform.position = position; var main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, .7f));
            main.startSize = material == CrushMaterial.Liquid ? .075f : .035f;
            ps.Emit(LowPower ? 14 : failure ? 55 : 28);
        }

        private void Update()
        {
            if (fragments == null) return;
            int count = 0;
            foreach (var f in fragments)
            {
                if (!f.go.activeSelf) continue;
                if (Time.time > f.expires || f.go.transform.position.y < -2 || f.go.transform.position.sqrMagnitude > 200) f.go.SetActive(false);
                else { count++; if (f.expires - Time.time < .4f) f.go.transform.localScale *= Mathf.Max(0, 1 - Time.deltaTime * 7); }
            }
            ActiveFragments = count;
        }

        public void Clear()
        {
            foreach (var f in fragments) f.go.SetActive(false);
            foreach (var ps in particles) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ActiveFragments = 0;
        }
    }
}
