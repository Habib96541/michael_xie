using UnityEngine;

namespace PegRush
{
    public sealed class AudioEffects : MonoBehaviour
    {
        private AudioSource _source;
        private AudioClip _tone;
        private float _last;
        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            float[] samples = new float[2205];
            for (int i = 0; i < samples.Length; i++) samples[i] = Mathf.Sin(i * 2f * Mathf.PI * 660f / 22050f) * (1f - i / (float)samples.Length) * 0.12f;
            _tone = AudioClip.Create("Pocket chime", samples.Length, 1, 22050, false);
            _tone.SetData(samples, 0);
        }
        public void Play(bool muted)
        {
            if (muted || Time.unscaledTime - _last < 0.09f) return;
            _last = Time.unscaledTime;
            _source.PlayOneShot(_tone);
        }
    }
}
