using UnityEngine;

namespace TeslaRoadster
{
    internal sealed partial class RoadsterWorld
    {
        const float Tail = -2.16f;
        readonly System.Random particleRandom = new(8137);
        ParticleSystem vapor, dust, sparks;
        float vaporEmission, nitroEmission;

        void PoseCar(Vector3 floor, Quaternion rotation, float distance, float centerCompensation = 1f)
        {
            car.localRotation = rotation;
            car.localPosition = floor + (RotationCenter - rotation * RotationCenter) * centerCompensation;
            vehicle.RollWheels(distance);
        }

        void CreateParticles(Shader shader)
        {
            var material = new Material(shader) { name = "Soft vapor and impact particles" };
            vapor = CreateEmitter("Thruster vapor trail", material, 96, -0.07f, 1.6f);
            dust = CreateEmitter("Impact dust", material, 48, 0.25f, 1.8f);
            sparks = CreateEmitter("Impact sparks", material, 36, 1.1f, 0.5f);
            var renderer = sparks.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 1.8f;
            renderer.velocityScale = 0.09f;
        }

        ParticleSystem CreateEmitter(string name, Material material, int capacity, float gravity, float endSize)
        {
            var particles = Group(root, name).gameObject.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.maxParticles = capacity;
            main.gravityModifier = gravity;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            var emission = particles.emission;
            emission.enabled = false;
            var lifetime = particles.colorOverLifetime;
            lifetime.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.85f, 0.4f), new GradientAlphaKey(0f, 1f) });
            lifetime.color = fade;
            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.8f, 1f, endSize));
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.maxParticleSize = 0.3f;
            particles.Play();
            return particles;
        }

        void ClearParticles()
        {
            vaporEmission = nitroEmission = 0f;
            vapor.Clear();
            dust.Clear();
            sparks.Clear();
        }

        public void ShowLanding(float x, float pulse)
        {
            landingRing.gameObject.SetActive(pulse > 0.06f);
            if (landingRing.gameObject.activeSelf)
            {
                landingRing.localPosition = new Vector3(x, 0.045f, 0f);
                landingRing.localScale = new Vector3(1.35f - pulse * 0.63f, 1f, 1.35f - pulse * 0.63f);
            }
        }

        void UpdateVapor(float dt, float thrust, int sideJet = 0, bool driving = false)
        {
            if (thrust < 0.05f)
            {
                vaporEmission = 0f;
                return;
            }
            vaporEmission += dt * Mathf.Lerp(22f, 100f, thrust);
            int count = Mathf.FloorToInt(vaporEmission);
            vaporEmission -= count;
            for (int i = 0; i < count; i++)
            {
                float side = ParticleRange(-1f, 1f) < 0f ? -1f : 1f;
                Vector3 offset, drift;
                if (sideJet != 0)
                {
                    offset = new Vector3(-sideJet * 0.91f, 0.32f, ParticleRange(-0.95f, 0.85f));
                    drift = new Vector3(-sideJet * ParticleRange(4f, 7f), ParticleRange(-0.1f, 0.7f), ParticleRange(-8f, -4f));
                }
                else if (driving)
                {
                    offset = new Vector3(side * 0.57f, 0.20f, -0.57f);
                    drift = Vector3.back * ParticleRange(6f, 10f) + Vector3.down * ParticleRange(0.8f, 2.9f);
                }
                else
                {
                    offset = new Vector3(side * 0.39f, 0.38f, Tail);
                    drift = car.localRotation * Vector3.back * ParticleRange(3f, 7f);
                }
                vapor.Emit(new ParticleSystem.EmitParams
                {
                    position = car.localPosition + car.localRotation * offset,
                    velocity = drift + new Vector3(ParticleRange(-0.65f, 0.65f), ParticleRange(-0.18f, 0.6f), 0f),
                    startSize = ParticleRange(0.26f, 0.76f),
                    startLifetime = ParticleRange(0.36f, 0.64f),
                    startColor = new Color32(234, 243, 255, (byte)ParticleRange(142f, 216f))
                }, 1);
            }
        }

        public void EmitNitro(float dt)
        {
            if (dt <= 0f)
            {
                nitroEmission = 0f;
                return;
            }
            nitroEmission += 108f * dt;
            int pairs = Mathf.FloorToInt(nitroEmission * 0.5f);
            nitroEmission -= pairs * 2;
            for (int i = 0; i < pairs; i++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    vapor.Emit(new ParticleSystem.EmitParams
                    {
                        position = car.localPosition + car.localRotation * new Vector3(side * 0.54f, 0.29f, Tail),
                        velocity = car.localRotation * Vector3.back * ParticleRange(9f, 19f) +
                            new Vector3(ParticleRange(-0.38f, 0.38f), ParticleRange(-0.23f, 0.38f), 0f),
                        startSize = ParticleRange(0.17f, 0.36f),
                        startLifetime = ParticleRange(0.18f, 0.37f),
                        startColor = new Color32(83, 210, 255, 235)
                    }, 1);
                }
            }
        }

        public void BurstAt(Vector3 center, float strength)
        {
            for (int i = 0; i < 18; i++)
            {
                float angle = i * 2.39996f;
                Vector3 radial = new(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                dust.Emit(new ParticleSystem.EmitParams
                {
                    position = center + radial * ParticleRange(0.4f, 1.5f),
                    velocity = (radial * ParticleRange(2f, 5.7f) + Vector3.up * ParticleRange(0.4f, 1.65f)) * strength,
                    startLifetime = ParticleRange(0.38f, 0.74f),
                    startSize = ParticleRange(0.33f, 0.77f) * strength,
                    startColor = new Color32(232, 208, 194, 183)
                }, 1);
                if (i >= 12)
                    continue;
                sparks.Emit(new ParticleSystem.EmitParams
                {
                    position = center + radial * 0.72f,
                    velocity = (radial * ParticleRange(4f, 8f) + Vector3.up * ParticleRange(1.3f, 3.4f)) * strength,
                    startLifetime = ParticleRange(0.19f, 0.48f),
                    startSize = ParticleRange(0.08f, 0.15f),
                    startColor = new Color32(255, 206, 115, 255)
                }, 1);
            }
        }

        float ParticleRange(float min, float max) => Mathf.Lerp(min, max, (float)particleRandom.NextDouble());
    }
}
