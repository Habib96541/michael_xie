using UnityEngine;
using TMPro;

namespace BrickRush
{
    public sealed class PowerUpManager
    {
        public static readonly string[] Names={"MULTIBALL","WIDE PADDLE","FIREBALL","LASER PADDLE","SLOW BALL","SCORE x2","SHIELD"};
        public static readonly string[] Symbols={"M","W","F","L","S","x2","+"};
        public static readonly Color[] Colors={Art.Pink,Art.Cyan,Art.Orange,Art.Violet,Art.Hex("5AABFF"),Art.Gold,Art.Cyan};
        public readonly float[] Durations=new float[7];
        readonly GameManager game; readonly Drop[] drops=new Drop[12]; readonly Laser[] lasers=new Laser[16];
        float dropCooldown,laserCooldown;int nextPower,shots;
        public bool Shield {get;private set;}
        public int Collected {get;private set;}
        sealed class Drop {public SpriteRenderer Body,Glow;public TextMeshPro Label;public PowerKind Kind;public Vector2 Pos;public bool Active;}
        sealed class Laser {public SpriteRenderer Body;public Vector2 Pos;public bool Active;}
        public PowerUpManager(GameManager owner,Transform parent,TMP_FontAsset font)
        {
            game=owner;
            for(int i=0;i<drops.Length;i++)
            {
                var d=new Drop();d.Body=Art.Sprite("Power drop",Art.Panel,Vector2.zero,new Vector2(.54f,.45f),Art.Cyan,15,parent);
                d.Glow=Art.Sprite("Drop glow",Art.Glow,Vector2.zero,Vector2.one,Art.Cyan,14,parent);
                var go=new GameObject("Symbol");go.transform.SetParent(parent);d.Label=go.AddComponent<TextMeshPro>();d.Label.font=font;d.Label.fontSize=3;d.Label.alignment=TextAlignmentOptions.Center;d.Label.rectTransform.sizeDelta=new Vector2(.6f,.6f);d.Label.sortingOrder=17;d.Label.color=Art.Ink;
                drops[i]=d;SetDrop(d,false);
            }
            for(int i=0;i<lasers.Length;i++){lasers[i]=new Laser{Body=Art.Sprite("Laser",Art.Paddle,Vector2.zero,new Vector2(.055f,.38f),Art.Pink,15,parent)};lasers[i].Body.enabled=false;}
        }
        static void SetDrop(Drop d,bool active){d.Active=active;d.Body.enabled=d.Glow.enabled=active;d.Label.gameObject.SetActive(active);}
        public bool Has(PowerKind kind)=>kind==PowerKind.Shield?Shield:Durations[(int)kind]>0;
        public void Reset()
        {
            System.Array.Clear(Durations,0,Durations.Length);Shield=false;Collected=0;dropCooldown=0;nextPower=shots=0;laserCooldown=0;
            foreach(var d in drops)SetDrop(d,false);foreach(var l in lasers){l.Active=false;l.Body.enabled=false;}
        }
        public void Offer(Vector2 position,bool bonus)
        {
            if(dropCooldown>0)return;
            if(!bonus&&game.Score.Destroyed%7!=0)return;
            Drop free=null;for(int i=0;i<drops.Length;i++)if(!drops[i].Active){free=drops[i];break;}
            if(free==null)return;
            // Rotation guarantees every power can be discovered; only one drop per cooldown.
            free.Kind=(PowerKind)(nextPower++%7);free.Pos=position;free.Body.color=Colors[(int)free.Kind];free.Glow.color=new Color(free.Body.color.r,free.Body.color.g,free.Body.color.b,.35f);free.Label.text=Symbols[(int)free.Kind];SetDrop(free,true);dropCooldown=game.Elapsed>70?2.5f:4;
        }
        public void Activate(PowerKind kind)
        {
            if(game.State!=SessionState.Playing)return;
            if(kind==PowerKind.Multiball)game.Multiball();
            else if(kind==PowerKind.Shield)Shield=true;
            else {Durations[(int)kind]=game.Config.powerDuration;if(kind==PowerKind.Laser)shots=16;}
            game.VFX.Burst(new Vector2(game.Paddle.X,PaddleController.Y),Colors[(int)kind],16);
            game.UI.Toast(Names[(int)kind],Colors[(int)kind]);game.Audio.Play("collect");
        }
        public bool ConsumeShield(){if(!Shield)return false;Shield=false;return true;}
        public void Tick(float dt)
        {
            dropCooldown-=dt;for(int i=0;i<Durations.Length;i++)Durations[i]=Mathf.Max(0,Durations[i]-dt);
            for(int i=0;i<drops.Length;i++)
            {
                var d=drops[i];if(!d.Active)continue;d.Pos.y-=dt*2.25f;d.Body.transform.position=d.Pos;d.Glow.transform.position=d.Pos;d.Label.transform.position=(Vector3)d.Pos+new Vector3(0,-.015f,-.1f);
                if(Mathf.Abs(d.Pos.y-PaddleController.Y)<.4f&&Mathf.Abs(d.Pos.x-game.Paddle.X)<game.Paddle.Width/2+.25f){Collected++;Activate(d.Kind);SetDrop(d,false);}
                else if(d.Pos.y<-7.2f)SetDrop(d,false);
            }
            laserCooldown-=dt;
            if(Has(PowerKind.Laser)&&shots>0&&laserCooldown<=0)
            {
                laserCooldown=.45f;shots-=2;game.Audio.Play("laser",.3f);
                for(int side=-1;side<=1;side+=2)for(int i=0;i<lasers.Length;i++)if(!lasers[i].Active){lasers[i].Active=true;lasers[i].Pos=new Vector2(game.Paddle.X+side*game.Paddle.Width*.36f,PaddleController.Y+.2f);lasers[i].Body.enabled=true;break;}
                if(shots<=0)Durations[(int)PowerKind.Laser]=0;
            }
            for(int i=0;i<lasers.Length;i++)
            {
                var l=lasers[i];if(!l.Active)continue;float y=l.Pos.y;l.Pos.y+=dt*18;
                for(int j=0;j<game.Waves.Bricks.Length;j++)
                {
                    var b=game.Waves.Bricks[j];if(b.Alive&&Mathf.Abs(b.Position.x-l.Pos.x)<.48f&&b.Position.y>=y-.18f&&b.Position.y<=l.Pos.y+.18f){game.HitBrick(b,b.Position,false);l.Active=false;break;}
                }
                if(l.Pos.y>7)l.Active=false;l.Body.enabled=l.Active;l.Body.transform.position=l.Pos;
            }
        }
        public void DropForValidation(PowerKind kind)
        {
            if(!game.QA)return;
            foreach(var d in drops)if(!d.Active){d.Kind=kind;d.Pos=new Vector2(game.Paddle.X,PaddleController.Y+.25f);d.Body.color=Colors[(int)kind];d.Label.text=Symbols[(int)kind];SetDrop(d,true);break;}
        }
    }
}
