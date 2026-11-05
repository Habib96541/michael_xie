using System;
using UnityEngine;

namespace CyberWhack.Audio
{
    public class CyberAudioManager : MonoBehaviour
    {
        public static CyberAudioManager Instance { get; private set; }

        [Header("Audio Sources Pool")]
        [SerializeField] private int voicePoolSize = 10;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.16f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.82f;

        private AudioSource[] _sources;
        private AudioSource _musicSource;
        private int _poolIndex;

        // Cached clips
        private AudioClip _clipStandardHit;
        private AudioClip _clipGoldHit;
        private AudioClip _clipArmorCrack;
        private AudioClip _clipEmpHazard;
        private AudioClip _clipMutedMiss;
        private AudioClip _clipComboTierUp;
        private AudioClip _clipUiClick;
        private AudioClip _clipBackgroundMusic;
        private AudioClip _clipCountdownTick;
        private AudioClip _clipRoundStart;
        private AudioClip _clipRoundEnd;

        private bool _isAudioUnlocked;
        private bool _isMuted;

        public bool IsMuted => _isMuted;
        public event Action<bool> OnMuteStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            _isMuted = PlayerPrefs.GetInt("CyberWhack_AudioMuted", 0) == 1;
            AudioListener.pause = _isMuted;

            InitializeVoices();
            GenerateAndCacheClips();
        }

        private void Start()
        {
            StartBackgroundMusic();
        }

        private void InitializeVoices()
        {
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _musicSource.volume = musicVolume;
            _musicSource.spatialBlend = 0f;
            _musicSource.bypassEffects = true;
            _musicSource.bypassListenerEffects = true;

            _sources = new AudioSource[voicePoolSize];
            for (int i = 0; i < voicePoolSize; i++)
            {
                AudioSource src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f; // 2D flat mono/stereo
                src.bypassEffects = true;
                src.bypassListenerEffects = true;
                _sources[i] = src;
            }
        }

        private void GenerateAndCacheClips()
        {
            _clipStandardHit = ProceduralAudioGenerator.CreateMetallicClunk();
            _clipGoldHit = ProceduralAudioGenerator.CreateCrystalChime();
            _clipArmorCrack = ProceduralAudioGenerator.CreateArmorCrunch();
            _clipEmpHazard = ProceduralAudioGenerator.CreateEmpHazardBuzz();
            _clipMutedMiss = ProceduralAudioGenerator.CreateMutedHollowTap();
            _clipComboTierUp = ProceduralAudioGenerator.CreateComboTierUp();
            _clipUiClick = ProceduralAudioGenerator.CreateUiClick();
            _clipBackgroundMusic = ProceduralAudioGenerator.CreateBackgroundMusic();
            _clipCountdownTick = ProceduralAudioGenerator.CreateCountdownTick();
            _clipRoundStart = ProceduralAudioGenerator.CreateRoundStartCue();
            _clipRoundEnd = ProceduralAudioGenerator.CreateRoundEndCue();
        }

        private void StartBackgroundMusic()
        {
            if (_isMuted || _musicSource == null || _clipBackgroundMusic == null || _musicSource.isPlaying)
            {
                return;
            }

            _musicSource.clip = _clipBackgroundMusic;
            _musicSource.volume = musicVolume;
            _musicSource.Play();
        }

        /// <summary>
        /// Explicit trigger for mobile iOS Safari & Android Chrome WebAudio policies.
        /// Unlocks AudioContext on initial touch contact.
        /// </summary>
        public void UnlockMobileAudioContext()
        {
            if (_isAudioUnlocked) return;
            _isAudioUnlocked = true;

            // Trigger silent or micro-sample playback to unlock WebAudio pipeline
            if (_sources != null && _sources.Length > 0 && _clipMutedMiss != null)
            {
                _sources[0].PlayOneShot(_clipMutedMiss, 0.001f);
            }

            StartBackgroundMusic();
        }

        private void PlayClip(AudioClip clip, float volume = 1.0f, float pitch = 1.0f)
        {
            if (clip == null || _sources == null || _sources.Length == 0) return;

            if (!_isAudioUnlocked)
            {
                UnlockMobileAudioContext();
            }

            AudioSource source = _sources[_poolIndex];
            _poolIndex = (_poolIndex + 1) % _sources.Length;

            source.pitch = pitch;
            source.PlayOneShot(clip, volume * sfxVolume);
        }

        public void PlayStandardHit()
        {
            // Slight pitch variance (+/- 4%) without GC allocation
            float pitch = UnityEngine.Random.Range(0.96f, 1.04f);
            PlayClip(_clipStandardHit, 0.95f, pitch);
        }

        public void PlayGoldHit()
        {
            PlayClip(_clipGoldHit, 1.0f, 1.0f);
        }

        public void PlayArmorCrack()
        {
            float pitch = UnityEngine.Random.Range(0.95f, 1.05f);
            PlayClip(_clipArmorCrack, 1.0f, pitch);
        }

        public void PlayEmpHazard()
        {
            PlayClip(_clipEmpHazard, 1.0f, 0.9f);
        }

        public void PlayMiss()
        {
            float pitch = UnityEngine.Random.Range(0.9f, 1.1f);
            PlayClip(_clipMutedMiss, 0.7f, pitch);
        }

        public void PlayComboTierUp()
        {
            PlayClip(_clipComboTierUp, 0.9f, 1.0f);
        }

        public void PlayUiClick()
        {
            PlayClip(_clipUiClick, 0.8f, 1.0f);
        }

        public void PlayCountdownTick(int secondsRemaining)
        {
            int boundedSeconds = Mathf.Clamp(secondsRemaining, 1, 3);
            float pitch = 1.06f + (3 - boundedSeconds) * 0.12f;
            PlayClip(_clipCountdownTick, 0.68f, pitch);
        }

        public void PlayRoundStart()
        {
            PlayClip(_clipRoundStart, 0.82f, 1f);
        }

        public void PlayRoundEnd()
        {
            PlayClip(_clipRoundEnd, 0.72f, 1f);
        }

        public void ToggleMute()
        {
            _isMuted = !_isMuted;
            AudioListener.pause = _isMuted;
            PlayerPrefs.SetInt("CyberWhack_AudioMuted", _isMuted ? 1 : 0);
            PlayerPrefs.Save();
            OnMuteStateChanged?.Invoke(_isMuted);

            if (!_isMuted)
            {
                StartBackgroundMusic();
                PlayUiClick();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
