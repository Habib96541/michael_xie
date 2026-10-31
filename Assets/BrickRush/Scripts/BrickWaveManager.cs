using UnityEngine;

namespace BrickRush
{
    public sealed class BrickController : MonoBehaviour
    {
        public BrickKind Kind {get;private set;}
        public bool Alive {get;private set;}
        public int HP {get;private set;}
        public int Index {get;private set;}
        public Color Color {get;private set;}
        public Vector2 Position => transform.position;
        SpriteRenderer body,mark,glow; BoxCollider2D box;
        float flash,death; Vector3 baseScale;
        public void Initialize(int index)
        {
            Index=index; gameObject.layer=6;
            body=Art.Sprite("Gloss",Art.Brick,Vector2.zero,new Vector2(.91f,.36f),Color.white,4,transform);baseScale=body.transform.localScale;
            glow=Art.Sprite("Underlight",Art.Glow,new Vector2(0,-.1f),new Vector2(1.4f,.65f),Color.white,2,transform);
            mark=Art.Sprite("Type",Art.Marks[1],Vector2.zero,new Vector2(.75f,.36f),Color.white,6,transform);
            box=gameObject.AddComponent<BoxCollider2D>();box.size=new Vector2(.91f,.36f);
        }
        public void Configure(Vector2 position,BrickKind kind,Color color)
        {
            transform.position=position;Kind=kind;HP=kind==BrickKind.Armored?2:1;Alive=true;Color=kind==BrickKind.Obstacle?Art.Hex("495776"):color;
            flash=death=0;box.enabled=true;body.enabled=true;glow.enabled=true;
            body.color=Color;body.transform.localScale=baseScale;glow.color=new Color(Color.r,Color.g,Color.b,.25f);
            mark.enabled=kind!=BrickKind.Standard;if(mark.enabled)mark.sprite=Art.Marks[(int)kind];
            gameObject.SetActive(true);
        }
        public bool Damage()
        {
            if(!Alive)return false;
            flash=.16f;
            if(Kind==BrickKind.Obstacle)return false;
            if(--HP>0){mark.enabled=false;Color=Color.Lerp(Color,Art.Ink,.34f);return false;}
            Alive=false;box.enabled=false;death=.13f;return true;
        }
        public void Hide(){ Alive=false;box.enabled=false;gameObject.SetActive(false); }
        public void Tick(float dt)
        {
            if(!gameObject.activeSelf)return;
            flash=Mathf.Max(0,flash-dt);
            body.color=Color.Lerp(Color,Color.white,flash/.16f);
            body.transform.localScale=Vector3.Scale(baseScale,new Vector3(1+flash*.45f,1-flash*.9f,1));
            if(!Alive){death-=dt;body.color=new Color(1,1,1,Mathf.Clamp01(death/.13f));if(death<=0)Hide();}
        }
    }
    public sealed class BrickWaveManager
    {
        public readonly BrickController[] Bricks=new BrickController[81];
        public int Remaining {get;private set;}
        public int Wave {get;private set;}
        public int Completed => Mathf.Max(0,Wave-1)+(Wave>0&&Remaining==0?1:0);
        int stage;
        public static readonly string[] LayoutNames={"FIRST LIGHT","CHEVRON","THE GATE","DIAMOND CUT","CROSS CURRENT","NEON STEPS","PULSE ARRAY","FINAL FREQUENCY"};
        public BrickWaveManager(Transform parent)
        {
            for(int i=0;i<Bricks.Length;i++){var go=new GameObject("Brick "+i);go.transform.SetParent(parent);Bricks[i]=go.AddComponent<BrickController>();Bricks[i].Initialize(i);Bricks[i].Hide();}
        }
        public void Reset(){Wave=0;stage=0;}
        public void Generate(float elapsed)
        {
            Wave++;Remaining=0;stage=elapsed>=70?3:elapsed>=45?2:elapsed>=20?1:0;
            int layout=(Wave-1)%8;int rows=elapsed<20?6:7;
            for(int i=0;i<Bricks.Length;i++)Bricks[i].Hide();
            for(int r=0;r<rows;r++)for(int c=0;c<9;c++)
            {
                int d=Mathf.Abs(c-4); bool include=true;
                switch(layout){case 1:include=r>=d-1;break;case 2:include=c<2||c>6||r<2;break;case 3:include=d+Mathf.Abs(r-3)<=5;break;case 4:include=r%3!=1||c%3!=1;break;case 5:include=(c+r)%5!=0;break;case 6:include=(c%3!=1||r%2==0);break;case 7:include=r!=3||c%2==0;break;}
                if(!include)continue;
                BrickKind kind=BrickKind.Standard;
                if((c+r*3+Wave)%17==0)kind=BrickKind.Bonus;
                else if(elapsed>=20&&(c+r+Wave)%7==0)kind=BrickKind.Armored;
                else if((elapsed>=45||Wave>2)&&(c*2+r+Wave)%11==0)kind=BrickKind.Explosive;
                if(elapsed>=45&&r==rows-1&&(c==2||c==6)&&layout%2==1)kind=BrickKind.Obstacle;
                Bricks[r*9+c].Configure(new Vector2((c-4)*1.01f,5.0f-r*.53f),kind,Art.Palette[r%Art.Palette.Length]);
                if(kind!=BrickKind.Obstacle)Remaining++;
            }
            Physics2D.SyncTransforms();
        }
        public void Destroyed(){Remaining=Mathf.Max(0,Remaining-1);}
        public bool Evolve(float elapsed)
        {
            int next=elapsed>=70?3:elapsed>=45?2:elapsed>=20?1:0;
            if(next<=stage)return false;stage=next;
            foreach(var b in Bricks)
            {
                if(!b.Alive||b.Kind!=BrickKind.Standard)continue;
                if(next==1&&b.Index%6==0)b.Configure(b.Position,BrickKind.Armored,b.Color);
                else if(next==2&&b.Index%8==0)b.Configure(b.Position,BrickKind.Explosive,Art.Orange);
                else if(next==3&&b.Index%9==0)b.Configure(b.Position,BrickKind.Bonus,Art.Gold);
            }
            Physics2D.SyncTransforms();return true;
        }
        public void Tick(float dt){for(int i=0;i<Bricks.Length;i++)Bricks[i].Tick(dt);}
    }
}
