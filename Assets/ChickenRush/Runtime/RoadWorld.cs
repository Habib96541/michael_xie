using System;
using TMPro;
using UnityEngine;

namespace ChickenRush
{
    // Raised-view sprites keep the reference's straight road composition.
    // Traffic presents the seeded hop; it never consumes gameplay randomness.
    public sealed class RoadWorld
    {
        public const float Step=2.4f,LaneOffset=Step*.5f;
        const int PoolLanes=12,CarsPerLane=3;
        const float CoreY=.50f;
        static readonly string[] Kinds={"Compact","Taxi","Van","Truck","Delivery"};
        sealed class Car { public Transform transform;public SpriteRenderer image;public float y,previousY,width,length;public int kind; }
        sealed class Lane
        {
            public int id,direction;public float speed;public Transform root;public Car[] cars;
            public SpriteRenderer pad,barrier,plate,ring;public TextMeshPro reward,caption;
        }
        [Serializable] public sealed class VehicleSnapshot
        {
            public int lane,id,direction;public float x,y,vx,vy,width,length;public string kind;public bool visible;
        }
        [Serializable] public sealed class LaneSnapshot
        {
            public int lane,tier,direction,count;public float x,speed,clearGap;
        }
        public struct Feedback { public bool pass,nearMiss;public float passSpeed; }
        readonly Lane[] slots=new Lane[PoolLanes];
        readonly Transform root,roadSurface;
        readonly Sprite rounded,disc,square;
        readonly Sprite[] vehicleArt;
        readonly Sprite padArt,barrierArt;
        readonly TMP_FontAsset font;
        readonly Material material;
        readonly RushDifficulty config;
        readonly float[] gates;
        readonly double[] departures;
        int seed,activeLane,firstLane=1;
        bool plannedSuccess,attemptActive;
        float attemptClock,attemptDuration,threatStart,impactClock,impactY;
        double clock;
        public bool ImpactContact {get;private set;}
        public readonly VehicleSnapshot[] vehicleSnapshots=new VehicleSnapshot[PoolLanes*CarsPerLane];
        public readonly LaneSnapshot[] laneSnapshots;
        public static Color Hex(string value){ColorUtility.TryParseHtmlString("#"+value,out var color);return color;}
        public static float LaneX(int lane)=>lane*Step;

