using System;
using System.Collections.Generic;
using UnityEngine;

namespace BloomDoom
{
    // Original generated score and effects. All clips are built in memory; no recording dependencies.
    public sealed class GardenAudio : MonoBehaviour
    {
        const int Rate=22050;
        AudioSource music,effects;readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
        readonly Dictionary<string,float> last=new Dictionary<string,float>();
        public bool Muted {get;private set;} public float MusicVolume {get;private set;} public float EffectsVolume {get;private set;}
        void Awake()
        {
            Muted=PlayerPrefs.GetInt("BloomDoom.Muted",0)==1;MusicVolume=PlayerPrefs.GetFloat("BloomDoom.Music",.45f);EffectsVolume=PlayerPrefs.GetFloat("BloomDoom.Effects",.7f);
            music=gameObject.AddComponent<AudioSource>();effects=gameObject.AddComponent<AudioSource>();music.loop=true;music.playOnAwake=false;effects.playOnAwake=false;music.clip=MakeScore();Apply();
        }
        public void Begin(){if(!music.isPlaying)music.Play();}
        public void SetMuted(bool value){Muted=value;PlayerPrefs.SetInt("BloomDoom.Muted",value?1:0);Apply();}
        public void SetMusic(float value){MusicVolume=Mathf.Clamp01(value);PlayerPrefs.SetFloat("BloomDoom.Music",MusicVolume);Apply();}
        public void SetEffects(float value){EffectsVolume=Mathf.Clamp01(value);PlayerPrefs.SetFloat("BloomDoom.Effects",EffectsVolume);Apply();}
        public void Pause(bool value){if(music!=null)music.volume=Muted?0:MusicVolume*(value?.12f:.35f);}
        void Apply(){music.mute=effects.mute=Muted;music.volume=MusicVolume*.35f;effects.volume=EffectsVolume*.65f;}
        static float Note(int n)=>440*Mathf.Pow(2,(n-69)/12f);
        static void Tone(float[] data,float start,float length,float freq,float volume,float end=0,bool triangle=false)
        {
            int from=Mathf.RoundToInt(start*Rate),count=Mathf.RoundToInt(length*Rate);double phase=0;
            for(int i=0;i<count&&from+i<data.Length;i++){float t=i/(float)count,f=end>0?Mathf.Lerp(freq,end,t):freq;phase+=f/Rate;float wave=triangle?2*Mathf.Abs(2*((float)phase-Mathf.Floor((float)phase+.5f)))-1:Mathf.Sin((float)phase*Mathf.PI*2);float attack=Mathf.Min(1,i/(Rate*.008f));data[from+i]+=wave*volume*attack*Mathf.Pow(1-t,2);}
        }
        static void Noise(float[] data,float length,float volume,int seed=914){var random=new System.Random(seed);int count=Mathf.Min(data.Length,(int)(length*Rate));float previous=0;for(int i=0;i<count;i++){previous=previous*.55f+(float)(random.NextDouble()*2-1)*.45f;data[i]+=previous*volume*Mathf.Pow(1-i/(float)count,2);}}
        static AudioClip Clip(string name,float[] data){var clip=AudioClip.Create(name,data.Length,1,Rate,false);clip.SetData(data,0);return clip;}
        static AudioClip MakeScore()
        {
            const float step=.235f;var data=new float[Mathf.CeilToInt(128*step*Rate)];int[][] chords={new[]{48,55,60,64},new[]{45,52,57,60},new[]{53,57,60,65},new[]{43,50,55,59}};
            int[] melody={76,0,79,76,74,72,0,74,76,79,81,0,79,76,74,0,72,76,77,0,76,72,69,72,71,0,74,76,74,71,67,0};
            for(int s=0;s<128;s++){float t=s*step;int[] chord=chords[(s/16)%4];if(s%2==0){Tone(data,t,.35f,Note(chord[(s/2)%4]+12),.13f);int n=melody[(s/2)%32];if(n>0){Tone(data,t,.24f,Note(n),.15f);Tone(data,t,.08f,Note(n+12),.025f);}}if(s%4==0){Tone(data,t,.4f,Note(chord[0]-12),.22f,0,true);Tone(data,t,.045f,170,.06f,65,true);}if(s%4==2)Tone(data,t,.09f,Note(chord[2]),.09f,0,true);}
            return Clip("Homegrown - original garden soundtrack",data);
        }
        public void Play(string name)
        {
            if(Muted)return;float min=name=="bite"?.18f:name=="hit"||name=="shoot"?.085f:name=="groan"?3:0;
            if(last.TryGetValue(name,out float when)&&Time.unscaledTime-when<min)return;last[name]=Time.unscaledTime;
            if(!clips.TryGetValue(name,out var clip)){var data=new float[Rate*2];
                switch(name){
                    case "plant":case "shovel":Noise(data,.18f,.55f);Tone(data,0,.16f,160,.22f,70,true);break;
                    case "collect":Tone(data,0,.18f,880,.3f);Tone(data,.07f,.25f,1320,.22f);Tone(data,.14f,.22f,1760,.12f);break;
                    case "shoot":Tone(data,0,.095f,430,.2f,160);Noise(data,.045f,.1f);break;
                    case "hit":Tone(data,0,.07f,190,.22f,90,true);Noise(data,.07f,.15f);break;
                    case "bite":Noise(data,.085f,.35f);Tone(data,0,.07f,75,.12f,0,true);break;
                    case "kill":case "plantlost":Tone(data,0,.18f,230,.22f,50,true);Noise(data,.12f,.25f);break;
                    case "explode":Noise(data,.65f,.8f);Tone(data,0,.55f,135,.65f,25,true);break;
                    case "mower":Noise(data,1.3f,.6f);for(int i=0;i<6;i++)Tone(data,i*.16f,.23f,55+i*4,.3f,75+i*4,true);break;
                    case "groan":case "rage":Tone(data,0,.65f,95,.15f,55,true);Tone(data,0,.65f,101,.12f,58);break;
                    case "wave":case "start":case "waveclear":{int[] n={60,64,67,72};for(int i=0;i<n.Length;i++)Tone(data,i*.12f,.3f,Note(n[i]),.24f,0,true);break;}
                    case "win":{int[] n={60,64,67,72,76,79,84};for(int i=0;i<n.Length;i++)Tone(data,i*.13f,.6f,Note(n[i]),.28f,0,true);break;}
                    case "lose":{int[] n={64,62,59,55,48};for(int i=0;i<n.Length;i++)Tone(data,i*.2f,.5f,Note(n[i]),.3f,0,true);break;}
                    case "sunmade":case "armed":Tone(data,0,.15f,660,.1f);Tone(data,.1f,.18f,990,.1f);break;
                    case "error":Tone(data,0,.12f,150,.16f,110,true);break;
                    default:Tone(data,0,.09f,600,.18f,850);break;
                }clip=Clip(name,data);clips[name]=clip;
            }effects.PlayOneShot(clip);
        }
        void OnDestroy(){if(music!=null&&music.clip!=null)Destroy(music.clip);foreach(var clip in clips.Values)if(clip!=null)Destroy(clip);}
    }
}
