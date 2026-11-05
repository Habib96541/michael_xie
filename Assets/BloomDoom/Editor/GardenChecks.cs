#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;

namespace BloomDoom.Editor
{
    public static class GardenChecks
    {
        static int count;
        static void Check(bool value,string message){if(!value)throw new Exception("Garden check failed: "+message);count++;}
        static void Advance(GardenSimulation g,float seconds){for(int i=0;i<Mathf.CeilToInt(seconds*60);i++)g.Tick(1/60f);}
        static GardenSimulation Game(){var g=new GardenSimulation(921);g.Start();return g;}
        public static void Run()
        {
            count=0;
            for(int r=0;r<5;r++)for(int c=0;c<9;c++){Check(GardenData.Cell(GardenData.Center(r,c),out int rr,out int cc)&&rr==r&&cc==c,"tile mapping");}
            var g=Game();Check(g.Place(1,0,0,out _)&&g.Sunshine==100,"spend sunshine");Check(!g.Place(1,0,0,out _)&&!g.Place(1,1,0,out _)&&g.Sunshine==100,"occupied patch and recharge cannot spend");Advance(g,5.1f);Check(g.Place(1,1,0,out _)&&g.Sunshine==0,"recharge completes");Check(!g.Place(2,-1,0,out _),"invalid patch");
            g=Game();g.AddSun(300,300);int uid=g.Suns[0].Uid;Check(g.CollectSun(uid)&&!g.CollectSun(uid)&&g.Sunshine==225,"sun collected once");Advance(g,.7f);Check(g.Suns.Count==0,"collected sun flight cleans up");
            g=Game();g.Place(0,2,0,out _);Advance(g,5.2f);Check(g.Suns.Any(s=>!s.Sky),"Sunbud production");Check(g.Dig(2,0)&&g.At(2,0)==null&&g.Place(1,2,0,out _),"dig and replant");
            g=Game();g.Sunshine=500;g.Place(3,2,0,out _);var same=g.Spawn(3,2,480);var other=g.Spawn(0,1,480);Advance(g,2);Check(same.Health<same.MaxHealth&&same.Slow>0&&other.Health==other.MaxHealth,"frost and lane collision");
            g=Game();g.Sunshine=1000;g.Place(4,2,4,out _);var spot=GardenData.Center(2,4);g.Spawn(3,1,spot.x);g.Spawn(3,3,spot.x+40);var far=g.Spawn(0,4,spot.x);Advance(g,1);Check(g.Guests.Count==1&&g.Guests[0]==far,"cherry area");g.Place(7,4,3,out _);Advance(g,1);Check(g.Guests.Count==0&&g.Kills==3,"pepper lane clear");
            g=Game();g.Place(5,2,4,out _);Advance(g,9.1f);Check(g.Plants[0].Armed,"mine arming");g.Spawn(3,2,spot.x+35);Advance(g,.1f);Check(g.Guests.Count==0&&g.Plants.Count==0,"armed mine");
            g=Game();g.Spawn(0,1,193);Advance(g,.2f);Check(g.Kills==1&&g.Mowers[1].Active,"mower rescue");Advance(g,3);g.Spawn(0,1,121);Advance(g,.3f);Check(g.Phase==GardenPhase.Lost,"second breach loses");float time=g.Time;Advance(g,2);Check(g.Time==time,"defeat stops simulation");
            for(int day=0;day<6;day++){
                g=new GardenSimulation(921+day);g.Reset(day);g.Start();
                for(int tick=0;tick<16000&&g.Running;tick++){
                    g.CollectAll();int buds=g.Plants.Count(p=>p.Kind==0),desired=day<2?5:8;
                    if(buds<desired&&(g.Time<16||g.Plants.Count(p=>p.Kind==1||p.Kind==3||p.Kind==6)>=5))for(int c=0;c<2;c++)for(int r=0;r<5;r++)if(g.At(r,c)==null&&buds<desired&&g.Place(0,r,c,out _))buds++;
                    var rows=new[]{0,1,2,3,4}.OrderBy(r=>g.Guests.Where(z=>z.Row==r).Select(z=>z.X).DefaultIfEmpty(1500).Min()).ToArray();
                    foreach(int r in rows){if(g.At(r,2)==null)g.Place(1,r,2,out _);var near=g.Guests.Where(z=>z.Row==r).OrderBy(z=>z.X).FirstOrDefault();if(near!=null&&near.X<530&&g.Sunshine>=125)for(int c=5;c<8;c++)if(g.Place(7,r,c,out _))break;if(g.Time>30&&g.At(r,7)==null)g.Place(2,r,7,out _);if(g.Plants.Count(p=>p.Col==2)==5){if(g.At(r,3)==null)g.Place(day>1?6:1,r,3,out _);if(g.At(r,4)==null)g.Place(3,r,4,out _);if(g.At(r,5)==null)g.Place(6,r,5,out _);}}
                    g.Tick(.1f);
                }
                Check(g.Phase==GardenPhase.Won,"campaign day "+(day+1)+" finishes (phase "+g.Phase+")");
            }
            g.Reset(2,true);Check(g.Phase==GardenPhase.Ready&&g.Sunshine==250&&g.Plants.Count+g.Guests.Count+g.Suns.Count+g.Peas.Count==0&&g.Mowers.All(m=>!m.Spent&&!m.Active)&&g.Cooldowns.All(c=>c==0),"reset clears everything");g.Start();g.Wave=20;g.SendWave();Check(g.Phase==GardenPhase.Wave&&g.Waiting>0,"endless continues beyond the campaign");
            Debug.Log("[BloomDoom] PASS: "+count+" native Unity gameplay checks, including all six campaign days.");
        }
    }
}
#endif
