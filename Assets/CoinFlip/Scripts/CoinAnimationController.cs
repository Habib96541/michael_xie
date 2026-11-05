using System;
using System.Collections;
using UnityEngine;

namespace CoinFlip
{
    public sealed class CoinAnimationController : MonoBehaviour
    {
        UIManager ui; AudioManager sound; VFXManager vfx;GameSettings settings;
        Sprite heads,tails;
        public bool Suspended {get;set;}
        public string Phase {get;private set;}="Idle";
        public float Progress {get;private set;}
        public void Initialize(UIManager view, AudioManager sound, VFXManager effects,GameSettings config)
        {ui=view;this.sound=sound;vfx=effects;settings=config;heads=Resources.Load<Sprite>("Art/Heads");tails=Resources.Load<Sprite>("Art/Tails");}
        public IEnumerator Flip(Round round,Action landed)
        {
            RectTransform coin=ui.Coin;bool startsTails=ui.CoinImage.sprite==tails;
            float pre=.12f,flight=settings.flipDuration-.38f,bounce=.26f;
            Phase="Anticipation";
            for(float t=0;t<1;)
            {
                if(!Suspended)t+=Mathf.Min(Time.unscaledDeltaTime,.04f)/pre;
                Progress=t;
                float q=Mathf.Sin(Mathf.Clamp01(t)*Mathf.PI*.5f);coin.localScale=new Vector3(1+.055f*q,1-.1f*q,1);coin.anchoredPosition=new Vector2(0,-7*q);yield return null;
            }
            sound.Play(AudioManager.Cue.Launch);vfx.Launch();
            float start=startsTails?Mathf.PI:0;
            float end=(12+(int)round.Outcome)*Mathf.PI;
            int lastFace=startsTails?1:0;float nextSpark=.08f;
            Phase="Flight";
            for(float t=0;t<1;)
            {
                if(Suspended){yield return null;continue;}
                t=Mathf.Min(1,t+Mathf.Min(Time.unscaledDeltaTime,.04f)/flight);
                Progress=t;
                float eased=1-Mathf.Pow(1-t,2.05f),angle=Mathf.Lerp(start,end,eased);
                float cosine=Mathf.Cos(angle),height=Mathf.Sin(t*Mathf.PI)*Mathf.Min(settings.launchHeight,ui.CoinSize*.4f);
                int face=cosine>=0?0:1;
                // Face changes occur edge-on, where horizontal scale approaches zero.
                ui.CoinImage.sprite=face==0?heads:tails;
                coin.localScale=new Vector3(Mathf.Max(.045f,Mathf.Abs(cosine))*(1+.025f*Mathf.Sin(t*Mathf.PI)),1+.035f*Mathf.Sin(t*Mathf.PI),1);
                coin.anchoredPosition=new Vector2(0,height);coin.localEulerAngles=new Vector3(0,0,Mathf.Sin(t*Mathf.PI*2)*-7*(1-t));
                ui.Shadow.localScale=new Vector3(1-height/230,1-height/250,1);Color c=Color.white;c.a=1-height/160;ui.ShadowImage.color=c;
                if(face!=lastFace){sound.Play(t>.78f?AudioManager.Cue.Slow:AudioManager.Cue.Tick,Mathf.Lerp(1.25f,.8f,t));lastFace=face;}
                if(t>=nextSpark&&t<.8f){vfx.Spark(height);nextSpark+=.1f;}
                yield return null;
            }
            ui.CoinImage.sprite=round.Outcome==CoinSide.Heads?heads:tails;coin.localEulerAngles=Vector3.zero;coin.localScale=Vector3.one;
            sound.Play(AudioManager.Cue.Land);vfx.Land(round.Won);sound.Play(round.Won?AudioManager.Cue.Win:AudioManager.Cue.Lose);
            Phase="Bounce";
            for(float t=0;t<1;)
            {
                if(!Suspended)t=Mathf.Min(1,t+Mathf.Min(Time.unscaledDeltaTime,.04f)/bounce);
                Progress=t;
                float hop=Mathf.Sin(t*Mathf.PI)*(1-t)*11;
                coin.anchoredPosition=new Vector2(0,round.Won?hop:-hop*.35f);
                coin.localScale=new Vector3(1+.04f*Mathf.Sin(t*Mathf.PI*2)*(1-t),1-.045f*Mathf.Sin(t*Mathf.PI*2)*(1-t),1);
                if(round.Won)ui.CoinImage.color=Color.Lerp(Color.white,new Color(1,.94f,.76f),Mathf.Sin(t*Mathf.PI)*.7f);
                yield return null;
            }
            Settle(round.Outcome);landed();
        }
        public void Settle(CoinSide side)
        {
            Phase="Settled";Progress=1;
            ui.CoinImage.sprite=side==CoinSide.Heads?heads:tails;ui.CoinImage.color=Color.white;
            ui.Coin.anchoredPosition=Vector2.zero;ui.Coin.localScale=Vector3.one;ui.Coin.localEulerAngles=Vector3.zero;
            ui.Shadow.localScale=Vector3.one;ui.ShadowImage.color=Color.white;
        }
    }
}