        public RoadWorld(Transform parent,Sprite roundedSprite,Sprite discSprite,Sprite squareSprite,Sprite arrowSprite,Sprite asphaltSprite,
            TMP_FontAsset fontAsset,Material spriteMaterial,RushDifficulty difficulty,Sprite[] vehicles,Sprite pad,Sprite barrier,Sprite shrub,Sprite sidewalk)
        {
            root=parent;rounded=roundedSprite;disc=discSprite;square=squareSprite;font=fontAsset;material=spriteMaterial;config=difficulty;
            vehicleArt=vehicles;padArt=pad;barrierArt=barrier;gates=new float[config.multipliers.Length];departures=new double[gates.Length];laneSnapshots=new LaneSnapshot[gates.Length];
            var surface=Rect("Painted asphalt",root,Vector2.zero,new Vector2(50,32),Color.white,-110,false,true);
            surface.sprite=asphaltSprite;surface.drawMode=SpriteDrawMode.Tiled;surface.size=new Vector2(50,32);roadSurface=surface.transform;
            for(int i=0;i<PoolLanes;i++)slots[i]=CreateLane();
            for(int y=-7;y<=7;y++)Sprite("Raised start sidewalk",root,sidewalk,new Vector2(-.8f,y*2.3f),1.8f,-70);
            for(int y=-2;y<=3;y++)if(y!=0)Sprite("Wayside planter",root,shrub,new Vector2(-2.25f,y*2.6f),1.2f,-65);
            var start=Sprite("Start landing pad",root,padArt,new Vector2(0,-.10f),1.65f,-20);start.color=Hex("C8DFD8");
            Label("START",root,new Vector2(0,-1.3f),2.3f,Hex("9BAEBC"));
            for(int i=0;i<vehicleSnapshots.Length;i++)vehicleSnapshots[i]=new VehicleSnapshot{id=i};
            for(int i=0;i<laneSnapshots.Length;i++)laneSnapshots[i]=new LaneSnapshot{lane=i+1,x=LaneX(i+1),direction=Direction(i+1),count=CarsPerLane,tier=Math.Min(3,i/4),speed=Speed(i+1)};
            Reset(1);
        }
        int Direction(int lane)=>lane%3==0?1:-1;
        float Speed(int lane)=>4.8f*config.speedScale+lane*.14f;
        Lane CreateLane()
        {
            var l=new Lane{root=new GameObject("Recycled road segment").transform,cars=new Car[CarsPerLane]};l.root.SetParent(root,false);
            // A shared surface and sparse separators replace narrow colored strips.
            for(int y=-14;y<=14;y+=2)Rect("Painted lane dash",l.root,new Vector2(-Step*.5f,y),new Vector2(.045f,.65f),new Color(.55f,.68f,.78f,.19f),-80,false,true);
            l.ring=Rect("Next landing glow",l.root,new Vector2(0,-.12f),new Vector2(2.04f,1.4f),new Color(.2f,.95f,.75f,.16f),-32,true);
            l.pad=Sprite("Raised landing grate",l.root,padArt,new Vector2(0,-.1f),1.82f,-30);
            l.plate=Rect("Lane multiplier badge",l.root,new Vector2(0,-1.34f),new Vector2(1.72f,.52f),Hex("283F52"),70);
            l.reward=Label("1.00x",l.root,new Vector2(0,-1.34f),3.2f,Hex("DCE9EF"));
            l.caption=Label("01",l.root,new Vector2(0,4.8f),2.3f,Hex("617B8C"));
            l.barrier=Sprite("Protective traffic barrier",l.root,barrierArt,new Vector2(0,1.85f),1.70f,42);l.barrier.enabled=false;
            for(int i=0;i<CarsPerLane;i++)
            {
                var car=new GameObject("Pooled vehicle").transform;car.SetParent(l.root,false);
                Rect("Soft vehicle shadow",car,new Vector2(.08f,-.09f),new Vector2(1.35f,1.4f),new Color(.015f,.03f,.05f,.26f),5,true);
                l.cars[i]=new Car{transform=car,image=Sprite("Shaded original vehicle",car,vehicleArt[0],Vector2.zero,1.38f,12)};
            }
            return l;
        }
        void Bind(Lane l,int id)
        {
            l.id=id;l.root.gameObject.SetActive(id<=gates.Length);if(id>gates.Length)return;
            l.direction=Direction(id);l.speed=Speed(id);l.root.localPosition=new Vector3(LaneX(id),0,0);
            l.reward.text=config.multipliers[id-1].ToString("0.##")+"x";l.caption.text=id.ToString("00");
            l.barrier.transform.localPosition=new Vector3(0,CoreY-l.direction*1.65f,0);
            for(int i=0;i<CarsPerLane;i++)
            {
                var c=l.cars[i];c.kind=((id+i+seed%5)%5+5)%5;
                c.image.sprite=vehicleArt[c.kind+(l.direction>0?5:0)];SetWidth(c.image,1.38f);
                c.width=1.12f;c.length=c.image.sprite.bounds.size.y*c.image.transform.localScale.y*.78f;
                c.transform.name=Kinds[c.kind]+" lane "+id;
                c.y=c.previousY=Position(l,i);c.transform.localPosition=new Vector3(0,c.y,0);
            }
        }
        Lane Find(int lane){foreach(var l in slots)if(l.id==lane && l.root.gameObject.activeSelf)return l;return null;}
        public void Reset(int roundSeed)
        {
            seed=roundSeed;clock=0;attemptClock=0;attemptActive=false;activeLane=0;ImpactContact=false;
            Array.Clear(gates,0,gates.Length);firstLane=1;for(int i=0;i<PoolLanes;i++)Bind(slots[i],i+1);
        }
        public void BeginAttempt(int lane,bool success,float duration)
        {
            activeLane=lane;plannedSuccess=success;attemptDuration=duration;attemptClock=0;attemptActive=true;ImpactContact=false;
            var l=Find(lane);threatStart=CoreY-l.direction*3.5f;
            // This car was already visibly staged here before the input.
            l.cars[0].y=l.cars[0].previousY=threatStart;l.cars[0].transform.localPosition=new Vector3(0,threatStart,0);
        }
        public void Secure(int lane){gates[lane-1]=lane%4==0?2:1;departures[lane-1]=clock;attemptActive=false;}
        float Position(Lane l,int car)
        {
            if(car==0)
            {
                if(l.id==activeLane && attemptActive)
                {
                    float t=Mathf.Clamp01(attemptClock/attemptDuration);
                    if(plannedSuccess)return l.id%4==0?threatStart+l.direction*(8.9f*t):CoreY-l.direction*Mathf.Lerp(3.5f,2.85f,Mathf.SmoothStep(0,1,t));
                    return threatStart+l.direction*(4.9f*t);
                }
                if(l.id==activeLane && ImpactContact)return impactY+l.direction*Mathf.Min(12,(float)(clock-impactClock)*l.speed);
                if(gates[l.id-1]>1)return CoreY+l.direction*Mathf.Min(16,5.4f+(float)(clock-departures[l.id-1])*l.speed);
                if(gates[l.id-1]>0)return CoreY-l.direction*2.85f;
                return CoreY-l.direction*3.5f;
            }
            float distance=(float)(clock*l.speed)+(car==1?0:8)+(seed%17)*.13f;
            float phase=Mathf.Repeat(distance,9.4f);
            if(car==1)
            {
                float departure=4.8f+phase;
                if((attemptActive && plannedSuccess && l.id==activeLane && l.id%4==0) || gates[l.id-1]>1 || (l.id==activeLane && ImpactContact))
                {
                    float lead=(Position(l,0)-CoreY)*l.direction;
                    if(lead>0 && lead<16)departure=Mathf.Max(departure,lead+2.4f);
                }
                return CoreY+l.direction*departure;
            }
            return CoreY-l.direction*(15.4f-phase);
        }
        public Feedback Tick(float dt,Vector2 foot,bool crossing,bool listen)
        {
            clock+=dt;if(attemptActive)attemptClock+=dt;Feedback feedback=default;
            foreach(var l in slots)
            {
                if(l.id>gates.Length)continue;
                for(int i=0;i<CarsPerLane;i++)
                {
                    var c=l.cars[i];c.previousY=c.y;c.y=Position(l,i);if(Mathf.Abs(c.y-c.previousY)>5)c.previousY=c.y;
                    c.transform.localPosition=new Vector3(0,c.y,0);c.image.sortingOrder=c.y<CoreY?55:12;
                    float distance=Mathf.Abs(c.y-CoreY);
                    float alpha=i==0?1:i==1?Mathf.Clamp01((distance-4.8f)/.75f):Mathf.Clamp01((distance-6)/.75f);
                    c.image.color=new Color(1,1,1,alpha);
                }
            }
            if(attemptActive && plannedSuccess && activeLane%4==0)
            {
                var l=Find(activeLane);var c=l.cars[0];float gap=Mathf.Abs(c.y-(foot.y+CoreY))-c.length*.5f-.28f;
                if(Mathf.Abs(foot.x-LaneX(activeLane))<c.width*.5f+.27f && gap>0 && gap<.65f)
                {feedback.pass=true;feedback.nearMiss=true;feedback.passSpeed=Speed(activeLane);}
            }
            return feedback;
        }
        public bool Collides(Vector2 foot,Vector2 previousFoot)
        {
            if(!attemptActive)return false;
            var l=Find(activeLane);var c=l.cars[0];
            Vector2 from=new Vector2(previousFoot.x-LaneX(activeLane),previousFoot.y+CoreY-c.previousY),to=new Vector2(foot.x-LaneX(activeLane),foot.y+CoreY-c.y);
            bool hit=TrafficPattern.SweptHit(from,to,new Vector2(c.width*.5f+.27f,c.length*.5f+.28f));
            if(hit){ImpactContact=true;impactClock=(float)clock;impactY=c.y;attemptActive=false;}return hit;
        }
        public void Present(float viewportHalfHeight,int row,float time,float cameraX=0,float halfWidth=8)
        {
            roadSurface.localPosition=new Vector3(cameraX+8,0,0);
            int desired=Mathf.Max(1,Mathf.FloorToInt((cameraX-halfWidth)/Step));
            if(desired!=firstLane)
            {
                firstLane=desired;
                for(int i=0;i<PoolLanes;i++)if(slots[i].id<desired || slots[i].id>=desired+PoolLanes)
                    for(int id=desired;id<desired+PoolLanes;id++)if(Find(id)==null){Bind(slots[i],id);break;}
            }
            foreach(var l in slots)
            {
                if(l.id>gates.Length)continue;bool cleared=l.id<=row,next=l.id==row+1;
                l.ring.enabled=next;l.ring.color=new Color(.22f,.94f,.74f,.13f+.035f*Mathf.Sin(time*3));
                l.pad.color=cleared?Hex("AEDBCC"):next?Hex("DEECE5"):Hex("8C9EAD");
                l.plate.color=cleared?Hex("286A60"):next?Hex("D3A14F"):Hex("283F52");l.reward.color=next?Hex("172D3D"):cleared?Hex("B9FFE0"):Hex("A7BBC8");
                float barrierDelay=l.id%4==0?attemptDuration*.80f:.10f;
                l.barrier.enabled=cleared || (attemptActive && plannedSuccess && l.id==activeLane && attemptClock>barrierDelay);
                float pop=l.id==activeLane && attemptActive?Mathf.Clamp01((attemptClock-barrierDelay)/.10f):1;
                l.barrier.transform.localScale=new Vector3(1.70f/barrierArt.bounds.size.x,1.70f/barrierArt.bounds.size.x*Mathf.Max(.001f,pop),1);
            }
        }
        public float TimeToVehicle(int lane)=>lane==activeLane && attemptActive?(plannedSuccess?999:Mathf.Max(0,attemptDuration*.68f-attemptClock)):999;
        public void FillSnapshots()
        {
            int n=0;foreach(var l in slots)foreach(var c in l.cars)
            {
                var s=vehicleSnapshots[n];s.id=n++;s.lane=l.id;s.direction=l.direction;s.x=LaneX(l.id);s.y=c.y;s.vx=0;s.vy=l.speed*l.direction;
                s.width=c.width;s.length=c.length;s.kind=Kinds[c.kind];s.visible=l.root.gameObject.activeSelf;
            }
        }
        SpriteRenderer Sprite(string name,Transform parent,Sprite sprite,Vector2 position,float width,int order)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;var sr=go.AddComponent<SpriteRenderer>();
            sr.sprite=sprite;sr.sharedMaterial=material;sr.sortingOrder=order;SetWidth(sr,width);return sr;
        }
        static void SetWidth(SpriteRenderer sr,float width){sr.transform.localScale=Vector3.one*(width/sr.sprite.bounds.size.x);}
        TextMeshPro Label(string value,Transform parent,Vector2 position,float size,Color color)
        {
            var go=new GameObject(value);go.transform.SetParent(parent,false);go.transform.localPosition=position;
            var text=go.AddComponent<TextMeshPro>();text.font=font;text.text=value;text.fontSize=size;text.fontStyle=FontStyles.Bold;
            text.alignment=TextAlignmentOptions.Center;text.color=color;text.rectTransform.sizeDelta=new Vector2(2.1f,.65f);text.GetComponent<MeshRenderer>().sortingOrder=75;return text;
        }
        public SpriteRenderer Rect(string name,Transform parent,Vector2 position,Vector2 size,Color color,int order,bool circle=false,bool flat=false)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;
            var sr=go.AddComponent<SpriteRenderer>();sr.sprite=circle?disc:flat?square:rounded;sr.color=color;sr.sharedMaterial=material;sr.sortingOrder=order;
            if(circle)go.transform.localScale=new Vector3(size.x,size.y,1);else{sr.drawMode=SpriteDrawMode.Sliced;sr.size=size;}return sr;
        }
    }
}
