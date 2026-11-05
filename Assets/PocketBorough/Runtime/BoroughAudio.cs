using UnityEngine;
namespace PocketBorough
{
    public class BoroughAudio:MonoBehaviour
    {
        BoroughCatalog catalog;AudioSource music,sfx;float throttle;
        public float MusicVolume {get;private set;}
        public float SfxVolume {get;private set;}
        public void Initialize(BoroughCatalog data)
        {catalog=data;music=gameObject.AddComponent<AudioSource>();sfx=gameObject.AddComponent<AudioSource>();music.loop=true;music.clip=data.music;music.playOnAwake=sfx.playOnAwake=false;SetMusic(PlayerPrefs.GetFloat("Borough.Music",.32f));SetSfx(PlayerPrefs.GetFloat("Borough.Sfx",.65f));}
        public void Begin(){if(!music.isPlaying)music.Play();}
        public void Play(int sound){if(sound<0||sound>=catalog.sounds.Length)return;if(sound<2&&Time.unscaledTime<throttle)return;if(sound<2)throttle=Time.unscaledTime+.09f;sfx.PlayOneShot(catalog.sounds[sound],SfxVolume*(sound==0?.55f:1));}
        public void SetMusic(float value){MusicVolume=value;music.volume=value;PlayerPrefs.SetFloat("Borough.Music",value);}
        public void SetSfx(float value){SfxVolume=value;PlayerPrefs.SetFloat("Borough.Sfx",value);}
        public void Tick(bool paused){music.volume=Mathf.Lerp(music.volume,MusicVolume*(paused?.3f:1),.1f);}
    }
}
