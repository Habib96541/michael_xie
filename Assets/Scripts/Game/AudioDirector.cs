using System;
using UnityEngine;

namespace NeonDash
{
    /// <summary>
    /// Plays bundled arcade effects and the run theme through the scene listener.
    /// </summary>
    public sealed class AudioDirector : MonoBehaviour
    {
        AudioSource musicSource;
        AudioSource sfxSource;
        AudioSource rewardSource;

        static readonly int[] RewardNotes = { 0, 2, 4, 7, 9, 12, 14, 16 };
        public static int RewardTickCount => RewardNotes.Length;

        AudioClip jumpClip;
        AudioClip orbClip;
        AudioClip padClip;
        AudioClip portalClip;
        AudioClip deathClip;
        AudioClip clickClip;
        AudioClip coinClip;
        AudioClip highCoinClip;
        AudioClip bestClip;
        AudioClip clearClip;
        AudioClip rewardTick;
        AudioClip rewardFinish;

        public bool Muted { get; private set; }
        public event Action<bool> MuteChanged;

        void Awake()
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = 0.57f;
            musicSource.clip = LoadEffect("RunTheme");

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.volume = 0.78f;

            rewardSource = gameObject.AddComponent<AudioSource>();
            rewardSource.playOnAwake = false;
            rewardSource.volume = 0.63f;

            jumpClip = LoadEffect("Jump");
            orbClip = LoadEffect("Orb");
            padClip = LoadEffect("Pad");
            portalClip = LoadEffect("Portal");
            deathClip = LoadEffect("Crash");
            clickClip = LoadEffect("UiConfirm");
            coinClip = LoadEffect("Coin");
            highCoinClip = LoadEffect("HighCoin");
            bestClip = LoadEffect("Best");
            clearClip = LoadEffect("Clear");
            rewardTick = LoadEffect("RewardTick");
            rewardFinish = LoadEffect("RewardFinish");
        }

        static AudioClip LoadEffect(string name)
        {
            AudioClip clip = Resources.Load<AudioClip>("Audio/" + name);
            if (clip == null) Debug.LogError("Missing NeonDash audio clip: " + name);
            return clip;
        }

        public void RestartMusic()
        {
            musicSource.Stop();
            musicSource.Play();
        }

        public void PauseMusic() { musicSource.Pause(); }
        public void ResumeMusic() { musicSource.UnPause(); }
        public void StopMusic() { musicSource.Stop(); }

        public void SetMuted(bool muted)
        {
            Muted = muted;
            musicSource.mute = muted;
            sfxSource.mute = muted;
            rewardSource.mute = muted;
            MuteChanged?.Invoke(muted);
        }

        public void PlayJump() { Play(jumpClip, 0.37f); }
        public void PlayOrb() { Play(orbClip, 0.52f); }
        public void PlayPad() { Play(padClip, 0.48f); }
        public void PlayPortal() { Play(portalClip, 0.48f); }
        public void PlayDeath() { Play(deathClip, 0.92f); }
        public void PlayClick() { Play(clickClip, 0.86f); }
        public void PlayCoin(bool high) { Play(high ? highCoinClip : coinClip, 0.78f); }
        public void PlayClear() { Play(clearClip, 0.64f); }

        public void PlayRewardTick(int step)
        {
            if (Muted || rewardTick == null) return;
            rewardSource.pitch = Mathf.Pow(2f, RewardNotes[step] / 12f);
            rewardSource.PlayOneShot(rewardTick, 0.69f);
        }

        public void PlayRewardFinish(bool personalBest)
        {
            if (Muted) return;
            AudioClip clip = personalBest ? bestClip : rewardFinish;
            if (clip == null) return;
            rewardSource.pitch = 1f;
            rewardSource.PlayOneShot(clip, 0.85f);
        }

        public void StopReward() { rewardSource.Stop(); }

        void Play(AudioClip clip, float volume)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip, volume);
        }
    }
}
