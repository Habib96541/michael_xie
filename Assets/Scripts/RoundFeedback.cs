using UnityEngine;

namespace FlapDash
{
    public sealed class RoundFeedback : MonoBehaviour
    {
        [SerializeField] private VisualEffectsSettings settings;
        [SerializeField] private Transform player;
        [SerializeField] private PlayerFeedback playerFeedback;
        [SerializeField] private RectTransform score;
        [SerializeField] private RectTransform panel;
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private ParticleSystem celebration;
        private float scoreAge = 1f;
        private float panelAge = 1f;
        public int ActiveParticles => celebration.particleCount;

        private void Update()
        {
            float amount = settings.Intensity;
            scoreAge += Time.unscaledDeltaTime;
            panelAge += Time.unscaledDeltaTime;
            float scorePulse = Mathf.Sin(Mathf.Clamp01(scoreAge / 0.28f) * Mathf.PI);
            score.localScale = Vector3.one * (1f + scorePulse * 0.28f * amount);
            float progress = Mathf.Clamp01(panelAge / 0.24f);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            panel.localScale = Vector3.one * Mathf.Lerp(1f - 0.16f * amount, 1f, eased);
            panelGroup.alpha = Mathf.Lerp(1f - amount, 1f, eased);
            if (amount == 0f) celebration.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        public void PointScored()
        {
            scoreAge = 0f;
            Burst(18);
        }

        public void ShowState(RoundState state)
        {
            if (state == RoundState.Playing)
            {
                ClearEffects();
                playerFeedback.ClearEffects();
            }
            else
            {
                panelAge = 0f;
                if (state == RoundState.Results) Burst(26);
            }
        }

        public void ClearEffects()
        {
            scoreAge = panelAge = 1f;
            score.localScale = panel.localScale = Vector3.one;
            panelGroup.alpha = 1f;
            celebration.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void Burst(int count)
        {
            celebration.transform.position = player.position;
            if (settings.Intensity > 0f) celebration.Play();
            celebration.Emit(Mathf.RoundToInt(count * settings.Intensity));
        }
    }
}
