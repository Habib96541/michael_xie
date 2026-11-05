using UnityEngine;

namespace HydraulicPress
{
    public sealed class AudioManager : MonoBehaviour
    {
        public AudioClip motor, impact, metal, glass, plastic, soft, liquid, elastic, success, collect, failure, button, multiplier, fragment, music;
        private AudioSource[] voices;
        private AudioSource motorVoice, musicVoice;
        private int voice;
        private SaveData settings;
        private bool unlocked;
        public void Initialize(SaveData data)
        {
            settings = data; voices = new AudioSource[8];
            for (int i = 0; i < voices.Length; i++) { voices[i] = gameObject.AddComponent<AudioSource>(); voices[i].playOnAwake = false; }
            motorVoice = gameObject.AddComponent<AudioSource>(); motorVoice.playOnAwake = false; motorVoice.loop = true; motorVoice.clip = motor;
            musicVoice = gameObject.AddComponent<AudioSource>(); musicVoice.playOnAwake = false; musicVoice.loop = true; musicVoice.clip = music;
            ApplyVolumes();
        }
        public void Unlock() { if (unlocked) return; unlocked = true; if (music != null) musicVoice.Play(); }
        public void ApplyVolumes() { motorVoice.volume = settings.sound * .3f; musicVoice.volume = settings.music * .35f; }
        public void Play(AudioClip clip, float gain = 1, float pitch = 1)
        {
            if (clip == null || !unlocked) return;
            var source = voices[voice++ % voices.Length]; source.clip = clip; source.volume = settings.sound * gain; source.pitch = pitch; source.Play();
        }
        public void Motor(bool active, float pitch = 1) { motorVoice.pitch = pitch; if (active && unlocked && motor != null) motorVoice.Play(); else motorVoice.Stop(); }
        public void MaterialImpact(CrushMaterial material)
        {
            Play(impact, .6f, Random.Range(.92f, 1.06f));
            AudioClip clip = material == CrushMaterial.Brittle ? glass : material == CrushMaterial.Metal ? metal : material == CrushMaterial.Liquid ? liquid : material == CrushMaterial.Elastic ? elastic : material == CrushMaterial.Plastic ? plastic : soft;
            Play(clip, .75f, Random.Range(.9f, 1.1f));
            Play(fragment, .18f, Random.Range(.85f, 1.2f));
        }
    }
}
