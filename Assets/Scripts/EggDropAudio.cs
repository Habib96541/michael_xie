using UnityEngine;

namespace EggDrop
{
    public sealed class EggDropAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource effectsSource;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource fallingSource;
        [SerializeField] private AudioClip dropClip;
        [SerializeField] private AudioClip landingClip;
        [SerializeField] private AudioClip successClip;
        [SerializeField] private AudioClip collectClip;
        [SerializeField] private AudioClip crackClip;
        [SerializeField] private AudioClip clickClip;
        [SerializeField, Range(0f, 1f)] private float effectsVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.35f;

        public bool IsMuted { get; private set; }
        public float EffectsVolume => effectsVolume;
        public float MusicVolume => musicVolume;

        private void Awake()
        {
            effectsVolume = PlayerPrefs.GetFloat("EggDrop.Effects", effectsVolume);
            musicVolume = PlayerPrefs.GetFloat("EggDrop.Music", musicVolume);
            IsMuted = PlayerPrefs.GetInt("EggDrop.Muted", 0) == 1;
            ApplyVolumes();
        }

        private void Start() => musicSource.Play();

        public void Drop(float risk)
        {
            Play(dropClip);
            fallingSource.pitch = 0.9f + risk * 0.4f;
            fallingSource.Play();
        }

        public void Land(bool survived)
        {
            fallingSource.Stop();
            Play(survived ? landingClip : crackClip);
        }

        public void Success() => Play(successClip, 0.75f);
        public void Collect() => Play(collectClip);
        public void Click() => Play(clickClip, 0.45f);

        public void ToggleMute()
        {
            IsMuted = !IsMuted;
            PlayerPrefs.SetInt("EggDrop.Muted", IsMuted ? 1 : 0);
            ApplyVolumes();
        }

        public void SetEffectsVolume(float value)
        {
            effectsVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat("EggDrop.Effects", effectsVolume);
            ApplyVolumes();
        }

        public void SetMusicVolume(float value)
        {
            musicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat("EggDrop.Music", musicVolume);
            ApplyVolumes();
        }

        private void ApplyVolumes()
        {
            effectsSource.volume = IsMuted ? 0f : effectsVolume * 0.7f;
            musicSource.volume = IsMuted ? 0f : musicVolume * 0.45f;
            fallingSource.volume = IsMuted ? 0f : effectsVolume * 0.16f;
        }

        private void Play(AudioClip clip, float volume = 1f)
        {
            if (clip != null) effectsSource.PlayOneShot(clip, volume);
        }
    }
}
