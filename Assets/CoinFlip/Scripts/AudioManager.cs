using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

namespace CoinFlip
{
    public sealed class AudioManager : MonoBehaviour
    {
        public enum Cue { Hover, Press, Select, Launch, Tick, Slow, Land, Win, Lose, Balance, Transition }
        readonly AudioClip[] clips = new AudioClip[11];
        readonly AudioSource[] voices = new AudioSource[6];
        readonly float[] lastPlayed = new float[11];
        AudioSource music;
        SaveData settings;
        int nextVoice;
        bool unlocked, focused = true;
        float volume;
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void CoinUnlockAudio();
#endif
        public void Initialize(SaveData data)
        {
            settings = data;
            for (int i = 0; i < clips.Length; i++) { clips[i] = Resources.Load<AudioClip>("Audio/" + (Cue)i); clips[i].LoadAudioData(); lastPlayed[i] = -100; }
            for (int i = 0; i < voices.Length; i++) { voices[i] = gameObject.AddComponent<AudioSource>(); voices[i].playOnAwake = false; }
            music = gameObject.AddComponent<AudioSource>(); music.playOnAwake = false; music.loop = true;
            music.clip = Resources.Load<AudioClip>("Audio/Music"); music.clip.LoadAudioData(); music.volume = 0;
            StartCoroutine(Mix());
        }
        public void Unlock()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            CoinUnlockAudio();
#endif
            if (unlocked) return;
            unlocked = true;
        }
        public void Play(Cue cue, float pitch = 1)
        {
            int index = (int)cue;
            if (!unlocked || !focused || settings.muted || Time.unscaledTime - lastPlayed[index] < .045f) return;
            if (clips[index].loadState != AudioDataLoadState.Loaded) return;
            lastPlayed[index] = Time.unscaledTime;
            AudioSource voice = voices[nextVoice++ % voices.Length];
            voice.pitch = pitch + Random.Range(-.025f, .025f); voice.volume = volume;
            voice.clip = clips[index]; voice.Play();
        }
        IEnumerator Mix()
        {
            while (true)
            {
                float target = focused && !settings.muted ? 1 : 0;
                if (unlocked && music.clip.loadState == AudioDataLoadState.Loaded && !music.isPlaying) music.Play();
                volume = Mathf.MoveTowards(volume, target * settings.sfx, Time.unscaledDeltaTime * 5);
                music.volume = Mathf.MoveTowards(music.volume, unlocked ? target * settings.music * .5f : 0, Time.unscaledDeltaTime);
                if (target == 0) foreach (AudioSource voice in voices) voice.volume = volume;
                yield return null;
            }
        }
        void OnApplicationFocus(bool value) { focused = value; }
        void OnApplicationPause(bool value) { focused = !value; }
    }
}
