using UnityEngine;
using TMPro;

namespace BrickRush
{
    public sealed class VFXManager
    {
        readonly Particle[] particles=new Particle[224];readonly Floater[] floaters=new Floater[24];readonly Ring[] rings=new Ring[12];
        readonly GameManager game;int cursor,floatCursor,ringCursor;float shake,slow;
        sealed class Particle {public SpriteRenderer Body;public Vector2 Pos,Vel;public float Life,Max,Size,Spin;public Color Color;}
        sealed class Floater {public TextMeshPro Text;public Vector2 Pos;public float Life;public Color Color;}
        sealed class Ring {public SpriteRenderer Body;public float Life,Size;public Color Color;}
        public float SimulationScale=>slow>0?.45f:1;
        public VFXManager(GameManager owner,Transform parent,TMP_FontAsset font)
        {
            game=owner;
            for(int i=0;i<particles.Length;i++)particles[i]=new Particle{Body=Art.Sprite("Fragment",i%3==0?Art.Brick:Art.Spark,Vector2.zero,Vector2.one*.1f,Color.clear,25,parent)};
            for(int i=0;i<floaters.Length;i++){var go=new GameObject("Score pop");go.transform.SetParent(parent);var t=go.AddComponent<TextMeshPro>();t.font=font;t.fontSize=2.6f;t.alignment=TextAlignmentOptions.Center;t.rectTransform.sizeDelta=new Vector2(2,1);t.sortingOrder=30;t.enabled=false;floaters[i]=new Floater{Text=t};}
            for(int i=0;i<rings.Length;i++)rings[i]=new Ring{Body=Art.Sprite("Impact ring",Art.Ring,Vector2.zero,Vector2.one,Color.clear,23,parent)};
        }
        public void Burst(Vector2 position,Color color,int count=9)
        {
            count=game.ReducedEffects?Mathf.Min(4,count):count;
            for(int i=0;i<count;i++)
            {
                var p=particles[cursor++%particles.Length];float angle=Random.Range(0,Mathf.PI*2);float speed=Random.Range(.7f,3.5f);
                p.Pos=position;p.Vel=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*speed;p.Life=p.Max=Random.Range(.22f,.55f);p.Size=Random.Range(.045f,.13f);p.Color=color;p.Spin=Random.Range(-260,260);p.Body.enabled=true;
            }
            var r=rings[ringCursor++%rings.Length];r.Life=.3f;r.Size=count>15?2.8f:1;r.Color=color;r.Body.transform.position=position;r.Body.enabled=true;
        }
        public void Number(Vector2 position,int points,Color color)
        {
            var f=floaters[floatCursor++%floaters.Length];f.Life=.65f;f.Pos=position;f.Color=color;f.Text.SetText("+{0}",points);f.Text.enabled=true;
        }
        public void Shake(float amount){if(game.ScreenShake)shake=Mathf.Max(shake,amount);}
        public void SlowMotion(){if(!game.ReducedEffects)slow=.14f;}
        public void Reset(){shake=slow=0;foreach(var p in particles){p.Life=0;p.Body.enabled=false;}foreach(var f in floaters){f.Life=0;f.Text.enabled=false;}foreach(var r in rings){r.Life=0;r.Body.enabled=false;}}
        public void Tick(float dt)
        {
            slow=Mathf.Max(0,slow-dt);shake=Mathf.MoveTowards(shake,0,dt*.45f);
            var offset=game.ScreenShake?Random.insideUnitCircle*shake:Vector2.zero;game.Camera.transform.position=new Vector3(offset.x,offset.y,-10);
            for(int i=0;i<particles.Length;i++)
            {
                var p=particles[i];if(p.Life<=0)continue;p.Life-=dt;if(p.Life<=0){p.Body.enabled=false;continue;}
                p.Vel.y-=dt*4;p.Pos+=p.Vel*dt;p.Body.transform.position=p.Pos;p.Body.transform.Rotate(0,0,p.Spin*dt);
                Art.Size(p.Body,new Vector2(p.Size*(1+p.Life/p.Max),p.Size*.6f));p.Body.color=new Color(p.Color.r,p.Color.g,p.Color.b,p.Life/p.Max);
            }
            for(int i=0;i<floaters.Length;i++)
            {
                var f=floaters[i];if(f.Life<=0)continue;f.Life-=dt;if(f.Life<=0){f.Text.enabled=false;continue;}f.Pos.y+=dt*.65f;f.Text.transform.position=f.Pos;f.Text.color=new Color(f.Color.r,f.Color.g,f.Color.b,Mathf.Min(1,f.Life*3));
            }
            for(int i=0;i<rings.Length;i++)
            {
                var r=rings[i];if(r.Life<=0)continue;r.Life-=dt;Art.Size(r.Body,Vector2.one*(.2f+(1-r.Life/.3f)*r.Size));r.Body.color=new Color(r.Color.r,r.Color.g,r.Color.b,r.Life/.3f*.6f);if(r.Life<=0)r.Body.enabled=false;
            }
        }
    }
}
