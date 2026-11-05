using UnityEngine;

namespace ChickenRush
{
    public sealed class RushEffects
    {
        readonly SpriteRenderer[] sprites=new SpriteRenderer[32];
        readonly Vector2[] velocity=new Vector2[32];
        readonly float[] life=new float[32];
        readonly AudioSource audio;
        readonly AudioSource tension;
        readonly AudioClip[] clips=new AudioClip[7];
        public RushEffects(Transform parent,Sprite disc,Material material)
        {
            for(int i=0;i<sprites.Length;i++)
            {
                var go=new GameObject("Pooled sparkle "+i);go.transform.SetParent(parent,false);
                sprites[i]=go.AddComponent<SpriteRenderer>();sprites[i].sprite=disc;sprites[i].sortingOrder=80;sprites[i].sharedMaterial=material;go.SetActive(false);
            }
            audio=parent.gameObject.AddComponent<AudioSource>();audio.playOnAwake=false;audio.volume=.18f;
            tension=parent.gameObject.AddComponent<AudioSource>();tension.playOnAwake=false;tension.loop=true;tension.volume=0;
            var pulse=new float[24000];
            for(int i=0;i<pulse.Length;i++){float t=i/24000f,beat=Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI*4)),12);pulse[i]=Mathf.Sin(t*Mathf.PI*2*83)*beat*.32f;}
            var pulseClip=AudioClip.Create("Original subtle danger pulse",pulse.Length,1,24000,false);pulseClip.SetData(pulse,0);tension.clip=pulseClip;
            float[] frequencies={510,280,120,830,620,95,680};
            var random=new System.Random(71);
            for(int k=0;k<clips.Length;k++)
            {
                int length=k==3?16000:6000;var samples=new float[length];
                for(int j=0;j<length;j++)
                {
                    float t=j/24000f,envelope=Mathf.Sin(Mathf.PI*j/length)*Mathf.Exp(-t*8);
                    float frequency=frequencies[k]*(k==2?Mathf.Max(.25f,1-t*3):1+t*.5f);
                    samples[j]=k==5?((float)random.NextDouble()*2-1)*envelope*.30f:(.7f*Mathf.Sin(2*Mathf.PI*frequency*t)+.2f*Mathf.Sin(2*Mathf.PI*frequency*1.5f*t))*envelope;
                }
                clips[k]=AudioClip.Create("Original rush tone "+k,length,1,24000,false);clips[k].SetData(samples,0);
            }
        }
        public void Play(int index,bool enabled) {if(enabled)audio.PlayOneShot(clips[index]);}
        public void Pass(float speed,bool enabled){if(enabled)audio.PlayOneShot(clips[5],Mathf.Clamp(speed/8,.25f,.75f));}
        public void Tension(int lane,bool active,bool enabled)
        {
            bool play=active && enabled && lane>=3;
            if(play){tension.pitch=1+Mathf.Min(12,lane)*.045f;tension.volume=Mathf.Min(.065f,.012f+lane*.003f);if(!tension.isPlaying)tension.Play();}
            else if(tension.isPlaying)tension.Stop();
        }
        public void Clear(){for(int i=0;i<life.Length;i++){life[i]=0;sprites[i].gameObject.SetActive(false);}}
        public void NearMiss(Vector2 at)
        {
            for(int i=24;i<30;i++)
            {
                life[i]=.45f;velocity[i]=new Vector2(Mathf.Cos(i)*.9f,Mathf.Sin(i)*.6f+.8f);sprites[i].gameObject.SetActive(true);
                sprites[i].transform.position=(Vector3)at+new Vector3(.10f,.4f,0);sprites[i].transform.localScale=Vector3.one*.065f;sprites[i].color=RoadWorld.Hex("8DE9D0");
            }
        }
        public void Burst(Vector2 at,bool victory)
        {
            int count=victory?28:7;
            for(int i=0;i<count;i++)
            {
                int slot=i;float angle=i*2.39996f;
                velocity[slot]=new Vector2(Mathf.Cos(angle)*(victory?2.4f:.6f),Mathf.Abs(Mathf.Sin(angle))*(victory?3.2f:.4f)+.1f);
                life[slot]=victory?1.5f:.38f;sprites[slot].gameObject.SetActive(true);
                sprites[slot].transform.position=new Vector3(at.x,at.y+.15f,0);
                sprites[slot].transform.localScale=Vector3.one*(victory?.11f:.12f);
                sprites[slot].color=RoadWorld.Hex(victory?(i%3==0?"F9D25D":i%3==1?"FF8F82":"BCA6EF"):"E9D7BB");
            }
        }
        public void Tick(float dt)
        {
            for(int i=0;i<sprites.Length;i++)if(life[i]>0)
            {
                life[i]-=dt;
                if(life[i]<=0){sprites[i].gameObject.SetActive(false);continue;}
                velocity[i]+=Vector2.down*dt*3;
                sprites[i].transform.position+=(Vector3)(velocity[i]*dt);
                var c=sprites[i].color;c.a=Mathf.Min(1,life[i]*3);sprites[i].color=c;
            }
        }
    }
}
