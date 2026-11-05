using System.Collections;
using UnityEngine;
using Image = UnityEngine.UI.Image;

namespace CoinFlip
{
    public sealed class VFXManager : MonoBehaviour
    {
        sealed class Particle
        {
            public RectTransform rect; public Image image; public Vector2 origin, velocity;
            public float age, life, spin, rotation, size; public Color color; public bool active, gravity;
        }
        Particle[] particles;
        Sprite sparkle, circle;
        UIManager ui;
        Coroutine simulation;
        Image pulse,screenPulse;
        public void Initialize(UIManager view, int count)
        {
            ui=view;sparkle=Resources.Load<Sprite>("Art/Sparkle");circle=Resources.Load<Sprite>("Art/Circle");
            var factory=new UIFactory(null);particles=new Particle[count];
            for(int i=0;i<count;i++)
            {
                Image im=factory.Image("Pooled sparkle "+i,ui.Stage,"Sparkle",Palette.Gold);im.rectTransform.anchorMin=im.rectTransform.anchorMax=new Vector2(0,1);
                im.gameObject.SetActive(false);particles[i]=new Particle{rect=im.rectTransform,image=im};
            }
            pulse=factory.Image("Landing ripple",ui.Stage,"Ring",Palette.Gold);pulse.gameObject.SetActive(false);
            screenPulse=factory.Image("Soft screen pulse",ui.GetComponentInChildren<Canvas>().transform,null,Palette.Gold);UIFactory.Fill(screenPulse.rectTransform);screenPulse.gameObject.SetActive(false);
        }
        Vector2 Center => ui.CoinHome.anchoredPosition;
        public void Spark(float lift)
        {
            Spawn(Center+new Vector2(Random.Range(-45,45),lift),new Vector2(Random.Range(-20,20),Random.Range(-30,20)),.25f,Random.Range(3,7),Palette.Gold,false,true);
        }
        public void Launch()
        {
            for(int i=0;i<9;i++)
            {
                float a=i*Mathf.PI*2/9;
                Spawn(Center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*ui.CoinSize*.54f,new Vector2(Mathf.Cos(a),Mathf.Sin(a))*65,.3f,5,new Color(.7f,.8f,.9f,.45f),false,true);
            }
        }
        public void Land(bool won)
        {
            StartCoroutine(Ripple(won?Palette.Gold:Palette.Red));
            StartCoroutine(ScreenPulse(won?Palette.Gold:Palette.Red));
            int count=won?24:6;
            for(int i=0;i<count;i++)
            {
                float a=Random.Range(.1f,Mathf.PI-.1f);float speed=Random.Range(80,200);
                Color color=won?(i%3==0?Palette.Cyan:i%3==1?Palette.Gold:Palette.Green):Palette.Red;
                Spawn(Center,new Vector2(Mathf.Cos(a)*speed,Mathf.Sin(a)*speed),Random.Range(.45f,.9f),Random.Range(4,10),color,true,i%3==0);
            }
        }
        void Spawn(Vector2 origin,Vector2 velocity,float life,float size,Color color,bool gravity,bool star)
        {
            foreach(Particle p in particles)
            {
                if(p.active)continue;
                p.active=true;p.age=0;p.life=life;p.origin=origin;p.velocity=velocity;p.color=color;p.gravity=gravity;p.size=size;p.spin=Random.Range(-220,220);p.rotation=Random.Range(0,180);
                p.image.sprite=star?sparkle:circle;p.rect.anchoredPosition=origin;p.rect.sizeDelta=Vector2.one*size;p.image.color=color;p.image.gameObject.SetActive(true);
                if(simulation==null)simulation=StartCoroutine(Simulate());break;
            }
        }
        IEnumerator Simulate()
        {
            bool any=true;
            while(any)
            {
                any=false;
                foreach(Particle p in particles)
                {
                    if(!p.active)continue;
                    p.age+=Mathf.Min(Time.unscaledDeltaTime,.04f);float t=p.age/p.life;
                    if(t>=1){p.active=false;p.image.gameObject.SetActive(false);continue;}
                    any=true;p.rect.anchoredPosition=p.origin+p.velocity*p.age+new Vector2(0,p.gravity?-150*p.age*p.age:0);
                    p.rect.localEulerAngles=new Vector3(0,0,p.rotation+p.spin*p.age);p.rect.localScale=Vector3.one*(1-t*.5f);
                    Color c=p.color;c.a*=1-t;p.image.color=c;
                }
                yield return null;
            }
            simulation=null;
        }
        IEnumerator Ripple(Color color)
        {
            pulse.gameObject.SetActive(true);pulse.rectTransform.anchorMin=pulse.rectTransform.anchorMax=new Vector2(0,1);pulse.rectTransform.anchoredPosition=Center;
            for(float t=0;t<1;t+=Time.unscaledDeltaTime/.4f)
            {pulse.rectTransform.sizeDelta=Vector2.one*ui.CoinSize*Mathf.Lerp(.9f,1.65f,t);color.a=(1-t)*.5f;pulse.color=color;yield return null;}
            pulse.gameObject.SetActive(false);
        }
        IEnumerator ScreenPulse(Color color)
        {
            screenPulse.gameObject.SetActive(true);
            for(float t=0;t<1;t+=Time.unscaledDeltaTime/.28f){color.a=Mathf.Sin(t*Mathf.PI)*.025f;screenPulse.color=color;yield return null;}
            screenPulse.gameObject.SetActive(false);
        }
    }
}
