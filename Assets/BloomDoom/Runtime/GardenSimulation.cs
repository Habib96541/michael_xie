using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BloomDoom
{
    public enum GardenPhase { Ready, Preparation, Wave, Intermission, Won, Lost }
    public sealed class SeedDefinition
    {
        public string Id, Name, Description; public int Cost; public float Cooldown, Health;
        public SeedDefinition(string id,string name,int cost,float cooldown,float health,string description)
        { Id=id;Name=name;Cost=cost;Cooldown=cooldown;Health=health;Description=description; }
    }
    public sealed class GardenDay
    {
        public string Name, Theme, Tip; public int Waves, Sunshine; public int[] Guests;
        public GardenDay(string name,string theme,int waves,int sun,int[] guests,string tip)
        {Name=name;Theme=theme;Waves=waves;Sunshine=sun;Guests=guests;Tip=tip;}
    }
    public static class GardenData
    {
        public static readonly SeedDefinition[] Seeds={
            new SeedDefinition("sunbud","Sunbud",50,4,260,"Grows 25 sunshine every 10 seconds. Plant these first!"),
            new SeedDefinition("sprout","Sproutshot",100,5,300,"Small sprout. Big attitude. Fires peas down its lane."),
            new SeedDefinition("nut","Shellnut",50,12,1800,"A very brave acorn with a very tough shell. Holds back hungry guests."),
            new SeedDefinition("frost","Frostbloom",175,8,300,"Icy peas slow a zombie to half speed for four seconds."),
            new SeedDefinition("cherry","Cherry Pop",150,24,300,"An explosive pair. Clears a three-by-three patch of zombies."),
            new SeedDefinition("mine","Spud Mine",25,14,180,"Needs nine seconds to arm. Pops when a zombie steps too close."),
            new SeedDefinition("double","Twinleaf",200,9,300,"Two peas are better than one. Shoots twice in quick succession."),
            new SeedDefinition("pepper","Hot Pepper",125,25,300,"A fiery little friend. Clears every zombie in its lane.")
        };
        public static readonly string[] GuestIds={"basic","cone","runner","bucket","paper","giant"};
        public static readonly float[] GuestHealth={160,340,135,600,260,1350};
        public static readonly float[] GuestSpeed={14,12,26,11,13,9};
        public static readonly float[] GuestDamage={28,30,24,32,30,105};
        public static readonly int[] GuestScore={10,20,20,35,25,100};
        public static readonly GardenDay[] Days={
            new GardenDay("The front yard","day",3,200,new[]{0,1},"Plant Sunbuds first. Sunshine keeps the whole garden growing."),
            new GardenDay("Garden gate","day",3,225,new[]{0,1,2},"Frostblooms can take the spring out of a runner's step."),
            new GardenDay("Golden hour","sunset",3,250,new[]{0,1,2,3},"Shellnuts buy your shooters time. Plant them near the front."),
            new GardenDay("Moonlight mischief","night",3,275,new[]{0,4,1,2},"Less sky sunshine at night. Keep those Sunbuds safe."),
            new GardenDay("The overgrown way","night",4,300,new[]{1,3,2,4},"Cherry Pops clear a patch. Hot Peppers clear a whole lane."),
            new GardenDay("Last lawn standing","sunset",4,325,new[]{0,1,3,2,4,5},"Big Moss is coming. Save some sunshine for an explosive welcome.")
        };
        public static Vector2 Center(int row,int col)=>new Vector2(226+(col+.5f)*112,105+(row+.5f)*102);
        public static bool Cell(Vector2 point,out int row,out int col)
        {row=Mathf.FloorToInt((point.y-105)/102);col=Mathf.FloorToInt((point.x-226)/112);return row>=0&&row<5&&col>=0&&col<9;}
    }
    public sealed class GardenPlant
    { public int Uid,Kind,Row,Col; public float X,Y,Health,MaxHealth,Age,Timer,Recoil,Flash,Burst; public bool Armed; }
    public sealed class GardenGuest
    { public int Uid,Kind,Row; public float X,Y,Health,MaxHealth,Speed,Damage,Age,Bite,Flash,Slow; public bool Eating,Enraged; }
    public sealed class GardenPea
    { public int Uid,Row;public float X,Y;public bool Frost,Dead; }
    public sealed class GardenSun
    { public int Uid;public float X,Y,TargetY,Age,Life=14,FlightAge,FromX,FromY;public bool Sky,Collected; }
    public sealed class GardenMower
    { public int Row;public float X=170;public bool Active,Spent; }
    public sealed class GardenSimulation
    {
        readonly System.Random random;int nextUid;float skyTimer,spawnTimer;readonly Queue<Vector2Int> pending=new Queue<Vector2Int>();
        public GardenPhase Phase {get;private set;}
        public int Day {get;private set;} public bool Endless {get;private set;}
        public int Sunshine,Wave,Kills,Planted,Collected,Score,WaveSpawned,WaveTotal;
        public float Time,Countdown;public readonly float[] Cooldowns=new float[8];
        public readonly List<GardenPlant> Plants=new List<GardenPlant>();
        public readonly List<GardenGuest> Guests=new List<GardenGuest>();
        public readonly List<GardenPea> Peas=new List<GardenPea>();
        public readonly List<GardenSun> Suns=new List<GardenSun>();
        public readonly GardenMower[] Mowers=new GardenMower[5];
        public GardenDay Configuration=>GardenData.Days[Day];
        public int Waiting=>pending.Count; public bool Running=>Phase==GardenPhase.Preparation||Phase==GardenPhase.Wave||Phase==GardenPhase.Intermission;
        public int Stars {get {int used=Mowers.Count(m=>m.Spent||m.Active);return used==0?3:used<=2?2:1;}}
        public event Action<string,string> Event;
        public event Action<string,float,float,int> Effect;
        public GardenSimulation(int seed=0){random=new System.Random(seed==0?Environment.TickCount:seed);Reset();}
        void Say(string name,string text="")=>Event?.Invoke(name,text);
        void Fx(string name,float x,float y,int row=0)=>Effect?.Invoke(name,x,y,row);
        public void Reset(int day=0,bool endless=false)
        {
            Day=Mathf.Clamp(day,0,5);Endless=endless;Phase=GardenPhase.Ready;Time=Wave=Kills=Planted=Collected=Score=WaveSpawned=WaveTotal=0;
            Sunshine=Configuration.Sunshine;Countdown=20;skyTimer=4;spawnTimer=0;pending.Clear();Plants.Clear();Guests.Clear();Peas.Clear();Suns.Clear();Array.Clear(Cooldowns,0,8);
            for(int row=0;row<5;row++)Mowers[row]=new GardenMower{Row=row};
        }
        public void Start(){if(Phase==GardenPhase.Ready){Phase=GardenPhase.Preparation;Say("start");}}
        public GardenPlant At(int row,int col)=>Plants.Find(p=>p.Row==row&&p.Col==col&&p.Health>0);
        public bool Place(int kind,int row,int col,out string reason)
        {
            reason="";if(!Running){reason="Start an adventure to plant your garden.";return false;}
            if(kind<0||kind>7||row<0||row>4||col<0||col>8){reason="Choose a patch on the lawn.";return false;}
            var seed=GardenData.Seeds[kind];if(At(row,col)!=null){reason="A little friend is already growing here.";return false;}
            if(Cooldowns[kind]>0){reason=seed.Name+" is still recharging.";return false;}
            if(Sunshine<seed.Cost){reason="Collect "+(seed.Cost-Sunshine)+" more sunshine.";return false;}
            Sunshine-=seed.Cost;Cooldowns[kind]=seed.Cooldown;var pos=GardenData.Center(row,col);
            Plants.Add(new GardenPlant{Uid=++nextUid,Kind=kind,Row=row,Col=col,X=pos.x,Y=pos.y,Health=seed.Health,MaxHealth=seed.Health,Timer=kind==0?5:.4f});Planted++;Fx("dirt",pos.x,pos.y+30);Say("plant");return true;
        }
        public bool Dig(int row,int col){if(!Running)return false;var p=At(row,col);if(p==null)return false;p.Health=0;Fx("dirt",p.X,p.Y+25);Say("shovel");return true;}
        public bool CollectSun(int uid)
        {
            if(!Running)return false;var s=Suns.Find(item=>item.Uid==uid&&!item.Collected);if(s==null)return false;
            s.Collected=true;s.FromX=s.X;s.FromY=s.Y;s.FlightAge=0;Sunshine+=25;Collected+=25;Score+=5;Say("collect");return true;
        }
        public void CollectAll(){foreach(var s in Suns)CollectSun(s.Uid);}
        public void AddSun(float x,float y,bool sky=false)=>Suns.Add(new GardenSun{Uid=++nextUid,X=x,Y=sky?-25:y-25,TargetY=y,Sky=sky});
        public void SendWave(){if(Phase==GardenPhase.Preparation||Phase==GardenPhase.Intermission)BeginWave();}
        void BeginWave()
        {
            Phase=GardenPhase.Wave;Wave++;WaveSpawned=0;WaveTotal=4+Day+Wave*2+(Endless?Wave*2:0);pending.Clear();
            int[] lanes={0,1,2,3,4};for(int i=4;i>0;i--){int j=random.Next(i+1);(lanes[i],lanes[j])=(lanes[j],lanes[i]);}
            for(int i=0;i<WaveTotal;i++){
                int kind=Configuration.Guests[random.Next(Configuration.Guests.Length)];if(Day==0&&Wave==1)kind=0;if(kind==5&&(Wave<3||i!=WaveTotal-1))kind=3;
                if(Day==5&&Wave==Configuration.Waves&&i==WaveTotal-1||Endless&&Wave>4&&i==WaveTotal-1)kind=5;
                pending.Enqueue(new Vector2Int(kind,lanes[i%5]));
            }
            spawnTimer=1.5f;Say("wave",!Endless&&Wave==Configuration.Waves?"The garden party just got bigger!":"Wave "+Wave+" - Here come the guests!");
        }
        public GardenGuest Spawn(int kind,int row,float x=1380)
        {
            float health=GardenData.GuestHealth[kind]*(Endless?1+Mathf.Max(0,Wave-3)*.12f:1);
            var z=new GardenGuest{Uid=++nextUid,Kind=kind,Row=row,X=x,Y=GardenData.Center(row,0).y,Health=health,MaxHealth=health,Speed=GardenData.GuestSpeed[kind],Damage=GardenData.GuestDamage[kind]};Guests.Add(z);return z;
        }
        public void Damage(GardenGuest z,float amount,bool slow=false)
        {
            if(z.Health<=0)return;z.Health-=amount;z.Flash=.12f;if(slow)z.Slow=4;
            if(z.Kind==4&&z.Health<z.MaxHealth*.5f&&!z.Enraged){z.Enraged=true;z.Speed=30;Fx("paper",z.X,z.Y-22);Say("rage");}
            if(z.Health<=0){Kills++;Score+=GardenData.GuestScore[z.Kind];Fx("death",z.X,z.Y,z.Kind);Say("kill");}
        }
        void Blast(GardenPlant p,bool lane)
        {
            foreach(var z in Guests)if(z.Health>0&&(lane?z.Row==p.Row:Mathf.Abs(z.Row-p.Row)<=1&&Mathf.Abs(z.X-p.X)<179))Damage(z,2200);
            Fx(lane?"fire":"explosion",p.X,p.Y,p.Row);p.Health=0;Say("explode");
        }
        void Shoot(GardenPlant p){Peas.Add(new GardenPea{Uid=++nextUid,Row=p.Row,X=p.X+36,Y=p.Y-27,Frost=p.Kind==3});p.Recoil=1;Say("shoot");}
        public void Tick(float dt)
        {
            if(!Running||float.IsNaN(dt)||dt<=0)return;dt=Mathf.Min(.1f,dt);Time+=dt;
            for(int i=0;i<8;i++)Cooldowns[i]=Mathf.Max(0,Cooldowns[i]-dt);
            skyTimer-=dt;if(skyTimer<=0){skyTimer=Configuration.Theme=="night"?13:7.5f;var spot=GardenData.Center(random.Next(5),random.Next(8));AddSun(spot.x,spot.y,true);}
            if(Phase==GardenPhase.Preparation||Phase==GardenPhase.Intermission){Countdown-=dt;if(Countdown<=0)BeginWave();}
            if(Phase==GardenPhase.Wave&&pending.Count>0){spawnTimer-=dt;if(spawnTimer<=0){var next=pending.Dequeue();Spawn(next.x,next.y);WaveSpawned++;spawnTimer=Mathf.Max(1.5f,5-Day*.35f-Wave*.35f);if(WaveSpawned%4==1)Say("groan");}}
            foreach(var p in Plants){
                if(p.Health<=0)continue;p.Age+=dt;p.Timer-=dt;p.Recoil=Mathf.Max(0,p.Recoil-dt*4);p.Flash=Mathf.Max(0,p.Flash-dt);
                if(p.Kind==0&&p.Timer<=0){AddSun(p.X+10,p.Y+22);p.Timer=10;Say("sunmade");}
                if(p.Kind==5&&!p.Armed&&p.Age>=9){p.Armed=true;Fx("spark",p.X,p.Y);Say("armed");}
                if(p.Kind==5&&p.Armed&&Guests.Any(z=>z.Health>0&&z.Row==p.Row&&Mathf.Abs(z.X-p.X)<53)){
                    foreach(var z in Guests)if(z.Health>0&&z.Row==p.Row&&Mathf.Abs(z.X-p.X)<125)Damage(z,1800);p.Health=0;Fx("explosion",p.X,p.Y);Say("explode");}
                if((p.Kind==4||p.Kind==7)&&p.Age>.85f)Blast(p,p.Kind==7);
                if(p.Kind==1||p.Kind==3||p.Kind==6){if(p.Burst>0){p.Burst-=dt;if(p.Burst<=0)Shoot(p);}if(p.Timer<=0&&Guests.Any(z=>z.Health>0&&z.Row==p.Row&&z.X>p.X-15&&z.X<1420)){Shoot(p);p.Timer=p.Kind==6?1.65f:1.45f;if(p.Kind==6)p.Burst=.2f;}}
            }
            foreach(var b in Peas){float old=b.X;b.X+=365*dt;GardenGuest target=null;foreach(var z in Guests)if(z.Health>0&&z.Row==b.Row&&z.X+24>=old&&z.X-25<=b.X&&(target==null||z.X<target.X))target=z;if(target!=null){Damage(target,26,b.Frost);b.Dead=true;Fx(b.Frost?"ice":"hit",target.X-18,target.Y-22);Say("hit");}}
            foreach(var z in Guests){
                if(z.Health<=0)continue;z.Age+=dt;z.Flash=Mathf.Max(0,z.Flash-dt);z.Slow=Mathf.Max(0,z.Slow-dt);float slow=z.Slow>0?.5f:1;GardenPlant target=null;
                foreach(var p in Plants)if(p.Health>0&&p.Row==z.Row&&p.X<=z.X+25&&z.X-p.X<63&&!(p.Kind==5&&p.Armed)&&(target==null||p.X>target.X))target=p;
                z.Eating=target!=null;if(target!=null){z.Bite-=dt*slow;if(z.Bite<=0){z.Bite=.6f;target.Health-=z.Damage;target.Flash=.16f;Say("bite");if(target.Health<=0){Fx("dirt",target.X,target.Y+28);Say("plantlost");}}}else{z.X-=z.Speed*dt*slow;z.Bite=Mathf.Min(z.Bite,.15f);}
                var mower=Mowers[z.Row];if(z.X<194&&!mower.Spent&&!mower.Active){mower.Active=true;Say("mower");}
                if(z.X<120){Phase=GardenPhase.Lost;Say("lose");return;}
            }
            foreach(var m in Mowers)if(m.Active){float old=m.X;m.X+=560*dt;foreach(var z in Guests)if(z.Health>0&&z.Row==m.Row&&z.X>=old-50&&z.X<=m.X+50)Damage(z,10000);if(m.X>1490){m.Active=false;m.Spent=true;}}
            foreach(var s in Suns){s.Age+=dt;if(s.Collected){s.FlightAge+=dt;float t=Mathf.Clamp01(s.FlightAge/.45f),e=1-Mathf.Pow(1-t,3);s.X=Mathf.Lerp(s.FromX,56,e);s.Y=Mathf.Lerp(s.FromY,-65,e);}else{if(s.Sky)s.Y=Mathf.Min(s.TargetY,s.Y+125*dt);else{float t=Mathf.Clamp01(s.Age/.65f);s.Y=s.TargetY-25+t*25-Mathf.Sin(t*Mathf.PI)*45;}if(s.Y>=s.TargetY-.1f)s.Life-=dt;}}
            Plants.RemoveAll(p=>p.Health<=0);Guests.RemoveAll(z=>z.Health<=0);Peas.RemoveAll(b=>b.Dead||b.X>1490);Suns.RemoveAll(s=>s.Collected?s.FlightAge>.5f:s.Life<=0);
            if(Phase==GardenPhase.Wave&&pending.Count==0&&Guests.Count==0){if(!Endless&&Wave>=Configuration.Waves){Phase=GardenPhase.Won;Score+=Mowers.Count(m=>!m.Spent&&!m.Active)*100;Say("win");}else{Phase=GardenPhase.Intermission;Countdown=9;Sunshine+=75;Say("waveclear","A little breathing room. +75 sunshine");}}
        }
    }
}
