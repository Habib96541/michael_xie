using UnityEngine;

/// <summary>Sound cues backed by the game's bundled audio clips.</summary>
[DisallowMultipleComponent]
public sealed class ChestShuffleAudio : MonoBehaviour
{
    AudioSource source;
    AudioClip buttonClickClip;
    AudioClip coinSpendClip;
    AudioClip boxDropClip;
    AudioClip cashRegisterClip;
    AudioClip levelUpClip;
    AudioClip slotReelClip;
    AudioSource reelSource;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        if (source == null) source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = 0.65f;

        buttonClickClip = Resources.Load<AudioClip>("Audio/button-click");
        coinSpendClip = Resources.Load<AudioClip>("Audio/coin-spend");
        boxDropClip = Resources.Load<AudioClip>("Audio/box-drop");
        cashRegisterClip = Resources.Load<AudioClip>("Audio/cash-register");
        levelUpClip = Resources.Load<AudioClip>("Audio/level-up");
        slotReelClip = Resources.Load<AudioClip>("Audio/slot-reels");

        reelSource = gameObject.AddComponent<AudioSource>();
        reelSource.playOnAwake = false;
        reelSource.loop = false;
        reelSource.spatialBlend = 0f;
        reelSource.volume = 0.55f;
    }

    public void PlayButtonClick() { Play(buttonClickClip); }
    public void PlayCoinSpend() { Play(coinSpendClip); }
    public void PlayShuffleDrop() { Play(boxDropClip); }
    public void PlayCashResult() { Play(cashRegisterClip); }
    public void PlayLevelUp() { Play(levelUpClip); }

    public void PlaySlotReel()
    {
        if (reelSource == null || slotReelClip == null) return;
        reelSource.clip = slotReelClip;
        reelSource.Play();
    }

    public void StopSlotReel()
    {
        if (reelSource != null && reelSource.isPlaying) reelSource.Stop();
    }

    void Play(AudioClip clip)
    {
        if (source != null && clip != null) source.PlayOneShot(clip);
    }

}
