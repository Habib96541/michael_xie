using UnityEngine;

namespace BrickRush
{
    public sealed class BallController
    {
        public const float Radius=.115f;
        public bool Active {get;private set;}
        public bool Docked {get;private set;}
        public Vector2 Position {get;private set;}
        public Vector2 Direction {get;private set;}
        readonly GameManager game;readonly SpriteRenderer body,glow;readonly TrailRenderer trail;
        readonly RaycastHit2D[] hits=new RaycastHit2D[16];
        readonly ContactFilter2D filter;
        BrickController lastBrick;float lastHitTime;
        public BallController(GameManager owner,Transform parent,int index)
        {
            game=owner;var color=index==0?Art.White:Art.Palette[index];
            body=Art.Sprite("Ball "+index,Art.Ball,Vector2.zero,Vector2.one*.29f,color,20,parent);
            glow=Art.Sprite("Ball halo",Art.Glow,Vector2.zero,Vector2.one*.9f,new Color(color.r,color.g,color.b,.55f),19,parent);
            trail=body.gameObject.AddComponent<TrailRenderer>();trail.sharedMaterial=Art.TrailMaterial;trail.time=.13f;trail.startWidth=.16f;trail.endWidth=.012f;trail.minVertexDistance=.05f;trail.numCapVertices=4;trail.sortingOrder=18;
            trail.startColor=new Color(color.r,color.g,color.b,.7f);trail.endColor=new Color(color.r,color.g,color.b,0);
            filter=new ContactFilter2D();filter.SetLayerMask(1<<6);filter.useTriggers=false;Hide();
        }
        public void Spawn(Vector2 position,Vector2 direction,bool docked=false)
        {
            Position=position;Direction=GameRules.SafeDirection(direction);Docked=docked;Active=true;lastBrick=null;
            body.gameObject.SetActive(true);glow.gameObject.SetActive(true);body.transform.position=Position;trail.Clear();trail.emitting=!docked;Draw();
        }
        public void Launch(){Docked=false;trail.emitting=true;}
        public void Hide(){Active=false;trail.emitting=false;trail.Clear();body.gameObject.SetActive(false);glow.gameObject.SetActive(false);}
        public void Step(float dt,float speed)
        {
            if(!Active)return;
            if(Docked){Position=new Vector2(game.Paddle.X,PaddleController.Y+.30f);Draw();return;}
            float remaining=speed*dt;
            for(int iteration=0;iteration<10&&remaining>.0001f;iteration++)
            {
                float distance=remaining;Vector2 normal=Vector2.zero;BrickController brick=null;bool paddleHit=false;
                // Static wall planes and paddle top plane; cast bricks with their actual 2D colliders.
                if(Direction.x<0)Plane((-4.67f+Radius-Position.x)/Direction.x,Vector2.right,ref distance,ref normal);
                else Plane((4.67f-Radius-Position.x)/Direction.x,Vector2.left,ref distance,ref normal);
                if(Direction.y>0)Plane((6.8f-Radius-Position.y)/Direction.y,Vector2.down,ref distance,ref normal);
                if(Direction.y<0&&Position.y>=PaddleController.Y+.19f)
                {
                    float d=(PaddleController.Y+.19f+Radius-Position.y)/Direction.y;
                    float x=Position.x+Direction.x*d;
                    if(d>=0&&d<=distance&&Mathf.Abs(x-game.Paddle.X)<=game.Paddle.Width/2+Radius*.65f){distance=d;normal=Vector2.up;paddleHit=true;}
                }
                int count=Physics2D.CircleCast(Position,Radius,Direction,filter,hits,distance+.0001f);
                for(int i=0;i<count;i++)
                {
                    var hit=hits[i];if(hit.distance<=.00001f)continue;
                    var candidate=hit.collider.GetComponent<BrickController>();
                    if(candidate==null||!candidate.Alive||(candidate==lastBrick&&Time.fixedTime-lastHitTime<.06f))continue;
                    if(hit.distance<=distance){distance=hit.distance;normal=hit.normal;brick=candidate;paddleHit=false;}
                }
                Position+=Direction*distance;remaining-=distance;
                if(normal==Vector2.zero)break;
                bool pierce=brick!=null&&game.Powers.Has(PowerKind.Fire)&&brick.Kind!=BrickKind.Obstacle;
                if(brick!=null){lastBrick=brick;lastHitTime=Time.fixedTime;game.HitBrick(brick,Position,pierce);}
                if(paddleHit)
                {
                    Direction=GameRules.PaddleBounce((Position.x-game.Paddle.X)/(game.Paddle.Width/2));game.PaddleReturn(Position);
                }
                else if(!pierce){Direction=GameRules.SafeDirection(Vector2.Reflect(Direction,normal));if(brick==null)game.WallHit(Position);}
                Position+= (pierce?Direction:normal)*.012f;remaining=Mathf.Max(0,remaining-.012f);
            }
            if(Position.y<-6.85f)
            {
                if(game.Powers.ConsumeShield()){Position=new Vector2(Mathf.Clamp(Position.x,-4.4f,4.4f),-6.3f);Direction=GameRules.SafeDirection(new Vector2(Direction.x,Mathf.Abs(Direction.y)));game.ShieldReturn(Position);}
                else Hide();
            }
            Draw();
        }
        static void Plane(float d,Vector2 n,ref float distance,ref Vector2 normal){if(d>=0&&d<=distance){distance=d;normal=n;}}
        public void Draw()
        {
            body.transform.position=Position;glow.transform.position=Position;
            body.color=game.Powers!=null&&game.Powers.Has(PowerKind.Fire)?Art.Orange:Art.White;
            trail.time=game.ReducedEffects ? .06f : .10f+game.Elapsed/1800;
        }
    }
}
