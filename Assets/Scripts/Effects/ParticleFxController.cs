using UnityEngine;

namespace DiceGame.Effects
{
    public sealed class ParticleFxController : MonoBehaviour
    {
        [SerializeField] private ParticleSystem winSparkleParticles;
        [SerializeField] private ParticleSystem confettiBurstParticles;
        [SerializeField] private ParticleSystem jackpotCritParticles;
        [SerializeField] private ParticleSystem diceImpactParticles;
        [SerializeField] private ParticleSystem feverAuraParticles;

        public void PlayWinSparkles(Vector3 worldPos)
        {
            if (winSparkleParticles != null)
            {
                winSparkleParticles.transform.position = worldPos;
                winSparkleParticles.Play();
            }
        }

        public void PlayConfettiBurst(Vector3 worldPos)
        {
            if (confettiBurstParticles != null)
            {
                confettiBurstParticles.transform.position = worldPos;
                confettiBurstParticles.Play();
            }
        }

        public void PlayJackpotCrit(Vector3 worldPos)
        {
            if (jackpotCritParticles != null)
            {
                jackpotCritParticles.transform.position = worldPos;
                jackpotCritParticles.Play();
            }
        }

        public void PlayDiceImpact(Vector3 worldPos)
        {
            if (diceImpactParticles != null)
            {
                diceImpactParticles.transform.position = worldPos;
                diceImpactParticles.Play();
            }
        }

        public void SetFeverAuraActive(bool active)
        {
            if (feverAuraParticles != null)
            {
                if (active && !feverAuraParticles.isPlaying)
                {
                    feverAuraParticles.Play();
                }
                else if (!active && feverAuraParticles.isPlaying)
                {
                    feverAuraParticles.Stop();
                }
            }
        }
    }
}

