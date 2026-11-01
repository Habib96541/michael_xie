using System;
using System.Collections.Generic;
using UnityEngine;

namespace HiLo
{
    public enum Sound { Click, Deal, Flip, Correct, Wrong, Grow, Milestone, CashOut }
    public sealed class AudioManager : MonoBehaviour
    {
        readonly Dictionary<Sound,AudioClip> clips=new Dictionary<Sound,AudioClip>();
        AudioSource effects, music;
        bool unlocked;
        public void Initialize()
        {
            effects=gameObject.AddComponent<AudioSource>();effects.playOnAwake=false;effects.volume=.32f;
            music=gameObject.AddComponent<AudioSource>();music.playOnAwake=false;music.loop=true;music.volume=.10f;
            foreach(Sound sound in Enum.GetValues(typeof(Sound))) clips[sound]=Tone(sound);
            const int rate=22050;var samples=new float[rate*12];
            double[] chord={130.8128,164.8138,195.9977,261.6256};
            for(int i=0;i<samples.Length;i++){double t=(double)i/rate;double envelope=Math.Pow(Math.Sin(Math.PI*t/12),2);double v=0;foreach(double f in chord)v+=Math.Sin(2*Math.PI*f*t)*.04; samples[i]=(float)(v*envelope);}
            music.clip=AudioClip.Create("Soft felt ambient",samples.Length,1,rate,false);music.clip.SetData(samples,0);
        }
        AudioClip Tone(Sound sound)
        {
            const int rate=22050;float duration=sound==Sound.Milestone?.7f:sound==Sound.CashOut?.55f:sound==Sound.Wrong?.3f:.16f;
            var samples=new float[(int)(rate*duration)];
            for(int i=0;i<samples.Length;i++)
            {
                double t=(double)i/rate,p=t/duration;double f=sound==Sound.Wrong?170-90*p:sound==Sound.CashOut?523*Math.Pow(2,Math.Floor(p*4)/4):sound==Sound.Milestone?440*Math.Pow(2,Math.Floor(p*5)/3):sound==Sound.Deal?220:sound==Sound.Flip?330:sound==Sound.Correct?660:sound==Sound.Grow?880:460;
                double envelope=Math.Sin(Math.PI*p)*Math.Exp(-p*3);samples[i]=(float)((Math.Sin(t*f*Math.PI*2)+.2*Math.Sin(t*f*Math.PI*4))*envelope*.4);
            }
            var clip=AudioClip.Create(sound.ToString(),samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
        }
        public void Unlock(){unlocked=true;Refresh();}
        public void Refresh(){if(music==null)return;if(unlocked && GameManager.Instance.Save.Data.music){if(!music.isPlaying)music.Play();}else music.Pause();}
        public void Play(Sound sound){if(unlocked && GameManager.Instance.Save.Data.sound)effects.PlayOneShot(clips[sound]);}
    }
}
