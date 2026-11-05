using UnityEngine;

namespace Watermelon
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class WatermelonAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip bandClip;
        [SerializeField] private AudioClip burstClip;
        [SerializeField, Range(0f, 1f)] private float bandVolume = 0.45f;
        [SerializeField, Range(0f, 1f)] private float burstVolume = 0.65f;

        private AudioSource audioSource;

        private void Awake()
        {
            TryGetComponent(out audioSource);
        }

        public void PlayBand()
        {
            audioSource.PlayOneShot(bandClip, bandVolume);
        }

        public void PlayBurst()
        {
            audioSource.PlayOneShot(burstClip, burstVolume);
        }

        public void StopSounds()
        {
            audioSource.Stop();
        }
    }
}
