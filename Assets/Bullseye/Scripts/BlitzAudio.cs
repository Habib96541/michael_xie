using UnityEngine;

namespace BullseyeBlitz
{
    public sealed class BlitzAudio : MonoBehaviour
    {
        private AudioSource _music;
        private AudioSource _sound;
        private BlitzConfig _config;
        private WalletStore _wallet;
        private AudioClip _tone;
        public void Configure(BlitzConfig config, WalletStore wallet)
        {
            _config = config; _wallet = wallet;
            _music = gameObject.AddComponent<AudioSource>();
            _music.loop = true; _music.clip = config.Music;
            _sound = gameObject.AddComponent<AudioSource>();
            float[] samples = new float[2205];
            for (int i = 0; i < samples.Length; i++) { samples[i] = Mathf.Sin(i * 2f * Mathf.PI * 660f / 44100f) * (1f - i / (float)samples.Length) * 0.15f; }
            _tone = AudioClip.Create("Generated UI tone", samples.Length, 1, 44100, false);
            _tone.SetData(samples, 0);
            Apply();
        }
        public void Activate() { if (!_music.isPlaying && _music.clip != null) { _music.Play(); } }
        public void Apply() { _music.volume = _wallet.Data.Music; _sound.volume = _wallet.Data.Sound; }
        public void Play(bool reward = false, float pitch = 1f)
        {
            _sound.pitch = pitch;
            AudioClip clip = reward ? _config.Reward : _config.Pop;
            _sound.PlayOneShot(clip != null ? clip : _tone);
        }
        private void OnDestroy() { if (_tone != null) { Destroy(_tone); } }
    }
}
