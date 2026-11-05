using UnityEngine;

namespace FlapDash
{
    public sealed class PlayerFeedback : MonoBehaviour
    {
        [SerializeField] private RoundController round;
        [SerializeField] private VisualEffectsSettings settings;
        [SerializeField] private Transform visual;
        [SerializeField] private SpriteRenderer bird;
        [SerializeField] private Sprite[] flightFrames;
        [SerializeField] private TrailRenderer trail;
        [SerializeField] private ParticleSystem sparks;
        private Rigidbody2D body;
        private float animationAge;
        private float flapAge = 1f;
        public Transform Visual => visual;
        public int ActiveParticles => sparks.particleCount;
        public Sprite CurrentFrame => bird.sprite;
        public int FrameCount => flightFrames.Length;

        private void Awake()
        {
            TryGetComponent(out body);
        }

        private void LateUpdate()
        {
            float intensity = settings.Intensity;
            flapAge += Time.unscaledDeltaTime;
            float pulse = Mathf.Sin(Mathf.Clamp01(flapAge / 0.12f) * Mathf.PI) * intensity;
            float tilt = round.IsPlaying ? Mathf.Clamp(body.linearVelocity.y * 4f, -35f, 25f) * intensity : 0f;
            visual.localRotation = Quaternion.Slerp(visual.localRotation, Quaternion.Euler(0f, 0f, tilt), 1f - Mathf.Exp(-20f * Time.deltaTime));
            visual.localScale = new Vector3(1f - pulse * 0.16f, 1f + pulse * 0.2f, 1f);
            if (round.State != RoundState.Results) animationAge += Time.unscaledDeltaTime * intensity;
            bird.sprite = flightFrames[Mathf.FloorToInt(animationAge * 14f) % flightFrames.Length];
            trail.emitting = round.IsPlaying && intensity > 0f;
            trail.widthMultiplier = 0.2f * intensity;
            if (intensity == 0f) ClearEffects();
        }

        public void PlayFlap()
        {
            flapAge = 0f;
            if (settings.Intensity > 0f) sparks.Play();
            sparks.Emit(Mathf.RoundToInt(6f * settings.Intensity));
        }

        public void ClearEffects()
        {
            flapAge = 1f;
            trail.Clear();
            sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            visual.localScale = Vector3.one;
            visual.localRotation = Quaternion.identity;
            animationAge = 0f;
            bird.sprite = flightFrames[0];
        }
    }
}
