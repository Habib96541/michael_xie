using UnityEngine;
using System.Collections.Generic;

namespace BrickRush
{
    public sealed class AudioManager
    {
        readonly AudioSource music;readonly AudioSource[] voices=new AudioSource[14];readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();int cursor;bool unlocked,gameplay;
        public float MusicVolume {get;private set;}
        public float SfxVolume {get;private set;}
        float soundGate;
        public AudioManager(GameObject owner)
        {
            MusicVolume=SaveManager.Volume("music",.45f);SfxVolume=SaveManager.Volume("sfx",.7f);
            foreach(var clip in Resources.LoadAll<AudioClip>("Audio"))clips[clip.name]=clip;
            music=owner.AddComponent<AudioSource>();music.loop=true;music.playOnAwake=false;music.volume=MusicVolume*.65f;
            for(int i=0;i<voices.Length;i++){voices[i]=owner.AddComponent<AudioSource>();voices[i].playOnAwake=false;}
        }
        public void Unlock(){if(unlocked)return;unlocked=true;SetGameplay(false);}
        public void SetGameplay(bool value){gameplay=value;if(!unlocked)return;music.clip=clips[value?"music":"menu"];music.volume=MusicVolume*.65f;music.Play();}
        public void Volumes(float musicValue,float sfxValue){MusicVolume=musicValue;SfxVolume=sfxValue;music.volume=MusicVolume*.65f;SaveManager.Set("music",musicValue);SaveManager.Set("sfx",sfxValue);}
        public void Play(string name,float volume=1,bool vary=false)
        {
            if(!unlocked||!clips.TryGetValue(name,out var clip))return;
            if((name=="break"||name=="hit")&&Time.unscaledTime<soundGate)return;
            if(name=="break"||name=="hit")soundGate=Time.unscaledTime+.025f;
            var voice=voices[cursor++%voices.Length];voice.clip=clip;voice.volume=SfxVolume*volume;voice.pitch=vary?Random.Range(.91f,1.12f):1;voice.Play();
        }
        public void Pause(bool value){if(value)music.Pause();else if(unlocked)music.UnPause();}
    }
}
