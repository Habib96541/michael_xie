using BullRide.Core;
using UnityEngine;

namespace BullRide
{
    public sealed class DustFx : MonoBehaviour
    {
        public event System.Action<bool> HoofContact;

        ParticleSystem dust;
        bool airborne;
        float delay;

        public void Build()
        {
            dust = gameObject.AddComponent<ParticleSystem>();
            dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = dust.main;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.45f, .85f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.40f, 1.45f);
            main.startSize = new ParticleSystem.MinMaxCurve(.52f, .86f);
            main.startColor = new Color(.78f, .57f, .37f, .42f);
            main.maxParticles = 90;
            main.gravityModifier = .16f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = dust.emission;
            emission.enabled = false;
            var shape = dust.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 60;
            shape.radius = .34f;
            shape.rotation = new Vector3(-90, 0, 0);
            var color = dust.colorOverLifetime;
            color.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(.55f, .40f), new GradientAlphaKey(0, 1) });
            color.color = fade;
            var size = dust.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1.65f, AnimationCurve.Linear(0, .38f, 1, 1));
            GetComponent<ParticleSystemRenderer>().sharedMaterial = new Material(Resources.Load<Shader>("RodeoDust"));
            dust.Play();
        }

        public void Clear()
        {
            dust.Clear();
            airborne = false;
            delay = 0;
        }

        public void Tick(RideSim sim, float dt)
        {
            if (!sim.CanBalance) return;
            bool lifted = sim.Pose.Hop + .17f * sim.Pose.Kick > .12f;
            delay -= dt;
            if (!lifted && (airborne || (sim.Pose.Speed > 1 && delay <= 0)))
            {
                transform.position = new Vector3(sim.Position.x - Mathf.Sin(sim.Yaw)*.56f, .11f,
                    sim.Position.y - Mathf.Cos(sim.Yaw)*.56f);
                dust.Emit(airborne ? 12 : 3);
                HoofContact?.Invoke(airborne);
                delay = .13f;
            }
            airborne = lifted;
        }
    }
}
